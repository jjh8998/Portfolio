using UnityEngine;

public class FactionBuildingDatabase : MonoBehaviour
{
    private static EconomicBuildingDatabaseSO economicDb;
    private static PowerBuildingDatabaseSO powerDb;
    private static ResearchBuildingDatabaseSO researchDb;
    private static ResearchDatabaseSO researchTechDb;
    private static FactoryBuildingDatabaseSO factoryDb;
    private static SupportBuildingDatabaseSO supportDb;

    public static EconomicBuildingDatabaseSO GetEconomicDb()
    {
        return BuildingDatabaseLoader.GetEconomicDatabase(ref economicDb, "[AI]");
    }

    public static PowerBuildingDatabaseSO GetPowerDb()
    {
        return BuildingDatabaseLoader.GetPowerDatabase(ref powerDb, "[AI]");
    }

    public static ResearchBuildingDatabaseSO GetResearchDb()
    {
        return BuildingDatabaseLoader.GetResearchDatabase(ref researchDb, "[AI]");
    }

    public static ResearchDatabaseSO GetResearchTechDb()
    {
        if (researchTechDb == null)
        {
            researchTechDb = Resources.Load<ResearchDatabaseSO>("Databases/ResearchDatabase");
            if (researchTechDb == null)
            {
                researchTechDb = ScriptableObject.CreateInstance<ResearchDatabaseSO>();
                researchTechDb.LoadCSV();
                AIDebugLogger.LogAIWarning("[AI] Research Tech DB asset not found in Resources. Fallback instance created.");
            }
        }

        return researchTechDb;
    }

    public static FactoryBuildingDatabaseSO GetFactoryDb()
    {
        return BuildingDatabaseLoader.GetFactoryDatabase(ref factoryDb, "[AI]");
    }

    public static SupportBuildingDatabaseSO GetSupportDb()
    {
        return BuildingDatabaseLoader.GetSupportDatabase(ref supportDb, "[AI]");
    }
}
