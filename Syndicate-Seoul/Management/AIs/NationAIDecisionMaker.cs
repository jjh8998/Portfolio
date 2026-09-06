using System.Collections.Generic;

public enum NationAIActionType
{
    BuildEconomy,
    BuildPower,
    BuildResearch,
    BuildFactory,
    BuildSupport,
    EmergencyOrder,
    DeclareWar,
    BuyShare,
    Trade,
    SaveMoney,
    ScavengerUnlock
}

public class NationAIActionScore
{
    public NationAIActionType actionType;
    public float score;
    public bool isAvailable;
    public string reason;
    public string categoryName;
    public string unavailableReason;
    public NationAIScoreParts scoreParts;

    public NationAIActionScore(NationAIActionType _actionType)
    {
        actionType = _actionType;
        score = 0f;
        isAvailable = true;
        reason = string.Empty;
        categoryName = string.Empty;
        unavailableReason = string.Empty;
        scoreParts = new NationAIScoreParts(0f, 0f, 0f);
    }
}

public struct NationAIScoreParts
{
    public float contextScore;
    public float personalScore;
    public float noiseScore;

    public NationAIScoreParts(float _contextScore, float _personalScore, float _noiseScore)
    {
        contextScore = UnityEngine.Mathf.Clamp(_contextScore, 0f, 70f);
        personalScore = UnityEngine.Mathf.Clamp(_personalScore, 0f, 27f);
        noiseScore = UnityEngine.Mathf.Clamp(_noiseScore, 0f, 3f);
    }

    public float FinalScore => contextScore + personalScore + noiseScore;
}

public class NationAIActionCandidate
{
    public NationAIActionType actionType;
    public CityScript city;
    public int slotIndex = -1;
    public BuildingData building;
    public CityScript targetCity;
    public FactionManager attackerFaction;
    public FactionManager targetFaction;
    public DiplomacyTradeRequest? tradeRequest;
    public int sharePurchaseAmount;
    public int sharePurchaseCost;
    public float score;
    public string reason;
    public bool isNewConstruction;
}

public class NationAIDecisionResult
{
    public NationAIActionType selectedActionType;
    public List<NationAIActionScore> actionScores;
    public NationAIActionCandidate selectedCandidate;

    public NationAIDecisionResult(
        NationAIActionType _selectedActionType,
        List<NationAIActionScore> _actionScores,
        NationAIActionCandidate _selectedCandidate)
    {
        selectedActionType = _selectedActionType;
        actionScores = _actionScores;
        selectedCandidate = _selectedCandidate;
    }
}

public class NationAIDecisionMaker
{
    private readonly FactionAIActionScorer scorer;
    private readonly FactionAIReasonFormatter reasonFormatter;

    public NationAIDecisionMaker()
        : this(new FactionAIBuildingProvider(), new NationAISceneTargetProvider())
    {
    }

    public NationAIDecisionMaker(
        IFactionAIBuildingProvider _buildingProvider,
        INationAITargetProvider _targetProvider)
        : this(_buildingProvider, _targetProvider, null)
    {
    }

    public NationAIDecisionMaker(
        IFactionAIBuildingProvider _buildingProvider,
        INationAITargetProvider _targetProvider,
        FactionAITargetCityMemory _targetCityMemory)
    {
        scorer = new FactionAIActionScorer(_buildingProvider, _targetProvider, _targetCityMemory);
        reasonFormatter = new FactionAIReasonFormatter();
    }

    public NationAIDecisionResult Decide(BigFivePersonality _personality, FactionAIContext _context)
    {
        return Decide(_personality, _context, null);
    }

    public NationAIDecisionResult Decide(
        BigFivePersonality _personality,
        FactionAIContext _context,
        HashSet<NationAIActionType> _excludedActionTypes)
    {
        List<NationAIActionScore> actionScores = scorer.EvaluateActionScores(_personality, _context);
        ApplyExcludedActionTypes(actionScores, _excludedActionTypes);
        ApplyActionReasons(actionScores);

        if (!IsActionExcluded(NationAIActionType.ScavengerUnlock, _excludedActionTypes) &&
            _context != null &&
            _context.HasNoBuildableEmptySlotButScavengerLockedSlot)
        {
            NationAIActionCandidate forcedScavengerUnlockCandidate = BuildCandidate(NationAIActionType.ScavengerUnlock, _personality, _context);
            if (forcedScavengerUnlockCandidate != null)
                return new NationAIDecisionResult(NationAIActionType.ScavengerUnlock, actionScores, forcedScavengerUnlockCandidate);
        }

        if (!IsActionExcluded(NationAIActionType.BuildFactory, _excludedActionTypes) &&
            FactionAIMandatoryBuildRuleService.ShouldForceFactoryOnLastBuildableSlot(_context))
        {
            NationAIActionCandidate forcedFactoryCandidate = BuildCandidate(NationAIActionType.BuildFactory, _personality, _context);
            if (forcedFactoryCandidate != null)
                return new NationAIDecisionResult(NationAIActionType.BuildFactory, actionScores, forcedFactoryCandidate);

            return CreateSaveMoneyDecisionResult(
                actionScores,
                _context,
                "Last buildable slot is reserved for factory, but no valid factory candidate is available.");
        }

        if (!IsActionExcluded(NationAIActionType.BuildPower, _excludedActionTypes) && scorer.ShouldForcePowerBuild(_context))
        {
            NationAIActionCandidate forcedPowerCandidate = BuildCandidate(NationAIActionType.BuildPower, _personality, _context);
            if (forcedPowerCandidate != null)
                return new NationAIDecisionResult(NationAIActionType.BuildPower, actionScores, forcedPowerCandidate);
        }

        NationAIActionType selectedActionType = SelectBestAction(actionScores, out bool hasAvailableAction);
        if (!hasAvailableAction)
            return CreateSaveMoneyDecisionResult(actionScores, _context, "No available action. Save money this turn.");

        NationAIActionCandidate selectedCandidate = BuildCandidate(selectedActionType, _personality, _context);

        return new NationAIDecisionResult(selectedActionType, actionScores, selectedCandidate);
    }

    private void ApplyExcludedActionTypes(
        List<NationAIActionScore> _actionScores,
        HashSet<NationAIActionType> _excludedActionTypes)
    {
        if (_actionScores == null || _excludedActionTypes == null || _excludedActionTypes.Count == 0)
            return;

        for (int i = 0; i < _actionScores.Count; i++)
        {
            NationAIActionScore actionScore = _actionScores[i];
            if (actionScore == null || !_excludedActionTypes.Contains(actionScore.actionType))
                continue;

            actionScore.isAvailable = false;
            actionScore.unavailableReason = "Excluded this turn.";
        }
    }

    private void ApplyActionReasons(List<NationAIActionScore> _actionScores)
    {
        if (_actionScores == null)
            return;

        for (int i = 0; i < _actionScores.Count; i++)
        {
            NationAIActionScore actionScore = _actionScores[i];
            if (actionScore == null)
                continue;

            actionScore.reason = reasonFormatter.FormatActionReason(actionScore);
        }
    }

    private NationAIActionCandidate BuildCandidate(
        NationAIActionType _actionType,
        BigFivePersonality _personality,
        FactionAIContext _context)
    {
        NationAIActionCandidate candidate = scorer.FindBestCandidate(_actionType, _personality, _context);
        if (candidate == null)
            return null;

        candidate.reason = reasonFormatter.FormatCandidateReason(candidate);
        return candidate;
    }

    private NationAIActionType SelectBestAction(List<NationAIActionScore> _actionScores, out bool hasAvailableAction)
    {
        hasAvailableAction = false;
        float bestScore = float.MinValue;
        NationAIActionType bestAction = NationAIActionType.SaveMoney;

        for (int i = 0; i < _actionScores.Count; i++)
        {
            NationAIActionScore actionScore = _actionScores[i];
            if (actionScore == null || !actionScore.isAvailable)
                continue;

            hasAvailableAction = true;
            if (actionScore.score > bestScore)
            {
                bestScore = actionScore.score;
                bestAction = actionScore.actionType;
            }
        }

        return bestAction;
    }

    private NationAIDecisionResult CreateSaveMoneyDecisionResult(
        List<NationAIActionScore> _actionScores,
        FactionAIContext _context,
        string _reason)
    {
        return new NationAIDecisionResult(
            NationAIActionType.SaveMoney,
            _actionScores,
            new NationAIActionCandidate
            {
                actionType = NationAIActionType.SaveMoney,
                attackerFaction = _context != null ? _context.faction : null,
                score = 0f,
                reason = _reason
            });
    }

    private bool IsActionExcluded(NationAIActionType _actionType, HashSet<NationAIActionType> _excludedActionTypes)
    {
        return _excludedActionTypes != null && _excludedActionTypes.Contains(_actionType);
    }
}

public static class FactionAIMandatoryBuildRuleService
{
    public static bool ShouldForceFactoryOnLastBuildableSlot(FactionAIContext context)
    {
        if (context == null)
            return false;

        if (context.faction == null)
            return false;

        if (context.OwnedCityCount <= 0)
            return false;

        if (context.BuildableEmptySlotCount != 1)
            return false;

        if (context.HasFactoryOrFactoryInProgress)
            return false;

        return true;
    }
}
