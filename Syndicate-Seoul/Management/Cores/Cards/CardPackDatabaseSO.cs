using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CardPackDatabase", menuName = "City/Card Pack Database")]
public class CardPackDatabaseSO : ScriptableObject
{
    private static readonly string[] CsvResourcePaths =
    {
        "CSVs/Syndicate_CardPack",
        "CSVs/CardPack",
        "CardPack"
    };

    public TextAsset csvFile;
    public List<CardPackData> allCardPacks = new List<CardPackData>();

    private void OnEnable()
    {
        LoadCSV();
    }

    [ContextMenu("Load CSV Now")]
    public void LoadCSV()
    {
        if (allCardPacks == null)
            allCardPacks = new List<CardPackData>();

        allCardPacks.Clear();

        if (!CSVParserUtility.TryResolveCsvResource(ref csvFile, "[CardPackDatabaseSO] Card pack CSV", CsvResourcePaths))
            return;

        string[] lines = csvFile.text.Split(
            new char[] { '\n', '\r' },
            System.StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length <= 1)
        {
            Debug.LogWarning("[CardPackDatabaseSO] CSV has no data rows.");
            return;
        }

        List<string> headerRow = CSVParserUtility.ParseCsvLine(lines[0]);
        Dictionary<string, int> headerMap = CSVParserUtility.BuildHeaderMap(headerRow);
        HashSet<string> loadedIds = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

        for (int i = 1; i < lines.Length; i++)
        {
            List<string> row = CSVParserUtility.ParseCsvLine(lines[i]);
            string id = CSVParserUtility.GetColumnValue(row, headerMap, "ID", 0).Trim();

            if (string.IsNullOrWhiteSpace(id))
            {
                Debug.LogWarning($"[CardPackDatabaseSO] Skipped card pack row with empty ID at line {i + 1}.");
                continue;
            }

            if (!loadedIds.Add(id))
            {
                Debug.LogWarning($"[CardPackDatabaseSO] Duplicate card pack ID skipped: {id}");
                continue;
            }

            CardPackData cardPack = new CardPackData
            {
                id = id,
                name = CSVParserUtility.GetColumnValue(row, headerMap, "Name", 1).Trim(),
                theme = CSVParserUtility.GetOptionalStringByHeaders(row, headerMap, string.Empty, "Theme"),
                cardPackImage = CSVParserUtility.GetOptionalStringByHeaders(row, headerMap, string.Empty, "CardPackImage"),
                tier1Rate = CSVParserUtility.GetOptionalIntByHeaders(row, headerMap, 0, "CardPackDatabaseSO", i + 1, "1TierRate"),
                tier2Rate = CSVParserUtility.GetOptionalIntByHeaders(row, headerMap, 0, "CardPackDatabaseSO", i + 1, "2TierRate"),
                tier3Rate = CSVParserUtility.GetOptionalIntByHeaders(row, headerMap, 0, "CardPackDatabaseSO", i + 1, "3TierRate"),
                tier4Rate = CSVParserUtility.GetOptionalIntByHeaders(row, headerMap, 0, "CardPackDatabaseSO", i + 1, "4TierRate"),
                tier5Rate = CSVParserUtility.GetOptionalIntByHeaders(row, headerMap, 0, "CardPackDatabaseSO", i + 1, "5TierRate")
            };

            allCardPacks.Add(cardPack);
        }
    }

    public CardPackData GetCardPackByID(string _id)
    {
        if (allCardPacks == null || allCardPacks.Count == 0)
            LoadCSV();

        if (string.IsNullOrWhiteSpace(_id) || allCardPacks == null)
            return null;

        return allCardPacks.Find(x => x != null && x.id == _id);
    }
}
