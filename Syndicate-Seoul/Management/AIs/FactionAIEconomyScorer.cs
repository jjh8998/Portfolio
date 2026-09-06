using UnityEngine;

public class FactionAIEconomyScorer
{
    public NationAIActionScore EvaluateEconomy(
        BigFivePersonality _personality,
        FactionAIContext _context,
        bool _canBuildEconomy,
        bool _canBuildNonPowerCategory)
    {
        if (!_canBuildEconomy)
            return CreateUnavailableScore(NationAIActionType.BuildEconomy, "No economy build available.");

        if (!_canBuildNonPowerCategory)
            return CreateUnavailableScore(NationAIActionType.BuildEconomy, "Not enough power to build any economy building.");

        float contextScore = EvaluateEconomyContextScore(_context);
        float personalScore = EvaluatePersonalScore(_personality);
        return CreateAvailableScore(NationAIActionType.BuildEconomy, "Economy", contextScore, personalScore);
    }

    public float EvaluateBuildCandidateScore(
        CityScript _city,
        BuildingData _building,
        BigFivePersonality _personality,
        FactionAIContext _context)
    {
        float score = 10f;
        score -= _building.constructionCost * 0.05f;
        score -= _building.constructionDay * 0.03f;
        score += _building.bonusIncome * 1.5f;
        score += Mathf.Clamp(100 - _context.money, 0, 100) * 0.08f;
        score += Mathf.Clamp(40 - _context.totalIncome, 0, 40) * 0.25f;
        score += _personality.conscientiousness * 0.08f;
        score += GetCityCreditIncome(_city) * 0.05f;
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

    private float EvaluateEconomyContextScore(FactionAIContext _context)
    {
        if (_context.money < _context.lowMoneyThreshold)
            return 70f;

        float targetIncome = Mathf.Max(1f, _context.targetIncome);
        float contextScore = ((targetIncome - _context.totalIncome) / targetIncome) * 70f;
        return Clamp70(contextScore);
    }

    private float EvaluatePersonalScore(BigFivePersonality _personality)
    {
        float weightedBigFive =
            _personality.Conscientiousness01 * 0.65f +
            _personality.Neuroticism01 * 0.35f;

        return Mathf.Clamp01(weightedBigFive) * 27f;
    }

    private int GetCityCreditIncome(CityScript _city)
    {
        return ManagementResourceCalculator.CalculateCity(_city).creditIncome;
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
