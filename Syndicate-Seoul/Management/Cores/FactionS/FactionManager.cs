using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class DeckSlot
{
    public string deckName;
    public List<string> cardIds = new List<string>();
}

public partial class FactionManager : MonoBehaviour
{
    public static event Action<FactionManager> InitialEmployeeUnlocked;

    [Header("Faction Info")]
    [SerializeField] private string factionId;
    public string factionName;
    public string ceoId;
    public string portraitId;
    [SerializeField] private int credit = 0;
    [SerializeField] private bool isEliminated;
    [Tooltip("Starting city that the faction owns at the beginning of the game.")]
    public CityScript startingCity;
    public List<CityScript> ownedCities = new List<CityScript>();

    [Header("Cards")]
    [SerializeField] private FactionCardInventoryScript factionCardInventory = new FactionCardInventoryScript();
    [SerializeField] private FactionCardPackInventoryScript factionCardPackInventory = new FactionCardPackInventoryScript();
    [SerializeField] private DeckSlot[] savedDeckSlots = new DeckSlot[5];
    [SerializeField] private int lastSelectedSlot = 0;

    public int LastSelectedSlot
    {
        get => lastSelectedSlot;
        set => lastSelectedSlot = Mathf.Clamp(value, 0, 4);
    }

    [Header("UI")]
    public CalendarScript calendar;

    [Header("Resources")]
    [SerializeField] private int rpIncome = 0;
    [SerializeField] private FactionResearchScript factionResearch = new FactionResearchScript();
    [SerializeField] private int totalPowerProduction = 0;
    [SerializeField] private int totalPowerConsumption = 0;
    [SerializeField] private int tradePowerOffset = 0;

    [Header("Employees")]
    [SerializeField] private FactionEmployeeScript factionEmployee = new FactionEmployeeScript();
    [SerializeField] private bool initialEmployeeUnlocked;
    [SerializeField] private int initialEmployeeRequiredBuildingCount = 3;

    public event Action<int> CreditChanged;
    public event Action<int> RPChanged;
    public event Action<int, int, int> PowerChanged;
    public event Action ResearchStateChanged;
    public event Action ResearchQueueChanged;
    public event Action<string> ResearchStarted;
    public event Action<string> ResearchCompleted;
    public event Action<string, int> CardInventoryChanged;
    public event Action<string, int> CardPackInventoryChanged;
    public event Action EmployeeAssignmentChanged;
    public event Action CitiesChanged;

    private bool subsystemEventsBound;

    private void Awake()
    {
        EnsureSubsystemsInitialized();
    }

    private void Start()
    {
        EnsureSubsystemsInitialized();
        InitializeSlots();
        ReconcileOwnedCityReferences();
        GiveStartingCity();
        RecalculateFactionResources();
        TryUnlockInitialEmployeeByBuildingCount();
    }

    private void OnDestroy()
    {
        UnbindSubsystemEvents();
    }

    private void EnsureSubsystemsInitialized()
    {
        if (factionResearch == null)
            factionResearch = new FactionResearchScript();

        if (factionCardInventory == null)
            factionCardInventory = new FactionCardInventoryScript();

        if (factionCardPackInventory == null)
            factionCardPackInventory = new FactionCardPackInventoryScript();

        if (factionEmployee == null)
            factionEmployee = new FactionEmployeeScript();

        factionResearch.Initialize(this);
        factionCardInventory.Initialize();
        factionCardPackInventory.Initialize();
        factionEmployee.Initialize(this);
        BindSubsystemEvents();
    }

    private void BindSubsystemEvents()
    {
        if (subsystemEventsBound)
            return;

        factionResearch.ResearchStateChanged += RelayResearchStateChanged;
        factionResearch.ResearchQueueChanged += RelayResearchQueueChanged;
        factionResearch.ResearchStarted += RelayResearchStarted;
        factionResearch.ResearchCompleted += RelayResearchCompleted;
        factionCardInventory.CardInventoryChanged += RelayCardInventoryChanged;
        factionCardPackInventory.CardPackInventoryChanged += RelayCardPackInventoryChanged;
        subsystemEventsBound = true;
    }

    private void UnbindSubsystemEvents()
    {
        if (!subsystemEventsBound)
            return;

        if (factionResearch != null)
        {
            factionResearch.ResearchStateChanged -= RelayResearchStateChanged;
            factionResearch.ResearchQueueChanged -= RelayResearchQueueChanged;
            factionResearch.ResearchStarted -= RelayResearchStarted;
            factionResearch.ResearchCompleted -= RelayResearchCompleted;
        }

        if (factionCardInventory != null)
            factionCardInventory.CardInventoryChanged -= RelayCardInventoryChanged;

        if (factionCardPackInventory != null)
            factionCardPackInventory.CardPackInventoryChanged -= RelayCardPackInventoryChanged;

        subsystemEventsBound = false;
    }

    private void RelayResearchStateChanged()
    {
        ResearchStateChanged?.Invoke();
    }

    private void RelayResearchQueueChanged()
    {
        ResearchQueueChanged?.Invoke();
    }

    private void RelayResearchStarted(string _researchId)
    {
        ResearchStarted?.Invoke(_researchId);
    }

    private void RelayResearchCompleted(string _researchId)
    {
        NotifyEmployeeSlotChangedForCompletedResearch(_researchId);
        RecalculateFactionResources();
        ResearchCompleted?.Invoke(_researchId);
    }

    private void NotifyEmployeeSlotChangedForCompletedResearch(string _researchId)
    {
        ResearchData research = factionResearch != null
            ? factionResearch.GetResearchById(_researchId)
            : null;

        if (research == null || research.effectType != ResearchEffectType.ResearchEffect_MaxEmployeeSlotUp)
            return;

        int slotBonus = Mathf.Max(0, research.effectValue);
        if (slotBonus <= 0)
            return;

        EmployeeAssignmentChanged?.Invoke();
        Debug.Log($"[FactionManager] Employee slot increased by research. faction={factionName}, research={research.id}, bonus={slotBonus}");
    }

    public bool TryUnlockInitialEmployeeByBuildingCount()
    {
        EnsureSubsystemsInitialized();

        if (initialEmployeeUnlocked)
            return false;

        int requiredBuildingCount = Mathf.Max(1, initialEmployeeRequiredBuildingCount);
        int completedBuildingCount = CountMaxCompletedBuildingsInOwnedCity();
        if (completedBuildingCount < requiredBuildingCount)
            return false;

        initialEmployeeUnlocked = true;
        EmployeeAssignmentChanged?.Invoke();
        InitialEmployeeUnlocked?.Invoke(this);
        return true;
    }

    private int CountMaxCompletedBuildingsInOwnedCity()
    {
        if (ownedCities == null)
            return 0;

        int maxCount = 0;
        for (int i = 0; i < ownedCities.Count; i++)
        {
            CityScript city = ownedCities[i];
            if (city == null || city.cityData == null || city.cityData.buildings == null)
                continue;

            int cityCount = 0;
            for (int j = 0; j < city.cityData.buildings.Count; j++)
            {
                BuildingInstance building = city.cityData.buildings[j];
                if (building == null || building.IsEmptySlot() || building.IsUnderConstruction() || building.IsUpgrading())
                    continue;

                cityCount++;
            }

            maxCount = Mathf.Max(maxCount, cityCount);
        }

        return maxCount;
    }

    private bool HasAnyEmployee()
    {
        IReadOnlyList<EmployeeData> employees = factionEmployee != null ? factionEmployee.GetEmployees() : null;
        return employees != null && employees.Count > 0;
    }

    private void RelayCardInventoryChanged(string _cardId, int _currentCount)
    {
        CardInventoryChanged?.Invoke(_cardId, _currentCount);
    }

    private void RelayCardPackInventoryChanged(string _packId, int _currentCount)
    {
        CardPackInventoryChanged?.Invoke(_packId, _currentCount);
    }

    private void InitializeSlots()
    {
        if (savedDeckSlots == null || savedDeckSlots.Length != 5)
            savedDeckSlots = new DeckSlot[5];

        for (int i = 0; i < 5; i++)
        {
            if (savedDeckSlots[i] == null)
            {
                savedDeckSlots[i] = new DeckSlot();
                savedDeckSlots[i].deckName = $"Deck {i + 1}";
            }

            if (string.IsNullOrEmpty(savedDeckSlots[i].deckName))
                savedDeckSlots[i].deckName = $"Deck {i + 1}";
        }
    }

    private void GiveStartingCity()
    {
        if (startingCity == null)
            startingCity = ResolveFallbackStartingCity();

        if (startingCity == null) return;
        if (IsCityOwnedByThis(startingCity))
        {
            SeedScavengerLockForAIStartingCity();
            return;
        }

        if (CityOwnershipManager.instance == null)
        {
            Debug.LogWarning($"[FactionManager] {factionName}: CityOwnershipManager 인스턴스가 없어 startingCity 소유권을 직접 설정합니다.");
            if (startingCity.cityData != null)
                startingCity.cityData.owner = this;
            AddCity(startingCity);
            if (CityShareManager.instance != null)
                CityShareManager.instance.TryInitializeCityShares(startingCity, false, out _);
            SeedScavengerLockForAIStartingCity();
            return;
        }

        CityOwnershipManager.TransferResult result = CityOwnershipManager.instance.Transfer(startingCity, this);

        if (CityShareManager.instance != null
            && (result == CityOwnershipManager.TransferResult.Success || result == CityOwnershipManager.TransferResult.SameOwner))
        {
            CityShareManager.instance.TryInitializeCityShares(startingCity, false, out _);
        }

        if (result == CityOwnershipManager.TransferResult.Success || result == CityOwnershipManager.TransferResult.SameOwner)
            SeedScavengerLockForAIStartingCity();

        if (result != CityOwnershipManager.TransferResult.Success)
            Debug.LogWarning($"[FactionManager] {factionName}: Transfer 결과 = {result}");
    }

    private void SeedScavengerLockForAIStartingCity()
    {
        if (IsPlayerFaction || startingCity == null)
            return;

        startingCity.TrySeedScavengerLock();
    }

    private void ReconcileOwnedCityReferences()
    {
        if (ownedCities == null)
        {
            ownedCities = new List<CityScript>();
            return;
        }

        var seenCities = new HashSet<CityScript>();
        for (int i = ownedCities.Count - 1; i >= 0; i--)
        {
            CityScript city = ownedCities[i];
            if (city == null || !seenCities.Add(city))
            {
                ownedCities.RemoveAt(i);
                continue;
            }

            if (!IsCityOwnedByThis(city))
            {
                AssignCityOwner(city);
            }
        }
    }

    private bool IsCityOwnedByThis(CityScript city)
    {
        return city != null
            && city.cityData != null
            && ReferenceEquals(city.cityData.owner, this);
    }

    private void AssignCityOwner(CityScript city)
    {
        if (city == null)
            return;

        if (city.cityData == null)
            city.cityData = new CityData();

        if (CityOwnershipManager.instance != null)
        {
            CityOwnershipManager.instance.Transfer(city, this);
            return;
        }

        FactionManager oldOwner = city.cityData.owner;
        if (oldOwner != null && oldOwner != this)
        {
            oldOwner.RemoveCity(city);
        }

        city.cityData.owner = this;
        if (!HasCity(city))
        {
            AddCity(city);
        }
    }

    private CityScript ResolveFallbackStartingCity()
    {
        if (!ShouldUseFallbackStartingCity())
            return null;

        CityScript[] cities = FindObjectsByType<CityScript>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        CityScript bestCity = null;
        int bestRegionId = int.MaxValue;
        string bestName = null;

        for (int i = 0; i < cities.Length; i++)
        {
            CityScript city = cities[i];
            if (city == null || city.cityData == null)
                continue;

            if (city.cityData.owner != null && city.cityData.owner != this)
                continue;

            int regionId = GetHexTerrainRegionId(city);
            string cityName = !string.IsNullOrWhiteSpace(city.cityData.cityName)
                ? city.cityData.cityName
                : city.name;

            bool isBetter = bestCity == null
                || regionId < bestRegionId
                || (regionId == bestRegionId && string.CompareOrdinal(cityName, bestName) < 0);

            if (!isBetter)
                continue;

            bestCity = city;
            bestRegionId = regionId;
            bestName = cityName;
        }

        if (bestCity != null)
            Debug.LogWarning($"[FactionManager] {factionName}: startingCity가 비어 있어 '{bestName}' 지역을 시작 도시로 자동 지정했습니다.");

        return bestCity;
    }

    private bool ShouldUseFallbackStartingCity()
    {
        if (!IsPlayerFaction)
            return false;

        if (ownedCities != null)
            ownedCities.RemoveAll(city => city == null);

        return ownedCities == null || ownedCities.Count == 0;
    }

    private static int GetHexTerrainRegionId(CityScript city)
    {
        var regionObject = city.GetComponent<NewWorld.HexTerrain.HexTerrainRegionObject>();
        return regionObject != null && regionObject.RegionId > 0 ? regionObject.RegionId : int.MaxValue;
    }

    private void OnEnable()
    {
        if (calendar != null)
            calendar.MonthChanged += OnMonthPassed;
    }

    private void OnDisable()
    {
        if (calendar != null)
            calendar.MonthChanged -= OnMonthPassed;
    }

    void OnMonthPassed(int _year, int _month)
    {
        CollectTaxes();
        factionResearch.ProcessResearchMonth(rpIncome);
    }

    void CollectTaxes()
    {
        FactionResourceSnapshot snapshot = ManagementResourceCalculator.CalculateFaction(this);
        ChangeCredit(snapshot.creditIncome);
    }

    public void RecalculateFactionResources()
    {
        EnsureSubsystemsInitialized();
        FactionResourceSnapshot snapshot = ManagementResourceCalculator.CalculateFaction(this);

        rpIncome = snapshot.totalRP;
        totalPowerProduction = snapshot.totalPowerProduction;
        totalPowerConsumption = snapshot.totalPowerConsumption;

        RPChanged?.Invoke(rpIncome);
        NotifyPowerChanged();
    }

    public bool ChangeCredit(int _delta)
    {
        if (credit + _delta < 0)
            return false;

        credit += _delta;
        CreditChanged?.Invoke(credit);
        return true;
    }

    public bool ChangeTradePower(int _delta)
    {
        int nextTradePowerOffset = tradePowerOffset + _delta;
        int nextNetPower = totalPowerProduction + nextTradePowerOffset - totalPowerConsumption;

        if (nextNetPower < 0)
            return false;

        tradePowerOffset = nextTradePowerOffset;
        RecalculateFactionResources();
        return true;
    }

    public void ForceChangeTradePower(int _delta)
    {
        if (_delta == 0)
            return;

        tradePowerOffset += _delta;
        RecalculateFactionResources();
    }

    public void ForceChangeRPStock(int _delta)
    {
        EnsureSubsystemsInitialized();
        ForceSetRPStock(GetRPStock + _delta);
    }

    public void ForceSetRPStock(int _value)
    {
        EnsureSubsystemsInitialized();

        FactionResearchState researchState = factionResearch.GetResearchState();
        if (researchState == null)
            return;

        int value = Mathf.Max(0, _value);
        researchState.rpStock = value;
        researchState.currentResearchProgressRP = value;
        ResearchStateChanged?.Invoke();
    }

    private void NotifyPowerChanged()
    {
        PowerChanged?.Invoke(totalPowerProduction, totalPowerConsumption, GetNetPower);
    }

    public bool HasCompletedResearch(string _researchId)
    {
        EnsureSubsystemsInitialized();
        return factionResearch.HasCompletedResearch(_researchId);
    }

    public bool IsResearchCompleted(string _researchId)
    {
        EnsureSubsystemsInitialized();
        return factionResearch.IsResearchCompleted(_researchId);
    }

    public bool IsResearchQueued(string _researchId)
    {
        EnsureSubsystemsInitialized();
        return factionResearch.IsResearchQueued(_researchId);
    }

    public bool IsResearchInProgress(string _researchId)
    {
        EnsureSubsystemsInitialized();
        return factionResearch.IsResearchInProgress(_researchId);
    }

    public ResearchData GetCurrentResearch()
    {
        EnsureSubsystemsInitialized();
        return factionResearch.GetCurrentResearch();
    }

    public IReadOnlyList<string> GetQueuedResearchIds()
    {
        EnsureSubsystemsInitialized();
        return factionResearch.GetQueuedResearchIds();
    }

    public bool CanStartResearchNow(string _researchId)
    {
        EnsureSubsystemsInitialized();
        return factionResearch.CanStartResearchNow(_researchId);
    }

    public bool QueueResearchPath(string _targetResearchId)
    {
        EnsureSubsystemsInitialized();
        return factionResearch.QueueResearchPath(_targetResearchId);
    }

    public bool StartResearchDirectly(string _researchId)
    {
        EnsureSubsystemsInitialized();
        return factionResearch.StartResearchDirectly(_researchId);
    }

    public bool CancelResearch(string _researchId)
    {
        EnsureSubsystemsInitialized();
        return factionResearch.CancelResearch(_researchId);
    }

    public void ClearResearchQueue()
    {
        EnsureSubsystemsInitialized();
        factionResearch.ClearResearchQueue();
    }

    public bool CanUseBuilding(BuildingData _building)
    {
        EnsureSubsystemsInitialized();
        return factionResearch.CanUseBuilding(_building);
    }

    public void ApplyExternalResearchEffect(ResearchData _research)
    {
        EnsureSubsystemsInitialized();
        factionResearch.ApplyExternalResearchEffect(_research);
        RecalculateFactionResources();
    }

    public void RemoveExternalResearchEffect(ResearchData _research)
    {
        EnsureSubsystemsInitialized();
        factionResearch.RemoveExternalResearchEffect(_research);
        RecalculateFactionResources();
    }

    public IReadOnlyList<EmployeeData> GetEmployees()
    {
        EnsureSubsystemsInitialized();
        return factionEmployee.GetEmployees();
    }

    public int GetEmployeeSlotCount()
    {
        EnsureSubsystemsInitialized();

        if (!initialEmployeeUnlocked)
            return 0;

        int baseSlotCount = 1;
        FactionResearchState researchState = factionResearch != null ? factionResearch.GetResearchState() : null;
        int bonus = researchState != null && researchState.modifiers != null
            ? Mathf.Max(0, researchState.modifiers.maxEmployeeSlotBonus)
            : 0;

        return baseSlotCount + bonus;
    }

    public IReadOnlyList<EmployeeData> GetAssignedEmployees()
    {
        EnsureSubsystemsInitialized();
        return factionEmployee.GetAssignedEmployees();
    }

    public IReadOnlyList<EmployeeData> GetSpyEmployees()
    {
        EnsureSubsystemsInitialized();
        return factionEmployee.GetSpyEmployees();
    }

    public bool HasAssignedEmployeeInOwnedCity(CityScript _city)
    {
        EnsureSubsystemsInitialized();
        return factionEmployee.HasAssignedEmployeeInOwnedCity(_city);
    }

    public bool HasAssignedEmployeeAbilityInOwnedCity(CityScript _city, EmployeeAbilityType _abilityType)
    {
        EnsureSubsystemsInitialized();
        return factionEmployee.HasAssignedEmployeeAbilityInOwnedCity(_city, _abilityType);
    }

    public bool AssignEmployee(string _employeeId, CityScript _city, out string _message)
    {
        EnsureSubsystemsInitialized();
        CityScript previousCity = FindAssignedEmployeeCity(_employeeId);
        FactionManager previousCityOwner = previousCity != null && previousCity.cityData != null
            ? previousCity.cityData.owner
            : null;

        bool success = factionEmployee.AssignEmployee(_employeeId, _city, out _message);
        if (success)
        {
            EmployeeData employee = FindEmployee(_employeeId);
            Debug.Log($"[FactionManager] AssignEmployee success. employee={GetEmployeeName(employee, _employeeId)}, from={GetCityName(previousCity)}, to={GetCityName(_city)}");

            RecalculateFactionResources();

            if (previousCityOwner != null && previousCityOwner != this)
                previousCityOwner.RecalculateFactionResources();

            FactionManager newCityOwner = _city != null && _city.cityData != null ? _city.cityData.owner : null;
            if (newCityOwner != null && newCityOwner != this && newCityOwner != previousCityOwner)
                newCityOwner.RecalculateFactionResources();

            EmployeeAssignmentChanged?.Invoke();
        }

        return success;
    }

    public bool UnassignEmployee(string _employeeId, out string _message)
    {
        EnsureSubsystemsInitialized();
        CityScript previousCity = FindAssignedEmployeeCity(_employeeId);
        FactionManager previousCityOwner = previousCity != null && previousCity.cityData != null
            ? previousCity.cityData.owner
            : null;

        bool success = factionEmployee.UnassignEmployee(_employeeId, out _message);
        if (success)
        {
            RecalculateFactionResources();

            if (previousCityOwner != null && previousCityOwner != this)
                previousCityOwner.RecalculateFactionResources();

            EmployeeAssignmentChanged?.Invoke();
        }

        return success;
    }

    public bool TryHireEmployeeFromCEO(CEOData _ceo, out EmployeeData _employee, out string _message)
    {
        return TryHireEmployeeFromCEO(_ceo, EmployeeAbilityType.None, out _employee, out _message);
    }

    public bool TryHireEmployeeFromCEO(CEOData _ceo, EmployeeAbilityType _abilityType, out EmployeeData _employee, out string _message)
    {
        EnsureSubsystemsInitialized();

        _employee = null;
        _message = string.Empty;

        if (!IsEmployeeHiringUnlocked)
        {
            _message = "Employee hiring is locked.";
            return false;
        }

        if (_ceo == null || string.IsNullOrWhiteSpace(_ceo.id))
        {
            _message = "CEO data is missing.";
            return false;
        }

        int slotCount = GetEmployeeSlotCount();
        IReadOnlyList<EmployeeData> employees = GetEmployees();
        int currentEmployeeCount = employees != null ? employees.Count : 0;
        if (currentEmployeeCount >= slotCount)
        {
            _message = "Employee slots are full.";
            return false;
        }

        bool success = factionEmployee.AddEmployeeFromCEO(_ceo, _abilityType, out _employee, out _message);
        if (success)
            EmployeeAssignmentChanged?.Invoke();

        return success;
    }

    public bool ProgressReturningEmployees()
    {
        EnsureSubsystemsInitialized();

        bool changed = factionEmployee.ProgressReturningEmployees();
        if (changed)
            EmployeeAssignmentChanged?.Invoke();

        return changed;
    }

    public bool BeginEmployeeReturn(EmployeeData _employee, int _returnMonths, out string _message)
    {
        EnsureSubsystemsInitialized();

        bool success = factionEmployee.BeginEmployeeReturn(_employee, _returnMonths, out _message);
        if (success)
            EmployeeAssignmentChanged?.Invoke();

        return success;
    }

    public bool MarkEmployeeLost(EmployeeData _employee, out string _message)
    {
        EnsureSubsystemsInitialized();

        bool success = factionEmployee.MarkEmployeeLost(_employee, out _message);
        if (success)
            EmployeeAssignmentChanged?.Invoke();

        return success;
    }

    public bool HasEmployeeFromCEO(string _ceoId)
    {
        EnsureSubsystemsInitialized();
        return factionEmployee.HasEmployeeFromCEO(_ceoId);
    }

    private CityScript FindAssignedEmployeeCity(string _employeeId)
    {
        if (factionEmployee == null || string.IsNullOrWhiteSpace(_employeeId))
            return null;

        IReadOnlyList<EmployeeData> employees = factionEmployee.GetEmployees();
        if (employees == null)
            return null;

        for (int i = 0; i < employees.Count; i++)
        {
            EmployeeData employee = employees[i];
            if (employee == null)
                continue;

            if (string.Equals(employee.employeeId, _employeeId, StringComparison.OrdinalIgnoreCase))
                return employee.assignedCity;
        }

        return null;
    }

    private EmployeeData FindEmployee(string _employeeId)
    {
        if (factionEmployee == null || string.IsNullOrWhiteSpace(_employeeId))
            return null;

        IReadOnlyList<EmployeeData> employees = factionEmployee.GetEmployees();
        if (employees == null)
            return null;

        for (int i = 0; i < employees.Count; i++)
        {
            EmployeeData employee = employees[i];
            if (employee == null)
                continue;

            if (string.Equals(employee.employeeId, _employeeId, StringComparison.OrdinalIgnoreCase))
                return employee;
        }

        return null;
    }

    private string GetEmployeeName(EmployeeData employee, string fallbackId)
    {
        if (employee != null && !string.IsNullOrWhiteSpace(employee.employeeName))
            return employee.employeeName;

        return string.IsNullOrWhiteSpace(fallbackId) ? "Unknown" : fallbackId;
    }

    private string GetCityName(CityScript city)
    {
        if (city == null || city.cityData == null || string.IsNullOrWhiteSpace(city.cityData.cityName))
            return "Unassigned";

        string cityName = CityDisplayNameUtility.ToKoreanDisplayName(city.cityData.cityName);
        return string.IsNullOrWhiteSpace(cityName) ? city.cityData.cityName : cityName;
    }

    public bool HasOperationalBuilding(string _buildingId)
    {
        return TryGetOperationalBuilding(_buildingId, out _);
    }

    public bool TryGetOperationalBuilding(string _buildingId, out BuildingData _buildingData)
    {
        _buildingData = null;

        if (string.IsNullOrWhiteSpace(_buildingId) || ownedCities == null)
            return false;

        for (int i = 0; i < ownedCities.Count; i++)
        {
            CityScript city = ownedCities[i];
            if (city == null || city.cityData == null || city.cityData.buildings == null)
                continue;

            for (int j = 0; j < city.cityData.buildings.Count; j++)
            {
                BuildingInstance building = city.cityData.buildings[j];
                if (building == null || !building.IsOperational() || building.data == null)
                    continue;

                if (!string.Equals(building.data.ID, _buildingId, StringComparison.OrdinalIgnoreCase))
                    continue;

                _buildingData = building.data;
                return true;
            }
        }

        return false;
    }

    #region Get

    public int GetCredit => credit;
    public int GetRPIncome => rpIncome;
    public int GetRPStock
    {
        get
        {
            EnsureSubsystemsInitialized();
            return factionResearch.GetRPStock();
        }
    }

    public FactionResearchState GetResearchState
    {
        get
        {
            EnsureSubsystemsInitialized();
            return factionResearch.GetResearchState();
        }
    }

    public int GetTotalRP => rpIncome;
    public int GetTotalPowerProduction => totalPowerProduction;
    public int GetTotalPowerConsumption => totalPowerConsumption;
    public int GetTradePowerOffset => tradePowerOffset;
    public int GetNetPower => totalPowerProduction + tradePowerOffset - totalPowerConsumption;
    public float GetPowerSatisfactionRate => ManagementResourceCalculator.CalculatePowerSatisfactionRate(this);
    public bool IsEmployeeHiringUnlocked => initialEmployeeUnlocked;
    public bool IsPlayerFaction => string.Equals(GetSaveKey(), "player", StringComparison.OrdinalIgnoreCase);
    public bool IsEliminated => isEliminated;

    public bool GainCard(string _cardId, int _count = 1)
    {
        EnsureCardInventoryAvailable();
        return factionCardInventory.GainCard(_cardId, _count);
    }

    public bool GainCard(CardData _card, int _count = 1)
    {
        EnsureCardInventoryAvailable();
        return factionCardInventory.GainCard(_card, _count);
    }

    public bool GainCards(Dictionary<string, int> _cardCounts)
    {
        EnsureCardInventoryAvailable();
        return factionCardInventory.GainCards(_cardCounts);
    }

    public bool GainCardPack(string _packId, int _count = 1)
    {
        EnsureCardPackInventoryAvailable();
        return factionCardPackInventory.GainCardPack(_packId, _count);
    }

    public bool RemoveCardPack(string _packId, int _count = 1)
    {
        EnsureCardPackInventoryAvailable();
        return factionCardPackInventory.RemoveCardPack(_packId, _count);
    }

    public bool RemoveCardPacks(Dictionary<string, int> _packCounts)
    {
        EnsureCardPackInventoryAvailable();
        return factionCardPackInventory.RemoveCardPacks(_packCounts);
    }

    public int GetCardPackCount(string _packId)
    {
        EnsureCardPackInventoryAvailable();
        return factionCardPackInventory.GetCardPackCount(_packId);
    }

    public bool HasCardPack(string _packId, int _count = 1)
    {
        EnsureCardPackInventoryAvailable();
        return factionCardPackInventory.HasCardPack(_packId, _count);
    }

    public FactionCardPackInventoryScript GetCardPackInventory()
    {
        EnsureCardPackInventoryAvailable();
        return factionCardPackInventory;
    }

    private void EnsureCardInventoryAvailable()
    {
        if (factionCardInventory != null)
            return;

        factionCardInventory = new FactionCardInventoryScript();
        factionCardInventory.Initialize();

        if (subsystemEventsBound)
            factionCardInventory.CardInventoryChanged += RelayCardInventoryChanged;
    }

    private void EnsureCardPackInventoryAvailable()
    {
        if (factionCardPackInventory != null)
            return;

        factionCardPackInventory = new FactionCardPackInventoryScript();
        factionCardPackInventory.Initialize();

        if (subsystemEventsBound)
            factionCardPackInventory.CardPackInventoryChanged += RelayCardPackInventoryChanged;
    }

    public bool RemoveCard(string _cardId, int _count = 1)
    {
        EnsureSubsystemsInitialized();

        if (!factionCardInventory.RemoveCard(_cardId, _count, out Dictionary<string, int> removedMap))
            return false;

        CleanupDeckSlotsAfterConsume(removedMap);
        return true;
    }

    public void ConsumeCards(List<string> _cardIds)
    {
        EnsureSubsystemsInitialized();
        Dictionary<string, int> consumedMap = factionCardInventory.ConsumeCards(_cardIds);
        LogConsumedCards(consumedMap);
        CleanupDeckSlotsAfterConsume(consumedMap);
    }

    public void ConsumeCards(Dictionary<string, int> _cardCounts)
    {
        EnsureSubsystemsInitialized();
        Dictionary<string, int> consumedMap = factionCardInventory.ConsumeCards(_cardCounts);
        LogConsumedCards(consumedMap);
        CleanupDeckSlotsAfterConsume(consumedMap);
    }

    private void LogConsumedCards(Dictionary<string, int> _consumedMap)
    {
        if (_consumedMap == null || _consumedMap.Count == 0)
            return;

        string displayFactionName = string.IsNullOrWhiteSpace(factionName) ? name : factionName;

        foreach (KeyValuePair<string, int> pair in _consumedMap)
        {
            if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value <= 0)
                continue;

            AIDebugLogger.LogAI(this, $"[AI][{displayFactionName}] 카드 소모: {pair.Key} x{pair.Value}");
        }
    }

    private void CleanupDeckSlotsAfterConsume(Dictionary<string, int> _consumedMap)
    {
        if (_consumedMap == null || _consumedMap.Count == 0)
            return;

        if (savedDeckSlots == null)
            return;

        EnsureSubsystemsInitialized();

        foreach (KeyValuePair<string, int> pair in _consumedMap)
        {
            string cardId = pair.Key;
            int nowOwned = factionCardInventory.GetCardCount(cardId);

            for (int s = 0; s < savedDeckSlots.Length; s++)
            {
                DeckSlot slot = savedDeckSlots[s];
                if (slot?.cardIds == null)
                    continue;

                int inDeck = 0;
                for (int i = 0; i < slot.cardIds.Count; i++)
                {
                    if (slot.cardIds[i] == cardId)
                        inDeck++;
                }

                int excess = inDeck - nowOwned;
                for (int i = 0; i < excess; i++)
                    slot.cardIds.Remove(cardId);

                if (excess > 0)
                    Debug.Log($"[FactionManager] 덱 슬롯 {s} 정리: {cardId} {excess}장 제거 (인벤토리 {nowOwned}장 남음)");
            }
        }
    }

    public int GetCardCount(string _cardId)
    {
        EnsureSubsystemsInitialized();
        return factionCardInventory.GetCardCount(_cardId);
    }

    public CardInventory GetCardInventory()
    {
        EnsureSubsystemsInitialized();
        return factionCardInventory.GetCardInventory();
    }

    public List<string> GetSavedDeck(int slotIndex = -1)
    {
        InitializeSlots();
        int idx = slotIndex == -1 ? lastSelectedSlot : Mathf.Clamp(slotIndex, 0, 4);
        return new List<string>(savedDeckSlots[idx].cardIds);
    }

    public string GetSavedDeckName(int slotIndex = -1)
    {
        InitializeSlots();
        int idx = slotIndex == -1 ? lastSelectedSlot : Mathf.Clamp(slotIndex, 0, 4);
        return savedDeckSlots[idx].deckName;
    }

    public void SaveDeck(List<string> deckIds, string deckName = null, int slotIndex = -1)
    {
        InitializeSlots();
        int idx = slotIndex == -1 ? lastSelectedSlot : Mathf.Clamp(slotIndex, 0, 4);

        savedDeckSlots[idx].cardIds = new List<string>(deckIds);
        if (!string.IsNullOrEmpty(deckName))
            savedDeckSlots[idx].deckName = deckName;

        lastSelectedSlot = idx;
    }

    #endregion

    public bool HasCity(CityScript _city)
    {
        if (_city == null) return false;
        return ownedCities != null && ownedCities.Contains(_city);
    }

    public void AddCity(CityScript _city)
    {
        if (_city == null) return;

        if (ownedCities == null)
            ownedCities = new List<CityScript>();

        if (!ownedCities.Contains(_city))
        {
            ownedCities.Add(_city);
            RecalculateFactionResources();
            CitiesChanged?.Invoke();
            TryUnlockInitialEmployeeByBuildingCount();
        }
    }

    public void RemoveCity(CityScript _city)
    {
        if (_city == null) return;
        if (ownedCities == null) return;

        if (ownedCities.Remove(_city))
        {
            RecalculateFactionResources();
            CitiesChanged?.Invoke();
        }
    }

    public bool TryEliminateByCityLoss()
    {
        if (isEliminated)
            return false;

        if (IsPlayerFaction)
            return false;

        if (ownedCities == null)
            ownedCities = new List<CityScript>();

        ownedCities.RemoveAll(city => city == null);
        if (ownedCities.Count > 0)
            return false;

        isEliminated = true;

        if (PatentResearchManager.Instance != null)
            PatentResearchManager.Instance.HandleFactionGameOver(this);

        DisableAIControllerIfEliminated();
        AIDebugLogger.LogAI(this, $"[AI] Faction eliminated by city loss. faction={factionName}");
        return true;
    }

    private void DisableAIControllerIfEliminated()
    {
        if (!isEliminated)
            return;

        NationAIController aiController = GetComponent<NationAIController>();
        if (aiController != null)
            aiController.enabled = false;
    }

    public string GetSaveKey()
    {
        if (!string.IsNullOrWhiteSpace(factionId))
            return factionId.Trim();

        return gameObject != null ? gameObject.name : string.Empty;
    }
}
