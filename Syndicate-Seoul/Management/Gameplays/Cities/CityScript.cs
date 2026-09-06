using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.Serialization;

public class CityScript : MonoBehaviour
{
    private const int DefaultBuildingSlotCount = 5;
    private const int MaxBuildingSlotCount = 6;

    /// <summary>게임 최초 시작 시 스캐빈저 무리가 점거하는 슬롯 인덱스(5번째 슬롯).</summary>
    public const int ScavengerLockedSlotIndex = 4;

    [System.Serializable]
    public class CitySaveData
    {
        public int creditIncome;
        public CityShareData.ShareSaveData shareData = new CityShareData.ShareSaveData();
        public List<BuildingSlotSaveData> buildings = new List<BuildingSlotSaveData>();
    }

    [System.Serializable]
    public class BuildingSlotSaveData
    {
        public string buildingId;
        public string upgradeTargetBuildingId;
        public int remainConstructionDay;
        public int remainActivationIntervalDays;
        public float factoryProductionProgressBuffer;
        public int lastProcessedYear;
        public int lastProcessedMonth;
        public int lastProcessedDay;
        public bool isDisabled;
        public bool scavengerLocked;
    }

    public CityData cityData;
    public int maxBuildingCount = MaxBuildingSlotCount;

    [Header("Etc")]
    [SerializeField] private CityUIController uiController;
    [SerializeField] private CalendarScript calendar;

    [Header("World Building Placement")]
    [SerializeField] private HexBuildingPlacementArea buildingPlacementArea;
    [SerializeField] private bool requireBuildableCellForConstruction = true;

    [Header("Scavenger")]
    [Tooltip("신규 게임 시작 시 5번째 슬롯(인덱스 4)을 스캐빈저 점거 상태로 잠근다. AI 시작 도시는 FactionManager가 자동으로 시드한다. 세이브 로드 시에는 저장된 값이 우선한다.")]
    [SerializeField] private bool seedScavengerLockOnStart = false;

    private static PowerBuildingDatabaseSO powerDb;
    private static EconomicBuildingDatabaseSO economicDb;
    private static ResearchBuildingDatabaseSO researchDb;
    private static FactoryBuildingDatabaseSO factoryDb;
    private static SupportBuildingDatabaseSO supportDb;
    private static readonly List<RaycastResult> uiRaycastResults = new List<RaycastResult>();
    private static int lastCityPanelOpenFrame = -1;

    private void Awake()
    {
        GetBuildingPlacementArea();
    }

    void Start()
    {
        if (uiController == null)
        {
            uiController = FindFirstObjectByType<CityUIController>();

            if (uiController == null)
                Debug.LogError("CityScript : No ui controller");
        }

        InitializeBuildings();
        CalculateCityPower();
        SyncWorldBuildings();
    }

    private void Update()
    {
        DispatchWorldClickFallback();
    }

    void InitializeBuildings()
    {
        if (cityData == null)
        {
            Debug.LogError("CityScript : cityData is null");
            return;
        }

        if (cityData.buildings == null)
        {
            cityData.buildings = new System.Collections.Generic.List<BuildingInstance>();
        }

        if (cityData.shareData == null)
            cityData.shareData = new CityShareData();

        NormalizeBuildingSlots();
        ApplyStarterBuildingsToEmptySlots();
        SeedScavengerLockIfNeeded();
        SyncWorldBuildings();
    }

    /// <summary>
    /// 신규 게임에서만 지정된 슬롯을 스캐빈저 점거 상태로 잠근다.
    /// 세이브 로드 시에는 ImportSaveData가 Start 이후 buildings를 덮어쓰므로 저장된 값이 우선한다.
    /// </summary>
    private void SeedScavengerLockIfNeeded()
    {
        if (!seedScavengerLockOnStart)
            return;

        TrySeedScavengerLock();
    }

    public bool TrySeedScavengerLock()
    {
        if (cityData == null || cityData.buildings == null)
        {
            if (cityData == null)
                return false;

            cityData.buildings = new List<BuildingInstance>();
        }

        NormalizeBuildingSlots();
        ApplyStarterBuildingsToEmptySlots();

        if (ScavengerLockedSlotIndex < 0 || ScavengerLockedSlotIndex >= cityData.buildings.Count)
            return false;

        BuildingInstance slot = cityData.buildings[ScavengerLockedSlotIndex];
        if (slot == null)
            return false;

        if (slot.IsScavengerLocked())
            return true;

        if (!slot.IsEmptySlot())
            return false;

        slot.SetScavengerLocked(true);
        SyncWorldBuildingSlot(ScavengerLockedSlotIndex);
        return true;
    }

    internal void NormalizeBuildingSlots()
    {
        int maxSlots = GetMaxBuildingSlotCount();

        while (cityData.buildings.Count > maxSlots)
        {
            cityData.buildings.RemoveAt(cityData.buildings.Count - 1);
        }

        for (int i = 0; i < maxSlots; i++)
        {
            if (i >= cityData.buildings.Count)
            {
                cityData.buildings.Add(CreateEmptySlot());
            }
            else if (cityData.buildings[i] == null)
            {
                cityData.buildings[i] = CreateEmptySlot();
            }
        }
    }

    public int GetAvailableBuildingSlotCount()
    {
        int extraSlots = 0;

        if (cityData != null && cityData.owner != null)
        {
            FactionResearchState researchState = cityData.owner.GetResearchState;
            if (researchState != null && researchState.modifiers != null)
                extraSlots = researchState.modifiers.addBuildingSlot;
        }

        int researchSlotCount = Mathf.Clamp(DefaultBuildingSlotCount + extraSlots, DefaultBuildingSlotCount, GetMaxBuildingSlotCount());
        HexBuildingPlacementArea placementArea = GetBuildingPlacementArea();
        if (placementArea == null || !requireBuildableCellForConstruction)
        {
            return researchSlotCount;
        }

        return Mathf.Min(researchSlotCount, placementArea.BuildableSlotCount);
    }

    public int GetMaxBuildingSlotCount()
    {
        return MaxBuildingSlotCount;
    }

    internal void ApplyStarterBuildingsToEmptySlots()
    {
        var starterBuildings = BuildingCatalog.GetStarterBuildings();

        for (int i = 0; i < cityData.buildings.Count; i++)
        {
            BuildingInstance slot = cityData.buildings[i];
            if (slot == null || slot.data is not EmptySc emptySlot)
            {
                continue;
            }

            ConfigureEmptySlotForConstruction(emptySlot, starterBuildings);
        }
    }

    private BuildingInstance CreateEmptySlot()
    {
        EmptySc emptySlot = new EmptySc();
        ConfigureEmptySlotForConstruction(emptySlot, BuildingCatalog.GetStarterBuildings());
        return new BuildingInstance(emptySlot);
    }

    private static void ConfigureEmptySlotForConstruction(EmptySc emptySlot, IReadOnlyList<BuildingData> starterBuildings)
    {
        if (emptySlot == null)
            return;

        if (emptySlot.nextUpgradeBuildings == null)
            emptySlot.nextUpgradeBuildings = new List<BuildingData>();

        emptySlot.nextUpgradeBuildings.Clear();

        if (starterBuildings == null)
            return;

        for (int i = 0; i < starterBuildings.Count; i++)
        {
            if (starterBuildings[i] != null)
                emptySlot.nextUpgradeBuildings.Add(starterBuildings[i]);
        }
    }

    void OnEnable()
    {
        if (calendar == null)
            calendar = FindFirstObjectByType<CalendarScript>();

        if (calendar != null)
            calendar.DayChanged += OnDayPassed;
    }

    void OnDisable()
    {
        if (calendar != null)
            calendar.DayChanged -= OnDayPassed;
    }

    void OnMouseDown()
    {
        TryOpenCityPanelFromPointer();
    }

    private void DispatchWorldClickFallback()
    {
        if (!Input.GetMouseButtonDown(0) || lastCityPanelOpenFrame == Time.frameCount)
        {
            return;
        }

        if (IsPointerBlockedByUIOrDeck())
        {
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, float.PositiveInfinity);
        if (hits == null || hits.Length == 0)
        {
            return;
        }

        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        for (int i = 0; i < hits.Length; i++)
        {
            CityScript city = hits[i].collider != null
                ? hits[i].collider.GetComponentInParent<CityScript>()
                : null;

            if (city != this)
            {
                continue;
            }

            TryOpenCityPanelFromPointer();
            return;
        }
    }

    private void TryOpenCityPanelFromPointer()
    {
        if (lastCityPanelOpenFrame == Time.frameCount || IsPointerBlockedByUIOrDeck())
        {
            return;
        }

        if (TutorialManager.Instance != null && !TutorialManager.Instance.IsCityClickAllowed(this))
        {
            return;
        }

        if (uiController == null)
        {
            uiController = FindFirstObjectByType<CityUIController>();
        }

        if (uiController == null || cityData == null)
        {
            return;
        }

        lastCityPanelOpenFrame = Time.frameCount;
        ManagementSFXManager.Instance?.PlayCityClick();
        uiController.OpenCityPanel(this);
    }

    private static bool IsPointerBlockedByUIOrDeck()
    {
        if (TutorialManager.IsMouseInputBlocked)
        {
            return true;
        }

        if (DeckBuilderUI.IsOpen)
        {
            return true;
        }

        return IsPointerOverBlockingUI();
    }

    private static bool IsPointerOverBlockingUI()
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        uiRaycastResults.Clear();
        EventSystem.current.RaycastAll(pointerData, uiRaycastResults);

        for (int i = 0; i < uiRaycastResults.Count; i++)
        {
            GameObject target = uiRaycastResults[i].gameObject;
            if (target == null)
            {
                continue;
            }

            if (target.GetComponentInParent<Selectable>() != null)
            {
                return true;
            }
        }

        return EventSystem.current.IsPointerOverGameObject();
    }

    private void OnDayPassed(GameDate _date)
    {
        AdvanceConstruction();
        ProgressFactoryBuildings();
    }

    private void AdvanceConstruction()
    {
        if (cityData == null || cityData.buildings == null) return;

        bool powerNeedsRefresh = false;

        for (int i = 0; i < cityData.buildings.Count; i++)
        {
            BuildingInstance building = cityData.buildings[i];
            if (building == null || !building.IsUnderConstruction())
                continue;

            if (building.ProgressConstructionDay())
            {
                powerNeedsRefresh = true;
            }
        }

        if (powerNeedsRefresh)
        {
            CalculateCityPower();
            SyncWorldBuildings();

            if (cityData.owner != null)
                cityData.owner.TryUnlockInitialEmployeeByBuildingCount();
        }
    }

    public void CalculateCityPower()
    {
        if (cityData == null)
            return;

        CityResourceSnapshot snapshot = ManagementResourceCalculator.CalculateCity(this);

        cityData.totalRPProduction = snapshot.rpProduction;
        cityData.totalPowerProduction = snapshot.powerProduction;
        cityData.totalPowerConsumption = snapshot.powerConsumption;

        if (cityData.owner != null)
        {
            cityData.owner.RecalculateFactionResources();
        }
    }

    public CitySaveData ExportSaveData()
    {
        return CitySaveService.ExportSaveData(this);
    }

    public void ImportSaveData(CitySaveData data, System.Collections.Generic.IDictionary<string, FactionManager> factionsByName = null)
    {
        CitySaveService.ImportSaveData(this, data, factionsByName);
    }

    private static PowerBuildingDatabaseSO GetPowerDatabase()
    {
        return BuildingDatabaseLoader.GetPowerDatabase(ref powerDb, "[CityScript]");
    }

    private static EconomicBuildingDatabaseSO GetEconomicDatabase()
    {
        return BuildingDatabaseLoader.GetEconomicDatabase(ref economicDb, "[CityScript]");
    }

    private static ResearchBuildingDatabaseSO GetResearchDatabase()
    {
        return BuildingDatabaseLoader.GetResearchDatabase(ref researchDb, "[CityScript]");
    }

    private static FactoryBuildingDatabaseSO GetFactoryDatabase()
    {
        return BuildingDatabaseLoader.GetFactoryDatabase(ref factoryDb, "[CityScript]");
    }

    private static SupportBuildingDatabaseSO GetSupportDatabase()
    {
        return BuildingDatabaseLoader.GetSupportDatabase(ref supportDb, "[CityScript]");
    }

    private void ProgressFactoryBuildings()
    {
        if (cityData == null || cityData.buildings == null) return;
        if (calendar == null || calendar.CurrentDate == null)
        {
            Debug.LogWarning("CityScript : no calendar date for factory progress");
            return;
        }

        for (int i = 0; i < cityData.buildings.Count; i++)
        {
            BuildingInstance building = cityData.buildings[i];
            if (building == null || building.data == null || !building.data.IsFactory())
                continue;

            FactoryBuildingProgressService.ProgressDay(building, calendar.CurrentDate, cityData.owner);
        }
    }

    public int GetRequiredAdditionalPowerForBuild(int _index, BuildingData _building)
    {
        if (_building == null)
            return 0;

        int targetPowerConsumption = Mathf.Max(0, _building.powerConsumption);
        BuildingInstance currentInstance = GetBuildingInstance(_index);
        if (currentInstance == null || currentInstance.IsEmptySlot() || currentInstance.data == null)
            return targetPowerConsumption;

        int currentPowerConsumption = Mathf.Max(0, currentInstance.data.powerConsumption);
        return Mathf.Max(0, targetPowerConsumption - currentPowerConsumption);
    }

    public BuildingInstance GetBuildingInstance(int _index)
    {
        if (cityData == null || cityData.buildings == null)
            return null;

        if (_index < 0 || _index >= cityData.buildings.Count)
            return null;

        return cityData.buildings[_index];
    }

    public bool IsSlotScavengerLocked(int _index)
    {
        BuildingInstance building = GetBuildingInstance(_index);
        return building != null && building.IsScavengerLocked();
    }

    /// <summary>
    /// 스캐빈저 전투 승리 후 해당 슬롯을 해금하여 일반 빈 슬롯으로 전환한다.
    /// </summary>
    public bool UnlockScavengerSlot(int _index)
    {
        BuildingInstance building = GetBuildingInstance(_index);
        if (building == null || !building.IsScavengerLocked())
            return false;

        building.SetScavengerLocked(false);
        SyncWorldBuildingSlot(_index);

        if (uiController != null)
            uiController.RefreshSelectedCityUI();

        return true;
    }

    public bool HasAnyBuildingUnderConstruction()
    {
        if (cityData == null || cityData.buildings == null)
            return false;

        for (int i = 0; i < cityData.buildings.Count; i++)
        {
            BuildingInstance building = cityData.buildings[i];
            if (building != null && building.IsUnderConstruction())
                return true;
        }

        return false;
    }

    public bool ExecuteEmergencyOrder(int _index, out string _failReason)
    {
        return EmergencyOrderService.TryExecute(this, _index, cityData != null ? cityData.owner : null, out _failReason);
    }

    public bool SetBuildingDisabled(int _index, bool _isDisabled)
    {
        BuildingInstance building = GetBuildingInstance(_index);
        if (building == null || building.IsEmptySlot() || building.IsUnderConstruction())
            return false;

        building.SetDisabled(_isDisabled);
        CalculateCityPower();

        if (uiController != null)
            uiController.RefreshSelectedCityUI();

        return true;
    }

    public bool ToggleBuildingDisabled(int _index)
    {
        BuildingInstance building = GetBuildingInstance(_index);
        if (building == null || building.IsEmptySlot() || building.IsUnderConstruction())
            return false;

        return SetBuildingDisabled(_index, !building.IsDisabled());
    }

    public bool DestroyBuilding(int _index, float _refundRate, out int _refundCredit, out string _failReason)
    {
        _refundCredit = 0;
        _failReason = string.Empty;

        if (cityData == null)
        {
            _failReason = "City data is missing.";
            return false;
        }

        if (cityData.owner == null)
        {
            _failReason = "City owner is missing.";
            return false;
        }

        if (cityData.buildings == null)
        {
            _failReason = "Building list is missing.";
            return false;
        }

        if (_index < 0 || _index >= cityData.buildings.Count)
        {
            _failReason = "Invalid building slot.";
            return false;
        }

        if (_index >= GetAvailableBuildingSlotCount())
        {
            _failReason = "Building slot is locked.";
            return false;
        }

        BuildingInstance currentInstance = cityData.buildings[_index];
        if (currentInstance != null && currentInstance.IsScavengerLocked())
        {
            _failReason = "Building slot is occupied by scavengers.";
            return false;
        }

        if (currentInstance == null || currentInstance.IsEmptySlot() || currentInstance.data == null)
        {
            _failReason = "Building slot is empty.";
            return false;
        }

        if (HasAnyBuildingUnderConstruction())
        {
            _failReason = "A building is under construction or upgrade.";
            return false;
        }

        BuildingData currentBuilding = currentInstance.data;
        float refundRate = Mathf.Clamp01(_refundRate);
        _refundCredit = Mathf.Max(0, Mathf.RoundToInt(currentBuilding.constructionCost * refundRate));

        BuildingData previousBuilding = FindPreviousTierBuilding(currentBuilding);
        BuildingInstance replacement = previousBuilding != null
            ? new BuildingInstance(previousBuilding)
            : CreateEmptySlot();

        replacement.SetDisabled(false);
        replacement.upgradeTargetData = null;
        replacement.remainConstructionDay = 0;
        replacement.factoryProductionProgressBuffer = 0f;

        cityData.buildings[_index] = replacement;

        if (_refundCredit > 0)
            cityData.owner.ChangeCredit(_refundCredit);

        CalculateCityPower();
        SyncWorldBuildingSlot(_index);

        if (uiController != null)
            uiController.RefreshSelectedCityUI();

        return true;
    }

    public bool BuildBuilding(int _index, BuildingData _building)
    {
        BuildingConstructionValidationResult validation = BuildingConstructionValidator.Validate(
            this,
            _index,
            _building,
            cityData != null ? cityData.owner : null);

        if (!validation.canBuild)
        {
            Debug.Log($"CityScript : {validation.message}");
            return false;
        }

        if (!cityData.owner.ChangeCredit(-_building.constructionCost))
        {
            Debug.Log("CityScript : not enough credit");
            return false;
        }

        BuildingInstance instance = cityData.buildings[_index];
        if (instance == null || instance.IsEmptySlot())
        {
            instance = new BuildingInstance(new EmptySc());
            instance.SetDisabled(false);
            instance.StartConstruction(_building);
            cityData.buildings[_index] = instance;
        }
        else
        {
            instance.StartConstruction(_building);
        }

        CalculateCityPower();
        SyncWorldBuildingSlot(_index);
        return true;
    }

    public bool IsBuildingSlotBuildable(int slotIndex)
    {
        HexBuildingPlacementArea placementArea = GetBuildingPlacementArea();
        return placementArea == null || !requireBuildableCellForConstruction || placementArea.CanPlaceSlot(slotIndex);
    }

    public void SyncWorldBuildings()
    {
        HexBuildingPlacementArea placementArea = GetBuildingPlacementArea();
        if (placementArea == null || cityData == null)
        {
            return;
        }

        placementArea.SyncBuildings(cityData.buildings);
    }

    private void SyncWorldBuildingSlot(int slotIndex)
    {
        HexBuildingPlacementArea placementArea = GetBuildingPlacementArea();
        if (placementArea == null || cityData == null || cityData.buildings == null)
        {
            return;
        }

        BuildingInstance building = slotIndex >= 0 && slotIndex < cityData.buildings.Count
            ? cityData.buildings[slotIndex]
            : null;

        placementArea.PlaceOrClearSlot(slotIndex, building);
    }

    private BuildingData FindPreviousTierBuilding(BuildingData currentBuilding)
    {
        if (currentBuilding == null || currentBuilding.IsEmptySlot())
            return null;

        BuildingData previousBuilding = FindPreviousTierBuildingInList(GetPowerDatabase().allBuildings, currentBuilding);
        if (previousBuilding != null) return previousBuilding;

        previousBuilding = FindPreviousTierBuildingInList(GetEconomicDatabase().allBuildings, currentBuilding);
        if (previousBuilding != null) return previousBuilding;

        previousBuilding = FindPreviousTierBuildingInList(GetResearchDatabase().allBuildings, currentBuilding);
        if (previousBuilding != null) return previousBuilding;

        previousBuilding = FindPreviousTierBuildingInList(GetFactoryDatabase().allBuildings, currentBuilding);
        if (previousBuilding != null) return previousBuilding;

        return FindPreviousTierBuildingInList(GetSupportDatabase().allBuildings, currentBuilding);
    }

    private static BuildingData FindPreviousTierBuildingInList(List<BuildingData> buildings, BuildingData currentBuilding)
    {
        if (buildings == null || currentBuilding == null)
            return null;

        for (int i = 0; i < buildings.Count; i++)
        {
            BuildingData candidate = buildings[i];
            if (candidate == null || candidate.IsEmptySlot() || candidate.category != currentBuilding.category)
                continue;

            if (string.Equals(candidate.ID, currentBuilding.ID, System.StringComparison.OrdinalIgnoreCase))
                continue;

            if (candidate.nextUpgradeBuildings == null)
                continue;

            for (int j = 0; j < candidate.nextUpgradeBuildings.Count; j++)
            {
                BuildingData nextBuilding = candidate.nextUpgradeBuildings[j];
                if (nextBuilding == null)
                    continue;

                if (ReferenceEquals(nextBuilding, currentBuilding)
                    || string.Equals(nextBuilding.ID, currentBuilding.ID, System.StringComparison.OrdinalIgnoreCase))
                {
                    return candidate;
                }
            }
        }

        return null;
    }

    private HexBuildingPlacementArea GetBuildingPlacementArea()
    {
        if (buildingPlacementArea != null)
        {
            return buildingPlacementArea;
        }

        buildingPlacementArea = GetComponent<HexBuildingPlacementArea>();
        if (buildingPlacementArea == null)
        {
            buildingPlacementArea = GetComponentInChildren<HexBuildingPlacementArea>(true);
        }

        if (buildingPlacementArea == null)
        {
            buildingPlacementArea = GetComponentInParent<HexBuildingPlacementArea>();
        }

        return buildingPlacementArea;
    }
}
