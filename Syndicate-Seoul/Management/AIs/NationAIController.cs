using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class NationAIController : MonoBehaviour
{
    private const int MaxAlternativeActionRetryCount = 3;
    private const int MinBaseActionDay = 1;
    private const int MaxBaseActionDay = 15;
    private const int SecondActionDayOffset = 10;

    [Header("References")]
    [SerializeField] private FactionManager factionManager;
    [SerializeField] private CalendarScript calendar;
    [SerializeField] private WarManager warManager;
    [SerializeField] private AIDebugLogger aiDebugLogger;
    [SerializeField] private NationAIActionScheduler aiActionScheduler;
    [SerializeField] private CardPackDatabaseSO cardPackDatabase;

    [Header("AI Personality")]
    [SerializeField] private BigFivePersonality personality = new BigFivePersonality();

    [Header("AI Schedule")]
    [FormerlySerializedAs("runOnMonthChanged")]
    [SerializeField] private bool runOnScheduledDays = true;
    [SerializeField] private int baseActionDay;

    private NationAIDecisionMaker decisionMaker;
    private NationAIExecutor executor;
    private FactionAIResearchPlanner researchPlanner;
    private FactionAIEmployeePlanner employeePlanner;
    private FactionAITargetCityMemory targetCityMemory;
    private CardIncinerationPowerService cardIncinerationPowerService;
    private bool hasLoggedCardPackDatabaseFallback;
    private bool hasAssignedBaseActionDay;
    private int lastActionYear = -1;
    private int lastActionMonth = -1;
    private int lastActionDay = -1;

    private void Awake()
    {
        EnsureRuntimeDependencies();
        EnsureActionSchedule();
    }

    public void SetPersonality(BigFivePersonality _personality)
    {
        if (_personality == null)
            return;

        personality = new BigFivePersonality
        {
            openness = Mathf.Clamp(_personality.openness, 0, 100),
            conscientiousness = Mathf.Clamp(_personality.conscientiousness, 0, 100),
            extraversion = Mathf.Clamp(_personality.extraversion, 0, 100),
            agreeableness = Mathf.Clamp(_personality.agreeableness, 0, 100),
            neuroticism = Mathf.Clamp(_personality.neuroticism, 0, 100)
        };
    }

    public FactionAITargetCityMemory GetTargetCityMemory()
    {
        EnsureRuntimeDependencies();
        return targetCityMemory;
    }

    public bool CanThinkAndAct()
    {
        if (factionManager == null)
            factionManager = GetComponent<FactionManager>();

        if (factionManager == null)
            return false;

        if (factionManager.IsEliminated)
            return false;

        if (factionManager.ownedCities == null || factionManager.ownedCities.Count == 0)
            return false;

        return true;
    }

    public FactionAISaveData ExportAISaveData()
    {
        EnsureRuntimeDependencies();
        EnsureActionSchedule();

        FactionAISaveData data = new FactionAISaveData
        {
            actionBaseDay = baseActionDay,
            lastWarMonth = executor != null ? executor.GetLastWarMonth(factionManager) : -1
        };

        if (targetCityMemory != null)
            targetCityMemory.ExportSaveData(data);

        return data;
    }

    public void ImportAISaveData(FactionAISaveData _data, IDictionary<string, CityScript> _citiesByName)
    {
        if (_data == null)
            return;

        EnsureRuntimeDependencies();

        if (_data.actionBaseDay >= MinBaseActionDay && _data.actionBaseDay <= MaxBaseActionDay)
        {
            baseActionDay = _data.actionBaseDay;
            hasAssignedBaseActionDay = true;
        }
        else
        {
            hasAssignedBaseActionDay = false;
            EnsureActionSchedule();
        }

        if (targetCityMemory != null)
            targetCityMemory.ImportSaveData(_data, _citiesByName);

        if (executor != null)
            executor.SetLastWarMonth(factionManager, _data.lastWarMonth);
    }

    private void EnsureRuntimeDependencies()
    {
        if (factionManager == null)
            factionManager = GetComponent<FactionManager>();

        if (calendar == null)
            calendar = FindFirstObjectByType<CalendarScript>();

        if (warManager == null)
            warManager = FindFirstObjectByType<WarManager>();

        if (aiDebugLogger == null)
            aiDebugLogger = GetComponent<AIDebugLogger>();

        if (aiActionScheduler == null)
            aiActionScheduler = FindFirstObjectByType<NationAIActionScheduler>();

        if (aiActionScheduler == null)
            aiActionScheduler = FindFirstObjectByType<NationAIActionScheduler>(FindObjectsInactive.Include);

        if (aiActionScheduler == null)
        {
            GameObject schedulerObject = new GameObject("NationAIActionScheduler");
            aiActionScheduler = schedulerObject.AddComponent<NationAIActionScheduler>();
        }

        if (researchPlanner == null)
        {
            researchPlanner = GetComponent<FactionAIResearchPlanner>();
            if (researchPlanner == null)
                researchPlanner = gameObject.AddComponent<FactionAIResearchPlanner>();
        }

        if (employeePlanner == null)
        {
            employeePlanner = GetComponent<FactionAIEmployeePlanner>();
            if (employeePlanner == null)
                employeePlanner = gameObject.AddComponent<FactionAIEmployeePlanner>();
        }

        if (targetCityMemory == null)
        {
            targetCityMemory = GetComponent<FactionAITargetCityMemory>();
            if (targetCityMemory == null)
                targetCityMemory = gameObject.AddComponent<FactionAITargetCityMemory>();
        }

        if (factionManager != null)
        {
            if (cardIncinerationPowerService == null)
            {
                cardIncinerationPowerService = GetComponent<CardIncinerationPowerService>();
                if (cardIncinerationPowerService == null)
                    cardIncinerationPowerService = gameObject.AddComponent<CardIncinerationPowerService>();
            }

            if (cardIncinerationPowerService != null)
            {
                if (!cardIncinerationPowerService.enabled)
                    cardIncinerationPowerService.enabled = true;

                cardIncinerationPowerService.SetFactionManager(factionManager);
            }
        }

        if (cardPackDatabase == null)
        {
            cardPackDatabase = Resources.Load<CardPackDatabaseSO>("Databases/CardPackDatabase");
            if (cardPackDatabase == null)
            {
                cardPackDatabase = ScriptableObject.CreateInstance<CardPackDatabaseSO>();
                cardPackDatabase.LoadCSV();

                if (!hasLoggedCardPackDatabaseFallback)
                {
                    LogAIWarning("[AI] CardPackDatabase asset not found in Resources. Fallback instance created.");
                    hasLoggedCardPackDatabaseFallback = true;
                }
            }
        }

        if (personality == null)
            personality = new BigFivePersonality();

        if (decisionMaker == null)
        {
            decisionMaker = new NationAIDecisionMaker(
                new FactionAIBuildingProvider(),
                new NationAISceneTargetProvider(),
                targetCityMemory);
        }

        if (executor == null)
            executor = new NationAIExecutor(aiDebugLogger);
    }

    private void OnEnable()
    {
        EnsureRuntimeDependencies();
        EnsureActionSchedule();

        if (calendar != null)
        {
            calendar.DayChanged -= OnDayChanged;
            calendar.DayChanged += OnDayChanged;
        }
    }

    private void OnDisable()
    {
        if (calendar != null)
            calendar.DayChanged -= OnDayChanged;
    }

    private void OnDayChanged(GameDate _date)
    {
        if (!runOnScheduledDays)
            return;

        if (TutorialManager.Instance != null && TutorialManager.Instance.IsActive)
            return;

        if (_date == null)
            return;

        EnsureActionSchedule();

        if (!IsScheduledActionDay(_date.day))
            return;

        if (!CanThinkAndAct())
            return;

        if (lastActionYear == _date.year
            && lastActionMonth == _date.month
            && lastActionDay == _date.day)
        {
            return;
        }

        lastActionYear = _date.year;
        lastActionMonth = _date.month;
        lastActionDay = _date.day;

        if (aiActionScheduler == null || !aiActionScheduler.isActiveAndEnabled)
        {
            ThinkAndAct();
            return;
        }

        aiActionScheduler.Enqueue(this);
    }

    [ContextMenu("AI Think And Act")]
    public void ThinkAndAct()
    {
        EnsureRuntimeDependencies();

        if (!CanThinkAndAct())
            return;

        if (factionManager == null)
        {
            LogAIWarning("[AI] factionManager가 없습니다.");
            return;
        }

        TryOpenCardPacksBeforeDecision();
        TrySelectResearchBeforeDecision();
        TryUseBuildingAbilityBeforeDecision();
        TryUseEmployeeBeforeDecision();

        HashSet<NationAIActionType> excludedActionTypes = new HashSet<NationAIActionType>();
        int retryCount = 0;

        while (true)
        {
            FactionAIContext context = FactionAIContextBuilder.Build(factionManager, GetCurrentMonthIndex());
            NationAIDecisionResult result = decisionMaker.Decide(personality, context, excludedActionTypes);

            LogAIDecision(result);

            if (result == null)
            {
                ExecuteSaveMoney("No candidate available.");
                return;
            }

            if (result.selectedCandidate == null)
            {
                LogAI($"[AI] Action candidate is null. action={result.selectedActionType}");

                if (result.selectedActionType == NationAIActionType.SaveMoney || retryCount >= MaxAlternativeActionRetryCount)
                {
                    ExecuteSaveMoney($"Fallback after null candidate. action={result.selectedActionType}");
                    return;
                }

                excludedActionTypes.Add(result.selectedActionType);
                retryCount++;
                LogAI(
                    $"[AI] Reselecting action after null candidate. excludedAction={result.selectedActionType}, " +
                    $"retry={retryCount}/{MaxAlternativeActionRetryCount}");
                continue;
            }

            NationAIExecuteResult executeResult = executor.Execute(result.selectedCandidate, warManager);
            LogAIExecuteResult(result.selectedCandidate, executeResult);

            if (executeResult.success)
                return;

            LogAI($"[AI] Action failed. action={result.selectedCandidate.actionType}, failReason={executeResult.failReason}, message={executeResult.message}");

            if (executeResult.failReason == NationAIExecuteFailReason.NotEnoughMoney)
            {
                ExecuteSaveMoney($"Fallback due to NotEnoughMoney after {result.selectedCandidate.actionType}.");
                return;
            }

            excludedActionTypes.Add(result.selectedCandidate.actionType);
            if (result.selectedCandidate.actionType != result.selectedActionType)
                excludedActionTypes.Add(result.selectedActionType);

            if (retryCount >= MaxAlternativeActionRetryCount)
            {
                ExecuteSaveMoney($"Fallback after retry limit. lastFailedAction={result.selectedCandidate.actionType}, failReason={executeResult.failReason}");
                return;
            }

            retryCount++;
            LogAI(
                $"[AI] Reselecting action. excludedAction={result.selectedCandidate.actionType}, " +
                $"failReason={executeResult.failReason}, retry={retryCount}/{MaxAlternativeActionRetryCount}");
        }
    }

    private void TryOpenCardPacksBeforeDecision()
    {
        if (cardPackDatabase == null)
        {
            LogAIWarning("[AI] cardPackDatabase is not assigned. Skipped auto-opening card packs.");
            return;
        }

        CardInventory cardInventory = factionManager.GetCardInventory();
        int cardCount = cardInventory != null ? cardInventory.Count : 0;
        bool shouldOpen = cardCount <= 25 || Random.value < 0.8f;

        if (!shouldOpen)
            return;

        CardPackOpenService.TryOpenAllPacks(
            factionManager,
            cardPackDatabase,
            out int openedCount,
            out int failedCount,
            CardPackOpenService.DefaultDrawCount);

        if (openedCount > 0 || failedCount > 0)
            LogAI($"[AI] Auto-opened card packs. opened={openedCount}, failed={failedCount}");
    }

    private void TrySelectResearchBeforeDecision()
    {
        if (factionManager.GetCurrentResearch() != null)
            return;

        if (researchPlanner == null)
            return;

        ResearchData selectedResearch = researchPlanner.PickScoredAvailableResearch(factionManager, personality);
        if (selectedResearch == null)
            return;

        if (factionManager.StartResearchDirectly(selectedResearch.id))
        {
            LogAI(
                $"[AI] Selected research directly. researchId={selectedResearch.id}, " +
                $"name={selectedResearch.name}, {researchPlanner.LastSelectionSummary}");
            return;
        }

        LogAIWarning($"[AI] Failed to select research directly. researchId={selectedResearch.id}");
    }

    private void TryUseBuildingAbilityBeforeDecision()
    {
        if (factionManager == null || factionManager.IsPlayerFaction)
            return;

        if (cardIncinerationPowerService == null)
            return;

        if (!cardIncinerationPowerService.TryAutoIncinerateAvailableCard())
            return;
    }

    private void TryUseEmployeeBeforeDecision()
    {
        if (factionManager == null || factionManager.IsPlayerFaction)
            return;

        if (employeePlanner == null)
            return;

        employeePlanner.TryRun(factionManager, personality, targetCityMemory, GetCurrentMonthIndex());
    }

    private int GetCurrentMonthIndex()
    {
        if (calendar == null || calendar.CurrentDate == null)
            return -1;

        return calendar.CurrentDate.year * 12 + calendar.CurrentDate.month;
    }

    private void EnsureActionSchedule()
    {
        if (hasAssignedBaseActionDay)
            return;

        if (baseActionDay < MinBaseActionDay || baseActionDay > MaxBaseActionDay)
            baseActionDay = Random.Range(MinBaseActionDay, MaxBaseActionDay + 1);
        else
            baseActionDay = Mathf.Clamp(baseActionDay, MinBaseActionDay, MaxBaseActionDay);

        hasAssignedBaseActionDay = true;
    }

    private bool IsScheduledActionDay(int day)
    {
        return day == baseActionDay || day == baseActionDay + SecondActionDayOffset;
    }

    private void LogAI(string _message)
    {
        if (aiDebugLogger != null)
            aiDebugLogger.Log(factionManager, _message);
        else
            AIDebugLogger.LogAI(factionManager, _message);
    }

    private void LogAIWarning(string _message)
    {
        if (aiDebugLogger != null)
            aiDebugLogger.LogWarning(factionManager, _message);
        else
            AIDebugLogger.LogAIWarning(factionManager, _message);
    }

    private void LogAIDecision(NationAIDecisionResult _result)
    {
        if (aiDebugLogger != null)
            aiDebugLogger.LogDecision(factionManager, _result);
        else
            AIDebugLogger.LogAIDecision(factionManager, _result);
    }

    private void LogAIExecuteResult(NationAIActionCandidate _candidate, NationAIExecuteResult _result)
    {
        if (aiDebugLogger != null)
            aiDebugLogger.LogExecuteResult(factionManager, _candidate, _result);
        else
            AIDebugLogger.LogAIExecuteResult(factionManager, _candidate, _result);
    }

    private void ExecuteSaveMoney(string reason)
    {
        LogAI($"[AI] SaveMoney fallback triggered. reason={reason}");

        NationAIActionCandidate saveMoneyCandidate = new NationAIActionCandidate
        {
            actionType = NationAIActionType.SaveMoney,
            attackerFaction = factionManager,
            reason = reason
        };

        NationAIExecuteResult executeResult = executor.Execute(saveMoneyCandidate, warManager);
        LogAIExecuteResult(saveMoneyCandidate, executeResult);
    }
}
