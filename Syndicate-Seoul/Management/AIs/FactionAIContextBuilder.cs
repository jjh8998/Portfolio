using System.Collections.Generic;
using UnityEngine;

public static class FactionAIContextBuilder
{
    public static FactionAIContext Build(FactionManager _factionManager)
    {
        return Build(_factionManager, -1);
    }

    public static FactionAIContext Build(FactionManager _factionManager, int _currentMonthIndex)
    {
        FactionAIContext context = new FactionAIContext();

        if (_factionManager == null)
        {
            AIDebugLogger.LogAIWarning("[AI] BuildContext failed: factionManager is null.");
            return context;
        }

        FactionResourceSnapshot snapshot = ManagementResourceCalculator.CalculateFaction(_factionManager);

        context.faction = _factionManager;
        context.money = _factionManager.GetCredit;
        context.totalIncome = snapshot.creditIncome;

        context.powerProduction = snapshot.totalPowerProduction;
        context.powerConsumption = snapshot.totalPowerConsumption;
        context.netPower = _factionManager.GetNetPower;

        context.currentRP = snapshot.totalRP;
        context.currentProduction = ManagementResourceCalculator.CountCompletedFactoryBuildings(_factionManager);
        context.completedFactoryCount = context.currentProduction;
        context.factoryInProgressCount = ManagementResourceCalculator.CountFactoryBuildingsInProgress(_factionManager);
        context.militaryScore = ManagementResourceCalculator.CalculateMilitaryScore(_factionManager);
        context.currentMonthIndex = _currentMonthIndex;

        context.ownedCities = _factionManager.ownedCities != null
            ? new List<CityScript>(_factionManager.ownedCities)
            : new List<CityScript>();

        context.emptySlotCount = ManagementResourceCalculator.CountEmptySlots(_factionManager);
        context.scavengerLockedSlotCount = CountScavengerLockedSlots(context.ownedCities);
        context.completedBuildingCount = CountCompletedBuildings(context.ownedCities);

        int cityCount = context.ownedCities.Count;

        context.lowMoneyThreshold = 100 + cityCount * 20;
        context.targetIncome = 50 + cityCount * 40;
        context.targetRP = 20 + cityCount * 15;
        context.nearEnemyPower = GetNearEnemyPower(_factionManager);

        return context;
    }

    private static int GetNearEnemyPower(FactionManager _factionManager)
    {
        if (_factionManager == null)
            return 0;

        NationAISceneTargetProvider provider = new NationAISceneTargetProvider();
        List<FactionManager> enemies = provider.GetEnemyFactions(_factionManager);

        int bestPower = 0;

        for (int i = 0; i < enemies.Count; i++)
        {
            FactionManager enemy = enemies[i];
            if (enemy == null)
                continue;

            int power = ManagementResourceCalculator.CalculateMilitaryScore(enemy);
            if (power > bestPower)
                bestPower = power;
        }

        return bestPower;
    }

    private static int CountScavengerLockedSlots(List<CityScript> _ownedCities)
    {
        if (_ownedCities == null || _ownedCities.Count == 0)
            return 0;

        int count = 0;
        for (int i = 0; i < _ownedCities.Count; i++)
        {
            CityScript city = _ownedCities[i];
            if (city == null || city.cityData == null || city.cityData.buildings == null)
                continue;

            int slotCount = Mathf.Min(city.GetAvailableBuildingSlotCount(), city.cityData.buildings.Count);
            for (int j = 0; j < slotCount; j++)
            {
                if (city.IsSlotScavengerLocked(j))
                    count++;
            }
        }

        return count;
    }

    private static int CountCompletedBuildings(List<CityScript> _ownedCities)
    {
        if (_ownedCities == null || _ownedCities.Count == 0)
            return 0;

        int count = 0;
        for (int i = 0; i < _ownedCities.Count; i++)
        {
            CityScript city = _ownedCities[i];
            if (city == null || city.cityData == null || city.cityData.buildings == null)
                continue;

            for (int j = 0; j < city.cityData.buildings.Count; j++)
            {
                BuildingInstance building = city.cityData.buildings[j];
                if (building != null && building.IsOperational())
                    count++;
            }
        }

        return count;
    }
}
