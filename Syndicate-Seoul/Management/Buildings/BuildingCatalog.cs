using System.Collections.Generic;
using UnityEngine;

public static class BuildingCatalog
{
    private static bool isInitialized;
    private static readonly List<BuildingData> starterBuildings = new List<BuildingData>();

    private static PowerBuildingDatabaseSO powerDb;
    private static EconomicBuildingDatabaseSO economicDb;
    private static ResearchBuildingDatabaseSO researchDb;
    private static FactoryBuildingDatabaseSO factoryDb;
    private static SupportBuildingDatabaseSO supportDb;
    public static IReadOnlyList<BuildingData> GetStarterBuildings()
    {
        EnsureInitialized();
        return starterBuildings;
    }

    private static void EnsureInitialized()
    {
        if (isInitialized)
        {
            return;
        }

        starterBuildings.Clear();

        AddStarterBuilding(GetPowerDatabase().GetBuildingByID("Building_Power_T1"));
        AddStarterBuilding(GetEconomicDatabase().GetBuildingByID("Building_Economy_T1"));
        AddStarterBuilding(GetResearchDatabase().GetBuildingByID("Building_Research_T1"));
        AddStarterBuilding(GetFactoryDatabase().GetBuildingByID("Building_Factory_T1"));
        AddStarterBuilding(GetSupportDatabase().GetBuildingByID("Building_Support_T1"));
        isInitialized = true;
    }

    private static void AddStarterBuilding(BuildingData _building)
    {
        if (_building == null)
        {
            return;
        }

        if (starterBuildings.Exists(x => x.ID == _building.ID))
        {
            return;
        }

        starterBuildings.Add(_building);
    }

    private static PowerBuildingDatabaseSO GetPowerDatabase()
    {
        return BuildingDatabaseLoader.GetPowerDatabase(ref powerDb, "[BuildingCatalog]");
    }

    private static EconomicBuildingDatabaseSO GetEconomicDatabase()
    {
        return BuildingDatabaseLoader.GetEconomicDatabase(ref economicDb, "[BuildingCatalog]");
    }

    private static ResearchBuildingDatabaseSO GetResearchDatabase()
    {
        return BuildingDatabaseLoader.GetResearchDatabase(ref researchDb, "[BuildingCatalog]");
    }

    private static FactoryBuildingDatabaseSO GetFactoryDatabase()
    {
        return BuildingDatabaseLoader.GetFactoryDatabase(ref factoryDb, "[BuildingCatalog]");
    }

    private static SupportBuildingDatabaseSO GetSupportDatabase()
    {
        return BuildingDatabaseLoader.GetSupportDatabase(ref supportDb, "[BuildingCatalog]");
    }
}
