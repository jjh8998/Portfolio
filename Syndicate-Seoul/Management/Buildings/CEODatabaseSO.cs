using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CEOData
{
    public string id;
    public string name;
    public string description;
    public string imageId;
    public string iconImageId;
    public BigFivePersonality personality = new BigFivePersonality();
    public Sprite profileSprite;
    public Sprite iconSprite;
}

[CreateAssetMenu(fileName = "CEODatabase", menuName = "City/CEO Database")]
public class CEODatabaseSO : ScriptableObject
{
    private const string CsvResourcePath = "CSVs/Syndicate_CEOList";
    private const string ManagementCsvResourcePath = "Management/CSVs/Syndicate_CEOList";
    private const string ProfileImageResourceFolder = "Images/CEOImages";
    private const string IconImageResourceFolder = "Images/CEOIcons";
    private const int DefaultPersonalityValue = 50;
    private static readonly string[] CsvResourcePaths =
    {
        CsvResourcePath,
        ManagementCsvResourcePath
    };

    public TextAsset csvFile;
    public List<CEOData> allCEOs = new List<CEOData>();

    private readonly Dictionary<string, CEOData> ceoLookup =
        new Dictionary<string, CEOData>(StringComparer.OrdinalIgnoreCase);

    private void OnEnable()
    {
        LoadCSV();
    }

    [ContextMenu("Load CSV Now")]
    public void LoadCSV()
    {
        if (allCEOs == null)
            allCEOs = new List<CEOData>();

        allCEOs.Clear();
        ceoLookup.Clear();

        if (!CSVParserUtility.TryResolveCsvResource(ref csvFile, "[CEODatabaseSO] CEO CSV", CsvResourcePaths))
            return;

        string[] lines = csvFile.text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length <= 1)
        {
            Debug.LogWarning("[CEODatabaseSO] CSV has no data rows.");
            return;
        }

        Dictionary<string, int> headerMap = CSVParserUtility.BuildHeaderMap(CSVParserUtility.ParseCsvLine(lines[0]));
        HashSet<string> loadedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 1; i < lines.Length; i++)
        {
            List<string> row = CSVParserUtility.ParseCsvLine(lines[i]);
            if (row.Count == 0)
                continue;

            string id = CSVParserUtility.GetColumnValue(row, headerMap, "ID", 0).Trim();
            if (string.IsNullOrWhiteSpace(id))
            {
                Debug.LogWarning($"[CEODatabaseSO] Skipped CEO row with empty ID at line {i + 1}.");
                continue;
            }

            if (!loadedIds.Add(id))
            {
                Debug.LogWarning($"[CEODatabaseSO] Duplicate CEO ID skipped: {id}");
                continue;
            }

            bool descriptionMissing = headerMap.ContainsKey("Description")
                && headerMap.ContainsKey("Image")
                && headerMap.ContainsKey("IconImage")
                && row.Count == headerMap.Count - 1;

            Dictionary<string, int> effectiveHeaderMap = descriptionMissing ? null : headerMap;
            int imageIndex = descriptionMissing ? 2 : 3;
            int iconImageIndex = descriptionMissing ? 3 : 4;
            int personalityStartIndex = descriptionMissing ? 4 : 5;

            string imageId = CSVParserUtility.GetColumnValue(row, effectiveHeaderMap, "Image", imageIndex).Trim();
            string iconImageId = CSVParserUtility.GetColumnValue(row, effectiveHeaderMap, "IconImage", iconImageIndex).Trim();
            string name = CSVParserUtility.GetColumnValue(row, headerMap, "Name", 1).Trim();
            string description = descriptionMissing
                ? string.Empty
                : CSVParserUtility.GetColumnValue(row, headerMap, "Description", 2).Trim();

            CEOData ceo = new CEOData
            {
                id = id,
                name = name,
                description = description,
                imageId = imageId,
                iconImageId = iconImageId,
                profileSprite = ResolveProfileSprite(imageId, name),
                iconSprite = ResolveIconSprite(iconImageId, name),
                personality = new BigFivePersonality
                {
                    openness = GetPersonalityValue(row, effectiveHeaderMap, "Openness", personalityStartIndex, i + 1),
                    conscientiousness = GetPersonalityValue(row, effectiveHeaderMap, "Conscientiousness", personalityStartIndex + 1, i + 1),
                    extraversion = GetPersonalityValue(row, effectiveHeaderMap, "Extraversion", personalityStartIndex + 2, i + 1),
                    agreeableness = GetPersonalityValue(row, effectiveHeaderMap, "Agreeableness", personalityStartIndex + 3, i + 1),
                    neuroticism = GetPersonalityValue(row, effectiveHeaderMap, "Neuroticism", personalityStartIndex + 4, i + 1)
                }
            };

            allCEOs.Add(ceo);
        }

        RebuildLookup();
    }

    public CEOData GetCEOByID(string _id)
    {
        if (allCEOs == null || allCEOs.Count == 0)
            LoadCSV();

        if (string.IsNullOrWhiteSpace(_id))
            return null;

        if (ceoLookup.Count != allCEOs.Count)
            RebuildLookup();

        ceoLookup.TryGetValue(_id, out CEOData ceo);
        return ceo;
    }

    public CEOData GetCEOByName(string _name)
    {
        if (allCEOs == null || allCEOs.Count == 0)
            LoadCSV();

        if (string.IsNullOrWhiteSpace(_name) || allCEOs == null)
            return null;

        string targetName = _name.Trim();
        for (int i = 0; i < allCEOs.Count; i++)
        {
            CEOData ceo = allCEOs[i];
            if (ceo == null || string.IsNullOrWhiteSpace(ceo.name))
                continue;

            if (string.Equals(ceo.name.Trim(), targetName, StringComparison.OrdinalIgnoreCase))
                return ceo;
        }

        return null;
    }

    private void RebuildLookup()
    {
        ceoLookup.Clear();

        if (allCEOs == null)
            return;

        for (int i = 0; i < allCEOs.Count; i++)
        {
            CEOData ceo = allCEOs[i];
            if (ceo == null || string.IsNullOrWhiteSpace(ceo.id))
                continue;

            if (!ceoLookup.ContainsKey(ceo.id))
                ceoLookup.Add(ceo.id, ceo);
        }
    }

    private static int GetPersonalityValue(List<string> row, Dictionary<string, int> headerMap, string columnName, int fallbackIndex, int rowIndex)
    {
        int value = CSVParserUtility.GetOptionalInt(
            row,
            headerMap,
            columnName,
            fallbackIndex,
            DefaultPersonalityValue,
            "CEODatabaseSO",
            rowIndex);

        return Mathf.Clamp(value, 0, 100);
    }

    private static Sprite ResolveProfileSprite(string imageId, string fallbackName)
    {
        return ResolveSprite(ProfileImageResourceFolder, imageId, fallbackName);
    }

    private static Sprite ResolveIconSprite(string iconImageId, string fallbackName)
    {
        return ResolveSprite(IconImageResourceFolder, iconImageId, fallbackName);
    }

    private static Sprite ResolveSprite(string folder, string imageId, string fallbackName)
    {
        if (!string.IsNullOrWhiteSpace(imageId))
        {
            Sprite sprite = LoadSprite(folder, imageId);
            if (sprite != null)
                return sprite;
        }

        if (string.IsNullOrWhiteSpace(fallbackName))
            return null;

        return LoadSprite(folder, fallbackName);
    }

    private static Sprite LoadSprite(string folder, string spriteName)
    {
        if (string.IsNullOrWhiteSpace(spriteName))
            return null;

        string trimmedName = spriteName.Trim();
        Sprite sprite = Resources.Load<Sprite>($"{folder}/{trimmedName}");
        if (sprite != null)
            return sprite;

        Sprite[] sprites = Resources.LoadAll<Sprite>(folder);
        for (int i = 0; i < sprites.Length; i++)
        {
            if (sprites[i] != null && string.Equals(sprites[i].name, trimmedName, StringComparison.OrdinalIgnoreCase))
                return sprites[i];
        }

        return null;
    }
}
