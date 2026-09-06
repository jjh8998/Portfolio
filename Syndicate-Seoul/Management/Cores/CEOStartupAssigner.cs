using System;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class CEOStartupAssigner : MonoBehaviour
{
    [SerializeField] private CEODatabaseSO ceoDatabase;
    [SerializeField] private Transform ceoRoot;
    [SerializeField] private Transform cityRoot;
    [SerializeField] private FactionManager playerFaction;
    [SerializeField] private int seed = -1;

    private readonly List<CEOData> leftoverCEOs = new List<CEOData>();

    public IReadOnlyList<CEOData> GetLeftoverCEOs()
    {
        return leftoverCEOs;
    }

    private void Awake()
    {
        leftoverCEOs.Clear();

        if (IsSaveLoadStartup())
        {
            Debug.Log("[CEOStartupAssigner] Startup assignment skipped because save file will be loaded.");
            return;
        }

        if (ceoDatabase == null)
        {
            ceoDatabase = ScriptableObject.CreateInstance<CEODatabaseSO>();
        }

        ceoDatabase.LoadCSV();

        if (ceoDatabase.allCEOs == null || ceoDatabase.allCEOs.Count == 0)
        {
            Debug.LogWarning("[CEOStartupAssigner] CEO list is empty.");
            return;
        }

        List<CEOData> ceos = new List<CEOData>();
        HashSet<string> usedCeoIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < ceoDatabase.allCEOs.Count; i++)
        {
            CEOData ceo = ceoDatabase.allCEOs[i];
            if (ceo == null || string.IsNullOrWhiteSpace(ceo.id))
                continue;

            if (usedCeoIds.Add(ceo.id))
                ceos.Add(ceo);
        }

        FactionManager[] factionArray = ceoRoot != null
            ? ceoRoot.GetComponentsInChildren<FactionManager>(true)
            : FindObjectsByType<FactionManager>(FindObjectsSortMode.None);
        CityScript[] cityArray = cityRoot != null
            ? cityRoot.GetComponentsInChildren<CityScript>(true)
            : FindObjectsByType<CityScript>(FindObjectsSortMode.None);

        if (ceos.Count == 0)
        {
            Debug.LogWarning("[CEOStartupAssigner] CEO list is empty.");
            return;
        }

        if (factionArray == null || factionArray.Length == 0)
        {
            Debug.LogWarning("[CEOStartupAssigner] No FactionManager found in scene.");
            return;
        }

        if (cityArray == null || cityArray.Length == 0)
        {
            Debug.LogWarning("[CEOStartupAssigner] No CityScript found.");
            return;
        }

        List<FactionManager> factions = new List<FactionManager>();
        for (int i = 0; i < factionArray.Length; i++)
        {
            FactionManager faction = factionArray[i];
            if (faction == null)
                continue;

            if (playerFaction != null && ReferenceEquals(faction, playerFaction))
                continue;

            factions.Add(faction);
        }

        if (factions.Count == 0)
        {
            Debug.LogWarning("[CEOStartupAssigner] FactionManager list is empty.");
            return;
        }

        List<CityScript> cities = new List<CityScript>();
        for (int i = 0; i < cityArray.Length; i++)
        {
            CityScript city = cityArray[i];
            if (city == null)
                continue;

            if (playerFaction != null)
            {
                if (ReferenceEquals(city, playerFaction.startingCity))
                    continue;

                if (city.cityData != null && ReferenceEquals(city.cityData.owner, playerFaction))
                    continue;

                if (playerFaction.HasCity(city))
                    continue;
            }

            cities.Add(city);
        }

        if (cities.Count == 0)
        {
            Debug.LogWarning("[CEOStartupAssigner] CityScript list is empty.");
            return;
        }

        System.Random random = seed >= 0 ? new System.Random(seed) : new System.Random();

        for (int i = cities.Count - 1; i > 0; i--)
        {
            int swapIndex = random.Next(i + 1);
            CityScript temp = cities[i];
            cities[i] = cities[swapIndex];
            cities[swapIndex] = temp;
        }

        for (int i = ceos.Count - 1; i > 0; i--)
        {
            int swapIndex = random.Next(i + 1);
            CEOData temp = ceos[i];
            ceos[i] = ceos[swapIndex];
            ceos[swapIndex] = temp;
        }

        int assignCount = Mathf.Min(ceos.Count, factions.Count, cities.Count);

        for (int i = 0; i < assignCount; i++)
        {
            factions[i].factionName = ceos[i].name;
            factions[i].ceoId = ceos[i].id;
            factions[i].startingCity = cities[i];

            NationAIController nationAIController = factions[i].GetComponent<NationAIController>();
            if (nationAIController != null)
                nationAIController.SetPersonality(ceos[i].personality);
        }

        for (int i = assignCount; i < ceos.Count; i++)
        {
            if (ceos[i] != null)
                leftoverCEOs.Add(ceos[i]);
        }

        Debug.Log($"[CEOStartupAssigner] CEO assignment completed. assigned={assignCount}, leftover={leftoverCEOs.Count}");
    }

    private bool IsSaveLoadStartup()
    {
        SaveManager saveManager = FindFirstObjectByType<SaveManager>();
        if (saveManager == null)
            return false;

        // 저장 파일이 있어도 '게임 시작'으로 들어온 경우에는 새 게임 초기 배정을 건너뛰면 안 된다.
        if (!SaveManager.ResolveAutoLoadOnNextStart(saveManager.AutoLoadOnStart))
            return false;

        return saveManager.HasSaveFile(saveManager.AutoLoadSlotIndex);
    }
}
