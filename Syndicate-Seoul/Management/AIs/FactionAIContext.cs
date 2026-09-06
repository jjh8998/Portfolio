using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FactionAIContext
{
    public FactionManager faction;

    public int money;
    public int totalIncome;

    public int powerProduction;
    public int powerConsumption;
    public int netPower;

    public int militaryScore;

    public int emptySlotCount;
    public int scavengerLockedSlotCount;
    public int completedBuildingCount;
    public int completedFactoryCount;
    public int factoryInProgressCount;

    public int lowMoneyThreshold;
    public int targetIncome;
    public int currentRP;
    public int targetRP;
    public int currentProduction;
    public int nearEnemyPower;
    public int currentMonthIndex = -1;

    public List<CityScript> ownedCities = new List<CityScript>();

    public bool HasEmptySlot => emptySlotCount > 0;
    public bool HasScavengerLockedSlot => scavengerLockedSlotCount > 0;
    public int BuildableEmptySlotCount => Mathf.Max(0, emptySlotCount - scavengerLockedSlotCount);
    public bool HasNoBuildableEmptySlotButScavengerLockedSlot => BuildableEmptySlotCount <= 0 && HasScavengerLockedSlot;
    public bool HasFactoryOrFactoryInProgress => completedFactoryCount + factoryInProgressCount > 0;
    public int OwnedCityCount => ownedCities != null ? ownedCities.Count : 0;
    public int PowerDeficit => Mathf.Max(0, powerConsumption - powerProduction);
    public int PowerSurplus => Mathf.Max(0, powerProduction - powerConsumption);
}
