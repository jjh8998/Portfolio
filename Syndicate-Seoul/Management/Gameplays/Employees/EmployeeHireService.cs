using System;
using System.Collections.Generic;
using UnityEngine;

public class EmployeeHireService : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CalendarScript calendar;
    [SerializeField] private CEOStartupAssigner ceoStartupAssigner;
    [SerializeField] private CEODatabaseSO ceoDatabase;
    [SerializeField] private FactionManager playerFaction;

    [Header("Candidates")]
    [SerializeField] private int candidateCount = 3;
    [SerializeField] private int candidateKeepMonths = 6;
    [SerializeField] private int rerollCost = 100;

    public event Action CandidatesChanged;
    public event Action<EmployeeData> EmployeeHired;

    private readonly List<CEOData> candidates = new List<CEOData>();
    private readonly Dictionary<string, EmployeeAbilityType> candidateAbilities = new Dictionary<string, EmployeeAbilityType>(StringComparer.OrdinalIgnoreCase);
    private int elapsedMonths;
    private CalendarScript subscribedCalendar;

    public IReadOnlyList<CEOData> Candidates => candidates;
    public int RerollCost => Mathf.Max(0, rerollCost);
    public int RemainingMonths => Mathf.Max(0, Mathf.Max(1, candidateKeepMonths) - elapsedMonths);
    public bool IsUnlocked => playerFaction != null && playerFaction.IsEmployeeHiringUnlocked;

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        SubscribeCalendar();
        FactionManager.InitialEmployeeUnlocked -= OnInitialEmployeeUnlocked;
        FactionManager.InitialEmployeeUnlocked += OnInitialEmployeeUnlocked;
        RefreshNow();
    }

    private void OnDisable()
    {
        UnsubscribeCalendar();
        FactionManager.InitialEmployeeUnlocked -= OnInitialEmployeeUnlocked;
    }

    private void OnDestroy()
    {
        UnsubscribeCalendar();
        FactionManager.InitialEmployeeUnlocked -= OnInitialEmployeeUnlocked;
    }

    public void RefreshNow()
    {
        ResolveReferences();

        if (IsUnlocked && candidates.Count == 0)
            GenerateCandidates(true);
    }

    public bool TryHireCandidate(string _ceoId, out EmployeeData _employee, out string _message)
    {
        ResolveReferences();

        _employee = null;
        _message = string.Empty;

        if (!IsUnlocked)
        {
            _message = "Employee hiring is locked.";
            return false;
        }

        CEOData ceo = FindCandidate(_ceoId);
        if (ceo == null)
        {
            _message = "Candidate CEO not found.";
            return false;
        }

        EmployeeAbilityType abilityType = GetOrCreateCandidateAbility(ceo.id);
        if (playerFaction == null || !playerFaction.TryHireEmployeeFromCEO(ceo, abilityType, out _employee, out _message))
            return false;

        RemoveCandidate(ceo.id);
        EmployeeHired?.Invoke(_employee);
        CandidatesChanged?.Invoke();
        return true;
    }

    public bool TryGetCandidateAbility(string _ceoId, out EmployeeAbilityType abilityType)
    {
        abilityType = EmployeeAbilityType.None;

        if (string.IsNullOrWhiteSpace(_ceoId))
            return false;

        CEOData ceo = FindCandidate(_ceoId);
        if (ceo == null)
            return false;

        abilityType = GetOrCreateCandidateAbility(ceo.id);
        return abilityType != EmployeeAbilityType.None;
    }

    public bool TryRerollCandidates(out string _message)
    {
        ResolveReferences();

        _message = string.Empty;

        if (!IsUnlocked)
        {
            _message = "Employee hiring is locked.";
            return false;
        }

        int cost = RerollCost;
        if (playerFaction == null || playerFaction.GetCredit < cost)
        {
            _message = "Not enough credit.";
            return false;
        }

        if (cost > 0 && !playerFaction.ChangeCredit(-cost))
        {
            _message = "Not enough credit.";
            return false;
        }

        GenerateCandidates(true);
        _message = "Employee candidates refreshed.";
        return true;
    }

    public EmployeeHireSaveData ExportSaveData()
    {
        ResolveReferences();

        EmployeeHireSaveData saveData = new EmployeeHireSaveData
        {
            isUnlocked = IsUnlocked,
            elapsedMonths = Mathf.Max(0, elapsedMonths),
            candidateCeoIds = new List<string>(),
            candidateAbilities = new List<EmployeeHireCandidateSaveData>()
        };

        for (int i = 0; i < candidates.Count; i++)
        {
            CEOData ceo = candidates[i];
            if (ceo == null || string.IsNullOrWhiteSpace(ceo.id))
                continue;

            EmployeeAbilityType abilityType = GetOrCreateCandidateAbility(ceo.id);
            saveData.candidateCeoIds.Add(ceo.id);
            saveData.candidateAbilities.Add(new EmployeeHireCandidateSaveData
            {
                ceoId = ceo.id,
                abilityType = abilityType
            });
        }

        return saveData;
    }

    public void ImportSaveData(EmployeeHireSaveData _data)
    {
        ResolveReferences();

        candidates.Clear();
        candidateAbilities.Clear();
        elapsedMonths = _data != null ? Mathf.Max(0, _data.elapsedMonths) : 0;

        Dictionary<string, EmployeeAbilityType> savedAbilities = BuildSavedCandidateAbilityMap(_data);
        HashSet<string> usedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (_data != null && _data.candidateCeoIds != null && _data.candidateCeoIds.Count > 0)
        {
            for (int i = 0; i < _data.candidateCeoIds.Count; i++)
            {
                string ceoId = _data.candidateCeoIds[i];
                EmployeeAbilityType abilityType = savedAbilities != null && !string.IsNullOrWhiteSpace(ceoId) && savedAbilities.TryGetValue(ceoId, out EmployeeAbilityType savedAbility)
                    ? savedAbility
                    : EmployeeAbilityType.None;
                AddImportedCandidate(ceoId, abilityType, usedIds);
            }
        }
        else if (_data != null && _data.candidateAbilities != null)
        {
            for (int i = 0; i < _data.candidateAbilities.Count; i++)
            {
                EmployeeHireCandidateSaveData candidateData = _data.candidateAbilities[i];
                if (candidateData == null)
                    continue;

                AddImportedCandidate(candidateData.ceoId, candidateData.abilityType, usedIds);
            }
        }

        if (IsUnlocked && candidates.Count == 0)
            GenerateCandidates(true);
        else
            CandidatesChanged?.Invoke();
    }

    private void OnMonthChanged(int _year, int _month)
    {
        if (!IsUnlocked)
            return;

        elapsedMonths++;
        if (elapsedMonths >= Mathf.Max(1, candidateKeepMonths) || candidates.Count == 0)
            GenerateCandidates(true);
        else
            CandidatesChanged?.Invoke();
    }

    private void OnInitialEmployeeUnlocked(FactionManager _faction)
    {
        ResolveReferences();

        if (_faction == null || playerFaction == null || _faction != playerFaction)
            return;

        if (candidates.Count == 0)
            GenerateCandidates(true);
        else
            CandidatesChanged?.Invoke();
    }

    private void GenerateCandidates(bool resetElapsedMonths)
    {
        List<CEOData> available = BuildAvailableCEOs();
        candidates.Clear();
        candidateAbilities.Clear();

        int targetCount = Mathf.Max(0, candidateCount);
        while (available.Count > 0 && candidates.Count < targetCount)
        {
            int index = UnityEngine.Random.Range(0, available.Count);
            AddCandidate(available[index], FactionEmployeeScript.GetRandomEmployeeAbilityType());
            available.RemoveAt(index);
        }

        if (resetElapsedMonths)
            elapsedMonths = 0;

        CandidatesChanged?.Invoke();
    }

    private List<CEOData> BuildAvailableCEOs()
    {
        List<CEOData> result = new List<CEOData>();
        IReadOnlyList<CEOData> source = GetCandidateSource();
        if (source == null)
            return result;

        HashSet<string> usedIds = CollectExcludedCEOIds(true);
        for (int i = 0; i < source.Count; i++)
        {
            CEOData ceo = source[i];
            if (ceo == null || string.IsNullOrWhiteSpace(ceo.id))
                continue;

            if (!usedIds.Add(ceo.id))
                continue;

            if (!CanUseCEOAsCandidate(ceo, false))
                continue;

            result.Add(ceo);
        }

        return result;
    }

    private IReadOnlyList<CEOData> GetCandidateSource()
    {
        IReadOnlyList<CEOData> leftoverCEOs = ceoStartupAssigner != null ? ceoStartupAssigner.GetLeftoverCEOs() : null;
        if (leftoverCEOs != null && leftoverCEOs.Count > 0)
            return leftoverCEOs;

        if (ceoDatabase == null)
            ceoDatabase = ScriptableObject.CreateInstance<CEODatabaseSO>();

        ceoDatabase.LoadCSV();
        return ceoDatabase.allCEOs;
    }

    private bool CanUseCEOAsCandidate(CEOData ceo, bool excludeCurrentCandidates)
    {
        if (ceo == null || string.IsNullOrWhiteSpace(ceo.id))
            return false;

        HashSet<string> excludedIds = CollectExcludedCEOIds(excludeCurrentCandidates);
        return !excludedIds.Contains(ceo.id)
            && (playerFaction == null || !playerFaction.HasEmployeeFromCEO(ceo.id));
    }

    private HashSet<string> CollectExcludedCEOIds(bool includeCurrentCandidates)
    {
        HashSet<string> excludedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        FactionManager[] factions = FindObjectsByType<FactionManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < factions.Length; i++)
        {
            FactionManager faction = factions[i];
            if (faction != null && !string.IsNullOrWhiteSpace(faction.ceoId))
                excludedIds.Add(faction.ceoId);
        }

        IReadOnlyList<EmployeeData> employees = playerFaction != null ? playerFaction.GetEmployees() : null;
        if (employees != null)
        {
            for (int i = 0; i < employees.Count; i++)
            {
                EmployeeData employee = employees[i];
                if (employee != null && !string.IsNullOrWhiteSpace(employee.ceoId))
                    excludedIds.Add(employee.ceoId);
            }
        }

        if (includeCurrentCandidates)
        {
            for (int i = 0; i < candidates.Count; i++)
            {
                CEOData candidate = candidates[i];
                if (candidate != null && !string.IsNullOrWhiteSpace(candidate.id))
                    excludedIds.Add(candidate.id);
            }
        }

        return excludedIds;
    }

    private CEOData FindCandidate(string ceoId)
    {
        if (string.IsNullOrWhiteSpace(ceoId))
            return null;

        for (int i = 0; i < candidates.Count; i++)
        {
            CEOData ceo = candidates[i];
            if (ceo != null && string.Equals(ceo.id, ceoId, StringComparison.OrdinalIgnoreCase))
                return ceo;
        }

        return null;
    }

    private CEOData GetCEOById(string ceoId)
    {
        if (string.IsNullOrWhiteSpace(ceoId))
            return null;

        if (ceoDatabase == null)
            ceoDatabase = ScriptableObject.CreateInstance<CEODatabaseSO>();

        return ceoDatabase.GetCEOByID(ceoId);
    }

    private void RemoveCandidate(string ceoId)
    {
        if (string.IsNullOrWhiteSpace(ceoId))
            return;

        for (int i = candidates.Count - 1; i >= 0; i--)
        {
            CEOData ceo = candidates[i];
            if (ceo != null && string.Equals(ceo.id, ceoId, StringComparison.OrdinalIgnoreCase))
                candidates.RemoveAt(i);
        }

        candidateAbilities.Remove(ceoId);
    }

    private void AddCandidate(CEOData ceo, EmployeeAbilityType abilityType)
    {
        if (ceo == null || string.IsNullOrWhiteSpace(ceo.id))
            return;

        candidates.Add(ceo);
        candidateAbilities[ceo.id] = abilityType == EmployeeAbilityType.None
            ? FactionEmployeeScript.GetRandomEmployeeAbilityType()
            : abilityType;
    }

    private void AddImportedCandidate(string ceoId, EmployeeAbilityType abilityType, HashSet<string> usedIds)
    {
        CEOData ceo = GetCEOById(ceoId);
        if (ceo == null || usedIds == null || !usedIds.Add(ceo.id) || !CanUseCEOAsCandidate(ceo, false))
            return;

        AddCandidate(ceo, abilityType);
    }

    private EmployeeAbilityType GetOrCreateCandidateAbility(string ceoId)
    {
        if (string.IsNullOrWhiteSpace(ceoId))
            return FactionEmployeeScript.GetRandomEmployeeAbilityType();

        if (!candidateAbilities.TryGetValue(ceoId, out EmployeeAbilityType abilityType)
            || abilityType == EmployeeAbilityType.None)
        {
            abilityType = FactionEmployeeScript.GetRandomEmployeeAbilityType();
            candidateAbilities[ceoId] = abilityType;
        }

        return abilityType;
    }

    private Dictionary<string, EmployeeAbilityType> BuildSavedCandidateAbilityMap(EmployeeHireSaveData data)
    {
        if (data == null || data.candidateAbilities == null || data.candidateAbilities.Count == 0)
            return null;

        Dictionary<string, EmployeeAbilityType> result = new Dictionary<string, EmployeeAbilityType>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < data.candidateAbilities.Count; i++)
        {
            EmployeeHireCandidateSaveData candidateData = data.candidateAbilities[i];
            if (candidateData == null || string.IsNullOrWhiteSpace(candidateData.ceoId))
                continue;

            result[candidateData.ceoId] = candidateData.abilityType;
        }

        return result;
    }

    private void ResolveReferences()
    {
        if (calendar == null)
            calendar = FindFirstObjectByType<CalendarScript>();

        if (ceoStartupAssigner == null)
            ceoStartupAssigner = FindFirstObjectByType<CEOStartupAssigner>();

        if (playerFaction == null || !playerFaction.IsPlayerFaction)
            playerFaction = FindPlayerFaction();

        if (ceoDatabase == null)
            ceoDatabase = ScriptableObject.CreateInstance<CEODatabaseSO>();
    }

    private void SubscribeCalendar()
    {
        if (subscribedCalendar == calendar)
            return;

        UnsubscribeCalendar();

        if (calendar == null)
            return;

        subscribedCalendar = calendar;
        subscribedCalendar.MonthChanged += OnMonthChanged;
    }

    private void UnsubscribeCalendar()
    {
        if (subscribedCalendar == null)
            return;

        subscribedCalendar.MonthChanged -= OnMonthChanged;
        subscribedCalendar = null;
    }

    private FactionManager FindPlayerFaction()
    {
        FactionManager[] factions = FindObjectsByType<FactionManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < factions.Length; i++)
        {
            FactionManager faction = factions[i];
            if (faction != null && faction.IsPlayerFaction)
                return faction;
        }

        for (int i = 0; i < factions.Length; i++)
        {
            FactionManager faction = factions[i];
            if (faction != null && faction.GetComponent<NationAIController>() == null)
                return faction;
        }

        return factions != null && factions.Length > 0 ? factions[0] : null;
    }
}
