using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

[Serializable]
public class CityData
{
    public string cityName;
    public FactionManager owner;
    public CityShareData shareData = new CityShareData();
    public int creditIncome;
    public int totalRPProduction;
    public int totalPowerProduction;
    public int totalPowerConsumption;
    public List<BuildingInstance> buildings;
}
