using System;
using System.Collections.Generic;
using UnityEngine;

public class FactionAIInfrastructureScorer
{
    private readonly IFactionAIBuildingProvider buildingProvider;
    private readonly Func<BuildingInstance, List<BuildingData>, FactionAIContext, List<BuildingData>> getAvailableBuildingsForSlot;

    public FactionAIInfrastructureScorer(
        IFactionAIBuildingProvider _buildingProvider,
        Func<BuildingInstance, List<BuildingData>, FactionAIContext, List<BuildingData>> _getAvailableBuildingsForSlot)
    {
        buildingProvider = _buildingProvider;
        getAvailableBuildingsForSlot = _getAvailableBuildingsForSlot;
    }

    public NationAIActionScore EvaluatePower(
        BigFivePersonality _personality,
        FactionAIContext _context,
        bool _canBuildPower)
    {
        return EvaluatePower(
            _personality,
            _context,
            _canBuildPower,
            GetMinBlockedNonPowerBuildingConsumption(_context));
    }

    public NationAIActionScore EvaluatePower(
        BigFivePersonality _personality,
        FactionAIContext _context,
        bool _canBuildPower,
        int _minBlockedNonPowerBuildingConsumption)
    {
        if (!_canBuildPower)
            return CreateUnavailableScore(NationAIActionType.BuildPower, "No power build available.");

        float contextScore = EvaluateInfrastructureContextScore(_context, _minBlockedNonPowerBuildingConsumption);
        float personalScore = EvaluatePersonalScore(_personality);
        return CreateAvailableScore(NationAIActionType.BuildPower, "Infrastructure", contextScore, personalScore);
    }

    public bool ShouldForcePowerBuild(FactionAIContext _context)
    {
        return ShouldForcePowerBuild(_context, GetMinBlockedNonPowerBuildingConsumption(_context));
    }

    public bool ShouldForcePowerBuild(
        FactionAIContext _context,
        int _minBlockedNonPowerBuildingConsumption)
    {
        if (_context == null)
            return false;

        float buildContextScore = EvaluateInfrastructureContextScore(_context, _minBlockedNonPowerBuildingConsumption);
        if (buildContextScore < 70f)
            return false;

        if (_context.netPower <= 0)
            return true;

        return _minBlockedNonPowerBuildingConsumption > 0 &&
               _context.netPower < _minBlockedNonPowerBuildingConsumption;
    }

    public float EvaluateBuildCandidateScore(
        BuildingData _building,
        BigFivePersonality _personality,
        FactionAIContext _context)
    {
        return EvaluateBuildCandidateScore(
            _building,
            _personality,
            _context,
            GetMinBlockedNonPowerBuildingConsumption(_context));
    }

    public float EvaluateBuildCandidateScore(
        BuildingData _building,
        BigFivePersonality _personality,
        FactionAIContext _context,
        int _minBlockedNonPowerBuildingConsumption)
    {
        float score = 10f;
        score -= _building.constructionCost * 0.05f;
        score -= _building.constructionDay * 0.03f;
        score += _building.powerOutput * 1.8f;
        score -= _building.powerConsumption * 0.8f;
        score += _context.PowerDeficit * 2.2f;
        score += _context.netPower <= 0 ? 20f : 0f;
        score += _personality.conscientiousness * 0.05f;

        if (_minBlockedNonPowerBuildingConsumption > 0 &&
            _context.netPower < _minBlockedNonPowerBuildingConsumption)
        {
            int shortage = _minBlockedNonPowerBuildingConsumption - _context.netPower;
            score += 18f;
            score += shortage * 1.2f;
        }

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

    private float EvaluateInfrastructureContextScore(
        FactionAIContext _context,
        int _minBlockedNonPowerBuildingConsumption)
    {
        float requiredPower = Mathf.Max(1f, _context.powerConsumption);
        float currentPower = _context.powerProduction;
        float powerRatio = currentPower / requiredPower;

        if (powerRatio < 1f)
            return 70f;

        float score = Clamp70((1.5f - powerRatio) * 70f);
        if (_minBlockedNonPowerBuildingConsumption > 0 &&
            _context.netPower < _minBlockedNonPowerBuildingConsumption)
        {
            int shortage = _minBlockedNonPowerBuildingConsumption - _context.netPower;
            score += 35f;
            score += shortage * 2f;
            return Clamp70(score);
        }

        return score;
    }

    private float EvaluatePersonalScore(BigFivePersonality _personality)
    {
        float weightedBigFive =
            _personality.Conscientiousness01 * 0.75f +
            _personality.Neuroticism01 * 0.25f;

        return Mathf.Clamp01(weightedBigFive) * 27f;
    }

    private int GetMinBlockedNonPowerBuildingConsumption(FactionAIContext _context)
    {
        int minConsumption = int.MaxValue;

        CollectMinBlockedConsumption(buildingProvider.GetEconomyBuildings(), _context, ref minConsumption);
        CollectMinBlockedConsumption(buildingProvider.GetResearchBuildings(), _context, ref minConsumption);
        CollectMinBlockedConsumption(buildingProvider.GetFactoryBuildings(), _context, ref minConsumption);
        CollectMinBlockedConsumption(buildingProvider.GetSupportBuildings(), _context, ref minConsumption);

        return minConsumption == int.MaxValue ? -1 : minConsumption;
    }

    private void CollectMinBlockedConsumption(
        List<BuildingData> _buildings,
        FactionAIContext _context,
        ref int _minConsumption)
    {
        if (_context == null || _context.ownedCities == null || getAvailableBuildingsForSlot == null)
            return;

        for (int i = 0; i < _context.ownedCities.Count; i++)
        {
            CityScript city = _context.ownedCities[i];
            if (city == null || city.cityData == null || city.cityData.owner == null || city.cityData.buildings == null)
                continue;

            if (city.HasAnyBuildingUnderConstruction())
                continue;

            for (int j = 0; j < city.cityData.buildings.Count; j++)
            {
                BuildingInstance slot = city.cityData.buildings[j];
                if (slot == null || slot.IsUnderConstruction() || !city.IsBuildingSlotBuildable(j) || city.IsSlotScavengerLocked(j))
                    continue;

                List<BuildingData> availableBuildings = getAvailableBuildingsForSlot(slot, _buildings, _context);
                for (int k = 0; k < availableBuildings.Count; k++)
                {
                    BuildingData building = availableBuildings[k];
                    if (building == null)
                        continue;

                    if (_context.money < building.constructionCost)
                        continue;

                    int requiredAdditionalPower = GetRequiredAdditionalPower(slot, building);
                    if (requiredAdditionalPower <= 0)
                        continue;

                    if (_context.netPower >= requiredAdditionalPower)
                        continue;

                    if (requiredAdditionalPower < _minConsumption)
                        _minConsumption = requiredAdditionalPower;
                }
            }
        }
    }

    private int GetRequiredAdditionalPower(BuildingInstance _slot, BuildingData _targetBuilding)
    {
        if (_targetBuilding == null)
            return 0;

        int targetPowerConsumption = Mathf.Max(0, _targetBuilding.powerConsumption);
        if (_slot == null || _slot.IsEmptySlot() || _slot.data == null)
            return targetPowerConsumption;

        int currentPowerConsumption = Mathf.Max(0, _slot.data.powerConsumption);
        return Mathf.Max(0, targetPowerConsumption - currentPowerConsumption);
    }

    private float Clamp70(float _value)
    {
        return Mathf.Clamp(_value, 0f, 70f);
    }

    private float GetNoiseScore()
    {
        return UnityEngine.Random.Range(0f, 3f);
    }

    private float GetCandidateRandomOffset()
    {
        return UnityEngine.Random.Range(0f, 0.5f);
    }
}
