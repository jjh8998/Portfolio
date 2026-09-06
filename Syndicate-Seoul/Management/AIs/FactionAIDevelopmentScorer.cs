using UnityEngine;

public class FactionAIDevelopmentScorer
{
    public NationAIActionScore EvaluateResearch(
        BigFivePersonality _personality,
        FactionAIContext _context,
        bool _canBuild,
        bool _canBuildNonPowerCategory)
    {
        if (!_canBuild)
            return CreateUnavailableScore(NationAIActionType.BuildResearch, "No research build available.");

        if (!_canBuildNonPowerCategory)
            return CreateUnavailableScore(NationAIActionType.BuildResearch, "Not enough power to build any research building.");

        float contextScore = EvaluateDevelopmentContextScore(_context);
        float personalScore = EvaluatePersonalScore(_personality);
        return CreateAvailableScore(NationAIActionType.BuildResearch, "Development", contextScore, personalScore);
    }

    public float EvaluateBuildCandidateScore(
        BuildingData _building,
        BigFivePersonality _personality,
        FactionAIContext _context)
    {
        float score = 10f;
        score -= _building.constructionCost * 0.05f;
        score -= _building.constructionDay * 0.03f;
        score += _building.rpOutput * 2.0f;
        score -= _building.powerConsumption * 0.9f;
        score += Mathf.Clamp(_context.PowerSurplus, 0, 20) * 0.80f;
        score += Mathf.Clamp(_context.money - 60, 0, 200) * 0.04f;
        score += _personality.openness * 0.09f;
        score += _personality.conscientiousness * 0.06f;
        score -= _context.netPower < _building.powerConsumption ? 15f : 0f;
        score -= _context.totalIncome < 20 ? 6f : 0f;
        score += GetCandidateRandomOffset();

        return score;
    }

    private NationAIActionScore CreateAvailableScore(
        NationAIActionType _actionType,
        string _categoryName,
        float _contextScore,
        float _personalScore)
    {
        NationAIScoreParts scoreParts = new NationAIScoreParts(_contextScore, _personalScore, GetNoiseScore());

        return new NationAIActionScore(_actionType)
        {
            score = scoreParts.FinalScore,
            isAvailable = true,
            categoryName = _categoryName,
            scoreParts = scoreParts
        };
    }

    private NationAIActionScore CreateUnavailableScore(NationAIActionType _actionType, string _reason)
    {
        return new NationAIActionScore(_actionType)
        {
            isAvailable = false,
            unavailableReason = _reason
        };
    }

    private float EvaluateDevelopmentContextScore(FactionAIContext _context)
    {
        float targetRp = Mathf.Max(1f, _context.targetRP);
        float score = ((_context.targetRP - _context.currentRP) / targetRp) * 70f;

        if (_context.currentRP <= 0f)
            score = Mathf.Min(score, 40f);

        return Clamp70(score);
    }

    private float EvaluatePersonalScore(BigFivePersonality _personality)
    {
        float weightedBigFive =
            _personality.Openness01 * 0.70f +
            _personality.Conscientiousness01 * 0.30f;

        return Mathf.Clamp01(weightedBigFive) * 27f;
    }

    private float Clamp70(float _value)
    {
        return Mathf.Clamp(_value, 0f, 70f);
    }

    private float GetNoiseScore()
    {
        return Random.Range(0f, 3f);
    }

    private float GetCandidateRandomOffset()
    {
        return Random.Range(0f, 0.5f);
    }
}
