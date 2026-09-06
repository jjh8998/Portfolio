using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SaveManager : MonoBehaviour
{
    private const int CurrentSaveVersion = 2;

    private static bool? autoLoadOnNextStart;
    private static int? requestedAutoLoadSlot;
    private static int currentSessionSlot = -1;

    public static bool IsLoadingGame { get; private set; }
    public static event Action LoadCompleted;

    [SerializeField] private JsonIOScript jsonIO;
    [SerializeField] private bool autoLoadOnStart = true;
    [SerializeField] private int autoLoadSlotIndex = 0;

    public bool AutoLoadOnStart => autoLoadOnStart;
    public int AutoLoadSlotIndex => requestedAutoLoadSlot ?? autoLoadSlotIndex;

    /// <summary>
    /// 현재 세션에서 마지막으로 로드/저장한 메인 슬롯(1~3). 미지정 시 -1.
    /// 전쟁 직전 자동저장(slot 0)과 별개.
    /// </summary>
    public static int CurrentSessionSlot
    {
        get => currentSessionSlot;
        set => currentSessionSlot = value;
    }

    public static void RequestAutoLoadOnNextStart(bool shouldAutoLoad)
    {
        autoLoadOnNextStart = shouldAutoLoad;
        requestedAutoLoadSlot = null;
    }

    public static void RequestAutoLoadOnNextStart(bool shouldAutoLoad, int slotIndex)
    {
        autoLoadOnNextStart = shouldAutoLoad;
        requestedAutoLoadSlot = shouldAutoLoad ? slotIndex : (int?)null;
    }

    public static bool ResolveAutoLoadOnNextStart(bool defaultAutoLoad)
    {
        return autoLoadOnNextStart ?? defaultAutoLoad;
    }

    public static int? ConsumeRequestedAutoLoadSlot()
    {
        int? slot = requestedAutoLoadSlot;
        requestedAutoLoadSlot = null;
        return slot;
    }

    [Serializable]
    public class TestSaveData
    {
        public int saveVersion;
        public string saveTime;
    }

    [Serializable]
    public class SaveGameData
    {
        public int saveVersion;
        public string saveTime;
        public string playerFactionName;
        public string playerPortraitId;
        public string displayDate;
        public int ownedCityCount;
        public GameDateSaveData date = new GameDateSaveData();
        public List<FactionSceneSaveEntry> factions = new List<FactionSceneSaveEntry>();
        public List<CitySceneSaveEntry> cities = new List<CitySceneSaveEntry>();
        public List<PatentResearchSaveData> claimedPatents = new List<PatentResearchSaveData>();
        public List<PatentLicenseSaveData> activePatentLicenses = new List<PatentLicenseSaveData>();
        public RelationManager.RelationManagerSaveData relations = new RelationManager.RelationManagerSaveData();
        public EmployeeHireSaveData employeeHire = new EmployeeHireSaveData();
    }

    [Serializable]
    public class GameDateSaveData
    {
        public int year;
        public int month;
        public int day;
    }

    [Serializable]
    public class FactionSceneSaveEntry
    {
        public string factionId;
        public string factionName;
        public FactionSaveData factionData;
    }

    [Serializable]
    public class CitySceneSaveEntry
    {
        public string cityName;
        public string ownerFactionId;
        public string ownerFactionName;
        public CityScript.CitySaveData cityData;
    }

    private void Awake()
    {
        if (jsonIO == null)
            jsonIO = GetComponent<JsonIOScript>();

        if (jsonIO == null)
            jsonIO = gameObject.AddComponent<JsonIOScript>();
    }

    private IEnumerator Start()
    {
        // 이번 요청은 SaveManager.Start에서만 소비해서 다음 씬 진입에 남지 않게 한다.
        bool shouldAutoLoad = ResolveAutoLoadOnNextStart(autoLoadOnStart);
        autoLoadOnNextStart = null;

        if (!shouldAutoLoad)
            yield break;

        CalendarScript calendar = FindFirstObjectByType<CalendarScript>();
        if (calendar != null)
            calendar.SetRunning(false);

        yield return null;

        int? requestedSlot = ConsumeRequestedAutoLoadSlot();
        int targetSlot = requestedSlot ?? autoLoadSlotIndex;

        if (!HasSaveFile(targetSlot))
        {
            Debug.LogWarning($"[SaveManager] Auto load skipped. Save file does not exist for slot {targetSlot}.");
            RestorePauseState(false);
            yield break;
        }

        if (TryLoadCurrentGame(targetSlot) && targetSlot >= JsonIOScript.MainSlotMinIndex && targetSlot <= JsonIOScript.MainSlotMaxIndex)
            CurrentSessionSlot = targetSlot;
    }

    public bool SaveTestData(int slotIndex)
    {
        var testData = new TestSaveData
        {
            saveVersion = 1,
            saveTime = DateTime.UtcNow.ToString("o")
        };

        return SaveToJson(slotIndex, testData);
    }

    public bool SaveCurrentGame(int slotIndex)
    {
        bool wasPaused = PauseGame();

        try
        {
            CalendarScript calendar = FindFirstObjectByType<CalendarScript>();
            if (calendar == null || calendar.CurrentDate == null)
            {
                Debug.LogWarning("[SaveManager] Save failed. CalendarScript not found.");
                return false;
            }

            GameDateSaveData dateData = ExportDate(calendar.CurrentDate);
            List<FactionSceneSaveEntry> factionEntries = ExportFactions();
            List<CitySceneSaveEntry> cityEntries = ExportCities();

            SaveGameData saveData = new SaveGameData
            {
                saveVersion = CurrentSaveVersion,
                saveTime = DateTime.UtcNow.ToString("o"),
                playerFactionName = ResolvePlayerFactionName(factionEntries),
                playerPortraitId = ResolvePlayerPortraitId(),
                displayDate = FormatDisplayDate(dateData),
                ownedCityCount = ResolvePlayerOwnedCityCount(cityEntries),
                date = dateData,
                factions = factionEntries,
                cities = cityEntries,
                claimedPatents = ExportPatentResearches(),
                activePatentLicenses = ExportPatentLicenses(),
                relations = ExportRelations(),
                employeeHire = ExportEmployeeHire()
            };

            bool saved = SaveToJson(slotIndex, saveData);
            if (saved && slotIndex >= JsonIOScript.MainSlotMinIndex && slotIndex <= JsonIOScript.MainSlotMaxIndex)
                CurrentSessionSlot = slotIndex;
            return saved;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveManager] SaveCurrentGame failed: {ex.Message}");
            return false;
        }
        finally
        {
            RestorePauseState(wasPaused);
        }
    }

    public bool TryLoadCurrentGame(int slotIndex)
    {
        bool wasPaused = PauseGame();
        bool keepPausedAfterLoad = false;
        bool loadSucceeded = false;

        try
        {
            if (!TryLoadFromJson(slotIndex, out SaveGameData saveData) || saveData == null)
                return false;

            CalendarScript calendar = FindFirstObjectByType<CalendarScript>();
            if (calendar == null)
            {
                Debug.LogWarning("[SaveManager] Load failed. CalendarScript not found.");
                return false;
            }

            if (CityOwnershipManager.instance == null)
            {
                Debug.LogWarning("[SaveManager] Load failed. CityOwnershipManager instance not found.");
                return false;
            }

            Dictionary<string, FactionManager> factionsById = BuildFactionsById();
            Dictionary<string, FactionManager> factionsByName = BuildFactionsByName();
            Dictionary<string, CityScript> citiesByName = BuildCityMap();

            IsLoadingGame = true;

            if (saveData.date != null)
                calendar.SetDate(new GameDate(saveData.date.year, saveData.date.month, saveData.date.day));

            ImportPatentResearches(saveData.claimedPatents);
            ImportFactions(saveData.factions, factionsById, factionsByName, citiesByName);
            ImportRelations(saveData.relations);
            ImportPatentLicenses(saveData.activePatentLicenses);
            factionsByName = BuildFactionsByName();
            ClearFactionOwnerships(factionsById.Values);
            Dictionary<CityScript, CitySceneSaveEntry> ownerEntries = ImportCities(saveData.cities, citiesByName, factionsById);
            RestoreOwnerships(ownerEntries, factionsById, factionsByName);
            RecalculateCities(citiesByName.Values);
            RecalculateFactions(factionsById.Values);
            ImportEmployeeHire(saveData.employeeHire);
            RefreshUI();

            keepPausedAfterLoad = true;
            if (slotIndex >= JsonIOScript.MainSlotMinIndex && slotIndex <= JsonIOScript.MainSlotMaxIndex)
                CurrentSessionSlot = slotIndex;
            Debug.Log($"[SaveManager] Load completed for slot {slotIndex}.");
            loadSucceeded = true;
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SaveManager] TryLoadCurrentGame failed: {ex.Message}");
            return false;
        }
        finally
        {
            IsLoadingGame = false;
            RestorePauseState(keepPausedAfterLoad ? true : wasPaused);

            if (loadSucceeded)
                LoadCompleted?.Invoke();
        }
    }

    public bool SaveToJson<T>(int slotIndex, T data)
    {
        return jsonIO != null && jsonIO.SaveToJson(slotIndex, data);
    }

    public bool TryLoadFromJson<T>(int slotIndex, out T data)
    {
        if (jsonIO != null)
            return jsonIO.TryLoadFromJson(slotIndex, out data);

        data = default(T);
        return false;
    }

    public bool TryLoadTestData(int slotIndex, out TestSaveData data)
    {
        return TryLoadFromJson(slotIndex, out data);
    }

    public bool HasSaveFile(int slotIndex)
    {
        if (jsonIO == null)
            jsonIO = GetComponent<JsonIOScript>();

        if (jsonIO == null)
            jsonIO = gameObject.AddComponent<JsonIOScript>();

        return jsonIO != null && jsonIO.HasSaveFile(slotIndex);
    }

    public bool DeleteSaveFile(int slotIndex)
    {
        return jsonIO != null && jsonIO.DeleteSaveFile(slotIndex);
    }

    private bool PauseGame()
    {
        TimeController timeController = FindFirstObjectByType<TimeController>();
        if (timeController != null)
        {
            bool wasPaused = timeController.IsPaused();
            timeController.SetPaused(true);
            return wasPaused;
        }

        CalendarScript calendar = FindFirstObjectByType<CalendarScript>();
        if (calendar != null)
            calendar.SetRunning(false);

        return false;
    }

    private void RestorePauseState(bool wasPaused)
    {
        TimeController timeController = FindFirstObjectByType<TimeController>();
        if (timeController != null)
        {
            timeController.SetPaused(wasPaused);
            return;
        }

        CalendarScript calendar = FindFirstObjectByType<CalendarScript>();
        if (calendar != null)
            calendar.SetRunning(!wasPaused);
    }

    private string ResolvePlayerFactionName(List<FactionSceneSaveEntry> factions)
    {
        // 플레이어 식별: HumanPlayerController 또는 NationAIController 없는 FactionManager 우선,
        // 못 찾으면 첫 번째 엔트리 폴백.
        FactionManager[] sceneFactions = FindObjectsByType<FactionManager>(FindObjectsSortMode.None);
        for (int i = 0; i < sceneFactions.Length; i++)
        {
            FactionManager faction = sceneFactions[i];
            if (faction == null)
                continue;

            if (faction.GetComponent<NationAIController>() == null && !string.IsNullOrWhiteSpace(faction.factionName))
                return faction.factionName;
        }

        if (factions != null && factions.Count > 0 && !string.IsNullOrWhiteSpace(factions[0].factionName))
            return factions[0].factionName;

        return string.Empty;
    }

    private string ResolvePlayerPortraitId()
    {
        FactionManager[] sceneFactions = FindObjectsByType<FactionManager>(FindObjectsSortMode.None);
        for (int i = 0; i < sceneFactions.Length; i++)
        {
            FactionManager faction = sceneFactions[i];
            if (faction == null)
                continue;

            if (faction.IsPlayerFaction)
                return faction.portraitId;
        }

        for (int i = 0; i < sceneFactions.Length; i++)
        {
            FactionManager faction = sceneFactions[i];
            if (faction != null && faction.GetComponent<NationAIController>() == null)
                return faction.portraitId;
        }

        return string.Empty;
    }

    private int ResolvePlayerOwnedCityCount(List<CitySceneSaveEntry> cities)
    {
        if (cities == null || cities.Count == 0)
            return 0;

        string playerName = ResolvePlayerFactionName(null);
        if (string.IsNullOrWhiteSpace(playerName))
            return 0;

        int count = 0;
        for (int i = 0; i < cities.Count; i++)
        {
            if (cities[i] != null && string.Equals(cities[i].ownerFactionName, playerName, StringComparison.Ordinal))
                count++;
        }
        return count;
    }

    private static string FormatDisplayDate(GameDateSaveData date)
    {
        if (date == null)
            return string.Empty;
        return $"{date.year:D4}.{date.month:D2}.{date.day:D2}";
    }

    private GameDateSaveData ExportDate(GameDate date)
    {
        if (date == null)
            return new GameDateSaveData();

        return new GameDateSaveData
        {
            year = date.year,
            month = date.month,
            day = date.day
        };
    }

    private List<FactionSceneSaveEntry> ExportFactions()
    {
        List<FactionSceneSaveEntry> result = new List<FactionSceneSaveEntry>();
        HashSet<string> seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        FactionManager[] factions = FindObjectsByType<FactionManager>(FindObjectsSortMode.None);

        for (int i = 0; i < factions.Length; i++)
        {
            FactionManager faction = factions[i];
            if (faction == null)
            {
                continue;
            }

            string factionKey = faction.GetSaveKey();
            if (string.IsNullOrWhiteSpace(factionKey))
            {
                Debug.LogWarning("[SaveManager] Skipped faction save with empty save key.");
                continue;
            }

            if (string.IsNullOrWhiteSpace(faction.factionName))
                Debug.LogWarning($"[SaveManager] Faction save key '{factionKey}' has empty factionName.");

            if (!seenKeys.Add(factionKey))
            {
                Debug.LogWarning($"[SaveManager] Duplicate faction save key skipped: {factionKey}");
                continue;
            }

            result.Add(new FactionSceneSaveEntry
            {
                factionId = factionKey,
                factionName = faction.factionName,
                factionData = faction.ExportSaveData()
            });
        }

        return result;
    }

    private List<CitySceneSaveEntry> ExportCities()
    {
        List<CitySceneSaveEntry> result = new List<CitySceneSaveEntry>();
        HashSet<string> seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        CityScript[] cities = FindObjectsByType<CityScript>(FindObjectsSortMode.None);

        for (int i = 0; i < cities.Length; i++)
        {
            CityScript city = cities[i];
            string cityName = city != null && city.cityData != null ? city.cityData.cityName : null;

            if (string.IsNullOrWhiteSpace(cityName))
            {
                Debug.LogWarning("[SaveManager] Skipped city save with empty cityName.");
                continue;
            }

            string canonicalCityName = CityDisplayNameUtility.ToCanonicalName(cityName);
            if (!seenNames.Add(canonicalCityName))
            {
                Debug.LogWarning($"[SaveManager] Duplicate cityName skipped: {canonicalCityName}");
                continue;
            }

            result.Add(new CitySceneSaveEntry
            {
                cityName = canonicalCityName,
                ownerFactionId = city.cityData.owner != null ? city.cityData.owner.GetSaveKey() : null,
                ownerFactionName = city.cityData.owner != null ? city.cityData.owner.factionName : null,
                cityData = city.ExportSaveData()
            });
        }

        return result;
    }

    private List<PatentResearchSaveData> ExportPatentResearches()
    {
        return PatentResearchManager.Instance.ExportSaveData();
    }

    private List<PatentLicenseSaveData> ExportPatentLicenses()
    {
        return PatentResearchManager.Instance.ExportLicenseSaveData();
    }

    private void ImportPatentResearches(List<PatentResearchSaveData> _data)
    {
        PatentResearchManager.Instance.ImportSaveData(_data);
    }

    private void ImportPatentLicenses(List<PatentLicenseSaveData> _data)
    {
        PatentResearchManager.Instance.ImportLicenseSaveData(_data, false);
    }

    private EmployeeHireSaveData ExportEmployeeHire()
    {
        EmployeeHireService service = FindEmployeeHireService();
        return service != null ? service.ExportSaveData() : new EmployeeHireSaveData();
    }

    private void ImportEmployeeHire(EmployeeHireSaveData _data)
    {
        EmployeeHireService service = FindEmployeeHireService();
        if (service != null)
            service.ImportSaveData(_data);
    }

    private EmployeeHireService FindEmployeeHireService()
    {
        EmployeeHireService[] services = FindObjectsByType<EmployeeHireService>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < services.Length; i++)
        {
            if (services[i] != null)
                return services[i];
        }

        return null;
    }

    private RelationManager.RelationManagerSaveData ExportRelations()
    {
        RelationManager relationManager = FindFirstObjectByType<RelationManager>();
        if (relationManager == null)
        {
            Debug.LogWarning("[SaveManager] RelationManager not found while exporting relations.");
            return null;
        }

        return relationManager.ExportSaveData();
    }

    private void ImportRelations(RelationManager.RelationManagerSaveData _data)
    {
        RelationManager relationManager = FindFirstObjectByType<RelationManager>();
        if (relationManager == null)
        {
            if (_data != null)
                Debug.LogWarning("[SaveManager] RelationManager not found while importing relations.");

            return;
        }

        relationManager.ImportSaveData(_data);
    }

    private Dictionary<string, FactionManager> BuildFactionsById()
    {
        Dictionary<string, FactionManager> result = new Dictionary<string, FactionManager>(StringComparer.OrdinalIgnoreCase);
        FactionManager[] factions = FindObjectsByType<FactionManager>(FindObjectsSortMode.None);

        for (int i = 0; i < factions.Length; i++)
        {
            FactionManager faction = factions[i];
            if (faction == null)
            {
                continue;
            }

            string factionKey = faction.GetSaveKey();
            if (string.IsNullOrWhiteSpace(factionKey))
            {
                Debug.LogWarning("[SaveManager] Scene faction skipped with empty save key.");
                continue;
            }

            if (result.ContainsKey(factionKey))
            {
                Debug.LogWarning($"[SaveManager] Duplicate scene faction save key skipped: {factionKey}");
                continue;
            }

            result.Add(factionKey, faction);
        }

        return result;
    }

    private Dictionary<string, FactionManager> BuildFactionsByName()
    {
        Dictionary<string, FactionManager> result = new Dictionary<string, FactionManager>(StringComparer.OrdinalIgnoreCase);
        FactionManager[] factions = FindObjectsByType<FactionManager>(FindObjectsSortMode.None);

        for (int i = 0; i < factions.Length; i++)
        {
            FactionManager faction = factions[i];
            if (faction == null || string.IsNullOrWhiteSpace(faction.factionName))
                continue;

            if (result.ContainsKey(faction.factionName))
            {
                Debug.LogWarning($"[SaveManager] Duplicate scene factionName skipped for legacy fallback: {faction.factionName}");
                continue;
            }

            result.Add(faction.factionName, faction);
        }

        return result;
    }

    private Dictionary<string, CityScript> BuildCityMap()
    {
        Dictionary<string, CityScript> result = new Dictionary<string, CityScript>(StringComparer.OrdinalIgnoreCase);
        CityScript[] cities = FindObjectsByType<CityScript>(FindObjectsSortMode.None);

        for (int i = 0; i < cities.Length; i++)
        {
            CityScript city = cities[i];
            string cityName = city != null && city.cityData != null ? city.cityData.cityName : null;

            if (string.IsNullOrWhiteSpace(cityName))
            {
                Debug.LogWarning("[SaveManager] Scene city skipped with empty cityName.");
                continue;
            }

            string canonicalCityName = CityDisplayNameUtility.ToCanonicalName(cityName);
            if (result.ContainsKey(canonicalCityName))
            {
                Debug.LogWarning($"[SaveManager] Duplicate scene cityName skipped: {canonicalCityName}");
                continue;
            }

            result.Add(canonicalCityName, city);
            if (!string.Equals(cityName, canonicalCityName, StringComparison.OrdinalIgnoreCase)
                && !result.ContainsKey(cityName))
            {
                result.Add(cityName, city);
            }
        }

        return result;
    }

    private void ImportFactions(
        List<FactionSceneSaveEntry> entries,
        Dictionary<string, FactionManager> factionsById,
        Dictionary<string, FactionManager> factionsByName,
        Dictionary<string, CityScript> citiesByName)
    {
        if (entries == null)
            return;

        for (int i = 0; i < entries.Count; i++)
        {
            FactionSceneSaveEntry entry = entries[i];
            if (entry == null)
                continue;

            if (!TryResolveFactionForLoad(entry.factionId, entry.factionName, factionsById, factionsByName, out FactionManager faction))
            {
                string fallbackKey = !string.IsNullOrWhiteSpace(entry.factionId) ? entry.factionId : entry.factionName;
                Debug.LogWarning($"[SaveManager] Scene faction not found for load: {fallbackKey}");
                continue;
            }

            if (entry.factionData == null)
                entry.factionData = new FactionSaveData();

            if (string.IsNullOrWhiteSpace(entry.factionData.factionName) && !string.IsNullOrWhiteSpace(entry.factionName))
                entry.factionData.factionName = entry.factionName;

            faction.ImportSaveData(entry.factionData, citiesByName);
        }
    }

    private void ClearFactionOwnerships(IEnumerable<FactionManager> factions)
    {
        if (factions == null)
            return;

        foreach (FactionManager faction in factions)
        {
            if (faction == null)
                continue;

            if (faction.ownedCities == null)
                faction.ownedCities = new List<CityScript>();
            else
                faction.ownedCities.Clear();
        }

        CityScript[] cities = FindObjectsByType<CityScript>(FindObjectsSortMode.None);
        for (int i = 0; i < cities.Length; i++)
        {
            CityScript city = cities[i];
            if (city != null && city.cityData != null)
                city.cityData.owner = null;
        }
    }

    private Dictionary<CityScript, CitySceneSaveEntry> ImportCities(
        List<CitySceneSaveEntry> entries,
        Dictionary<string, CityScript> citiesByName,
        Dictionary<string, FactionManager> factionsById)
    {
        Dictionary<CityScript, CitySceneSaveEntry> result = new Dictionary<CityScript, CitySceneSaveEntry>();
        if (entries == null)
            return result;

        for (int i = 0; i < entries.Count; i++)
        {
            CitySceneSaveEntry entry = entries[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.cityName))
                continue;

            string canonicalCityName = CityDisplayNameUtility.ToCanonicalName(entry.cityName);
            if (!citiesByName.TryGetValue(canonicalCityName, out CityScript city))
            {
                Debug.LogWarning($"[SaveManager] Scene city not found for load: {entry.cityName}");
                continue;
            }

            city.ImportSaveData(entry.cityData, factionsById);
            result[city] = entry;
        }

        return result;
    }

    private void RestoreOwnerships(
        Dictionary<CityScript, CitySceneSaveEntry> ownerEntriesByCity,
        Dictionary<string, FactionManager> factionsById,
        Dictionary<string, FactionManager> factionsByName)
    {
        if (ownerEntriesByCity == null)
            return;

        foreach (KeyValuePair<CityScript, CitySceneSaveEntry> pair in ownerEntriesByCity)
        {
            CityScript city = pair.Key;
            CitySceneSaveEntry ownerEntry = pair.Value;
            string ownerFactionId = ownerEntry != null ? ownerEntry.ownerFactionId : null;
            string ownerFactionName = ownerEntry != null ? ownerEntry.ownerFactionName : null;

            if (city == null || (string.IsNullOrWhiteSpace(ownerFactionId) && string.IsNullOrWhiteSpace(ownerFactionName)))
                continue;

            if (!TryResolveFactionForLoad(ownerFactionId, ownerFactionName, factionsById, factionsByName, out FactionManager ownerFaction))
            {
                string fallbackKey = !string.IsNullOrWhiteSpace(ownerFactionId) ? ownerFactionId : ownerFactionName;
                Debug.LogWarning($"[SaveManager] Owner faction not found for city '{city.cityData.cityName}': {fallbackKey}");
                continue;
            }

            CityOwnershipManager.TransferResult transferResult = CityOwnershipManager.instance.Transfer(city, ownerFaction);
            if (transferResult != CityOwnershipManager.TransferResult.Success
                && transferResult != CityOwnershipManager.TransferResult.SameOwner)
            {
                Debug.LogWarning($"[SaveManager] Ownership restore failed for city '{city.cityData.cityName}': {transferResult}");
            }
        }
    }

    private bool TryResolveFactionForLoad(
        string factionId,
        string factionName,
        IDictionary<string, FactionManager> factionsById,
        IDictionary<string, FactionManager> factionsByName,
        out FactionManager faction)
    {
        faction = null;

        if (!string.IsNullOrWhiteSpace(factionId)
            && factionsById != null
            && factionsById.TryGetValue(factionId, out faction)
            && faction != null)
        {
            return true;
        }

        if (!string.IsNullOrWhiteSpace(factionName)
            && factionsByName != null
            && factionsByName.TryGetValue(factionName, out faction)
            && faction != null)
        {
            return true;
        }

        faction = null;
        return false;
    }

    private void RecalculateCities(IEnumerable<CityScript> cities)
    {
        if (cities == null)
            return;

        foreach (CityScript city in cities)
        {
            if (city != null)
                city.CalculateCityPower();
        }
    }

    private void RecalculateFactions(IEnumerable<FactionManager> factions)
    {
        if (factions == null)
            return;

        foreach (FactionManager faction in factions)
        {
            if (faction != null)
                faction.RecalculateFactionResources();
        }
    }

    private void RefreshUI()
    {
        DateUIScript[] dateUIs = FindObjectsByType<DateUIScript>(FindObjectsSortMode.None);
        for (int i = 0; i < dateUIs.Length; i++)
        {
            if (dateUIs[i] != null)
                dateUIs[i].RefreshNow();
        }

        PlayerInfoUIScript[] playerInfoUIs = FindObjectsByType<PlayerInfoUIScript>(FindObjectsSortMode.None);
        for (int i = 0; i < playerInfoUIs.Length; i++)
        {
            if (playerInfoUIs[i] != null)
                playerInfoUIs[i].RefreshNow();
        }

        EmployeeManagementUIController[] employeeUIs = FindObjectsByType<EmployeeManagementUIController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < employeeUIs.Length; i++)
        {
            if (employeeUIs[i] != null)
                employeeUIs[i].RefreshNow();
        }
    }
}
