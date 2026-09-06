using System;
using System.Collections.Generic;
using UnityEngine;

public class EmployeeAssignmentService : MonoBehaviour
{
    private const int SpyReturnDelayMonths = 3;
    private const int SpyDetectionFavorPenalty = -20;
    private const float SpyShareAcquireChancePercent = 10f;
    private const float SpyShareAssociationConvertChancePercent = 20f;
    private const float OperativeSpyManipulationBonusPercent = 10f;

    public enum SpyShareOperationType
    {
        StealShare,
        ConvertShareToCorporateAssociation
    }

    public enum SpyDetectionResultType
    {
        SafeExit,
        Escape,
        Removed
    }

    [SerializeField] private CalendarScript calendar;
    [SerializeField] private int spyDetectionChancePercent = 10;
    [SerializeField] private int defensiveEmployeeDetectionBonusPercent = 10;

    private bool isSubscribed;

    public event Action<EmployeeData> SpyProgressChanged;
    public event Action<EmployeeData, CityScript, FactionManager, FactionManager, int, SpyShareOperationType> SpyShareOperationSucceeded;
    public event Action<EmployeeData, CityScript, FactionManager, SpyDetectionResultType> SpyDetectionResolved;

    private void Awake()
    {
        EnsureReferences();
    }

    private void OnEnable()
    {
        EnsureReferences();

        if (calendar == null || isSubscribed)
            return;

        calendar.MonthChanged += OnMonthChanged;
        isSubscribed = true;
    }

    private void OnDisable()
    {
        if (calendar != null && isSubscribed)
            calendar.MonthChanged -= OnMonthChanged;

        isSubscribed = false;
    }

    private void OnMonthChanged(int _year, int _month)
    {
        ProcessAllSpies();
    }

    private void ProcessAllSpies()
    {
        FactionManager[] factions = FindObjectsByType<FactionManager>(FindObjectsSortMode.None);
        for (int i = 0; i < factions.Length; i++)
        {
            FactionManager faction = factions[i];
            if (faction != null)
                faction.ProgressReturningEmployees();
        }

        for (int i = 0; i < factions.Length; i++)
        {
            FactionManager faction = factions[i];
            if (faction == null)
                continue;

            IReadOnlyList<EmployeeData> spyEmployees = faction.GetSpyEmployees();
            for (int j = 0; j < spyEmployees.Count; j++)
                ProcessSpyEmployee(spyEmployees[j]);
        }
    }

    private void ProcessSpyEmployee(EmployeeData employee)
    {
        if (employee == null || !employee.IsSpy)
            return;

        if (TryResolveSpyDetection(employee))
            return;

        employee.assignedMonthCounter++;
        if (employee.assignedMonthCounter < 3)
        {
            NotifySpyProgressChanged(employee);
            return;
        }

        employee.assignedMonthCounter = 0;
        NotifySpyProgressChanged(employee);

        float acquireChance = CalculateSpyShareAcquireChance(employee);
        float associationConvertChance = CalculateSpyShareAssociationConvertChance(employee, acquireChance);
        float roll = UnityEngine.Random.Range(0f, 100f);
        if (roll < acquireChance)
        {
            TransferShareToSpyOwner(employee);
            return;
        }

        if (roll < acquireChance + associationConvertChance)
        {
            ConvertShareToCorporateAssociation(employee);
            return;
        }

        Debug.Log($"[EmployeeAssignmentService] Spy action had no effect. employee={employee.employeeName}, city={GetCityName(employee.assignedCity)}, roll={roll}");
    }

    private float CalculateSpyShareAcquireChance(EmployeeData employee)
    {
        float baseSuccessChance = SpyShareAcquireChancePercent + SpyShareAssociationConvertChancePercent;
        float totalSuccessChance = CalculateSpyShareTotalSuccessChance(employee);
        float acquireRatio = SpyShareAcquireChancePercent / baseSuccessChance;
        return totalSuccessChance * acquireRatio;
    }

    private float CalculateSpyShareAssociationConvertChance(EmployeeData employee, float acquireChance)
    {
        float totalSuccessChance = CalculateSpyShareTotalSuccessChance(employee);
        return totalSuccessChance - acquireChance;
    }

    private float CalculateSpyShareTotalSuccessChance(EmployeeData employee)
    {
        float totalSuccessChance = SpyShareAcquireChancePercent + SpyShareAssociationConvertChancePercent;
        if (employee != null && employee.IsSpy && employee.abilityType == EmployeeAbilityType.Operative)
            totalSuccessChance += OperativeSpyManipulationBonusPercent;

        return Mathf.Clamp(totalSuccessChance, 0f, 100f);
    }

    private bool TryResolveSpyDetection(EmployeeData employee)
    {
        int chance = CalculateSpyDetectionChance(employee);
        if (chance <= 0)
            return false;

        int detectionRoll = UnityEngine.Random.Range(0, 100);
        if (detectionRoll >= chance)
            return false;

        CityScript city = employee.assignedCity;
        FactionManager targetFaction = city != null && city.cityData != null ? city.cityData.owner : null;
        SpyDetectionResultType resultType = RollSpyDetectionResult();

        switch (resultType)
        {
            case SpyDetectionResultType.SafeExit:
                BeginSpyReturn(employee, city, targetFaction, resultType);
                break;
            case SpyDetectionResultType.Escape:
                ApplySpyDetectionFavorPenalty(targetFaction, employee.ownerFaction);
                BeginSpyReturn(employee, city, targetFaction, resultType);
                break;
            case SpyDetectionResultType.Removed:
                ApplySpyDetectionFavorPenalty(targetFaction, employee.ownerFaction);
                MarkSpyLost(employee, city, targetFaction, resultType);
                break;
        }

        return true;
    }

    private int CalculateSpyDetectionChance(EmployeeData employee)
    {
        int chance = spyDetectionChancePercent;

        CityScript city = employee != null ? employee.assignedCity : null;
        FactionManager targetFaction = city != null && city.cityData != null ? city.cityData.owner : null;

        if (targetFaction != null
            && targetFaction.HasAssignedEmployeeAbilityInOwnedCity(city, EmployeeAbilityType.SecurityExpert))
        {
            chance += defensiveEmployeeDetectionBonusPercent;
        }

        return Mathf.Clamp(chance, 0, 100);
    }

    private SpyDetectionResultType RollSpyDetectionResult()
    {
        int roll = UnityEngine.Random.Range(0, 100);
        if (roll < 25)
            return SpyDetectionResultType.SafeExit;

        if (roll < 75)
            return SpyDetectionResultType.Escape;

        return SpyDetectionResultType.Removed;
    }

    private void BeginSpyReturn(EmployeeData employee, CityScript city, FactionManager targetFaction, SpyDetectionResultType resultType)
    {
        if (employee == null || employee.ownerFaction == null)
            return;

        if (!employee.ownerFaction.BeginEmployeeReturn(employee, SpyReturnDelayMonths, out string message))
        {
            Debug.LogWarning($"[EmployeeAssignmentService] Spy return failed. employee={employee.employeeName}, reason={message}");
            return;
        }

        NotifySpyDetectionResolved(employee, city, targetFaction, resultType);
        NotifySpyProgressChanged(employee);
    }

    private void MarkSpyLost(EmployeeData employee, CityScript city, FactionManager targetFaction, SpyDetectionResultType resultType)
    {
        if (employee == null || employee.ownerFaction == null)
            return;

        if (!employee.ownerFaction.MarkEmployeeLost(employee, out string message))
        {
            Debug.LogWarning($"[EmployeeAssignmentService] Spy removal failed. employee={employee.employeeName}, reason={message}");
            return;
        }

        NotifySpyDetectionResolved(employee, city, targetFaction, resultType);
        NotifySpyProgressChanged(employee);
    }

    private void ApplySpyDetectionFavorPenalty(FactionManager targetFaction, FactionManager spyOwnerFaction)
    {
        if (targetFaction == null || spyOwnerFaction == null)
            return;

        RelationManager relationManager = FindFirstObjectByType<RelationManager>();
        if (relationManager == null)
        {
            Debug.LogWarning("[EmployeeAssignmentService] RelationManager not found while applying spy detection favor penalty.");
            return;
        }

        relationManager.AddFavor(targetFaction, spyOwnerFaction, SpyDetectionFavorPenalty);
    }

    private void TransferShareToSpyOwner(EmployeeData employee)
    {
        if (!TryGetSpyTarget(employee, out FactionManager targetFaction, out int amount))
            return;

        string message;
        bool success = CityShareManager.instance.TransferShare(employee.assignedCity, targetFaction, employee.ownerFaction, amount, out message);
        if (success)
        {
            Debug.Log($"[EmployeeAssignmentService] Spy gained share. city={GetCityName(employee.assignedCity)}, from={GetFactionName(targetFaction)}, to={GetFactionName(employee.ownerFaction)}, amount={amount}%");
            NotifySpyShareOperationSucceeded(employee, employee.assignedCity, targetFaction, employee.ownerFaction, amount, SpyShareOperationType.StealShare);
            return;
        }

        Debug.LogWarning($"[EmployeeAssignmentService] Spy share transfer failed. city={GetCityName(employee.assignedCity)}, reason={message}");
    }

    private void ConvertShareToCorporateAssociation(EmployeeData employee)
    {
        if (!TryGetSpyTarget(employee, out FactionManager targetFaction, out int amount))
            return;

        string message;
        bool success = CityShareManager.instance.TransferShare(employee.assignedCity, targetFaction, null, amount, out message);
        if (success)
        {
            Debug.Log($"[EmployeeAssignmentService] Spy converted share to corporate association. city={GetCityName(employee.assignedCity)}, from={GetFactionName(targetFaction)}, amount={amount}%");
            NotifySpyShareOperationSucceeded(employee, employee.assignedCity, targetFaction, null, amount, SpyShareOperationType.ConvertShareToCorporateAssociation);
            return;
        }

        Debug.LogWarning($"[EmployeeAssignmentService] Spy corporate association conversion failed. city={GetCityName(employee.assignedCity)}, reason={message}");
    }

    private bool TryGetSpyTarget(EmployeeData employee, out FactionManager targetFaction, out int amount)
    {
        targetFaction = null;
        amount = 0;

        if (employee == null || employee.assignedCity == null || employee.ownerFaction == null)
        {
            Debug.LogWarning("[EmployeeAssignmentService] Spy data is missing.");
            return false;
        }

        if (CityShareManager.instance == null)
        {
            Debug.LogWarning("[EmployeeAssignmentService] CityShareManager instance is missing.");
            return false;
        }

        CityScript city = employee.assignedCity;
        if (city.cityData == null || city.cityData.shareData == null)
        {
            Debug.LogWarning($"[EmployeeAssignmentService] City share data is missing. city={GetCityName(city)}");
            return false;
        }

        IReadOnlyList<CityShareData.FactionShareEntry> shares = city.cityData.shareData.Shares;
        int targetSharePercent = 0;

        for (int i = 0; i < shares.Count; i++)
        {
            CityShareData.FactionShareEntry entry = shares[i];
            if (entry == null || entry.faction == null || entry.sharePercent <= 0)
                continue;

            if (entry.faction == employee.ownerFaction)
                continue;

            if (entry.sharePercent <= targetSharePercent)
                continue;

            targetFaction = entry.faction;
            targetSharePercent = entry.sharePercent;
        }

        if (targetFaction == null || targetSharePercent <= 0)
        {
            Debug.Log($"[EmployeeAssignmentService] Spy found no valid target share. employee={employee.employeeName}, city={GetCityName(city)}");
            return false;
        }

        amount = Mathf.Min(5, targetSharePercent);
        return amount > 0;
    }

    private void NotifySpyShareOperationSucceeded(
        EmployeeData employee,
        CityScript city,
        FactionManager fromFaction,
        FactionManager toFaction,
        int amount,
        SpyShareOperationType operationType)
    {
        SpyShareOperationSucceeded?.Invoke(employee, city, fromFaction, toFaction, amount, operationType);
    }

    private void NotifySpyProgressChanged(EmployeeData employee)
    {
        SpyProgressChanged?.Invoke(employee);
    }

    private void NotifySpyDetectionResolved(
        EmployeeData employee,
        CityScript city,
        FactionManager targetFaction,
        SpyDetectionResultType resultType)
    {
        SpyDetectionResolved?.Invoke(employee, city, targetFaction, resultType);
    }

    private void EnsureReferences()
    {
        if (calendar == null)
            calendar = GetComponent<CalendarScript>();

        if (calendar == null)
            calendar = GetComponentInParent<CalendarScript>();

        if (calendar == null)
            calendar = FindFirstObjectByType<CalendarScript>();
    }

    private string GetCityName(CityScript city)
    {
        return city != null && city.cityData != null ? city.cityData.cityName : "Unknown";
    }

    private string GetFactionName(FactionManager faction)
    {
        return faction != null && !string.IsNullOrWhiteSpace(faction.factionName) ? faction.factionName : "Corporate Association";
    }
}
