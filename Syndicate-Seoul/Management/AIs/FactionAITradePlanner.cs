using System.Collections.Generic;
using UnityEngine;

public class FactionAITradePlanner
{
    private const int MinProposerCredit = 20;
    private const int MinTradeCredit = 5;
    private const int MaxShareTradePercent = 5;
    private const float MaxShareTradeBudgetRatio = 0.3f;
    private const float ShareTradePriceMultiplier = 1.15f;
    private const float CityValueTradeScoreWeight = 0.005f;
    private const int StableBuildingCountPerCity = 3;
    private const float MinSharePurchaseBuildingStabilityFactor = 0.15f;

    private readonly INationAITargetProvider targetProvider;
    private readonly DiplomacyTradeEvaluator tradeEvaluator = new DiplomacyTradeEvaluator();

    public FactionAITradePlanner(INationAITargetProvider _targetProvider)
    {
        targetProvider = _targetProvider ?? new NationAISceneTargetProvider();
    }

    public NationAIActionCandidate FindBestTradeCandidate(FactionAIContext _context)
    {
        return FindBestShareTradeCandidate(_context);
    }

    public NationAIActionCandidate FindBestShareTradeCandidate(FactionAIContext _context)
    {
        if (_context == null || _context.faction == null)
            return null;

        FactionManager proposerFaction = _context.faction;
        if (proposerFaction.IsPlayerFaction || proposerFaction.GetCredit < MinProposerCredit)
            return null;

        float shareTradeFactor = CalculateSharePurchaseBuildingStabilityFactor(_context);
        int maxBudget = Mathf.FloorToInt(proposerFaction.GetCredit * MaxShareTradeBudgetRatio * shareTradeFactor);
        if (maxBudget < MinTradeCredit)
            return null;

        CityShareManager cityShareManager = CityShareManager.instance;
        if (cityShareManager == null)
            return null;

        List<FactionManager> targets = targetProvider.GetEnemyFactions(proposerFaction);
        if (targets == null || targets.Count == 0)
            return null;

        CityScript[] cities = Object.FindObjectsByType<CityScript>(FindObjectsSortMode.None);
        if (cities == null || cities.Length == 0)
            return null;

        NationAIActionCandidate bestCandidate = null;
        float bestScore = float.MinValue;

        for (int i = 0; i < targets.Count; i++)
        {
            FactionManager targetFaction = targets[i];
            if (targetFaction == null || targetFaction.IsPlayerFaction || ReferenceEquals(targetFaction, proposerFaction))
                continue;

            for (int j = 0; j < cities.Length; j++)
            {
                NationAIActionCandidate candidate = TryCreateShareTradeCandidate(
                    proposerFaction,
                    targetFaction,
                    cities[j],
                    cityShareManager,
                    maxBudget);
                if (candidate == null || candidate.score <= bestScore)
                    continue;

                bestScore = candidate.score;
                bestCandidate = candidate;
            }
        }

        return bestCandidate;
    }

    private NationAIActionCandidate TryCreateShareTradeCandidate(
        FactionManager proposerFaction,
        FactionManager targetFaction,
        CityScript city,
        CityShareManager cityShareManager,
        int maxBudget)
    {
        if (city == null || city.cityData == null)
            return null;

        if (ReferenceEquals(city.cityData.owner, targetFaction))
            return null;

        if (!cityShareManager.TryPrepareCityShares(city, out _))
            return null;

        CityShareData shareData = city.cityData.shareData;
        if (shareData == null || shareData.GetTotalShare() != 100)
            return null;

        if (cityShareManager.IsSoleTopShareHolder(city, proposerFaction))
            return null;

        if (cityShareManager.CanClaimOwnership(city, proposerFaction, out _))
            return null;

        int targetShareAmount = shareData.GetShare(targetFaction);
        if (targetShareAmount <= 0)
            return null;

        int sharePercent = Mathf.Min(MaxShareTradePercent, targetShareAmount);
        if (sharePercent <= 0)
            return null;

        DiplomacyTradeRequest valueRequest = CreateCreditForShareTradeRequest(0, city, sharePercent);
        tradeEvaluator.TryEvaluate(
            valueRequest,
            proposerFaction,
            targetFaction,
            out _,
            out _,
            out int targetGiveValue);
        int offerCredit = Mathf.CeilToInt(targetGiveValue * ShareTradePriceMultiplier);
        if (offerCredit <= 0 || offerCredit > maxBudget || offerCredit > proposerFaction.GetCredit)
            return null;

        DiplomacyTradeRequest request = CreateCreditForShareTradeRequest(offerCredit, city, sharePercent);
        if (!tradeEvaluator.TryEvaluate(request, proposerFaction, targetFaction, out string reason))
            return null;

        float score = CalculateShareTradeCandidateScore(proposerFaction, city, shareData, sharePercent, offerCredit);
        if (score <= 0f)
            return null;

        return new NationAIActionCandidate
        {
            actionType = NationAIActionType.Trade,
            attackerFaction = proposerFaction,
            targetFaction = targetFaction,
            tradeRequest = request,
            score = score,
            reason = BuildShareTradeReason(targetFaction, city, offerCredit, sharePercent, reason)
        };
    }

    private DiplomacyTradeRequest CreateCreditForShareTradeRequest(int credit, CityScript shareCity, int sharePercent)
    {
        TradeSideOffer proposerOffer = new TradeSideOffer(
            credit,
            0,
            string.Empty,
            0,
            null,
            null,
            0);
        TradeSideOffer targetOffer = new TradeSideOffer(
            0,
            0,
            string.Empty,
            0,
            null,
            shareCity,
            sharePercent);

        return new DiplomacyTradeRequest(proposerOffer, targetOffer);
    }

    private float CalculateShareTradeCandidateScore(
        FactionManager proposerFaction,
        CityScript city,
        CityShareData shareData,
        int sharePercent,
        int offerCredit)
    {
        float score = 5f;
        score += sharePercent * 0.8f;
        score += CityValueCalculator.CalculateCityValue(city) * CityValueTradeScoreWeight;
        score -= offerCredit * 0.03f;

        if (shareData != null && shareData.GetShare(proposerFaction) > 0)
            score += 2f;

        if (city != null && city.cityData != null && ReferenceEquals(city.cityData.owner, proposerFaction))
            score += 2f;

        return Mathf.Clamp(score, 0f, 18f);
    }

    private string BuildShareTradeReason(
        FactionManager targetFaction,
        CityScript city,
        int offerCredit,
        int sharePercent,
        string evaluatorReason)
    {
        string targetName = targetFaction != null ? targetFaction.factionName : "Unknown";
        string cityName = city != null && city.cityData != null ? city.cityData.cityName : "Unknown";
        return $"Trade with {targetName}: give {offerCredit} credit, receive {cityName} share {sharePercent}%. {evaluatorReason}";
    }

    private float CalculateSharePurchaseBuildingStabilityFactor(FactionAIContext _context)
    {
        if (_context == null)
            return MinSharePurchaseBuildingStabilityFactor;

        int ownedCityCount = Mathf.Max(1, _context.OwnedCityCount);
        int targetStableBuildingCount = ownedCityCount * StableBuildingCountPerCity;
        float buildingProgress = Mathf.Clamp01((float)_context.completedBuildingCount / targetStableBuildingCount);

        return Mathf.Lerp(
            MinSharePurchaseBuildingStabilityFactor,
            1f,
            buildingProgress * buildingProgress);
    }
}
