using System.Collections.Generic;
using UnityEngine;

public class FactionAIMilitaryScorer
{
    private const int EmergencyOrderTargetOwnedItemCount = 30;
    private const int EmergencyOrderMaxOwnedItemCount = 50;

    public NationAIActionScore EvaluateFactory(
        BigFivePersonality _personality,
        FactionAIContext _context,
        List<BuildingData> _factoryBuildings,
        bool _canBuild,
        bool _canBuildNonPowerCategory)
    {
        if (!_canBuild)
            return CreateUnavailableScore(NationAIActionType.BuildFactory, "No factory build available.");

        if (!_canBuildNonPowerCategory)
            return CreateUnavailableScore(NationAIActionType.BuildFactory, "Not enough power to build any factory building.");

        float contextScore = EvaluateMilitaryContextScore(_context);
        float personalScore = EvaluatePersonalScore(_personality);
        return CreateAvailableScore(NationAIActionType.BuildFactory, "Military", contextScore, personalScore);
    }

    public NationAIActionScore EvaluateEmergencyOrder(BigFivePersonality _personality, FactionAIContext _context)
    {
        if (!HasEmergencyOrderFactory(_context, out bool hasAffordableFactory))
            return CreateUnavailableScore(NationAIActionType.EmergencyOrder, "No emergency order factory available.");

        if (!hasAffordableFactory)
            return CreateUnavailableScore(NationAIActionType.EmergencyOrder, "Not enough credit for emergency order.");

        if (!CanUseEmergencyOrderByInventory(_context))
            return CreateUnavailableScore(NationAIActionType.EmergencyOrder, "Too many cards or card packs for emergency order.");

        float contextScore = EvaluateMilitaryContextScore(_context);
        float personalScore = EvaluatePersonalScore(_personality);
        float inventoryMultiplier = GetEmergencyOrderInventoryMultiplier(_context);
        contextScore *= inventoryMultiplier;
        personalScore *= inventoryMultiplier;

        return CreateAvailableScore(NationAIActionType.EmergencyOrder, "Military", contextScore, personalScore);
    }

    public float EvaluateBuildCandidateScore(
        BuildingData _building,
        BigFivePersonality _personality,
        FactionAIContext _context)
    {
        float score = 10f;
        score -= _building.constructionCost * 0.05f;
        score -= _building.constructionDay * 0.03f;
        score += _context.PowerSurplus * 0.8f;
        score += Mathf.Clamp(_context.money - 60, 0, 200) * 0.05f;
        score += _personality.openness * 0.05f;
        score += _personality.extraversion * 0.07f;
        score -= _context.totalIncome < 20 ? 10f : 0f;
        score -= _context.netPower < _building.powerConsumption ? 12f : 0f;
        score += GetCandidateRandomOffset();

        return score;
    }

    public float EvaluateEmergencyOrderCandidateScore(BuildingData _building, FactionAIContext _context)
    {
        if (_building == null || _context == null)
            return 0f;

        float score = 10f;
        score += EvaluateMilitaryContextScore(_context) * 0.5f;
        score += Mathf.Max(0, _building.emergencyOrderCost) * 0.05f;
        score += Mathf.Clamp(_context.money - _building.emergencyOrderCost, 0, 200) * 0.03f;
        score += GetCandidateRandomOffset();
        return score * GetEmergencyOrderInventoryMultiplier(_context);
    }

    public bool IsEmergencyOrderFactory(BuildingInstance _building)
    {
        if (_building == null || _building.data == null)
            return false;

        if (!_building.data.IsFactory())
            return false;

        if (_building.data.emergencyOrderCost <= 0)
            return false;

        if (_building.IsUnderConstruction() || _building.IsDisabled())
            return false;

        return true;
    }

    public bool CanUseEmergencyOrderByInventory(FactionAIContext _context)
    {
        int currentCount = GetOwnedCardAndPackCount(_context != null ? _context.faction : null);
        return currentCount < EmergencyOrderMaxOwnedItemCount;
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

    private float EvaluateMilitaryContextScore(FactionAIContext _context)
    {
        if (_context == null)
            return 0f;

        int cityCount = _context.ownedCities != null ? _context.ownedCities.Count : 0;
        float baseProduction = cityCount * 5f;
        float threatProduction = _context.nearEnemyPower * 0.5f;
        float targetProduction = Mathf.Max(1f, baseProduction + threatProduction);
        float contextScore = ((targetProduction - _context.currentProduction) / targetProduction) * 70f;
        return Clamp70(contextScore);
    }

    private float EvaluatePersonalScore(BigFivePersonality _personality)
    {
        float weightedBigFive =
            _personality.Extraversion01 * 0.55f +
            _personality.Openness01 * 0.45f;

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

    private float GetEmergencyOrderInventoryMultiplier(FactionAIContext _context)
    {
        int currentCount = GetOwnedCardAndPackCount(_context != null ? _context.faction : null);
        float ratio = EmergencyOrderTargetOwnedItemCount > 0
            ? Mathf.Clamp01((float)currentCount / EmergencyOrderTargetOwnedItemCount)
            : 1f;

        return 1f - (ratio * ratio);
    }

    private int GetOwnedCardAndPackCount(FactionManager faction)
    {
        if (faction == null)
            return 0;

        int totalCount = 0;

        CardInventory cardInventory = faction.GetCardInventory();
        if (cardInventory != null)
            totalCount += cardInventory.Count;

        FactionCardPackInventoryScript cardPackInventory = faction.GetCardPackInventory();
        if (cardPackInventory != null)
            totalCount += cardPackInventory.GetTotalCardPackCount();

        return totalCount;
    }

    private bool HasEmergencyOrderFactory(FactionAIContext _context, out bool _hasAffordableFactory)
    {
        _hasAffordableFactory = false;

        if (_context == null || _context.faction == null || _context.ownedCities == null)
            return false;

        bool hasFactory = false;
        for (int i = 0; i < _context.ownedCities.Count; i++)
        {
            CityScript city = _context.ownedCities[i];
            if (city == null || city.cityData == null || city.cityData.buildings == null)
                continue;

            for (int j = 0; j < city.cityData.buildings.Count; j++)
            {
                BuildingInstance building = city.cityData.buildings[j];
                if (!IsEmergencyOrderFactory(building))
                    continue;

                hasFactory = true;
                EmergencyOrderState state = EmergencyOrderService.GetState(city, j, _context.faction);
                if (state != null && state.canExecute)
                {
                    _hasAffordableFactory = true;
                    return true;
                }
            }
        }

        return hasFactory;
    }
}
