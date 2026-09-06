using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SupportBuildingDatabase", menuName = "City/Support Building Database")]
public class SupportBuildingDatabaseSO : ScriptableObject
{
    public TextAsset csvFile;
    public List<BuildingData> allBuildings = new List<BuildingData>();

    private void OnEnable()
    {
        LoadCSVIfEmpty();
    }

    public void LoadCSVIfEmpty()
    {
        if (allBuildings == null || allBuildings.Count == 0)
        {
            LoadCSV();
        }
        else
        {
            RefreshRuntimeAbilitiesFromCsv();
        }

        ParserScript.EnsureUpgradeLinksFromIds(allBuildings, "SupportBuildingDatabaseSO");
    }

    [ContextMenu("Load CSV Now")]
    public void LoadCSV()
    {
        if (allBuildings == null)
            allBuildings = new List<BuildingData>();

        string[] resourcePaths =
        {
            "CSVs/Syndicate_SupportBuilding"
        };

        ParserScript.LoadStandardBuildings(
            ref csvFile,
            "SupportBuildingDatabaseSO",
            resourcePaths,
            BuildingCategory.Support,
            allBuildings);

        ParserScript.EnsureUpgradeLinksFromIds(allBuildings, "SupportBuildingDatabaseSO");

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    public BuildingData GetBuildingByID(string _id)
    {
        LoadCSVIfEmpty();

        return allBuildings.Find(x => x != null && x.ID == _id);
    }

    private void RefreshRuntimeAbilitiesFromCsv()
    {
        if (allBuildings == null || allBuildings.Count == 0)
            return;

        TextAsset resolvedCsv = csvFile;
        if (resolvedCsv == null)
            resolvedCsv = Resources.Load<TextAsset>("CSVs/Syndicate_SupportBuilding");

        if (resolvedCsv == null)
            return;

        Dictionary<string, BuildingData> buildingById = new Dictionary<string, BuildingData>(StringComparer.Ordinal);
        for (int i = 0; i < allBuildings.Count; i++)
        {
            BuildingData building = allBuildings[i];
            if (building != null && !string.IsNullOrWhiteSpace(building.ID))
                buildingById[building.ID] = building;
        }

        string[] lines = resolvedCsv.text.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length <= 1)
            return;

        List<string> headerRow = CSVParserUtility.ParseCsvLine(lines[0]);
        Dictionary<string, int> headerMap = CSVParserUtility.BuildHeaderMap(headerRow);

        for (int i = 1; i < lines.Length; i++)
        {
            List<string> row = CSVParserUtility.ParseCsvLine(lines[i]);
            string id = CSVParserUtility.GetColumnValue(row, headerMap, "ID", 0).Trim();
            if (string.IsNullOrWhiteSpace(id) || !buildingById.TryGetValue(id, out BuildingData building))
                continue;

            string abilityId = CSVParserUtility.GetColumnValue(row, headerMap, "AbilityID", 11).Trim();
            if (ParserScript.IsNullToken(abilityId))
            {
                building.ability = null;
                continue;
            }

            string triggerType = CSVParserUtility.GetColumnValue(row, headerMap, "AbilityTriggerType", 12).Trim();
            int abilityValue = CSVParserUtility.ParseInt(
                CSVParserUtility.GetColumnValue(row, headerMap, "AbilityValue", 13),
                "SupportBuildingDatabaseSO",
                i,
                "AbilityValue");

            building.ability = BuildingAbilityFactory.Create(abilityId, triggerType, abilityValue);
        }
    }
}
