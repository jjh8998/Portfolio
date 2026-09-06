using UnityEngine;

public class AIDebugLogger : MonoBehaviour
{
    private static AIDebugLogger instance;

    [SerializeField] private bool enableAIDebugLog = false;
    [SerializeField] private bool debugSelectedAIOnly = false;
    [SerializeField] private FactionManager debugTargetFaction;
    [SerializeField] private bool enableMergerAcquisitionDebugLog = true;

    private bool hasLoggedMissingDebugTargetWarning;

    private void Awake()
    {
        instance = this;
    }

    private void OnEnable()
    {
        if (instance == null)
            instance = this;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    public static void LogAI(string _message)
    {
        if (instance == null)
            return;

        instance.Log(_message);
    }

    public static void LogAI(FactionManager _factionManager, string _message)
    {
        if (instance == null)
            return;

        instance.Log(_factionManager, _message);
    }

    public static void LogAIWarning(string _message)
    {
        if (instance == null)
            return;

        instance.LogWarning(_message);
    }

    public static void LogAIWarning(FactionManager _factionManager, string _message)
    {
        if (instance == null)
            return;

        instance.LogWarning(_factionManager, _message);
    }

    public static void LogAIDecision(FactionManager _factionManager, NationAIDecisionResult _result)
    {
        if (instance == null)
            return;

        instance.LogDecision(_factionManager, _result);
    }

    public static void LogAIExecuteResult(FactionManager _factionManager, NationAIActionCandidate _candidate, bool _success)
    {
        if (instance == null)
            return;

        instance.LogExecuteResult(_factionManager, _candidate, _success);
    }

    public static void LogAIExecuteResult(FactionManager _factionManager, NationAIActionCandidate _candidate, NationAIExecuteResult _result)
    {
        if (instance == null)
            return;

        instance.LogExecuteResult(_factionManager, _candidate, _result);
    }

    public static void LogMergerAcquisition(string _message)
    {
        if (instance == null)
            return;

        instance.LogMergerAcquisitionInternal(_message);
    }

    public static void LogMergerAcquisition(FactionManager _primaryFaction, FactionManager _relatedFaction, string _message)
    {
        if (instance == null)
            return;

        instance.LogMergerAcquisitionInternal(_primaryFaction, _relatedFaction, _message);
    }

    public void Log(string _message)
    {
        if (!ShouldShowDebugLog(null) || string.IsNullOrWhiteSpace(_message))
            return;

        Debug.Log(_message);
    }

    public void Log(FactionManager _factionManager, string _message)
    {
        if (!ShouldShowDebugLog(_factionManager) || string.IsNullOrWhiteSpace(_message))
            return;

        Debug.Log(_message);
    }

    public void LogWarning(string _message)
    {
        if (!ShouldShowDebugLog(null) || string.IsNullOrWhiteSpace(_message))
            return;

        Debug.LogWarning(_message);
    }

    public void LogWarning(FactionManager _factionManager, string _message)
    {
        if (!ShouldShowDebugLog(_factionManager) || string.IsNullOrWhiteSpace(_message))
            return;

        Debug.LogWarning(_message);
    }

    public void LogDecision(FactionManager _factionManager, NationAIDecisionResult _result)
    {
        if (_result == null || !ShouldShowDebugLog(_factionManager))
            return;

        string factionName = GetFactionName(_factionManager);
        Log(_factionManager, $"[AI][{factionName}] 1차 선택 : {_result.selectedActionType}");

        if (_result.actionScores != null)
        {
            for (int i = 0; i < _result.actionScores.Count; i++)
            {
                NationAIActionScore score = _result.actionScores[i];
                if (score == null)
                    continue;

                string unavailableText = string.IsNullOrEmpty(score.unavailableReason)
                    ? string.Empty
                    : $", UnavailableReason = {score.unavailableReason}";

                Log(
                    _factionManager,
                    $"[AI][{factionName}] Action = {score.actionType}, " +
                    $"Available = {score.isAvailable}, Score = {score.score:F2}, Reason = {score.reason}" +
                    $"{unavailableText}, " +
                    $"Context = {score.scoreParts.contextScore:F2}, " +
                    $"Personal = {score.scoreParts.personalScore:F2}, " +
                    $"Noise = {score.scoreParts.noiseScore:F2}");
            }
        }

        LogSelectedCandidate(_factionManager, _result.selectedCandidate);
    }

    public void LogExecuteResult(FactionManager _factionManager, NationAIActionCandidate _candidate, bool _success)
    {
        if (_candidate == null || !ShouldShowDebugLog(_factionManager))
            return;

        string factionName = GetFactionName(_factionManager);
        Log(
            _factionManager,
            $"[AI][{factionName}] Execute Result : " +
            $"{_candidate.actionType} / Success = {_success} / {_candidate.reason}");
    }

    public void LogExecuteResult(FactionManager _factionManager, NationAIActionCandidate _candidate, NationAIExecuteResult _result)
    {
        if (_candidate == null || _result == null || !ShouldShowDebugLog(_factionManager))
            return;

        string factionName = GetFactionName(_factionManager);
        string failReason = _result.success ? NationAIExecuteFailReason.None.ToString() : _result.failReason.ToString();

        Log(
            _factionManager,
            $"[AI][{factionName}] Execute Result : " +
            $"{_candidate.actionType} / Success = {_result.success} / FailReason = {failReason} / " +
            $"{_candidate.reason} / {_result.message}");
    }

    private void LogMergerAcquisitionInternal(string _message)
    {
        if (!ShouldShowDebugLog(null) || !enableMergerAcquisitionDebugLog || string.IsNullOrWhiteSpace(_message))
            return;

        Debug.Log(_message);
    }

    private void LogMergerAcquisitionInternal(FactionManager _primaryFaction, FactionManager _relatedFaction, string _message)
    {
        if (!ShouldShowDebugLog(_primaryFaction, _relatedFaction) || !enableMergerAcquisitionDebugLog || string.IsNullOrWhiteSpace(_message))
            return;

        Debug.Log(_message);
    }

    private void LogSelectedCandidate(FactionManager _factionManager, NationAIActionCandidate _candidate)
    {
        if (_candidate == null || !ShouldShowDebugLog(_factionManager))
            return;

        string factionName = GetFactionName(_factionManager);
        string cityName = GetCityName(_candidate.city);
        string targetName = GetCityName(_candidate.targetCity);
        string buildingName = _candidate.building != null ? _candidate.building.name : "None";

        Log(
            _factionManager,
            $"[AI][{factionName}] 2차 선택 : " +
            $"Action = {_candidate.actionType}, " +
            $"City = {cityName}, Slot = {_candidate.slotIndex}, " +
            $"Building = {buildingName}, Target = {targetName}, " +
            $"Score = {_candidate.score:F2}, Reason = {_candidate.reason}");
    }

    private bool ShouldShowDebugLog(FactionManager faction)
    {
        if (!enableAIDebugLog)
            return false;

        if (!debugSelectedAIOnly)
            return true;

        if (debugTargetFaction == null)
        {
            LogMissingDebugTargetWarning();
            return false;
        }

        return ReferenceEquals(faction, debugTargetFaction);
    }

    private bool ShouldShowDebugLog(FactionManager primaryFaction, FactionManager relatedFaction)
    {
        if (!enableAIDebugLog)
            return false;

        if (!debugSelectedAIOnly)
            return true;

        if (debugTargetFaction == null)
        {
            LogMissingDebugTargetWarning();
            return false;
        }

        return ReferenceEquals(primaryFaction, debugTargetFaction) || ReferenceEquals(relatedFaction, debugTargetFaction);
    }

    private void LogMissingDebugTargetWarning()
    {
        if (hasLoggedMissingDebugTargetWarning)
            return;

        Debug.LogWarning("[AI] Specific AI debug is enabled, but debug target faction is not assigned.");
        hasLoggedMissingDebugTargetWarning = true;
    }

    private static string GetFactionName(FactionManager _factionManager)
    {
        return _factionManager != null ? _factionManager.factionName : "Unknown";
    }

    private static string GetCityName(CityScript _city)
    {
        return _city != null && _city.cityData != null
            ? _city.cityData.cityName
            : "None";
    }
}
