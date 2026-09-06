using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class ResearchIconImageEntry
{
    public string key;
    public Image image;
}

[CreateAssetMenu(fileName = "ResearchDatabase", menuName = "City/Research Database")]
public class ResearchDatabaseSO : ScriptableObject
{
    private const string HeaderId = "ID";
    private const string HeaderName = "Name";
    private const string HeaderIconImage = "IconImage";
    private const string HeaderCategory = "Category";
    private const string HeaderPrerequisite = "Prerequisite";
    private const string HeaderUnlockCategoryRequirements = "UnlockCategoryRequirements";
    private const string HeaderDescription = "Description";
    private const string HeaderResearchDescription = "ResearchDescription";
    private const string HeaderAbilityDescription = "AbilityDescription";
    private const string HeaderCost = "RPCost";
    private const string HeaderEffectType = "EffectType";
    private const string HeaderResearchEffectId = "ResearchEffect_ID";
    private const string HeaderEffectTargetId = "EffectTargetId";
    private const string HeaderEffectValue = "EffectValue";
    private const string HeaderAbilityValue = "AbilityValue";
    private const string HeaderIsPatentResearch = "IsPatentResearch";

    private static readonly string[] CsvResourcePaths =
    {
        "CSVs/Syndicate_Research",
        "CSVs/ResearchCSV",
        "ResearchCSV"
    };

    private static readonly string[] IconResourceFolders =
    {
        "Images/ResearchIcons",
        string.Empty,
        "Images",
        "ResearchIcons",
        "UI/ResearchIcons"
    };

    public TextAsset csvFile;
    public List<ResearchData> allResearches = new List<ResearchData>();
    public List<ResearchIconImageEntry> iconImageEntries = new List<ResearchIconImageEntry>();

    private readonly Dictionary<string, ResearchData> researchLookup =
        new Dictionary<string, ResearchData>(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, Image> iconImageLookup =
        new Dictionary<string, Image>(StringComparer.OrdinalIgnoreCase);

    private void OnEnable()
    {
        LoadCSV();
    }

    [ContextMenu("Load CSV Now")]
    public void LoadCSV()
    {
        if (allResearches == null)
            allResearches = new List<ResearchData>();

        TryResolveCsv();
        RebuildIconLookup();

        if (csvFile == null)
        {
            Debug.LogError("[ResearchDatabaseSO] Research CSV not found in Resources.");
            allResearches.Clear();
            researchLookup.Clear();
            return;
        }

        allResearches.Clear();

        string[] lines = csvFile.text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length <= 1)
        {
            researchLookup.Clear();
            return;
        }

        Dictionary<string, int> headerMap = CSVParserUtility.BuildHeaderMap(CSVParserUtility.ParseCsvLine(lines[0]));
        HashSet<string> loadedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 1; i < lines.Length; i++)
        {
            List<string> columns = CSVParserUtility.ParseCsvLine(lines[i]);
            if (columns.Count == 0)
                continue;

            string id = CSVParserUtility.GetOptionalStringByHeaders(
                columns,
                headerMap,
                0,
                string.Empty,
                HeaderId,
                "ResearchID",
                "ResearchId",
                "researchID").Trim();
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning($"[ResearchDatabaseSO] Skipped research row with empty ID at line {i + 1}.");
                continue;
            }

            if (!loadedIds.Add(id))
            {
                Debug.LogWarning($"[ResearchDatabaseSO] Duplicate research ID skipped: {id}");
                continue;
            }

            string iconImageId = CSVParserUtility.GetColumnValue(columns, headerMap, HeaderIconImage, 2).Trim();
            string description = CSVParserUtility.GetOptionalStringByHeaders(
                columns,
                headerMap,
                7,
                string.Empty,
                HeaderResearchDescription,
                HeaderDescription).Trim();
            string abilityDescription = CSVParserUtility.GetOptionalStringByHeaders(
                columns,
                headerMap,
                8,
                string.Empty,
                HeaderAbilityDescription).Trim();
            string effectTypeValue = CSVParserUtility.GetOptionalStringByHeaders(
                columns,
                headerMap,
                10,
                string.Empty,
                HeaderResearchEffectId,
                HeaderEffectType).Trim();
            string effectTargetValue = CSVParserUtility.GetColumnValue(columns, headerMap, HeaderEffectTargetId, -1).Trim();
            string effectValue = CSVParserUtility.GetOptionalStringByHeaders(
                columns,
                headerMap,
                11,
                string.Empty,
                HeaderAbilityValue,
                HeaderEffectValue).Trim();
            string isPatentResearchValue = CSVParserUtility.GetOptionalStringByHeaders(
                columns,
                headerMap,
                4,
                string.Empty,
                HeaderIsPatentResearch).Trim();

            ResearchData research = new ResearchData
            {
                id = id,
                name = CSVParserUtility.GetColumnValue(columns, headerMap, HeaderName, 1).Trim(),
                category = CSVParserUtility.GetColumnValue(columns, headerMap, HeaderCategory, 3).Trim(),
                description = description,
                abilityDescription = abilityDescription,
                iconImageId = iconImageId,
                iconSprite = ResolveIconSprite(iconImageId),
                costRP = ParseResearchInt(
                    CSVParserUtility.GetColumnValue(columns, headerMap, HeaderCost, 9),
                    HeaderCost,
                    i + 1,
                    true),
                isPatentResearch = ParsePatentFlag(isPatentResearchValue),
                prerequisiteResearchIds = ParsePrerequisites(CSVParserUtility.GetColumnValue(columns, headerMap, HeaderPrerequisite, 5)),
                unlockCategoryRequirements = ParseUnlockCategoryRequirements(
                    CSVParserUtility.GetColumnValue(columns, headerMap, HeaderUnlockCategoryRequirements, 6),
                    i + 1),
                effectType = ParseEffectType(effectTypeValue, i + 1),
                effectTargetId = effectTargetValue,
                effectValue = ParseResearchInt(effectValue, HeaderAbilityValue, i + 1, true)
            };

            research.EnsureInitialized();
            allResearches.Add(research);
        }

        RebuildResearchLookup();
    }

    public ResearchData GetResearchById(string _id)
    {
        if (allResearches == null || allResearches.Count == 0)
            LoadCSV();

        if (string.IsNullOrWhiteSpace(_id))
            return null;

        if (researchLookup.Count != allResearches.Count)
            RebuildResearchLookup();

        researchLookup.TryGetValue(_id, out ResearchData research);
        return research;
    }

    public ResearchData GetResearchById(int _id)
    {
        if (_id <= 0)
            return null;

        ResearchData research = GetResearchById(_id.ToString());
        return research != null ? research : GetResearchById($"Research_{_id}");
    }

    private void TryResolveCsv()
    {
        CSVParserUtility.TryResolveCsvResource(ref csvFile, "[ResearchDatabaseSO] Research CSV", CsvResourcePaths);
    }

    private void RebuildResearchLookup()
    {
        researchLookup.Clear();

        if (allResearches == null)
            return;

        for (int i = 0; i < allResearches.Count; i++)
        {
            ResearchData research = allResearches[i];
            if (research == null || string.IsNullOrWhiteSpace(research.id))
                continue;

            research.EnsureInitialized();

            if (!researchLookup.ContainsKey(research.id))
                researchLookup.Add(research.id, research);
        }
    }

    private void RebuildIconLookup()
    {
        iconImageLookup.Clear();

        if (iconImageEntries == null)
            return;

        for (int i = 0; i < iconImageEntries.Count; i++)
        {
            ResearchIconImageEntry entry = iconImageEntries[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.key) || entry.image == null)
                continue;

            string key = entry.key.Trim();
            if (!iconImageLookup.ContainsKey(key))
                iconImageLookup.Add(key, entry.image);
        }
    }

    private Sprite ResolveIconSprite(string _iconImageId)
    {
        if (string.IsNullOrWhiteSpace(_iconImageId))
            return null;

        string key = _iconImageId.Trim().Replace('\\', '/');
        key = RemoveResourceExtension(key);
        string fileName = GetResourceFileName(key);
        HashSet<string> attemptedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (TryLoadIconSprite(key, attemptedPaths, out Sprite directSprite))
            return directSprite;

        for (int i = 0; i < IconResourceFolders.Length; i++)
        {
            string folder = IconResourceFolders[i];
            string resourcePath = string.IsNullOrWhiteSpace(folder) ? fileName : $"{folder}/{fileName}";

            if (TryLoadIconSprite(resourcePath, attemptedPaths, out Sprite iconSprite))
                return iconSprite;
        }

        Debug.LogWarning($"[ResearchDatabaseSO] Icon sprite not found for key: {key}, attempted: {string.Join(", ", attemptedPaths)}");
        return null;
    }

    private static bool TryLoadIconSprite(string resourcePath, HashSet<string> attemptedPaths, out Sprite iconSprite)
    {
        iconSprite = null;

        if (string.IsNullOrWhiteSpace(resourcePath) || attemptedPaths == null)
            return false;

        string normalizedPath = resourcePath.Trim().Replace('\\', '/');
        normalizedPath = RemoveResourceExtension(normalizedPath);
        if (!attemptedPaths.Add(normalizedPath))
            return false;

        iconSprite = Resources.Load<Sprite>(normalizedPath);
        return iconSprite != null;
    }

    private static string RemoveResourceExtension(string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath))
            return string.Empty;

        int slashIndex = resourcePath.LastIndexOf('/');
        int dotIndex = resourcePath.LastIndexOf('.');
        if (dotIndex > slashIndex)
            return resourcePath.Substring(0, dotIndex);

        return resourcePath;
    }

    private static string GetResourceFileName(string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath))
            return string.Empty;

        int slashIndex = resourcePath.LastIndexOf('/');
        if (slashIndex < 0 || slashIndex >= resourcePath.Length - 1)
            return resourcePath;

        return resourcePath.Substring(slashIndex + 1);
    }

    private static List<string> ParsePrerequisites(string rawValue)
    {
        List<string> prerequisites = new List<string>();

        if (string.IsNullOrWhiteSpace(rawValue))
            return prerequisites;

        string normalized = rawValue.Trim();
        if (normalized.Equals("None", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("Null", StringComparison.OrdinalIgnoreCase)
            || normalized.Equals("없음", StringComparison.OrdinalIgnoreCase))
        {
            return prerequisites;
        }

        string[] parts = normalized.Split(new[] { ';', '|', ',' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < parts.Length; i++)
        {
            string prerequisiteId = parts[i].Trim();
            if (!string.IsNullOrEmpty(prerequisiteId))
                prerequisites.Add(prerequisiteId);
        }

        return prerequisites;
    }

    private static List<ResearchCategoryRequirement> ParseUnlockCategoryRequirements(string rawValue, int rowIndex)
    {
        List<ResearchCategoryRequirement> requirements = new List<ResearchCategoryRequirement>();

        if (IsNoneLikeValue(rawValue))
            return requirements;

        string[] parts = rawValue.Trim().Split(new[] { ';', '|', ',' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < parts.Length; i++)
        {
            string part = parts[i].Trim();
            if (string.IsNullOrWhiteSpace(part))
                continue;

            string[] tokens = part.Split(new[] { ':' }, 2);
            string category = tokens.Length > 0 ? tokens[0].Trim() : string.Empty;
            if (string.IsNullOrWhiteSpace(category))
                continue;

            if (tokens.Length < 2 || string.IsNullOrWhiteSpace(tokens[1]))
            {
                Debug.LogWarning($"[ResearchDatabaseSO] Invalid UnlockCategoryRequirements at line {rowIndex}: '{part}'. Expected Category:Count.");
                continue;
            }

            int count = ParseResearchInt(tokens[1], HeaderUnlockCategoryRequirements, rowIndex, true);
            if (count < 1)
            {
                Debug.LogWarning($"[ResearchDatabaseSO] Invalid UnlockCategoryRequirements count at line {rowIndex}: '{part}'. Count must be 1 or greater.");
                continue;
            }

            requirements.Add(new ResearchCategoryRequirement
            {
                category = category,
                count = count
            });
        }

        return requirements;
    }

    private static int ParseResearchInt(string rawValue, string columnName, int rowIndex, bool warnOnInvalid)
    {
        if (IsNoneLikeValue(rawValue))
            return 0;

        string trimmed = rawValue.Trim();
        if (int.TryParse(trimmed, out int value))
            return value;

        if (float.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out float floatValue))
            return Mathf.RoundToInt(floatValue);

        if (float.TryParse(trimmed, out floatValue))
            return Mathf.RoundToInt(floatValue);

        if (warnOnInvalid)
            Debug.LogWarning($"[ResearchDatabaseSO] Failed to parse int at line {rowIndex}, column '{columnName}', value '{trimmed}'. Using 0.");

        return 0;
    }

    private static ResearchEffectType ParseEffectType(string rawValue, int rowIndex)
    {
        if (IsNoneLikeValue(rawValue))
            return ResearchEffectType.None;

        string trimmed = rawValue.Trim();
        if (Enum.TryParse(trimmed, true, out ResearchEffectType effectType))
            return effectType;

        Debug.LogWarning($"[ResearchDatabaseSO] Unknown research effect type at line {rowIndex}: {trimmed}. Using None.");
        return ResearchEffectType.None;
    }

    private static bool ParsePatentFlag(string rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            return false;

        string trimmed = rawValue.Trim();
        return trimmed.Equals("true", StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("1", StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("yes", StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("y", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsNoneLikeValue(string rawValue)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            return true;

        string trimmed = rawValue.Trim();
        return trimmed.Equals("None", StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("Null", StringComparison.OrdinalIgnoreCase)
            || trimmed.Equals("없음", StringComparison.OrdinalIgnoreCase);
    }
}
