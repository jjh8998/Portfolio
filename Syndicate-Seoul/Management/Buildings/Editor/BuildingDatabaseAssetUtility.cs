using System;
using UnityEditor;
using UnityEngine;

public static class BuildingDatabaseAssetUtility
{
    private const string DatabaseFolder = "Assets/Resources/Databases";

    [MenuItem("Tools/NewWorld/Buildings/Create Missing Database Assets From CSV")]
    public static void CreateMissingDatabaseAssetsFromCsv()
    {
        EnsureDatabaseFolder();

        CreateOrLoad<EconomicBuildingDatabaseSO>(
            "EconomicBuildingDatabase.asset",
            db => db.LoadCSV(),
            db => db.allBuildings == null || db.allBuildings.Count == 0);

        CreateOrLoad<PowerBuildingDatabaseSO>(
            "PowerBuildingDatabase.asset",
            db => db.LoadCSV(),
            db => db.allBuildings == null || db.allBuildings.Count == 0);

        CreateOrLoad<ResearchBuildingDatabaseSO>(
            "ResearchBuildingDatabase.asset",
            db => db.LoadCSV(),
            db => db.allBuildings == null || db.allBuildings.Count == 0);

        CreateOrLoad<FactoryBuildingDatabaseSO>(
            "FactoryBuildingDatabase.asset",
            db => db.LoadCSV(),
            db => db.allBuildings == null || db.allBuildings.Count == 0);

        CreateOrLoad<SupportBuildingDatabaseSO>(
            "SupportBuildingDatabase.asset",
            db => db.LoadCSV(),
            db => db.allBuildings == null || db.allBuildings.Count == 0);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    [MenuItem("Tools/NewWorld/Buildings/Reload Database Assets From CSV")]
    public static void ReloadDatabaseAssetsFromCsv()
    {
        EnsureDatabaseFolder();

        Reload<EconomicBuildingDatabaseSO>("EconomicBuildingDatabase.asset", db => db.LoadCSV());
        Reload<PowerBuildingDatabaseSO>("PowerBuildingDatabase.asset", db => db.LoadCSV());
        Reload<ResearchBuildingDatabaseSO>("ResearchBuildingDatabase.asset", db => db.LoadCSV());
        Reload<FactoryBuildingDatabaseSO>("FactoryBuildingDatabase.asset", db => db.LoadCSV());
        Reload<SupportBuildingDatabaseSO>("SupportBuildingDatabase.asset", db => db.LoadCSV());

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void CreateOrLoad<T>(string fileName, Action<T> loadCsv, Func<T, bool> isEmpty)
        where T : ScriptableObject
    {
        string path = $"{DatabaseFolder}/{fileName}";
        T database = AssetDatabase.LoadAssetAtPath<T>(path);
        bool created = false;

        if (database == null)
        {
            database = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(database, path);
            created = true;
        }

        if (created || isEmpty(database))
        {
            loadCsv(database);
        }

        EditorUtility.SetDirty(database);
    }

    private static void Reload<T>(string fileName, Action<T> loadCsv)
        where T : ScriptableObject
    {
        string path = $"{DatabaseFolder}/{fileName}";
        T database = AssetDatabase.LoadAssetAtPath<T>(path);
        if (database == null)
        {
            database = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(database, path);
        }

        loadCsv(database);
        EditorUtility.SetDirty(database);
    }

    private static void EnsureDatabaseFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        if (!AssetDatabase.IsValidFolder(DatabaseFolder))
        {
            AssetDatabase.CreateFolder("Assets/Resources", "Databases");
        }
    }
}
