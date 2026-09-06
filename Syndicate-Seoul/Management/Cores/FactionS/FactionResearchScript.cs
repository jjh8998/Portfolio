using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FactionResearchScript
{
    private const string DefaultCompletedResearchId = "research_0";

    [SerializeField] private FactionResearchState researchState = new FactionResearchState();
    [SerializeField] private ResearchDatabaseSO researchDatabase;

    private FactionManager ownerFaction;

    public event Action ResearchStateChanged;
    public event Action ResearchQueueChanged;
    public event Action<string> ResearchStarted;
    public event Action<string> ResearchCompleted;

    public void Initialize(FactionManager _ownerFaction = null)
    {
        ownerFaction = _ownerFaction;
        EnsureResearchState();
        EnsureDefaultCompletedResearch();
    }

    public bool HasCompletedResearch(string _researchId)
    {
        if (string.IsNullOrWhiteSpace(_researchId))
            return true;

        if (researchState == null || researchState.completedResearchIds == null)
            return false;

        string researchId = NormalizeResearchId(_researchId);

        for (int i = 0; i < researchState.completedResearchIds.Count; i++)
        {
            string completedResearchId = NormalizeResearchId(researchState.completedResearchIds[i]);
            if (string.IsNullOrWhiteSpace(completedResearchId))
                continue;

            if (string.Equals(completedResearchId, researchId, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    public bool IsResearchCompleted(string _researchId)
    {
        return HasCompletedResearch(_researchId);
    }

    public bool IsResearchQueued(string _researchId)
    {
        if (string.IsNullOrWhiteSpace(_researchId))
            return false;

        EnsureResearchState();
        string researchId = NormalizeResearchId(_researchId);

        for (int i = 0; i < researchState.queuedResearchIds.Count; i++)
        {
            string queuedResearchId = NormalizeResearchId(researchState.queuedResearchIds[i]);
            if (string.IsNullOrWhiteSpace(queuedResearchId))
                continue;

            if (string.Equals(queuedResearchId, researchId, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    public bool IsResearchInProgress(string _researchId)
    {
        if (string.IsNullOrWhiteSpace(_researchId))
            return false;

        EnsureResearchState();
        string researchId = NormalizeResearchId(_researchId);
        string currentResearchId = NormalizeResearchId(researchState.currentResearchId);
        return string.Equals(currentResearchId, researchId, StringComparison.OrdinalIgnoreCase);
    }

    public ResearchData GetCurrentResearch()
    {
        EnsureResearchState();

        if (string.IsNullOrWhiteSpace(researchState.currentResearchId))
            return null;

        return GetResearchDatabase().GetResearchById(researchState.currentResearchId);
    }

    public ResearchData GetResearchById(string _researchId)
    {
        if (string.IsNullOrWhiteSpace(_researchId))
            return null;

        ResearchDatabaseSO db = GetResearchDatabase();
        return db != null ? db.GetResearchById(_researchId) : null;
    }

    public IReadOnlyList<string> GetQueuedResearchIds()
    {
        EnsureResearchState();
        return researchState.queuedResearchIds;
    }

    public bool CanStartResearchNow(string _researchId)
    {
        if (string.IsNullOrWhiteSpace(_researchId))
            return false;

        if (IsResearchCompleted(_researchId))
            return false;

        ResearchDatabaseSO db = GetResearchDatabase();
        if (db == null)
            return false;

        ResearchData research = db.GetResearchById(_researchId);
        if (research == null)
            return false;

        if (IsPatentResearchClaimedByOtherFaction(research))
            return false;

        research.EnsureInitialized();

        for (int i = 0; i < research.prerequisiteResearchIds.Count; i++)
        {
            string prerequisiteId = research.prerequisiteResearchIds[i];
            if (!IsResearchCompleted(prerequisiteId))
                return false;
        }

        if (!AreUnlockCategoryRequirementsCompleted(research, db))
            return false;

        return true;
    }

    public bool QueueResearchPath(string _targetResearchId)
    {
        EnsureResearchState();

        if (string.IsNullOrWhiteSpace(_targetResearchId))
            return false;

        string targetResearchId = NormalizeResearchId(_targetResearchId);
        if (IsResearchCompleted(targetResearchId))
            return false;

        ResearchDatabaseSO db = GetResearchDatabase();
        if (db == null)
            return false;

        List<string> path = new List<string>();
        HashSet<string> visitedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        HashSet<string> recursionStack = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!BuildResearchQueuePath(targetResearchId, db, path, visitedIds, recursionStack))
            return false;

        bool queueChanged = false;

        for (int i = 0; i < path.Count; i++)
        {
            string researchId = path[i];
            ResearchData research = db.GetResearchById(researchId);
            if (research == null
                || IsPatentResearchClaimedByOtherFaction(research)
                || IsResearchCompleted(researchId)
                || IsResearchInProgress(researchId)
                || IsResearchQueued(researchId))
            {
                continue;
            }

            researchState.queuedResearchIds.Add(NormalizeResearchId(researchId));
            queueChanged = true;
        }

        bool startedNow = false;
        if (string.IsNullOrWhiteSpace(researchState.currentResearchId))
            startedNow = TryStartNextQueuedResearch();

        if (queueChanged || startedNow)
        {
            ResearchQueueChanged?.Invoke();
            ResearchStateChanged?.Invoke();
        }

        return queueChanged || startedNow;
    }

    public bool StartResearchDirectly(string _researchId)
    {
        EnsureResearchState();

        if (!string.IsNullOrWhiteSpace(researchState.currentResearchId))
            return false;

        if (!CanStartResearchNow(_researchId))
            return false;

        string researchId = NormalizeResearchId(_researchId);
        if (!TryStartResearch(researchId))
            return false;

        ResearchStateChanged?.Invoke();
        return true;
    }

    public bool CancelResearch(string _researchId)
    {
        if (string.IsNullOrWhiteSpace(_researchId))
            return false;

        EnsureResearchState();

        string researchId = NormalizeResearchId(_researchId);

        if (IsResearchInProgress(researchId))
            return CancelCurrentResearch(researchId);

        if (IsResearchQueued(researchId))
            return CancelQueuedResearch(researchId);

        return false;
    }

    public void ClearResearchQueue()
    {
        EnsureResearchState();

        if (researchState.queuedResearchIds.Count == 0)
            return;

        researchState.queuedResearchIds.Clear();
        ResearchQueueChanged?.Invoke();
        ResearchStateChanged?.Invoke();
    }

    public bool CanUseBuilding(BuildingData _building)
    {
        if (_building == null)
            return false;

        if (string.IsNullOrWhiteSpace(_building.requiredResearchId))
            return true;

        ResearchData research = GetResearchById(_building.requiredResearchId);
        if (research != null && research.isPatentResearch)
            return PatentResearchManager.Instance.HasPatentAccess(research.id, ownerFaction);

        return HasCompletedResearch(_building.requiredResearchId);
    }

    public void ProcessResearchMonth(int _rpIncome)
    {
        EnsureResearchState();

        int remainingMonthRP = Mathf.Max(0, _rpIncome);
        bool startedResearch = false;
        bool processedResearch = false;
        bool completedResearch = false;
        bool accumulatedUnassignedResearch = false;

        while (true)
        {
            ResearchData currentResearch = GetCurrentResearch();
            if (currentResearch == null)
            {
                bool startedNextResearch = TryStartNextQueuedResearch();
                if (!startedNextResearch)
                    break;

                startedResearch = true;
                currentResearch = GetCurrentResearch();
                if (currentResearch == null)
                    break;
            }

            if (IsPatentResearchClaimedByOtherFaction(currentResearch))
            {
                RefundAndCancelBlockedPatentResearch(currentResearch);
                completedResearch = true;
                continue;
            }

            int currentProgress = GetResearchCurrentRP(currentResearch.id);
            int maxProgress = Mathf.Max(0, currentResearch.costRP);
            int remainingRequiredRP = Mathf.Max(0, maxProgress - currentProgress);

            if (remainingMonthRP > 0 && remainingRequiredRP > 0)
            {
                int appliedRP = Mathf.Min(remainingMonthRP, remainingRequiredRP);
                currentProgress = Mathf.Min(maxProgress, currentProgress + appliedRP);
                remainingMonthRP -= appliedRP;
                SetResearchCurrentRP(currentResearch.id, currentProgress);
                processedResearch = true;
            }

            UpdateCurrentResearchProgressCache();

            if (GetResearchCurrentRP(currentResearch.id) >= maxProgress)
            {
                bool canContinueWithRemainingMonthRP = remainingMonthRP > 0;
                completedResearch = CompleteCurrentResearch(currentResearch) || completedResearch;

                if (canContinueWithRemainingMonthRP)
                    continue;
            }

            break;
        }

        if (remainingMonthRP > 0)
        {
            researchState.unassignedResearchPoint = Mathf.Max(0, researchState.unassignedResearchPoint + remainingMonthRP);
            accumulatedUnassignedResearch = true;
        }

        UpdateCurrentResearchProgressCache();

        if (startedResearch || processedResearch || completedResearch || accumulatedUnassignedResearch)
            ResearchStateChanged?.Invoke();
    }

    public FactionResearchState GetResearchState()
    {
        EnsureResearchState();
        return researchState;
    }

    public int GetRPStock()
    {
        return researchState != null ? researchState.rpStock : 0;
    }

    public ResearchStateSaveData ExportResearchState()
    {
        EnsureResearchState();

        return new ResearchStateSaveData
        {
            currentResearchId = researchState.currentResearchId,
            currentResearchProgressRP = string.IsNullOrWhiteSpace(researchState.currentResearchId)
                ? 0
                : GetResearchCurrentRP(researchState.currentResearchId),
            unassignedResearchPoint = Mathf.Max(0, researchState.unassignedResearchPoint),
            queuedResearchIds = new List<string>(researchState.queuedResearchIds),
            completedResearchIds = new List<string>(researchState.completedResearchIds),
            unlockedBuildingIds = new List<string>(researchState.unlockedBuildingIds),
            modifiers = CopyResearchModifiers(researchState.modifiers)
        };
    }

    public void ImportResearchState(ResearchStateSaveData _data)
    {
        researchState = new FactionResearchState();
        EnsureResearchState();

        if (_data == null)
        {
            EnsureDefaultCompletedResearch();
            UpdateCurrentResearchProgressCache();
            ResearchQueueChanged?.Invoke();
            ResearchStateChanged?.Invoke();
            return;
        }

        ResearchDatabaseSO db = GetResearchDatabase();
        HashSet<string> completedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (_data.completedResearchIds != null)
        {
            for (int i = 0; i < _data.completedResearchIds.Count; i++)
            {
                string researchId = _data.completedResearchIds[i];
                researchId = NormalizeResearchId(researchId);
                if (string.IsNullOrWhiteSpace(researchId))
                    continue;

                ResearchData research = db != null ? db.GetResearchById(researchId) : null;
                if (research == null)
                {
                    Debug.LogWarning($"[FactionManager] Research '{researchId}' not found in completed list import.");
                    continue;
                }

                if (!PatentResearchManager.Instance.TryClaimPatent(research, ownerFaction))
                {
                    Debug.LogWarning($"[FactionManager] Patent research '{researchId}' is already claimed by another faction.");
                    continue;
                }

                if (!completedIds.Add(researchId))
                    continue;

                researchState.completedResearchIds.Add(researchId);
                SetResearchCurrentRP(researchId, research.costRP);
            }
        }

        if (_data.unlockedBuildingIds != null)
        {
            for (int i = 0; i < _data.unlockedBuildingIds.Count; i++)
            {
                string buildingId = _data.unlockedBuildingIds[i];
                if (!string.IsNullOrWhiteSpace(buildingId) && !researchState.unlockedBuildingIds.Contains(buildingId))
                    researchState.unlockedBuildingIds.Add(buildingId);
            }
        }

        researchState.modifiers = CopyResearchModifiers(_data.modifiers);
        researchState.unassignedResearchPoint = Mathf.Max(0, _data.unassignedResearchPoint);
        EnsureDefaultCompletedResearch();

        if (!string.IsNullOrWhiteSpace(_data.currentResearchId))
        {
            string currentResearchId = NormalizeResearchId(_data.currentResearchId);
            ResearchData currentResearch = db != null ? db.GetResearchById(currentResearchId) : null;
            if (currentResearch == null)
            {
                Debug.LogWarning($"[FactionManager] Research '{_data.currentResearchId}' not found during current research import.");
            }
            else if (HasCompletedResearch(currentResearchId))
            {
                Debug.LogWarning($"[FactionManager] Research '{_data.currentResearchId}' is already completed and cannot be imported as current.");
            }
            else if (IsPatentResearchClaimedByOtherFaction(currentResearch))
            {
                Debug.LogWarning($"[FactionManager] Research '{_data.currentResearchId}' is already claimed by another faction and cannot be imported as current.");
                RefundBlockedPatentResearchProgress(currentResearch, _data.currentResearchProgressRP);
            }
            else
            {
                researchState.currentResearchId = NormalizeResearchId(currentResearch.id);
                SetResearchCurrentRP(currentResearch.id, _data.currentResearchProgressRP);
            }
        }

        HashSet<string> queuedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (_data.queuedResearchIds != null)
        {
            for (int i = 0; i < _data.queuedResearchIds.Count; i++)
            {
                string researchId = _data.queuedResearchIds[i];
                researchId = NormalizeResearchId(researchId);
                if (string.IsNullOrWhiteSpace(researchId))
                    continue;

                if (HasCompletedResearch(researchId)
                    || string.Equals(researchState.currentResearchId, researchId, StringComparison.OrdinalIgnoreCase)
                    || !queuedIds.Add(researchId))
                {
                    continue;
                }

                ResearchData research = db != null ? db.GetResearchById(researchId) : null;
                if (db != null && research == null)
                {
                    Debug.LogWarning($"[FactionManager] Research '{researchId}' not found in queue import.");
                    continue;
                }

                if (IsPatentResearchClaimedByOtherFaction(research))
                    continue;

                researchState.queuedResearchIds.Add(researchId);
            }
        }

        RemoveInvalidQueuedResearches();
        UpdateCurrentResearchProgressCache();
        ResearchQueueChanged?.Invoke();
        ResearchStateChanged?.Invoke();
    }

    public void ApplyExternalResearchEffect(ResearchData _research)
    {
        ApplyResearchEffect(_research);
        ResearchStateChanged?.Invoke();
    }

    public void RemoveExternalResearchEffect(ResearchData _research)
    {
        if (_research == null)
            return;

        EnsureResearchState();

        int amount = Mathf.Max(0, _research.effectValue);
        switch (_research.effectType)
        {
            case ResearchEffectType.ResearchEffect_AddBuildingSlot:
                researchState.modifiers.addBuildingSlot = Mathf.Max(0, researchState.modifiers.addBuildingSlot - amount);
                break;
            case ResearchEffectType.ResearchEffect_Battle_MaxHpUp:
                researchState.modifiers.ceoBattleMaxHpBonus = Mathf.Max(0, researchState.modifiers.ceoBattleMaxHpBonus - amount);
                break;
            case ResearchEffectType.ResearchEffect_BuildingPowerReduction:
                researchState.modifiers.buildingPowerReduction = Mathf.Max(0, researchState.modifiers.buildingPowerReduction - amount);
                break;
            case ResearchEffectType.ResearchEffect_MaxEnergyUp:
                researchState.modifiers.battleMaxEnergyBonus = Mathf.Max(0, researchState.modifiers.battleMaxEnergyBonus - amount);
                break;
            case ResearchEffectType.ResearchEffect_MaxEmployeeSlotUp:
                researchState.modifiers.maxEmployeeSlotBonus = Mathf.Max(0, researchState.modifiers.maxEmployeeSlotBonus - amount);
                break;
            case ResearchEffectType.ResearchEffect_CardPack_CardCountUp:
                researchState.modifiers.cardPackOpenCardCountBonus = Mathf.Max(0, researchState.modifiers.cardPackOpenCardCountBonus - amount);
                break;
            case ResearchEffectType.ResearchEffect_RpIncomeUp:
                researchState.modifiers.rpIncomeBonus = Mathf.Max(0, researchState.modifiers.rpIncomeBonus - amount);
                break;
        }

        ResearchStateChanged?.Invoke();
    }

    private void EnsureResearchState()
    {
        if (researchState == null)
            researchState = new FactionResearchState();

        researchState.EnsureInitialized();
    }

    private void EnsureDefaultCompletedResearch()
    {
        EnsureResearchState();

        string researchId = NormalizeResearchId(DefaultCompletedResearchId);
        if (HasCompletedResearch(researchId))
            return;

        ResearchDatabaseSO db = GetResearchDatabase();
        ResearchData research = db != null ? db.GetResearchById(researchId) : null;
        if (research == null)
        {
            Debug.LogWarning($"[FactionResearchScript] Default completed research not found: {DefaultCompletedResearchId}");
            return;
        }

        researchState.completedResearchIds.Add(NormalizeResearchId(research.id));
        SetResearchCurrentRP(research.id, Mathf.Max(0, research.costRP));
        ApplyResearchEffect(research);
    }

    private ResearchDatabaseSO GetResearchDatabase()
    {
        if (researchDatabase == null)
        {
            researchDatabase = Resources.Load<ResearchDatabaseSO>("Databases/ResearchDatabase");
            if (researchDatabase == null)
            {
                researchDatabase = ScriptableObject.CreateInstance<ResearchDatabaseSO>();
                researchDatabase.LoadCSV();
                Debug.LogWarning("[FactionManager] Research DB asset not found in Resources. Fallback instance created.");
            }
        }

        return researchDatabase;
    }

    private bool TryStartNextQueuedResearch()
    {
        EnsureResearchState();
        bool queueChanged = RemoveInvalidQueuedResearches();

        if (!string.IsNullOrWhiteSpace(researchState.currentResearchId))
        {
            if (queueChanged)
                ResearchQueueChanged?.Invoke();

            return false;
        }

        while (researchState.queuedResearchIds.Count > 0)
        {
            string nextResearchId = NormalizeResearchId(researchState.queuedResearchIds[0]);
            researchState.queuedResearchIds.RemoveAt(0);
            queueChanged = true;

            if (!CanStartResearchNow(nextResearchId))
                continue;

            if (!TryStartResearch(nextResearchId))
                continue;

            ResearchQueueChanged?.Invoke();
            return true;
        }

        if (queueChanged)
            ResearchQueueChanged?.Invoke();

        return false;
    }

    private bool TryStartResearch(string _researchId)
    {
        string researchId = NormalizeResearchId(_researchId);
        if (string.IsNullOrWhiteSpace(researchId))
            return false;

        if (!string.IsNullOrWhiteSpace(researchState.currentResearchId))
            return false;

        ResearchData research = GetResearchDatabase().GetResearchById(researchId);
        if (research == null || IsPatentResearchClaimedByOtherFaction(research))
            return false;

        if (!CanStartResearchNow(researchId))
            return false;

        researchState.currentResearchId = researchId;

        if (research != null && researchState.unassignedResearchPoint > 0)
        {
            int currentProgress = GetResearchCurrentRP(researchId);
            int maxProgress = Mathf.Max(0, research.costRP);
            int remainingRequiredRP = Mathf.Max(0, maxProgress - currentProgress);
            int appliedRP = Mathf.Min(Mathf.Max(0, researchState.unassignedResearchPoint), remainingRequiredRP);
            int targetProgress = Mathf.Clamp(currentProgress + appliedRP, 0, maxProgress);

            SetResearchCurrentRP(researchId, targetProgress);
            researchState.unassignedResearchPoint = Mathf.Max(0, researchState.unassignedResearchPoint - appliedRP);
        }

        UpdateCurrentResearchProgressCache();
        ResearchStarted?.Invoke(researchId);

        int requiredRP = Mathf.Max(0, research.costRP);
        if (GetResearchCurrentRP(researchId) >= requiredRP)
            CompleteCurrentResearch(research);

        return true;
    }

    private bool CancelCurrentResearch(string _researchId)
    {
        string researchId = NormalizeResearchId(_researchId);
        if (string.IsNullOrWhiteSpace(researchId))
            return false;

        if (!string.Equals(NormalizeResearchId(researchState.currentResearchId), researchId, StringComparison.OrdinalIgnoreCase))
            return false;

        researchState.currentResearchId = null;
        researchState.currentResearchProgressRP = 0;
        researchState.rpStock = 0;

        bool queueChanged = RemoveInvalidQueuedResearches();
        bool startedNextResearch = TryStartNextQueuedResearch();
        ResearchStateChanged?.Invoke();

        if (queueChanged && !startedNextResearch)
            ResearchQueueChanged?.Invoke();

        return true;
    }

    private bool CompleteCurrentResearch(ResearchData _currentResearch)
    {
        if (_currentResearch == null || string.IsNullOrWhiteSpace(_currentResearch.id))
            return false;

        string completedResearchId = NormalizeResearchId(_currentResearch.id);
        bool newlyCompleted = !HasCompletedResearch(completedResearchId);
        if (newlyCompleted)
        {
            if (!PatentResearchManager.Instance.TryClaimPatent(_currentResearch, ownerFaction))
            {
                Debug.LogWarning($"[FactionManager] Patent research '{completedResearchId}' completion blocked because it is already claimed.");
                RefundAndCancelBlockedPatentResearch(_currentResearch);
                return true;
            }

            researchState.completedResearchIds.Add(completedResearchId);
            ApplyResearchEffect(_currentResearch);
        }

        SetResearchCurrentRP(completedResearchId, Mathf.Max(GetResearchCurrentRP(completedResearchId), _currentResearch.costRP));

        researchState.currentResearchId = null;
        researchState.currentResearchProgressRP = 0;
        researchState.rpStock = 0;

        ResearchCompleted?.Invoke(completedResearchId);
        TryStartNextQueuedResearch();
        return true;
    }

    private bool CancelQueuedResearch(string _researchId)
    {
        if (string.IsNullOrWhiteSpace(_researchId) || researchState == null || researchState.queuedResearchIds == null)
            return false;

        string researchId = NormalizeResearchId(_researchId);

        for (int i = 0; i < researchState.queuedResearchIds.Count; i++)
        {
            string queuedResearchId = NormalizeResearchId(researchState.queuedResearchIds[i]);
            if (!string.Equals(queuedResearchId, researchId, StringComparison.OrdinalIgnoreCase))
                continue;

            researchState.queuedResearchIds.RemoveAt(i);
            RemoveInvalidQueuedResearches();
            ResearchQueueChanged?.Invoke();
            ResearchStateChanged?.Invoke();
            return true;
        }

        return false;
    }

    private void ApplyResearchEffect(ResearchData _research)
    {
        if (_research == null)
            return;

        EnsureResearchState();

        switch (_research.effectType)
        {
            case ResearchEffectType.ResearchEffect_AddBuildingSlot:
                researchState.modifiers.addBuildingSlot += _research.effectValue;
                break;
            case ResearchEffectType.ResearchEffect_Battle_MaxHpUp:
                researchState.modifiers.ceoBattleMaxHpBonus += Mathf.Max(0, _research.effectValue);
                break;
            case ResearchEffectType.ResearchEffect_BuildingPowerReduction:
                researchState.modifiers.buildingPowerReduction += Mathf.Max(0, _research.effectValue);
                break;
            case ResearchEffectType.ResearchEffect_MaxEnergyUp:
                researchState.modifiers.battleMaxEnergyBonus += Mathf.Max(0, _research.effectValue);
                break;
            case ResearchEffectType.ResearchEffect_MaxEmployeeSlotUp:
                researchState.modifiers.maxEmployeeSlotBonus += Mathf.Max(0, _research.effectValue);
                break;
            case ResearchEffectType.ResearchEffect_CardPack_CardCountUp:
                researchState.modifiers.cardPackOpenCardCountBonus += Mathf.Max(0, _research.effectValue);
                break;
            case ResearchEffectType.ResearchEffect_RpIncomeUp:
                researchState.modifiers.rpIncomeBonus += Mathf.Max(0, _research.effectValue);
                break;
        }
    }

    private bool RemoveInvalidQueuedResearches()
    {
        if (researchState.queuedResearchIds == null || researchState.queuedResearchIds.Count == 0)
            return false;

        HashSet<string> seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        ResearchDatabaseSO db = GetResearchDatabase();
        bool queueChanged = false;

        for (int i = 0; i < researchState.queuedResearchIds.Count; i++)
        {
            string researchId = NormalizeResearchId(researchState.queuedResearchIds[i]);
            ResearchData research = !string.IsNullOrWhiteSpace(researchId) && db != null
                ? db.GetResearchById(researchId)
                : null;

            if (string.IsNullOrWhiteSpace(researchId)
                || research == null
                || IsResearchCompleted(researchId)
                || IsResearchInProgress(researchId)
                || IsPatentResearchClaimedByOtherFaction(research)
                || !seenIds.Add(researchId)
                || !ArePrerequisitesCompleted(research))
            {
                researchState.queuedResearchIds.RemoveAt(i);
                queueChanged = true;
                i--;
                continue;
            }

            if (!string.Equals(researchState.queuedResearchIds[i], researchId, StringComparison.Ordinal))
            {
                researchState.queuedResearchIds[i] = researchId;
                queueChanged = true;
            }
        }

        return queueChanged;
    }

    private bool ArePrerequisitesCompleted(ResearchData _research)
    {
        if (_research == null)
            return false;

        if (_research.prerequisiteResearchIds == null)
            return true;

        if (_research.prerequisiteResearchIds.Count == 0)
            return true;

        for (int i = 0; i < _research.prerequisiteResearchIds.Count; i++)
        {
            string prerequisiteId = NormalizeResearchId(_research.prerequisiteResearchIds[i]);
            if (string.IsNullOrWhiteSpace(prerequisiteId))
                continue;

            if (!IsResearchCompleted(prerequisiteId))
                return false;
        }

        return true;
    }

    private bool AreUnlockCategoryRequirementsCompleted(ResearchData _research, ResearchDatabaseSO _db)
    {
        if (_research == null)
            return false;

        if (_research.unlockCategoryRequirements == null || _research.unlockCategoryRequirements.Count == 0)
            return true;

        for (int i = 0; i < _research.unlockCategoryRequirements.Count; i++)
        {
            ResearchCategoryRequirement requirement = _research.unlockCategoryRequirements[i];
            if (requirement == null || string.IsNullOrWhiteSpace(requirement.category) || requirement.count <= 0)
                continue;

            int completedCount = CountCompletedResearchesByCategory(requirement.category, _db);
            if (completedCount < requirement.count)
                return false;
        }

        return true;
    }

    private int CountCompletedResearchesByCategory(string _category, ResearchDatabaseSO _db)
    {
        if (string.IsNullOrWhiteSpace(_category) || _db == null)
            return 0;

        if (researchState == null || researchState.completedResearchIds == null)
            return 0;

        int count = 0;

        for (int i = 0; i < researchState.completedResearchIds.Count; i++)
        {
            string researchId = NormalizeResearchId(researchState.completedResearchIds[i]);
            if (string.IsNullOrWhiteSpace(researchId))
                continue;

            ResearchData completedResearch = _db.GetResearchById(researchId);
            if (completedResearch == null || string.IsNullOrWhiteSpace(completedResearch.category))
                continue;

            if (string.Equals(completedResearch.category.Trim(), _category.Trim(), StringComparison.OrdinalIgnoreCase))
                count++;
        }

        return count;
    }

    private bool BuildResearchQueuePath(
        string _researchId,
        ResearchDatabaseSO _db,
        List<string> _result,
        HashSet<string> _visitedIds,
        HashSet<string> _recursionStack)
    {
        string researchId = NormalizeResearchId(_researchId);
        if (string.IsNullOrWhiteSpace(researchId))
            return false;

        if (_visitedIds.Contains(researchId))
            return true;

        if (!_recursionStack.Add(researchId))
        {
            Debug.LogWarning($"[FactionManager] Research prerequisite cycle detected at '{researchId}'.");
            return false;
        }

        ResearchData research = _db.GetResearchById(researchId);
        if (research == null)
        {
            Debug.LogWarning($"[FactionManager] Research '{researchId}' not found.");
            _recursionStack.Remove(researchId);
            return false;
        }

        if (IsPatentResearchClaimedByOtherFaction(research))
        {
            _recursionStack.Remove(researchId);
            return false;
        }

        research.EnsureInitialized();

        for (int i = 0; i < research.prerequisiteResearchIds.Count; i++)
        {
            string prerequisiteId = NormalizeResearchId(research.prerequisiteResearchIds[i]);
            if (!BuildResearchQueuePath(prerequisiteId, _db, _result, _visitedIds, _recursionStack))
            {
                _recursionStack.Remove(researchId);
                return false;
            }
        }

        _recursionStack.Remove(researchId);
        _visitedIds.Add(researchId);

        if (!IsResearchCompleted(researchId) && !ContainsResearchId(_result, researchId))
            _result.Add(researchId);

        return true;
    }

    private bool ContainsResearchId(List<string> _researchIds, string _researchId)
    {
        if (_researchIds == null || string.IsNullOrWhiteSpace(_researchId))
            return false;

        string researchId = NormalizeResearchId(_researchId);

        for (int i = 0; i < _researchIds.Count; i++)
        {
            string listedResearchId = NormalizeResearchId(_researchIds[i]);
            if (string.Equals(listedResearchId, researchId, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private void RefundBlockedPatentResearchProgress(ResearchData research, int refundCredit)
    {
        if (research == null || refundCredit <= 0 || ownerFaction == null)
            return;

        ownerFaction.ChangeCredit(refundCredit);
        Debug.Log($"[FactionManager] Patent research blocked. Refunded RP as credit. faction={ownerFaction.factionName}, research={research.id}, credit={refundCredit}");
    }

    private void RefundAndCancelBlockedPatentResearch(ResearchData research)
    {
        if (research == null || string.IsNullOrWhiteSpace(research.id))
            return;

        string researchId = NormalizeResearchId(research.id);
        int refundCredit = GetResearchCurrentRP(researchId);
        RefundBlockedPatentResearchProgress(research, refundCredit);

        RemoveResearchProgress(researchId);
        researchState.currentResearchId = null;
        researchState.currentResearchProgressRP = 0;
        researchState.rpStock = 0;
        RemoveInvalidQueuedResearches();
        ResearchQueueChanged?.Invoke();
    }

    private bool IsPatentResearchClaimedByOtherFaction(string _researchId)
    {
        ResearchData research = GetResearchById(_researchId);
        return IsPatentResearchClaimedByOtherFaction(research);
    }

    private bool IsPatentResearchClaimedByOtherFaction(ResearchData research)
    {
        return research != null
            && research.isPatentResearch
            && PatentResearchManager.Instance.IsPatentClaimedByOtherFaction(research.id, ownerFaction);
    }

    private int GetResearchCurrentRP(string _researchId)
    {
        if (string.IsNullOrWhiteSpace(_researchId) || researchState == null || researchState.researchProgressEntries == null)
            return 0;

        string researchId = NormalizeResearchId(_researchId);

        for (int i = 0; i < researchState.researchProgressEntries.Count; i++)
        {
            ResearchProgressEntry entry = researchState.researchProgressEntries[i];
            if (entry == null || !string.Equals(NormalizeResearchId(entry.researchId), researchId, StringComparison.OrdinalIgnoreCase))
                continue;

            return Mathf.Max(0, entry.currentRP);
        }

        return 0;
    }

    private void SetResearchCurrentRP(string _researchId, int _currentRP)
    {
        if (string.IsNullOrWhiteSpace(_researchId) || researchState == null)
            return;

        researchState.EnsureInitialized();
        string researchId = NormalizeResearchId(_researchId);
        int clampedValue = Mathf.Max(0, _currentRP);

        for (int i = 0; i < researchState.researchProgressEntries.Count; i++)
        {
            ResearchProgressEntry entry = researchState.researchProgressEntries[i];
            if (entry == null || !string.Equals(NormalizeResearchId(entry.researchId), researchId, StringComparison.OrdinalIgnoreCase))
                continue;

            entry.researchId = researchId;
            entry.currentRP = clampedValue;
            return;
        }

        researchState.researchProgressEntries.Add(new ResearchProgressEntry
        {
            researchId = researchId,
            currentRP = clampedValue
        });
    }

    private void RemoveResearchProgress(string _researchId)
    {
        if (string.IsNullOrWhiteSpace(_researchId) || researchState == null || researchState.researchProgressEntries == null)
            return;

        string researchId = NormalizeResearchId(_researchId);
        for (int i = researchState.researchProgressEntries.Count - 1; i >= 0; i--)
        {
            ResearchProgressEntry entry = researchState.researchProgressEntries[i];
            if (entry == null || string.Equals(NormalizeResearchId(entry.researchId), researchId, StringComparison.OrdinalIgnoreCase))
                researchState.researchProgressEntries.RemoveAt(i);
        }
    }

    private string NormalizeResearchId(string _researchId)
    {
        return string.IsNullOrWhiteSpace(_researchId) ? string.Empty : _researchId.Trim();
    }

    private void UpdateCurrentResearchProgressCache()
    {
        if (researchState == null || string.IsNullOrWhiteSpace(researchState.currentResearchId))
        {
            if (researchState != null)
            {
                researchState.currentResearchProgressRP = 0;
                researchState.rpStock = 0;
            }

            return;
        }

        int currentProgress = GetResearchCurrentRP(researchState.currentResearchId);
        researchState.currentResearchProgressRP = currentProgress;
        researchState.rpStock = currentProgress;
    }

    private ResearchModifierSet CopyResearchModifiers(ResearchModifierSet _source)
    {
        return new ResearchModifierSet
        {
            creditIncomePercentBonus = _source != null ? _source.creditIncomePercentBonus : 0,
            researchOutputPercentBonus = _source != null ? _source.researchOutputPercentBonus : 0,
            powerOutputPercentBonus = _source != null ? _source.powerOutputPercentBonus : 0,
            buildingPowerReduction = _source != null ? _source.buildingPowerReduction : 0,
            factoryOutputPercentBonus = _source != null ? _source.factoryOutputPercentBonus : 0,
            addBuildingSlot = _source != null ? _source.addBuildingSlot : 0,
            ceoBattleMaxHpBonus = _source != null ? _source.ceoBattleMaxHpBonus : 0,
            battleMaxEnergyBonus = _source != null ? _source.battleMaxEnergyBonus : 0,
            maxEmployeeSlotBonus = _source != null ? _source.maxEmployeeSlotBonus : 0,
            cardPackOpenCardCountBonus = _source != null ? _source.cardPackOpenCardCountBonus : 0,
            rpIncomeBonus = _source != null ? _source.rpIncomeBonus : 0
        };
    }
}
