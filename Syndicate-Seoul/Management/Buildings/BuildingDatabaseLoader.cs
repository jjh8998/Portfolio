using System;
using UnityEngine;

public static class BuildingDatabaseLoader
{
    private const string EconomicPath = "Databases/EconomicBuildingDatabase";
    private const string PowerPath = "Databases/PowerBuildingDatabase";
    private const string ResearchPath = "Databases/ResearchBuildingDatabase";
    private const string FactoryPath = "Databases/FactoryBuildingDatabase";
    private const string SupportPath = "Databases/SupportBuildingDatabase";

    public static EconomicBuildingDatabaseSO GetEconomicDatabase(ref EconomicBuildingDatabaseSO cache, string logPrefix)
    {
        return LoadOrCreate(ref cache, EconomicPath, db => db.LoadCSVIfEmpty(), logPrefix);
    }

    public static PowerBuildingDatabaseSO GetPowerDatabase(ref PowerBuildingDatabaseSO cache, string logPrefix)
    {
        return LoadOrCreate(ref cache, PowerPath, db => db.LoadCSVIfEmpty(), logPrefix);
    }

    public static ResearchBuildingDatabaseSO GetResearchDatabase(ref ResearchBuildingDatabaseSO cache, string logPrefix)
    {
        return LoadOrCreate(ref cache, ResearchPath, db => db.LoadCSVIfEmpty(), logPrefix);
    }

    public static FactoryBuildingDatabaseSO GetFactoryDatabase(ref FactoryBuildingDatabaseSO cache, string logPrefix)
    {
        return LoadOrCreate(ref cache, FactoryPath, db => db.LoadCSVIfEmpty(), logPrefix);
    }

    public static SupportBuildingDatabaseSO GetSupportDatabase(ref SupportBuildingDatabaseSO cache, string logPrefix)
    {
        return LoadOrCreate(ref cache, SupportPath, db => db.LoadCSVIfEmpty(), logPrefix);
    }

    private static T LoadOrCreate<T>(ref T cache, string resourcePath, Action<T> ensureLoaded, string logPrefix)
        where T : ScriptableObject
    {
        if (cache != null)
        {
            ensureLoaded?.Invoke(cache);
            return cache;
        }

        cache = Resources.Load<T>(resourcePath);
        if (cache != null)
        {
            ensureLoaded?.Invoke(cache);
            return cache;
        }

        cache = ScriptableObject.CreateInstance<T>();
        ensureLoaded?.Invoke(cache);

        if (!string.IsNullOrWhiteSpace(logPrefix))
        {
            Debug.LogWarning($"{logPrefix} {typeof(T).Name} asset not found at Resources/{resourcePath}. CSV fallback instance created.");
        }

        return cache;
    }
}
