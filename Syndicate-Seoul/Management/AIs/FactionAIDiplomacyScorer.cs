using System.Collections.Generic;
using UnityEngine;

public class FactionAIDiplomacyScorer
{
    private const int MinWarReadyItemCount = 1;
    private const float BaseSharePurchaseInvestmentRatio = 0.5f;
    private const float MinSharePurchaseInvestmentRatio = 0.2f;
    private const float MaxSharePurchaseInvestmentRatio = 0.8f;
    private const float MaxTargetCityValueScore = 50f;
    private const float TargetCityBattleScoreWeight = 0.5f;
    private const float MaxTargetCityPersonalityScore = 25f;
    private const int StableBuildingCountPerCity = 3;
    private const float MinSharePurchaseBuildingStabilityFactor = 0.15f;

    private readonly INationAITargetProvider targetProvider;
    private readonly FactionAITradePlanner tradePlanner;
    private readonly FactionAITargetCityMemory targetCityMemory;

    public FactionAIDiplomacyScorer(INationAITargetProvider _targetProvider)
        : this(_targetProvider, null)
    {
    }

    public FactionAIDiplomacyScorer(INationAITargetProvider _targetProvider, FactionAITargetCityMemory _targetCityMemory)
    {
        targetProvider = _targetProvider ?? new NationAISceneTargetProvider();
        tradePlanner = new FactionAITradePlanner(targetProvider);
        targetCityMemory = _targetCityMemory;
    }

    public NationAIActionScore EvaluateWar(BigFivePersonality _personality, FactionAIContext _context)
    {
        NationAIActionCandidate bestWarCandidate = FindBestWarCandidate(_personality, _context);
        if (bestWarCandidate == null)
            return CreateUnavailableScore(NationAIActionType.DeclareWar, "No favorable war target.");

        float enemyPower = Mathf.Max(1f, bestWarCandidate.score);
        float contextScore = EvaluateDiplomacyContextScore(Mathf.Max(1f, _context.militaryScore), enemyPower);
        float personalScore = EvaluatePersonalScore(NationAIActionType.DeclareWar, _personality);
        return CreateAvailableScore(NationAIActionType.DeclareWar, "Diplomacy", contextScore, personalScore);
    }

    public NationAIActionScore EvaluateBuyShare(BigFivePersonality _personality, FactionAIContext _context)
    {
        NationAIActionCandidate bestShareAcquisitionCandidate = FindBestShareAcquisitionCandidate(_personality, _context);
        if (bestShareAcquisitionCandidate == null)
        {
            return CreateUnavailableScore(
                NationAIActionType.BuyShare,
                "No useful share acquisition target.");
        }

        float sharePurchaseFactor = CalculateSharePurchaseBuildingStabilityFactor(_context);
        float contextScore = Clamp70(bestShareAcquisitionCandidate.score * sharePurchaseFactor);
        float personalScore = EvaluatePersonalScore(NationAIActionType.BuyShare, _personality);
        return CreateAvailableScore(NationAIActionType.BuyShare, "Diplomacy", contextScore, personalScore);
    }

    public NationAIActionScore EvaluateTrade(BigFivePersonality _personality, FactionAIContext _context)
    {
        NationAIActionCandidate bestTradeCandidate = FindBestTradeCandidate(_context);
        if (bestTradeCandidate == null)
            return CreateUnavailableScore(NationAIActionType.Trade, "No favorable AI trade target.");

        float contextScore = Clamp70(bestTradeCandidate.score);
        float personalScore = EvaluatePersonalScore(NationAIActionType.Trade, _personality);
        return CreateAvailableScore(NationAIActionType.Trade, "Diplomacy", contextScore, personalScore);
    }

    public NationAIActionCandidate FindBestCandidate(
        NationAIActionType _actionType,
        BigFivePersonality _personality,
        FactionAIContext _context)
    {
        switch (_actionType)
        {
            case NationAIActionType.BuyShare:
                return FindBestShareAcquisitionCandidate(_personality, _context);
            case NationAIActionType.Trade:
                return FindBestTradeCandidate(_context);
            case NationAIActionType.DeclareWar:
                return FindBestWarCandidate(_personality, _context);
        }

        return null;
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

    private NationAIActionCandidate FindBestWarCandidate(BigFivePersonality _personality, FactionAIContext _context)
    {
        if (_context == null || _context.faction == null)
            return null;

        if (GetWarReadyItemCount(_context.faction) < MinWarReadyItemCount)
            return null;

        List<FactionManager> enemyFactions = targetProvider.GetEnemyFactions(_context.faction);
        if (enemyFactions == null || enemyFactions.Count == 0)
            return null;

        NationAIActionCandidate bestCandidate = null;
        float bestScore = float.MinValue;
        int attackerPower = ManagementResourceCalculator.CalculateMilitaryScore(_context.faction);
        CityShareManager cityShareManager = CityShareManager.instance != null
            ? CityShareManager.instance
            : Object.FindFirstObjectByType<CityShareManager>();

        CityScript targetCity = GetValidTargetCity(_context);
        if (targetCity != null)
        {
            NationAIActionCandidate targetCandidate = TryCreateWarCandidate(
                _context,
                _personality,
                targetCity,
                attackerPower,
                cityShareManager,
                out _);
            if (targetCandidate != null)
                return targetCandidate;
        }

        for (int i = 0; i < enemyFactions.Count; i++)
        {
            FactionManager targetFaction = enemyFactions[i];
            if (targetFaction == null || targetFaction.ownedCities == null || targetFaction.ownedCities.Count == 0)
                continue;

            for (int j = 0; j < targetFaction.ownedCities.Count; j++)
            {
                NationAIActionCandidate candidate = TryCreateWarCandidate(
                    _context,
                    _personality,
                    targetFaction.ownedCities[j],
                    attackerPower,
                    cityShareManager,
                    out float score);
                if (candidate == null)
                    continue;

                if (score <= bestScore)
                    continue;

                bestScore = score;
                bestCandidate = candidate;
            }
        }

        RememberTargetCity(bestCandidate, _context);
        return bestCandidate;
    }

    private NationAIActionCandidate TryCreateWarCandidate(
        FactionAIContext _context,
        BigFivePersonality _personality,
        CityScript targetCity,
        int attackerPower,
        CityShareManager cityShareManager,
        out float evaluationScore)
    {
        evaluationScore = 0f;

        if (_context == null || _context.faction == null)
            return null;

        if (targetCity == null || targetCity.cityData == null)
            return null;

        if (ReferenceEquals(targetCity.cityData.owner, _context.faction))
            return null;

        FactionManager defenderFaction = targetCity.cityData.owner;
        if (defenderFaction == null)
            return null;

        if (IsAIWarDeclarationToPlayerBlocked(defenderFaction))
            return null;

        int defenderPower = ManagementResourceCalculator.CalculateMilitaryScore(defenderFaction);
        if (attackerPower <= defenderPower)
            return null;

        if (cityShareManager != null)
        {
            if (!cityShareManager.TryPrepareCityShares(targetCity, out _))
                return null;

            if (!cityShareManager.CanClaimOwnership(targetCity, _context.faction, out _))
                return null;
        }

        float enemyPower = Mathf.Max(1f, defenderPower);
        float contextScore = EvaluateDiplomacyContextScore(Mathf.Max(1f, attackerPower), enemyPower);
        if (contextScore <= 0f)
            return null;

        float targetCityScore = CalculateTargetCityScore(targetCity, _context, _personality, cityShareManager);
        evaluationScore = targetCityScore + contextScore + GetNoiseScore();

        return new NationAIActionCandidate
        {
            actionType = NationAIActionType.DeclareWar,
            attackerFaction = _context.faction,
            targetCity = targetCity,
            score = enemyPower
        };
    }

    private NationAIActionCandidate FindBestSharePurchaseCandidate(BigFivePersonality _personality, FactionAIContext _context)
    {
        if (_context == null || _context.faction == null)
            return null;

        CityShareManager cityShareManager = CityShareManager.instance != null
            ? CityShareManager.instance
            : Object.FindFirstObjectByType<CityShareManager>();
        if (cityShareManager == null)
            return null;

        CitySharePurchaseService sharePurchaseService = Object.FindFirstObjectByType<CitySharePurchaseService>();
        if (sharePurchaseService == null)
            return null;

        List<FactionManager> enemyFactions = targetProvider.GetEnemyFactions(_context.faction);
        if (enemyFactions == null || enemyFactions.Count == 0)
            return null;

        NationAIActionCandidate bestCandidate = null;
        float bestScore = float.MinValue;
        float investmentRatio = CalculateSharePurchaseInvestmentRatio(_personality);
        float sharePurchaseFactor = CalculateSharePurchaseBuildingStabilityFactor(_context);
        int maxBudget = Mathf.FloorToInt(_context.money * investmentRatio * sharePurchaseFactor);
        if (maxBudget < 1)
            return null;

        CityScript targetCity = GetValidTargetCity(_context);
        if (targetCity != null)
        {
            NationAIActionCandidate targetCandidate = TryCreateSharePurchaseCandidate(
                _context,
                _personality,
                targetCity,
                cityShareManager,
                sharePurchaseService,
                maxBudget,
                out _);
            if (targetCandidate != null)
                return targetCandidate;
        }

        for (int i = 0; i < enemyFactions.Count; i++)
        {
            FactionManager targetFaction = enemyFactions[i];
            if (targetFaction == null)
                continue;

            if (targetFaction.ownedCities == null || targetFaction.ownedCities.Count == 0)
                continue;

            for (int j = 0; j < targetFaction.ownedCities.Count; j++)
            {
                NationAIActionCandidate candidate = TryCreateSharePurchaseCandidate(
                    _context,
                    _personality,
                    targetFaction.ownedCities[j],
                    cityShareManager,
                    sharePurchaseService,
                    maxBudget,
                    out float score);
                if (candidate == null || score <= 0f)
                    continue;

                if (score <= bestScore)
                    continue;

                bestScore = score;
                bestCandidate = candidate;
            }
        }

        RememberTargetCity(bestCandidate, _context);
        return bestCandidate;
    }

    private NationAIActionCandidate TryCreateSharePurchaseCandidate(
        FactionAIContext _context,
        BigFivePersonality _personality,
        CityScript targetCity,
        CityShareManager cityShareManager,
        CitySharePurchaseService sharePurchaseService,
        int maxBudget,
        out float evaluationScore)
    {
        evaluationScore = 0f;

        if (_context == null || _context.faction == null)
            return null;

        if (targetCity == null || targetCity.cityData == null)
            return null;

        if (targetCity.cityData.owner == null)
            return null;

        if (ReferenceEquals(targetCity.cityData.owner, _context.faction))
            return null;

        if (cityShareManager == null || sharePurchaseService == null)
            return null;

        if (!TryGetPreparedShareData(targetCity, cityShareManager, out CityShareData shareData))
            return null;

        FactionManager highestShareHolder = shareData.GetHighestShareHolder();
        if (ReferenceEquals(highestShareHolder, _context.faction))
            return null;

        if (cityShareManager.CanClaimOwnership(targetCity, _context.faction, out _))
            return null;

        int unassignedShare = shareData.UnassignedSharePercent;
        if (unassignedShare <= 0)
            return null;

        int pricePerShare = sharePurchaseService.GetPurchasePrice(1);
        if (pricePerShare <= 0)
            return null;

        int affordableShare = maxBudget / pricePerShare;
        if (affordableShare < 1)
            return null;

        int purchaseAmount = Mathf.Min(unassignedShare, affordableShare);
        int purchaseCost = sharePurchaseService.GetPurchasePrice(purchaseAmount);

        while (purchaseAmount > 0 && purchaseCost > maxBudget)
        {
            purchaseAmount--;
            purchaseCost = sharePurchaseService.GetPurchasePrice(purchaseAmount);
        }

        if (purchaseAmount < 1)
            return null;

        if (!sharePurchaseService.CanPurchaseShare(targetCity, _context.faction, purchaseAmount, out _))
            return null;

        float targetCityScore = CalculateTargetCityScore(targetCity, _context, _personality, cityShareManager);
        float purchaseBonus = purchaseAmount * 0.5f;
        float costPenalty = purchaseCost * 0.01f;
        evaluationScore = targetCityScore + purchaseBonus - costPenalty + GetNoiseScore();
        if (evaluationScore <= 0f)
            return null;

        return new NationAIActionCandidate
        {
            actionType = NationAIActionType.BuyShare,
            attackerFaction = _context.faction,
            targetCity = targetCity,
            sharePurchaseAmount = purchaseAmount,
            sharePurchaseCost = purchaseCost,
            score = evaluationScore
        };
    }

    private float CalculateTargetCityScore(
        CityScript _city,
        FactionAIContext _context,
        BigFivePersonality _personality,
        CityShareManager _cityShareManager)
    {
        if (_city == null || _context == null || _context.faction == null)
            return 0f;

        float cityValueScore = Mathf.Clamp(CityValueCalculator.CalculateCityValue(_city) / 10f, 0f, MaxTargetCityValueScore);
        float shareControlScore = CalculateShareControlScore(_city, _context, _cityShareManager);
        float unassignedShareScore = TryGetPreparedShareData(_city, _cityShareManager, out CityShareData shareData)
            ? CalculateUnassignedShareScore(shareData)
            : 0f;
        float battleAdvantageScore = CalculateBattleAdvantageScore(_city, _context) * TargetCityBattleScoreWeight;
        float personalityScore = CalculateTargetCityPersonalityScore(_personality);

        // Distance score and relation score will be added later.
        return cityValueScore
            + shareControlScore
            + unassignedShareScore
            + battleAdvantageScore
            + personalityScore;
    }

    private float CalculateShareControlScore(
        CityScript _city,
        FactionAIContext _context,
        CityShareManager _cityShareManager)
    {
        if (_city == null || _context == null || _context.faction == null)
            return 0f;

        if (!TryGetPreparedShareData(_city, _cityShareManager, out CityShareData shareData))
            return 0f;

        if (_cityShareManager != null && _cityShareManager.CanClaimOwnership(_city, _context.faction, out _))
            return 50f;

        int myShare = shareData.GetShare(_context.faction);
        int highestSharePercent = shareData.GetHighestSharePercent();
        int requiredShare = highestSharePercent + 1 - myShare;

        if (requiredShare <= 0)
            return 50f;

        if (requiredShare <= 5)
            return 30f;

        if (requiredShare <= 10)
            return 20f;

        if (requiredShare <= 30)
            return 10f;

        return 0f;
    }

    private float CalculateUnassignedShareScore(CityShareData _shareData)
    {
        if (_shareData == null)
            return 0f;

        int unassignedShare = _shareData.UnassignedSharePercent;
        if (unassignedShare >= 50)
            return 30f;

        if (unassignedShare >= 25)
            return 20f;

        if (unassignedShare >= 10)
            return 10f;

        return 0f;
    }

    private float CalculateBattleAdvantageScore(CityScript _city, FactionAIContext _context)
    {
        if (_city == null || _city.cityData == null || _context == null || _context.faction == null)
            return 0f;

        FactionManager defenderFaction = _city.cityData.owner;
        if (defenderFaction == null || ReferenceEquals(defenderFaction, _context.faction))
            return 0f;

        int myPower = _context.militaryScore > 0
            ? _context.militaryScore
            : ManagementResourceCalculator.CalculateMilitaryScore(_context.faction);
        int enemyPower = ManagementResourceCalculator.CalculateMilitaryScore(defenderFaction);

        return EvaluateDiplomacyContextScore(Mathf.Max(1f, myPower), Mathf.Max(1f, enemyPower));
    }

    private float CalculateTargetCityPersonalityScore(BigFivePersonality _personality)
    {
        if (_personality == null)
            return 0f;

        float personalityScore =
            _personality.Extraversion01 * 8f
            + (1f - _personality.Agreeableness01) * 8f
            + _personality.Conscientiousness01 * 5f
            + _personality.Neuroticism01 * 4f;

        return Mathf.Clamp(personalityScore, 0f, MaxTargetCityPersonalityScore);
    }

    private float CalculateSharePurchaseInvestmentRatio(BigFivePersonality _personality)
    {
        if (_personality == null)
            return BaseSharePurchaseInvestmentRatio;

        float investmentPropensity =
            _personality.Extraversion01 * 0.35f
            + _personality.Conscientiousness01 * 0.25f
            + (1f - _personality.Agreeableness01) * 0.25f
            + _personality.Openness01 * 0.15f;

        float personalityModifier = Mathf.Lerp(-0.3f, 0.3f, investmentPropensity);
        return Mathf.Clamp(
            BaseSharePurchaseInvestmentRatio + personalityModifier,
            MinSharePurchaseInvestmentRatio,
            MaxSharePurchaseInvestmentRatio);
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

    private bool TryGetPreparedShareData(
        CityScript _city,
        CityShareManager _cityShareManager,
        out CityShareData shareData)
    {
        shareData = null;

        if (_city == null || _city.cityData == null || _cityShareManager == null)
            return false;

        if (!_cityShareManager.TryPrepareCityShares(_city, out _))
            return false;

        shareData = _city.cityData.shareData;
        return shareData != null;
    }

    private CityScript GetValidTargetCity(FactionAIContext _context)
    {
        if (targetCityMemory == null || _context == null)
            return null;

        if (!targetCityMemory.HasValidTargetCity(_context.faction, _context.currentMonthIndex))
            return null;

        return targetCityMemory.GetTargetCity();
    }

    private void RememberTargetCity(NationAIActionCandidate candidate, FactionAIContext _context)
    {
        if (targetCityMemory == null || candidate == null || candidate.targetCity == null || _context == null)
            return;

        targetCityMemory.SetTargetCity(
            candidate.targetCity,
            _context.currentMonthIndex,
            targetCityMemory.DefaultKeepMonthCount);
    }

    private NationAIActionCandidate FindBestShareAcquisitionCandidate(BigFivePersonality _personality, FactionAIContext _context)
    {
        NationAIActionCandidate sharePurchaseCandidate = FindBestSharePurchaseCandidate(_personality, _context);
        if (sharePurchaseCandidate != null)
            return sharePurchaseCandidate;

        NationAIActionCandidate shareTradeCandidate = tradePlanner.FindBestShareTradeCandidate(_context);
        if (shareTradeCandidate != null)
            shareTradeCandidate.reason = $"BuyShare fallback: {shareTradeCandidate.reason}";

        return shareTradeCandidate;
    }

    private NationAIActionCandidate FindBestTradeCandidate(FactionAIContext _context)
    {
        return tradePlanner.FindBestTradeCandidate(_context);
    }

    private float EvaluateDiplomacyContextScore(float _myPower, float _enemyPower)
    {
        float myPower = Mathf.Max(1f, _myPower);
        float enemyPower = Mathf.Max(1f, _enemyPower);
        float powerRatio = myPower / enemyPower;
        return Clamp70((powerRatio - 1f) * 70f);
    }

    private float EvaluatePersonalScore(NationAIActionType _actionType, BigFivePersonality _personality)
    {
        float weightedBigFive = 0f;

        switch (_actionType)
        {
            case NationAIActionType.DeclareWar:
                weightedBigFive =
                    _personality.Extraversion01 * 0.50f +
                    (1f - _personality.Agreeableness01) * 0.35f +
                    _personality.Neuroticism01 * 0.15f;
                break;
            case NationAIActionType.BuyShare:
                weightedBigFive =
                    _personality.Extraversion01 * 0.35f +
                    (1f - _personality.Agreeableness01) * 0.35f +
                    _personality.Conscientiousness01 * 0.20f +
                    _personality.Neuroticism01 * 0.10f;
                break;
            case NationAIActionType.Trade:
                weightedBigFive =
                    _personality.Agreeableness01 * 0.55f +
                    _personality.Extraversion01 * 0.35f +
                    _personality.Conscientiousness01 * 0.10f;
                break;
        }

        return Mathf.Clamp01(weightedBigFive) * 27f;
    }

    private int GetWarReadyItemCount(FactionManager _faction)
    {
        if (_faction == null)
            return 0;

        int totalCount = 0;

        CardInventory cardInventory = _faction.GetCardInventory();
        if (cardInventory != null)
            totalCount += cardInventory.Count;

        FactionCardPackInventoryScript cardPackInventory = _faction.GetCardPackInventory();
        if (cardPackInventory != null)
            totalCount += cardPackInventory.GetTotalCardPackCount();

        return totalCount;
    }

    private bool IsAIWarDeclarationToPlayerBlocked(FactionManager _defenderFaction)
    {
        if (_defenderFaction == null || !_defenderFaction.IsPlayerFaction)
            return false;

        DeveloperModeManager developerModeManager = Object.FindFirstObjectByType<DeveloperModeManager>();
        return developerModeManager != null && developerModeManager.BlockAIWarDeclarationToPlayer;
    }

    private float Clamp70(float _value)
    {
        return Mathf.Clamp(_value, 0f, 70f);
    }

    private float GetNoiseScore()
    {
        return Random.Range(0f, 3f);
    }
}
