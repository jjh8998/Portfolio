using System.Collections.Generic;

public class FactionAIActionScorer
{
    private const string CanBuildLogPrefix = "[AI][CanBuild]";

    private struct BuildingCandidateReference
    {
        public CityScript city;
        public int slotIndex;
        public BuildingInstance slot;
        public BuildingData building;
    }

    private sealed class BuildingCategoryEvaluation
    {
        public bool canBuild;
        public bool canBuildNonPower;
        public readonly List<BuildingCandidateReference> candidates = new List<BuildingCandidateReference>();
    }

    private sealed class BuildingEvaluationCache
    {
        public FactionAIContext context;
        public int minBlockedNonPowerBuildingConsumption = -1;
        public readonly BuildingCategoryEvaluation economy = new BuildingCategoryEvaluation();
        public readonly BuildingCategoryEvaluation power = new BuildingCategoryEvaluation();
        public readonly BuildingCategoryEvaluation research = new BuildingCategoryEvaluation();
        public readonly BuildingCategoryEvaluation factory = new BuildingCategoryEvaluation();
        public readonly BuildingCategoryEvaluation support = new BuildingCategoryEvaluation();
    }

    private readonly IFactionAIBuildingProvider buildingProvider;
    private readonly FactionAIDiplomacyScorer diplomacyScorer;
    private readonly FactionAIDevelopmentScorer developmentScorer;
    private readonly FactionAIEconomyScorer economyScorer;
    private readonly FactionAIInfrastructureScorer infrastructureScorer;
    private readonly FactionAIMilitaryScorer militaryScorer;
    private readonly FactionAISupportScorer supportScorer;

    private BuildingEvaluationCache buildingEvaluationCache;

    public FactionAIActionScorer(
        IFactionAIBuildingProvider _buildingProvider,
        INationAITargetProvider _targetProvider)
        : this(_buildingProvider, _targetProvider, null)
    {
    }

    public FactionAIActionScorer(
        IFactionAIBuildingProvider _buildingProvider,
        INationAITargetProvider _targetProvider,
        FactionAITargetCityMemory _targetCityMemory)
    {
        buildingProvider = _buildingProvider;
        diplomacyScorer = new FactionAIDiplomacyScorer(_targetProvider, _targetCityMemory);
        developmentScorer = new FactionAIDevelopmentScorer();
        economyScorer = new FactionAIEconomyScorer();
        infrastructureScorer = new FactionAIInfrastructureScorer(_buildingProvider, GetAvailableBuildingsForSlot);
        militaryScorer = new FactionAIMilitaryScorer();
        supportScorer = new FactionAISupportScorer();
    }

    public List<NationAIActionScore> EvaluateActionScores(BigFivePersonality _personality, FactionAIContext _context)
    {
        List<BuildingData> economyBuildings = buildingProvider.GetEconomyBuildings();
        List<BuildingData> researchBuildings = buildingProvider.GetResearchBuildings();
        List<BuildingData> factoryBuildings = buildingProvider.GetFactoryBuildings();
        List<BuildingData> supportBuildings = buildingProvider.GetSupportBuildings();
        List<BuildingData> powerBuildings = buildingProvider.GetPowerBuildings();

        buildingEvaluationCache = EvaluateBuildingOptions(
            _context,
            economyBuildings,
            powerBuildings,
            researchBuildings,
            factoryBuildings,
            supportBuildings);

        List<NationAIActionScore> scores = new List<NationAIActionScore>
        {
            economyScorer.EvaluateEconomy(_personality, _context, buildingEvaluationCache.economy.canBuild, buildingEvaluationCache.economy.canBuildNonPower),
            infrastructureScorer.EvaluatePower(
                _personality,
                _context,
                buildingEvaluationCache.power.canBuild,
                buildingEvaluationCache.minBlockedNonPowerBuildingConsumption),
            developmentScorer.EvaluateResearch(_personality, _context, buildingEvaluationCache.research.canBuild, buildingEvaluationCache.research.canBuildNonPower),
            militaryScorer.EvaluateFactory(_personality, _context, factoryBuildings, buildingEvaluationCache.factory.canBuild, buildingEvaluationCache.factory.canBuildNonPower),
            supportScorer.EvaluateSupport(_personality, _context, buildingEvaluationCache.support.canBuild, buildingEvaluationCache.support.canBuildNonPower),
            militaryScorer.EvaluateEmergencyOrder(_personality, _context),
            EvaluateScavengerUnlock(_personality, _context),
            diplomacyScorer.EvaluateBuyShare(_personality, _context),
            diplomacyScorer.EvaluateWar(_personality, _context)
        };

        ApplyBuildPressureBonus(scores, _context);
        return scores;
    }

    public NationAIActionCandidate FindBestCandidate(
        NationAIActionType _actionType,
        BigFivePersonality _personality,
        FactionAIContext _context)
    {
        switch (_actionType)
        {
            case NationAIActionType.BuildEconomy:
            case NationAIActionType.BuildPower:
            case NationAIActionType.BuildResearch:
            case NationAIActionType.BuildFactory:
            case NationAIActionType.BuildSupport:
                return FindBestBuildingCandidate(_actionType, _personality, _context);
            case NationAIActionType.EmergencyOrder:
                return FindBestEmergencyOrderCandidate(_personality, _context);
            case NationAIActionType.BuyShare:
                return diplomacyScorer.FindBestCandidate(_actionType, _personality, _context);
            case NationAIActionType.Trade:
                return diplomacyScorer.FindBestCandidate(_actionType, _personality, _context);
            case NationAIActionType.DeclareWar:
                return diplomacyScorer.FindBestCandidate(_actionType, _personality, _context);
            case NationAIActionType.ScavengerUnlock:
                return FindBestScavengerUnlockCandidate(_personality, _context);
        }

        return null;
    }

    public bool ShouldForcePowerBuild(FactionAIContext _context)
    {
        BuildingEvaluationCache cache = EnsureBuildingEvaluationCache(_context);
        return infrastructureScorer.ShouldForcePowerBuild(
            _context,
            cache.minBlockedNonPowerBuildingConsumption);
    }

    private void ApplyBuildPressureBonus(List<NationAIActionScore> scores, FactionAIContext context)
    {
        if (scores == null)
            return;

        float bonus = FactionAIBuildPressureService.EvaluateContextBonus(context);
        if (bonus <= 0f)
            return;

        for (int i = 0; i < scores.Count; i++)
        {
            NationAIActionScore score = scores[i];
            if (score == null || !score.isAvailable)
                continue;

            if (!IsBuildAction(score.actionType))
                continue;

            NationAIScoreParts oldParts = score.scoreParts;
            score.scoreParts = new NationAIScoreParts(
                oldParts.contextScore + bonus,
                oldParts.personalScore,
                oldParts.noiseScore);
            score.score = score.scoreParts.FinalScore;
        }
    }

    private bool IsBuildAction(NationAIActionType actionType)
    {
        return actionType == NationAIActionType.BuildEconomy ||
               actionType == NationAIActionType.BuildPower ||
               actionType == NationAIActionType.BuildResearch ||
               actionType == NationAIActionType.BuildFactory ||
               actionType == NationAIActionType.BuildSupport;
    }

    private NationAIActionScore EvaluateScavengerUnlock(BigFivePersonality _personality, FactionAIContext _context)
    {
        if (_context == null || _context.faction == null)
            return CreateUnavailableScore(NationAIActionType.ScavengerUnlock, "Context is missing.");

        if (!_context.HasScavengerLockedSlot)
            return CreateUnavailableScore(NationAIActionType.ScavengerUnlock, "No scavenger locked slot.");

        int scavengerPowerScore = GetScavengerPowerScore();
        if (_context.militaryScore < scavengerPowerScore)
            return CreateUnavailableScore(NationAIActionType.ScavengerUnlock, "Military score is lower than scavenger power.");

        float contextScore = _context.HasNoBuildableEmptySlotButScavengerLockedSlot ? 52f : 16f;
        contextScore += UnityEngine.Mathf.Clamp(_context.scavengerLockedSlotCount * 5f, 0f, 12f);
        contextScore += UnityEngine.Mathf.Clamp((_context.militaryScore - scavengerPowerScore) * 0.8f, 0f, 12f);

        float personalScore = EvaluateScavengerUnlockPersonalScore(_personality);
        return CreateAvailableScore(
            NationAIActionType.ScavengerUnlock,
            "ScavengerUnlock",
            contextScore,
            personalScore);
    }

    private NationAIActionCandidate FindBestScavengerUnlockCandidate(
        BigFivePersonality _personality,
        FactionAIContext _context)
    {
        if (_context == null || _context.faction == null || _context.ownedCities == null)
            return null;

        if (!_context.HasScavengerLockedSlot)
            return null;

        if (_context.militaryScore < GetScavengerPowerScore())
            return null;

        NationAIActionCandidate bestCandidate = null;
        float bestScore = float.MinValue;

        for (int i = 0; i < _context.ownedCities.Count; i++)
        {
            CityScript city = _context.ownedCities[i];
            if (city == null || city.cityData == null || city.cityData.buildings == null)
                continue;

            if (!ReferenceEquals(city.cityData.owner, _context.faction))
                continue;

            if (city.HasAnyBuildingUnderConstruction())
                continue;

            for (int j = 0; j < city.cityData.buildings.Count; j++)
            {
                BuildingInstance slot = city.cityData.buildings[j];
                if (slot == null || slot.IsUnderConstruction())
                    continue;

                if (!city.IsBuildingSlotBuildable(j) || !city.IsSlotScavengerLocked(j))
                    continue;

                float score = EvaluateScavengerUnlockCandidateScore(city, _context);
                if (score <= bestScore)
                    continue;

                bestScore = score;
                bestCandidate = new NationAIActionCandidate
                {
                    actionType = NationAIActionType.ScavengerUnlock,
                    city = city,
                    slotIndex = j,
                    attackerFaction = _context.faction,
                    score = score,
                    isNewConstruction = false
                };
            }
        }

        return bestCandidate;
    }

    private float EvaluateScavengerUnlockCandidateScore(CityScript _city, FactionAIContext _context)
    {
        if (_city == null)
            return 0f;

        float score = CountBuildableEmptySlots(_city) <= 0 ? 100f : 35f;
        CityResourceSnapshot snapshot = ManagementResourceCalculator.CalculateCity(_city);
        score += UnityEngine.Mathf.Clamp(snapshot.creditIncome * 0.1f, 0f, 12f);
        score += UnityEngine.Mathf.Clamp(snapshot.rpProduction * 0.1f, 0f, 8f);
        score += UnityEngine.Mathf.Clamp(snapshot.netPower * 0.1f, 0f, 8f);
        score += CountCompletedBuildings(_city) * 2f;
        score += _context != null ? UnityEngine.Mathf.Clamp(_context.militaryScore - GetScavengerPowerScore(), 0, 20) * 0.2f : 0f;
        score += UnityEngine.Random.Range(0f, 1f);
        return score;
    }

    private int CountBuildableEmptySlots(CityScript _city)
    {
        if (_city == null || _city.cityData == null || _city.cityData.buildings == null)
            return 0;

        int count = 0;
        int slotCount = UnityEngine.Mathf.Min(_city.GetAvailableBuildingSlotCount(), _city.cityData.buildings.Count);
        for (int i = 0; i < slotCount; i++)
        {
            BuildingInstance slot = _city.cityData.buildings[i];
            if (slot == null || !slot.IsEmptySlot() || slot.IsUnderConstruction())
                continue;

            if (!_city.IsBuildingSlotBuildable(i) || _city.IsSlotScavengerLocked(i))
                continue;

            count++;
        }

        return count;
    }

    private int CountCompletedBuildings(CityScript _city)
    {
        if (_city == null || _city.cityData == null || _city.cityData.buildings == null)
            return 0;

        int count = 0;
        for (int i = 0; i < _city.cityData.buildings.Count; i++)
        {
            BuildingInstance building = _city.cityData.buildings[i];
            if (building != null && building.IsOperational())
                count++;
        }

        return count;
    }

    private int GetScavengerPowerScore()
    {
        AIWarResolver resolver = UnityEngine.Object.FindFirstObjectByType<AIWarResolver>();
        if (resolver != null)
            return resolver.ScavengerPowerScore;

        return AIWarResolver.DefaultScavengerPowerScore;
    }

    private float EvaluateScavengerUnlockPersonalScore(BigFivePersonality _personality)
    {
        if (_personality == null)
            return 0f;

        float weightedBigFive =
            _personality.Extraversion01 * 0.6f +
            (1f - _personality.Agreeableness01) * 0.4f;

        return UnityEngine.Mathf.Clamp01(weightedBigFive) * 12f;
    }

    private NationAIActionScore CreateAvailableScore(
        NationAIActionType _actionType,
        string _categoryName,
        float _contextScore,
        float _personalScore)
    {
        NationAIScoreParts scoreParts = new NationAIScoreParts(_contextScore, _personalScore, UnityEngine.Random.Range(0f, 3f));

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

    private NationAIActionCandidate FindBestBuildingCandidate(
        NationAIActionType _actionType,
        BigFivePersonality _personality,
        FactionAIContext _context)
    {
        BuildingEvaluationCache cache = EnsureBuildingEvaluationCache(_context);
        BuildingCategoryEvaluation category = GetBuildingCategoryEvaluation(cache, _actionType);
        if (category == null || category.candidates.Count == 0)
            return null;

        NationAIActionCandidate bestCandidate = null;
        float bestScore = float.MinValue;

        for (int i = 0; i < category.candidates.Count; i++)
        {
            BuildingCandidateReference candidate = category.candidates[i];
            float score = EvaluateBuildCandidateScore(
                _actionType,
                candidate.city,
                candidate.building,
                _personality,
                _context,
                cache.minBlockedNonPowerBuildingConsumption);
            if (score <= bestScore)
                continue;

            bestScore = score;
            bestCandidate = new NationAIActionCandidate
            {
                actionType = _actionType,
                city = candidate.city,
                slotIndex = candidate.slotIndex,
                building = candidate.building,
                attackerFaction = _context.faction,
                score = score,
                isNewConstruction = candidate.slot.IsEmptySlot()
            };
        }

        return bestCandidate;
    }

    private NationAIActionCandidate FindBestEmergencyOrderCandidate(
        BigFivePersonality _personality,
        FactionAIContext _context)
    {
        if (_context == null || _context.faction == null || _context.ownedCities == null)
            return null;

        if (!militaryScorer.CanUseEmergencyOrderByInventory(_context))
            return null;

        NationAIActionCandidate bestCandidate = null;
        float bestScore = float.MinValue;

        for (int i = 0; i < _context.ownedCities.Count; i++)
        {
            CityScript city = _context.ownedCities[i];
            if (city == null || city.cityData == null || city.cityData.buildings == null)
                continue;

            for (int j = 0; j < city.cityData.buildings.Count; j++)
            {
                BuildingInstance building = city.cityData.buildings[j];
                if (!militaryScorer.IsEmergencyOrderFactory(building))
                    continue;

                EmergencyOrderState state = EmergencyOrderService.GetState(city, j, _context.faction);
                if (state == null || !state.canExecute)
                    continue;

                float score = militaryScorer.EvaluateEmergencyOrderCandidateScore(building.data, _context);
                if (score <= bestScore)
                    continue;

                bestScore = score;
                bestCandidate = new NationAIActionCandidate
                {
                    actionType = NationAIActionType.EmergencyOrder,
                    city = city,
                    slotIndex = j,
                    building = building.data,
                    attackerFaction = _context.faction,
                    score = score,
                    isNewConstruction = false
                };
            }
        }

        return bestCandidate;
    }

    private float EvaluateBuildCandidateScore(
        NationAIActionType _actionType,
        CityScript _city,
        BuildingData _building,
        BigFivePersonality _personality,
        FactionAIContext _context,
        int _minBlockedNonPowerBuildingConsumption)
    {
        if (_actionType == NationAIActionType.BuildFactory)
            return militaryScorer.EvaluateBuildCandidateScore(_building, _personality, _context);

        if (_actionType == NationAIActionType.BuildPower)
            return infrastructureScorer.EvaluateBuildCandidateScore(
                _building,
                _personality,
                _context,
                _minBlockedNonPowerBuildingConsumption);

        if (_actionType == NationAIActionType.BuildEconomy)
            return economyScorer.EvaluateBuildCandidateScore(_city, _building, _personality, _context);

        if (_actionType == NationAIActionType.BuildResearch)
            return developmentScorer.EvaluateBuildCandidateScore(_building, _personality, _context);

        if (_actionType == NationAIActionType.BuildSupport)
            return supportScorer.EvaluateBuildCandidateScore(_building, _personality, _context);

        return 0f;
    }

    private BuildingEvaluationCache EnsureBuildingEvaluationCache(FactionAIContext _context)
    {
        if (buildingEvaluationCache != null && ReferenceEquals(buildingEvaluationCache.context, _context))
            return buildingEvaluationCache;

        buildingEvaluationCache = EvaluateBuildingOptions(
            _context,
            buildingProvider.GetEconomyBuildings(),
            buildingProvider.GetPowerBuildings(),
            buildingProvider.GetResearchBuildings(),
            buildingProvider.GetFactoryBuildings(),
            buildingProvider.GetSupportBuildings());
        return buildingEvaluationCache;
    }

    private BuildingEvaluationCache EvaluateBuildingOptions(
        FactionAIContext _context,
        List<BuildingData> _economyBuildings,
        List<BuildingData> _powerBuildings,
        List<BuildingData> _researchBuildings,
        List<BuildingData> _factoryBuildings,
        List<BuildingData> _supportBuildings)
    {
        BuildingEvaluationCache result = new BuildingEvaluationCache
        {
            context = _context
        };

        if (_context == null)
        {
            AIDebugLogger.LogAI((FactionManager)null, $"{CanBuildLogPrefix} context is null.");
            return result;
        }

        if (_context.faction == null)
        {
            AIDebugLogger.LogAI((FactionManager)null, $"{CanBuildLogPrefix} faction is null.");
            return result;
        }

        if (_context.ownedCities == null)
        {
            AIDebugLogger.LogAI(_context.faction, $"{CanBuildLogPrefix} ownedCities is null.");
            return result;
        }

        if (_context.ownedCities.Count == 0)
        {
            AIDebugLogger.LogAI(_context.faction, $"{CanBuildLogPrefix} ownedCities count is 0.");
            return result;
        }

        int minBlockedConsumption = int.MaxValue;

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

                CollectBuildingCandidates(
                    result.economy,
                    NationAIActionType.BuildEconomy,
                    city,
                    j,
                    slot,
                    _economyBuildings,
                    _context,
                    true,
                    ref minBlockedConsumption);
                CollectBuildingCandidates(
                    result.power,
                    NationAIActionType.BuildPower,
                    city,
                    j,
                    slot,
                    _powerBuildings,
                    _context,
                    false,
                    ref minBlockedConsumption);
                CollectBuildingCandidates(
                    result.research,
                    NationAIActionType.BuildResearch,
                    city,
                    j,
                    slot,
                    _researchBuildings,
                    _context,
                    true,
                    ref minBlockedConsumption);
                CollectBuildingCandidates(
                    result.factory,
                    NationAIActionType.BuildFactory,
                    city,
                    j,
                    slot,
                    _factoryBuildings,
                    _context,
                    true,
                    ref minBlockedConsumption);
                CollectBuildingCandidates(
                    result.support,
                    NationAIActionType.BuildSupport,
                    city,
                    j,
                    slot,
                    _supportBuildings,
                    _context,
                    true,
                    ref minBlockedConsumption);
            }
        }

        result.minBlockedNonPowerBuildingConsumption = minBlockedConsumption == int.MaxValue
            ? -1
            : minBlockedConsumption;
        return result;
    }

    private void CollectBuildingCandidates(
        BuildingCategoryEvaluation _category,
        NationAIActionType _actionType,
        CityScript _city,
        int _slotIndex,
        BuildingInstance _slot,
        List<BuildingData> _buildings,
        FactionAIContext _context,
        bool _collectBlockedPowerConsumption,
        ref int _minBlockedConsumption)
    {
        List<BuildingData> availableBuildings = GetAvailableBuildingsForSlot(_slot, _buildings, _context);
        for (int i = 0; i < availableBuildings.Count; i++)
        {
            BuildingData building = availableBuildings[i];

            if (_collectBlockedPowerConsumption && building != null && _context.money >= building.constructionCost)
            {
                int requiredAdditionalPower = GetRequiredAdditionalPower(_slot, building);
                if (requiredAdditionalPower > 0 &&
                    _context.netPower < requiredAdditionalPower &&
                    requiredAdditionalPower < _minBlockedConsumption)
                {
                    _minBlockedConsumption = requiredAdditionalPower;
                }
            }

            if (!CanUseBuildingCandidate(_actionType, _context, _slot, building, false))
                continue;

            _category.canBuild = true;
            if (!CanUseBuildingCandidate(_actionType, _context, _slot, building, true))
                continue;

            _category.canBuildNonPower = true;
            _category.candidates.Add(new BuildingCandidateReference
            {
                city = _city,
                slotIndex = _slotIndex,
                slot = _slot,
                building = building
            });
        }
    }

    private BuildingCategoryEvaluation GetBuildingCategoryEvaluation(
        BuildingEvaluationCache _cache,
        NationAIActionType _actionType)
    {
        if (_cache == null)
            return null;

        switch (_actionType)
        {
            case NationAIActionType.BuildEconomy: return _cache.economy;
            case NationAIActionType.BuildPower: return _cache.power;
            case NationAIActionType.BuildResearch: return _cache.research;
            case NationAIActionType.BuildFactory: return _cache.factory;
            case NationAIActionType.BuildSupport: return _cache.support;
            default: return null;
        }
    }

    private string GetCityLogName(CityScript _city)
    {
        if (_city != null && _city.cityData != null && !string.IsNullOrWhiteSpace(_city.cityData.cityName))
            return _city.cityData.cityName;

        return _city != null ? _city.name : "Unknown City";
    }

    private List<BuildingData> GetAvailableBuildingsForSlot(BuildingInstance _slot, List<BuildingData> _allBuildings, FactionAIContext _context)
    {
        if (_slot == null)
            return new List<BuildingData>();

        if (_slot.IsEmptySlot())
            return GetRootBuildings(_allBuildings, _context);

        return GetValidUpgradeBuildings(_slot.data, _allBuildings, _context);
    }

    private List<BuildingData> GetRootBuildings(List<BuildingData> _allBuildings, FactionAIContext _context)
    {
        List<BuildingData> result = new List<BuildingData>();
        if (_allBuildings == null || _allBuildings.Count == 0)
            return result;

        HashSet<string> childIds = new HashSet<string>();

        for (int i = 0; i < _allBuildings.Count; i++)
        {
            BuildingData building = _allBuildings[i];
            if (building == null || building.nextUpgradeBuildings == null)
                continue;

            for (int j = 0; j < building.nextUpgradeBuildings.Count; j++)
            {
                BuildingData child = building.nextUpgradeBuildings[j];
                if (child != null && !string.IsNullOrEmpty(child.ID))
                    childIds.Add(child.ID);
            }
        }

        for (int i = 0; i < _allBuildings.Count; i++)
        {
            BuildingData building = _allBuildings[i];
            if (building != null && !childIds.Contains(building.ID) && IsResearchUnlocked(_context, building))
                result.Add(building);
        }

        if (result.Count == 0 && _allBuildings.Count > 0)
        {
            BuildingData fallback = _allBuildings[0];
            if (IsResearchUnlocked(_context, fallback))
                result.Add(fallback);
        }

        return result;
    }

    private List<BuildingData> GetValidUpgradeBuildings(BuildingData _currentBuilding, List<BuildingData> _allBuildings, FactionAIContext _context)
    {
        List<BuildingData> result = new List<BuildingData>();
        if (_currentBuilding == null || _currentBuilding.nextUpgradeBuildings == null)
            return result;

        HashSet<string> validIds = new HashSet<string>();

        if (_allBuildings != null)
        {
            for (int i = 0; i < _allBuildings.Count; i++)
            {
                BuildingData building = _allBuildings[i];
                if (building != null && !string.IsNullOrEmpty(building.ID))
                    validIds.Add(building.ID);
            }
        }

        for (int i = 0; i < _currentBuilding.nextUpgradeBuildings.Count; i++)
        {
            BuildingData upgrade = _currentBuilding.nextUpgradeBuildings[i];
            if (upgrade == null)
                continue;

            if (validIds.Count > 0 && !string.IsNullOrEmpty(upgrade.ID) && !validIds.Contains(upgrade.ID))
                continue;

            if (!IsResearchUnlocked(_context, upgrade))
                continue;

            result.Add(upgrade);
        }

        return result;
    }

    private bool IsResearchUnlocked(FactionAIContext _context, BuildingData _building)
    {
        if (_building == null)
            return false;

        if (string.IsNullOrWhiteSpace(_building.requiredResearchId))
            return true;

        if (_context == null || _context.faction == null)
            return false;

        return _context.faction.HasCompletedResearch(_building.requiredResearchId);
    }

    private bool CanUseBuildingCandidate(
        NationAIActionType _actionType,
        FactionAIContext _context,
        BuildingInstance _slot,
        BuildingData _building,
        bool _requirePowerCheck)
    {
        if (_context == null || _building == null)
            return false;

        if (FactionAIBuildingRestrictionService.IsDisabledForAI(_building))
            return false;

        if (_context.money < _building.constructionCost)
            return false;

        if (_requirePowerCheck &&
            RequiresNonPowerBuildablePowerCheck(_actionType) &&
            _context.netPower < GetRequiredAdditionalPower(_slot, _building))
        {
            return false;
        }

        return true;
    }

    private int GetRequiredAdditionalPower(BuildingInstance _slot, BuildingData _targetBuilding)
    {
        if (_targetBuilding == null)
            return 0;

        int targetPowerConsumption = UnityEngine.Mathf.Max(0, _targetBuilding.powerConsumption);
        if (_slot == null || _slot.IsEmptySlot() || _slot.data == null)
            return targetPowerConsumption;

        int currentPowerConsumption = UnityEngine.Mathf.Max(0, _slot.data.powerConsumption);
        return UnityEngine.Mathf.Max(0, targetPowerConsumption - currentPowerConsumption);
    }

    private bool RequiresNonPowerBuildablePowerCheck(NationAIActionType _actionType)
    {
        return _actionType == NationAIActionType.BuildEconomy ||
               _actionType == NationAIActionType.BuildResearch ||
               _actionType == NationAIActionType.BuildFactory ||
               _actionType == NationAIActionType.BuildSupport;
    }

}

public static class FactionAIBuildPressureService
{
    private const float BonusPerEmptySlot = 8f;
    private const float MaxBuildPressureBonus = 30f;

    public static float EvaluateContextBonus(FactionAIContext context)
    {
        if (context == null || context.emptySlotCount <= 0)
            return 0f;

        return UnityEngine.Mathf.Clamp(context.emptySlotCount * BonusPerEmptySlot, 0f, MaxBuildPressureBonus);
    }
}

public class FactionAISupportScorer
{
    public NationAIActionScore EvaluateSupport(
        BigFivePersonality _personality,
        FactionAIContext _context,
        bool _canBuild,
        bool _canBuildNonPowerCategory)
    {
        if (!_canBuild)
            return CreateUnavailableScore(NationAIActionType.BuildSupport, "No support build available.");

        if (!_canBuildNonPowerCategory)
            return CreateUnavailableScore(NationAIActionType.BuildSupport, "Not enough power to build any support building.");

        float contextScore = EvaluateSupportContextScore(_context);
        float personalScore = EvaluatePersonalScore(_personality);
        return CreateAvailableScore(NationAIActionType.BuildSupport, "Support", contextScore, personalScore);
    }

    public float EvaluateBuildCandidateScore(
        BuildingData _building,
        BigFivePersonality _personality,
        FactionAIContext _context)
    {
        float score = 10f;
        score -= _building.constructionCost * 0.05f;
        score -= _building.constructionDay * 0.03f;
        score -= _building.powerConsumption * 0.8f;
        score += UnityEngine.Mathf.Clamp(_context.nearEnemyPower, 0, 100) * 0.20f;
        score += UnityEngine.Mathf.Clamp(_context.nearEnemyPower - _context.militaryScore, 0, 100) * 0.15f;
        score += _personality.Extraversion01 * 4f;
        score += _personality.Openness01 * 4f;
        score += UnityEngine.Random.Range(0f, 0.5f);
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

    private float EvaluateSupportContextScore(FactionAIContext _context)
    {
        if (_context == null)
            return 0f;

        float threatScore = UnityEngine.Mathf.Clamp(_context.nearEnemyPower * 0.5f, 0f, 50f);
        float militaryGapScore = UnityEngine.Mathf.Clamp((_context.nearEnemyPower - _context.militaryScore) * 0.4f, 0f, 20f);
        float emptySlotScore = _context.HasEmptySlot ? 10f : 0f;
        return Clamp70(threatScore + militaryGapScore + emptySlotScore);
    }

    private float EvaluatePersonalScore(BigFivePersonality _personality)
    {
        float weightedBigFive =
            _personality.Extraversion01 * 0.55f +
            _personality.Openness01 * 0.45f;

        return UnityEngine.Mathf.Clamp01(weightedBigFive) * 27f;
    }

    private float Clamp70(float _value)
    {
        return UnityEngine.Mathf.Clamp(_value, 0f, 70f);
    }

    private float GetNoiseScore()
    {
        return UnityEngine.Random.Range(0f, 3f);
    }
}
