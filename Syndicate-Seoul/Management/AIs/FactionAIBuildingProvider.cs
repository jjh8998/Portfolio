using System.Collections.Generic;
using UnityEngine;

public interface IFactionAIBuildingProvider
{
    List<BuildingData> GetEconomyBuildings();
    List<BuildingData> GetPowerBuildings();
    List<BuildingData> GetResearchBuildings();
    List<BuildingData> GetFactoryBuildings();
    List<BuildingData> GetSupportBuildings();
}

public class FactionAIBuildingProvider : IFactionAIBuildingProvider
{
    private readonly EconomicBuildingDatabaseSO economicDb;
    private readonly PowerBuildingDatabaseSO powerDb;
    private readonly ResearchBuildingDatabaseSO researchDb;
    private readonly FactoryBuildingDatabaseSO factoryDb;
    private readonly SupportBuildingDatabaseSO supportDb;

    private List<BuildingData> cachedFactoryBuildings;
    private List<BuildingData> cachedSupportBuildings;

    public FactionAIBuildingProvider()
    {
        economicDb = FactionBuildingDatabase.GetEconomicDb();
        powerDb = FactionBuildingDatabase.GetPowerDb();
        researchDb = FactionBuildingDatabase.GetResearchDb();
        factoryDb = FactionBuildingDatabase.GetFactoryDb();
        supportDb = FactionBuildingDatabase.GetSupportDb();
    }

    public List<BuildingData> GetEconomyBuildings()
    {
        return economicDb != null && economicDb.allBuildings != null
            ? economicDb.allBuildings
            : new List<BuildingData>();
    }

    public List<BuildingData> GetPowerBuildings()
    {
        return powerDb != null && powerDb.allBuildings != null
            ? powerDb.allBuildings
            : new List<BuildingData>();
    }

    public List<BuildingData> GetResearchBuildings()
    {
        return researchDb != null && researchDb.allBuildings != null
            ? researchDb.allBuildings
            : new List<BuildingData>();
    }

    public List<BuildingData> GetFactoryBuildings()
    {
        if (cachedFactoryBuildings != null)
            return cachedFactoryBuildings;

        cachedFactoryBuildings = new List<BuildingData>();

        if (factoryDb == null || factoryDb.allBuildings == null)
            return cachedFactoryBuildings;

        for (int i = 0; i < factoryDb.allBuildings.Count; i++)
        {
            if (factoryDb.allBuildings[i] != null)
                cachedFactoryBuildings.Add(factoryDb.allBuildings[i]);
        }

        return cachedFactoryBuildings;
    }

    public List<BuildingData> GetSupportBuildings()
    {
        if (cachedSupportBuildings != null)
            return cachedSupportBuildings;

        cachedSupportBuildings = new List<BuildingData>();

        if (supportDb == null || supportDb.allBuildings == null)
            return cachedSupportBuildings;

        for (int i = 0; i < supportDb.allBuildings.Count; i++)
        {
            if (supportDb.allBuildings[i] != null)
                cachedSupportBuildings.Add(supportDb.allBuildings[i]);
        }

        return cachedSupportBuildings;
    }
}
