using UnityEngine;

public readonly struct CityResourceSnapshot
{
    public readonly int creditIncome;
    public readonly int rpProduction;
    public readonly int powerProduction;
    public readonly int powerConsumption;

    public int netPower => powerProduction - powerConsumption;

    public CityResourceSnapshot(int _creditIncome, int _rpProduction, int _powerProduction, int _powerConsumption)
    {
        creditIncome = _creditIncome;
        rpProduction = _rpProduction;
        powerProduction = _powerProduction;
        powerConsumption = _powerConsumption;
    }
}

public readonly struct FactionResourceSnapshot
{
    public readonly int creditIncome;
    public readonly int totalRP;
    public readonly int rawCreditIncome;
    public readonly int rawTotalRP;
    public readonly int totalPowerProduction;
    public readonly int totalPowerConsumption;
    public readonly float powerSatisfactionRate;

    public int netPower => totalPowerProduction - totalPowerConsumption;

    public FactionResourceSnapshot(int _creditIncome, int _totalRP, int _totalPowerProduction, int _totalPowerConsumption)
        : this(_creditIncome, _totalRP, _totalPowerProduction, _totalPowerConsumption, ManagementResourceCalculator.CalculatePowerSatisfactionRate(_totalPowerProduction, _totalPowerConsumption))
    {
    }

    public FactionResourceSnapshot(int _rawCreditIncome, int _rawTotalRP, int _totalPowerProduction, int _totalPowerConsumption, float _powerSatisfactionRate)
    {
        rawCreditIncome = Mathf.Max(0, _rawCreditIncome);
        rawTotalRP = Mathf.Max(0, _rawTotalRP);
        totalPowerProduction = _totalPowerProduction;
        totalPowerConsumption = _totalPowerConsumption;
        powerSatisfactionRate = Mathf.Clamp01(_powerSatisfactionRate);
        creditIncome = Mathf.Max(0, Mathf.FloorToInt(rawCreditIncome * powerSatisfactionRate));
        totalRP = Mathf.Max(0, Mathf.FloorToInt(rawTotalRP * powerSatisfactionRate));
    }
}

public static class ManagementResourceCalculator
{
    private const int BaseCityCreditIncome = 10;
    private const int BaseCityPower = 10;
    private const float OwnedEmployeeProductionBonusRate = 0.1f;
    private const float FinancierCreditBonusRate = 0.1f;
    private const float ResearcherRpBonusRate = 0.1f;

    public static float CalculatePowerSatisfactionRate(int _totalPowerProduction, int _totalPowerConsumption)
    {
        if (_totalPowerConsumption <= 0)
            return 1f;

        return Mathf.Clamp01((float)_totalPowerProduction / _totalPowerConsumption);
    }

    public static float CalculatePowerSatisfactionRate(FactionManager _faction)
    {
        if (_faction == null)
            return 1f;

        return CalculatePowerSatisfactionRate(
            _faction.GetTotalPowerProduction + _faction.GetTradePowerOffset,
            _faction.GetTotalPowerConsumption);
    }

    public static CityResourceSnapshot CalculateCity(CityData _cityData)
    {
        int slotLimit = _cityData != null && _cityData.buildings != null ? _cityData.buildings.Count : 0;
        return CalculateCity(_cityData, slotLimit, null);
    }

    public static CityResourceSnapshot CalculateCity(CityScript _city)
    {
        if (_city == null)
            return default;

        return CalculateCity(_city.cityData, _city.GetAvailableBuildingSlotCount(), _city);
    }

    private static CityResourceSnapshot CalculateCity(CityData _cityData, int _slotLimit, CityScript _city)
    {
        if (_cityData == null)
            return default;

        int creditIncome = BaseCityCreditIncome + _cityData.creditIncome;
        int rpProduction = 0;
        int powerProduction = BaseCityPower;
        int powerConsumption = 0;
        int buildingPowerReduction = 0;

        if (_cityData.owner != null)
        {
            FactionResearchState researchState = _cityData.owner.GetResearchState;
            if (researchState != null && researchState.modifiers != null)
                buildingPowerReduction = Mathf.Max(0, researchState.modifiers.buildingPowerReduction);
        }

        if (_cityData.buildings == null)
            return new CityResourceSnapshot(creditIncome, rpProduction, powerProduction, powerConsumption);

        int count = Mathf.Min(Mathf.Max(0, _slotLimit), _cityData.buildings.Count);
        for (int i = 0; i < count; i++)
        {
            BuildingInstance building = _cityData.buildings[i];
            if (building == null)
                continue;

            BuildingData powerData = GetPowerCalculationData(building);
            if (powerData != null)
            {
                powerProduction += powerData.powerOutput;

                int reducedPowerConsumption = Mathf.Max(0, powerData.powerConsumption - buildingPowerReduction);
                powerConsumption += reducedPowerConsumption;
            }

            if (!building.IsOperational())
                continue;

            creditIncome += building.data.bonusIncome;
            rpProduction += building.data.rpOutput;
        }

        bool hasOwnedEmployee = HasOwnedEmployeeProductionBonus(_city);
        if (hasOwnedEmployee)
        {
            float creditBonusRate = OwnedEmployeeProductionBonusRate;
            if (HasOwnedEmployeeAbilityBonus(_city, EmployeeAbilityType.Financier))
                creditBonusRate += FinancierCreditBonusRate;

            float rpBonusRate = OwnedEmployeeProductionBonusRate;
            if (HasOwnedEmployeeAbilityBonus(_city, EmployeeAbilityType.Researcher))
                rpBonusRate += ResearcherRpBonusRate;

            creditIncome = ApplyProductionBonus(creditIncome, creditBonusRate);
            rpProduction = ApplyProductionBonus(rpProduction, rpBonusRate);
            powerProduction = ApplyProductionBonus(powerProduction, OwnedEmployeeProductionBonusRate);
        }

        return new CityResourceSnapshot(creditIncome, rpProduction, powerProduction, powerConsumption);
    }

    private static bool HasOwnedEmployeeProductionBonus(CityScript _city)
    {
        if (_city == null || _city.cityData == null || _city.cityData.owner == null)
            return false;

        return _city.cityData.owner.HasAssignedEmployeeInOwnedCity(_city);
    }

    private static bool HasOwnedEmployeeAbilityBonus(CityScript _city, EmployeeAbilityType _abilityType)
    {
        if (_city == null || _city.cityData == null || _city.cityData.owner == null)
            return false;

        return _city.cityData.owner.HasAssignedEmployeeAbilityInOwnedCity(_city, _abilityType);
    }

    private static int ApplyProductionBonus(int _value, float _bonusRate)
    {
        return Mathf.FloorToInt(Mathf.Max(0, _value) * (1f + Mathf.Max(0f, _bonusRate)));
    }

    private static BuildingData GetPowerCalculationData(BuildingInstance building)
    {
        if (building == null || building.IsEmptySlot() || building.IsDisabled())
            return null;

        if (building.IsUpgrading() && building.upgradeTargetData != null)
            return building.upgradeTargetData;

        return building.data;
    }

    public static FactionResourceSnapshot CalculateFaction(FactionManager _faction)
    {
        if (_faction == null || _faction.ownedCities == null)
            return default;

        int rawCreditIncome = 0;
        int rawTotalRP = 0;
        int totalPowerProduction = 0;
        int totalPowerConsumption = 0;

        for (int i = 0; i < _faction.ownedCities.Count; i++)
        {
            CityScript city = _faction.ownedCities[i];
            if (city == null || city.cityData == null)
                continue;

            CityResourceSnapshot citySnapshot = CalculateCity(city);

            rawCreditIncome += citySnapshot.creditIncome;
            rawTotalRP += citySnapshot.rpProduction;
            totalPowerProduction += citySnapshot.powerProduction;
            totalPowerConsumption += citySnapshot.powerConsumption;
        }

        FactionResearchState researchState = _faction.GetResearchState;
        if (researchState != null && researchState.modifiers != null)
            rawTotalRP += Mathf.Max(0, researchState.modifiers.rpIncomeBonus);

        float powerSatisfactionRate = CalculatePowerSatisfactionRate(
            totalPowerProduction + _faction.GetTradePowerOffset,
            totalPowerConsumption);

        return new FactionResourceSnapshot(rawCreditIncome, rawTotalRP, totalPowerProduction, totalPowerConsumption, powerSatisfactionRate);
    }

    public static bool HasAnyConstruction(FactionManager _faction)
    {
        if (_faction == null || _faction.ownedCities == null)
            return false;

        for (int i = 0; i < _faction.ownedCities.Count; i++)
        {
            CityScript city = _faction.ownedCities[i];
            if (city == null || city.cityData == null || city.cityData.buildings == null)
                continue;

            int slotCount = Mathf.Min(city.GetAvailableBuildingSlotCount(), city.cityData.buildings.Count);
            for (int j = 0; j < slotCount; j++)
            {
                BuildingInstance building = city.cityData.buildings[j];
                if (building != null && building.IsUnderConstruction())
                    return true;
            }
        }

        return false;
    }

    public static int CountEmptySlots(FactionManager _faction)
    {
        if (_faction == null || _faction.ownedCities == null)
            return 0;

        int count = 0;

        for (int i = 0; i < _faction.ownedCities.Count; i++)
        {
            CityScript city = _faction.ownedCities[i];
            if (city == null || city.cityData == null || city.cityData.buildings == null)
                continue;

            int slotCount = Mathf.Min(city.GetAvailableBuildingSlotCount(), city.cityData.buildings.Count);
            for (int j = 0; j < slotCount; j++)
            {
                BuildingInstance building = city.cityData.buildings[j];
                if (building != null && building.IsEmptySlot())
                    count++;
            }
        }

        return count;
    }

    public static int CountCompletedFactoryBuildings(FactionManager _faction)
    {
        if (_faction == null || _faction.ownedCities == null)
            return 0;

        int count = 0;

        for (int i = 0; i < _faction.ownedCities.Count; i++)
        {
            CityScript city = _faction.ownedCities[i];
            if (city == null || city.cityData == null || city.cityData.buildings == null)
                continue;

            int slotCount = Mathf.Min(city.GetAvailableBuildingSlotCount(), city.cityData.buildings.Count);
            for (int j = 0; j < slotCount; j++)
            {
                BuildingInstance building = city.cityData.buildings[j];
                if (building == null || !building.IsOperational())
                    continue;

                if (building.data != null && building.data.IsFactory())
                    count++;
            }
        }

        return count;
    }

    public static int CountFactoryBuildingsInProgress(FactionManager _faction)
    {
        if (_faction == null || _faction.ownedCities == null)
            return 0;

        int count = 0;

        for (int i = 0; i < _faction.ownedCities.Count; i++)
        {
            CityScript city = _faction.ownedCities[i];
            if (city == null || city.cityData == null || city.cityData.buildings == null)
                continue;

            int slotCount = Mathf.Min(city.GetAvailableBuildingSlotCount(), city.cityData.buildings.Count);
            for (int j = 0; j < slotCount; j++)
            {
                BuildingInstance building = city.cityData.buildings[j];
                if (building == null || !building.IsUnderConstruction() || building.IsUpgrading())
                    continue;

                if (building.data != null && building.data.IsFactory())
                    count++;
            }
        }

        return count;
    }

    public static int CalculateMilitaryScore(FactionManager _faction)
    {
        if (_faction == null)
            return 0;

        CardInventory inventory = _faction.GetCardInventory();
        if (inventory == null)
            return 0;

        CardDatabase cardDatabase = CardDatabase.Instance;
        if (cardDatabase == null || !cardDatabase.IsLoaded)
            return 0;

        System.Collections.Generic.Dictionary<string, int> cardCounts = inventory.GetCardCountMapForReadOnlyUse();
        if (cardCounts == null)
            return 0;

        int militaryScore = 0;
        foreach (System.Collections.Generic.KeyValuePair<string, int> pair in cardCounts)
        {
            if (string.IsNullOrWhiteSpace(pair.Key) || pair.Value <= 0)
                continue;

            CardData card = cardDatabase.GetById(pair.Key);
            if (card == null)
                continue;

            int cardScore = Mathf.Clamp(card.tier, 1, 5);
            militaryScore += cardScore * pair.Value;
        }

        return militaryScore;
    }

    public static void ApplyCitySnapshot(CityData _cityData, CityResourceSnapshot _snapshot)
    {
        if (_cityData == null)
            return;

        _cityData.totalRPProduction = _snapshot.rpProduction;
        _cityData.totalPowerProduction = _snapshot.powerProduction;
        _cityData.totalPowerConsumption = _snapshot.powerConsumption;
    }
}

public static class CityValueCalculator
{
    private const int BaseCityValue = 100;
    private const int CreditProductionValueWeight = 2;
    private const int ResearchProductionValueWeight = 2;
    private const int NetPowerValueWeight = 2;
    private const int BuildingTierValueWeight = 30;

    public static int CalculateCityValue(CityScript _city)
    {
        if (_city == null)
            return 0;

        CityResourceSnapshot snapshot = ManagementResourceCalculator.CalculateCity(_city);
        int slotLimit = _city.cityData != null && _city.cityData.buildings != null
            ? Mathf.Min(_city.GetAvailableBuildingSlotCount(), _city.cityData.buildings.Count)
            : 0;

        return CalculateCityValue(_city.cityData, snapshot, slotLimit);
    }

    public static int CalculateCityValue(CityData _cityData)
    {
        if (_cityData == null)
            return 0;

        CityResourceSnapshot snapshot = ManagementResourceCalculator.CalculateCity(_cityData);
        int slotLimit = _cityData.buildings != null ? _cityData.buildings.Count : 0;
        return CalculateCityValue(_cityData, snapshot, slotLimit);
    }

    public static int CalculateShareValue(CityScript _city, int _sharePercent)
    {
        return CalculateShareValue(CalculateCityValue(_city), _sharePercent);
    }

    public static int CalculateShareValue(CityData _cityData, int _sharePercent)
    {
        return CalculateShareValue(CalculateCityValue(_cityData), _sharePercent);
    }

    public static int CalculateShareValue(int _cityValue, int _sharePercent)
    {
        int sharePercent = Mathf.Clamp(_sharePercent, 0, 100);
        return Mathf.Max(0, _cityValue) * sharePercent / 100;
    }

    private static int CalculateCityValue(CityData cityData, CityResourceSnapshot snapshot, int slotLimit)
    {
        if (cityData == null)
            return 0;

        int cityValue = BaseCityValue;
        cityValue += snapshot.creditIncome * CreditProductionValueWeight;
        cityValue += snapshot.rpProduction * ResearchProductionValueWeight;
        cityValue += snapshot.netPower * NetPowerValueWeight;
        cityValue += CalculateCompletedBuildingTierValue(cityData, slotLimit);

        return Mathf.Max(0, cityValue);
    }

    private static int CalculateCompletedBuildingTierValue(CityData cityData, int slotLimit)
    {
        if (cityData == null || cityData.buildings == null)
            return 0;

        int buildingValue = 0;
        int count = Mathf.Min(Mathf.Max(0, slotLimit), cityData.buildings.Count);
        for (int i = 0; i < count; i++)
        {
            BuildingInstance building = cityData.buildings[i];
            if (building == null || building.data == null)
                continue;

            if (building.IsEmptySlot() || !building.IsOperational())
                continue;

            int tier = building.data.GetTierOrDefault();
            buildingValue += BuildingTierValueWeight * Mathf.Max(1, tier);
        }

        return buildingValue;
    }
}
