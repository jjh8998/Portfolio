using System;
using System.Collections.Generic;
using UnityEngine;

public static class ParserScript
{
    private const int BaseColumnCount = 10;
    private const string RequiredResearchColumn = "RequiredResearchId";

    public static void LoadStandardBuildings<T>(
        ref TextAsset _csvFile,
        string _logPrefix,
        string[] _resourcePaths,
        BuildingCategory _category,
        List<T> _allBuildings,
        Action<T, List<string>, int> _parseExtra = null,
        Action<T, List<string>, Dictionary<string, int>, int> _parseExtraWithHeaders = null)
        where T : BuildingData, new()
    {
        if (!TryResolveCsv(ref _csvFile, _resourcePaths, _logPrefix))
            return;

        _allBuildings.Clear();

        string[] lines = _csvFile.text.Split(
            new char[] { '\n', '\r' },
            StringSplitOptions.RemoveEmptyEntries);

        if (lines.Length <= 1)
        {
            Debug.LogWarning($"{_logPrefix} : CSV has no data rows.");
            return;
        }

        List<string> headerRow = CSVParserUtility.ParseCsvLine(lines[0]);
        Dictionary<string, int> headerMap = CSVParserUtility.BuildHeaderMap(headerRow);

        Dictionary<string, T> buildingById = new Dictionary<string, T>();
        Dictionary<string, string> pendingUpgradeTargets = new Dictionary<string, string>();

        for (int i = 1; i < lines.Length; i++)
        {
            List<string> row = CSVParserUtility.ParseCsvLine(lines[i]);

            if (row.Count < BaseColumnCount)
            {
                Debug.LogWarning($"{_logPrefix} : Row {i} has insufficient columns. Skipped.");
                continue;
            }

            try
            {
                string id = CSVParserUtility.GetColumnValue(row, headerMap, "ID", 0).Trim();
                if (string.IsNullOrEmpty(id))
                {
                    Debug.LogWarning($"{_logPrefix} : Row {i} has empty ID. Skipped.");
                    continue;
                }

                if (buildingById.ContainsKey(id))
                {
                    Debug.LogWarning($"{_logPrefix} : Duplicate ID '{id}' found at row {i}. Skipped.");
                    continue;
                }

                T building = new T();
                building.ID = id;
                building.category = _category;
                building.name = CSVParserUtility.GetColumnValue(row, headerMap, "name", 1).Trim();

                building.description = CSVParserUtility.GetColumnValue(
                    row,
                    headerMap,
                    "description",
                    1,
                    building.name).Trim();

                building.bonusIncome = CSVParserUtility.ParseInt(CSVParserUtility.GetColumnValue(row, headerMap, "Income", 3), _logPrefix, i, "Income");
                building.rpOutput = CSVParserUtility.ParseInt(CSVParserUtility.GetColumnValue(row, headerMap, "ResearchOutput", 4), _logPrefix, i, "ResearchOutput");
                building.powerOutput = CSVParserUtility.ParseInt(CSVParserUtility.GetColumnValue(row, headerMap, "PowerOutput", 5), _logPrefix, i, "PowerOutput");
                building.constructionCost = CSVParserUtility.ParseInt(CSVParserUtility.GetColumnValue(row, headerMap, "constructionCost", 6), _logPrefix, i, "constructionCost");
                building.constructionDay = CSVParserUtility.ParseInt(CSVParserUtility.GetColumnValue(row, headerMap, "constructionDay", 7), _logPrefix, i, "constructionDay");
                building.powerConsumption = CSVParserUtility.ParseInt(CSVParserUtility.GetColumnValue(row, headerMap, "PowerConsumption", 8), _logPrefix, i, "PowerConsumption");
                building.nextUpgradeBuildings = new List<BuildingData>();
                building.nextUpgradeBuildingIds = new List<string>();
                building.requiredResearchId = NormalizeNullableValue(
                    CSVParserUtility.GetOptionalStringByHeaders(
                        row,
                        headerMap,
                        10,
                        string.Empty,
                        RequiredResearchColumn,
                        "RequiredResearchID",
                        "ResearchID",
                        "ResearchId",
                        "researchID",
                        "requiredResearchId"));
                building.prefabResourcePath = NormalizeNullableValue(
                    CSVParserUtility.GetColumnValue(row, headerMap, "prefabResourcePath", -1));

                if (string.IsNullOrWhiteSpace(building.prefabResourcePath))
                {
                    building.prefabResourcePath = NormalizeNullableValue(
                        CSVParserUtility.GetColumnValue(row, headerMap, "prefabPath", -1));
                }

                string nextUpgradeToken = CSVParserUtility.GetColumnValue(row, headerMap, "nextUpgradeBuildings", 9).Trim();

                _parseExtra?.Invoke(building, row, i);
                _parseExtraWithHeaders?.Invoke(building, row, headerMap, i);
                ApplyAbilityColumns(building, row, headerMap, i, _category, _logPrefix);
                building.AutoAssignPrefabReference();

                buildingById.Add(building.ID, building);
                _allBuildings.Add(building);

                if (!IsNullToken(nextUpgradeToken))
                    pendingUpgradeTargets.Add(building.ID, nextUpgradeToken);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"{_logPrefix} : Error parsing row {i} ({lines[i]}): {e.Message}");
            }
        }

        LinkUpgrades(buildingById, _allBuildings, pendingUpgradeTargets, _logPrefix);
        EnsureUpgradeLinksFromIds(_allBuildings, _logPrefix);
    }

    public static void EnsureUpgradeLinksFromIds<T>(List<T> _allBuildings, string _logPrefix)
        where T : BuildingData
    {
        if (_allBuildings == null)
            return;

        Dictionary<string, T> buildingById = new Dictionary<string, T>();
        for (int i = 0; i < _allBuildings.Count; i++)
        {
            T building = _allBuildings[i];
            if (building == null || string.IsNullOrWhiteSpace(building.ID))
                continue;

            if (buildingById.ContainsKey(building.ID))
            {
                Debug.LogWarning($"{_logPrefix} : Duplicate building ID '{building.ID}' found while rebuilding upgrade links.");
                continue;
            }

            buildingById.Add(building.ID, building);
        }

        for (int i = 0; i < _allBuildings.Count; i++)
        {
            T building = _allBuildings[i];
            if (building == null)
                continue;

            if (building.nextUpgradeBuildingIds == null)
                building.nextUpgradeBuildingIds = new List<string>();

            if (building.nextUpgradeBuildings == null)
                building.nextUpgradeBuildings = new List<BuildingData>();
            else
                building.nextUpgradeBuildings.Clear();

            for (int j = 0; j < building.nextUpgradeBuildingIds.Count; j++)
            {
                string upgradeId = building.nextUpgradeBuildingIds[j];
                if (IsNullToken(upgradeId))
                    continue;

                string cleanUpgradeId = upgradeId.Trim();
                if (buildingById.TryGetValue(cleanUpgradeId, out T nextBuilding))
                {
                    if (!building.nextUpgradeBuildings.Contains(nextBuilding))
                        building.nextUpgradeBuildings.Add(nextBuilding);
                }
                else
                {
                    Debug.LogWarning(
                        $"{_logPrefix} : Upgrade building ID '{cleanUpgradeId}' not found for building '{building.ID}'.");
                }
            }
        }
    }

    private static void LinkUpgrades<T>(
        Dictionary<string, T> _buildingById,
        List<T> _allBuildings,
        Dictionary<string, string> _pendingUpgradeTargets,
        string _logPrefix)
        where T : BuildingData
    {
        foreach (var pair in _pendingUpgradeTargets)
        {
            string currentId = pair.Key;
            string targetToken = pair.Value;

            if (!_buildingById.TryGetValue(currentId, out T currentBuilding))
            {
                Debug.LogWarning($"{_logPrefix} : Current building ID '{currentId}' not found while linking upgrades.");
                continue;
            }

            if (currentBuilding.nextUpgradeBuildings == null)
                currentBuilding.nextUpgradeBuildings = new List<BuildingData>();

            if (currentBuilding.nextUpgradeBuildingIds == null)
                currentBuilding.nextUpgradeBuildingIds = new List<string>();

            string[] upgradeTokens = targetToken.Split(';');
            for (int i = 0; i < upgradeTokens.Length; i++)
            {
                string upgradeToken = upgradeTokens[i].Trim();
                if (IsNullToken(upgradeToken))
                    continue;

                T nextBuilding = null;

                if (_buildingById.TryGetValue(upgradeToken, out T nextById))
                {
                    nextBuilding = nextById;
                }
                else
                {
                    nextBuilding = _allBuildings.Find(x => x.name == upgradeToken);
                }

                if (nextBuilding != null)
                {
                    if (!currentBuilding.nextUpgradeBuildingIds.Contains(nextBuilding.ID))
                        currentBuilding.nextUpgradeBuildingIds.Add(nextBuilding.ID);

                    if (!currentBuilding.nextUpgradeBuildings.Contains(nextBuilding))
                        currentBuilding.nextUpgradeBuildings.Add(nextBuilding);
                }
                else
                {
                    Debug.LogWarning(
                        $"{_logPrefix} : Next upgrade target '{upgradeToken}' not found for building '{currentId}'. " +
                        "Use next building ID in CSV if possible.");
                }
            }
        }
    }

    private static bool TryResolveCsv(
        ref TextAsset _csvFile,
        string[] _resourcePaths,
        string _logPrefix)
    {
        return CSVParserUtility.TryResolveCsvResource(ref _csvFile, _logPrefix, _resourcePaths);
    }

    public static string GetColumnValue(
        List<string> _row,
        Dictionary<string, int> _headerMap,
        string _columnName,
        int _fallbackIndex,
        string _defaultValue = "")
    {
        return CSVParserUtility.GetColumnValue(_row, _headerMap, _columnName, _fallbackIndex, _defaultValue);
    }

    public static int GetOptionalInt(
        List<string> _row,
        Dictionary<string, int> _headerMap,
        string _columnName,
        int _fallbackIndex,
        int _defaultValue,
        string _logPrefix = "",
        int _rowIndex = -1)
    {
        return CSVParserUtility.GetOptionalInt(
            _row,
            _headerMap,
            _columnName,
            _fallbackIndex,
            _defaultValue,
            _logPrefix,
            _rowIndex);
    }

    public static int GetOptionalIntByHeaders(
        List<string> _row,
        Dictionary<string, int> _headerMap,
        int _defaultValue,
        string _logPrefix = "",
        int _rowIndex = -1,
        params string[] _columnNames)
    {
        return CSVParserUtility.GetOptionalIntByHeaders(
            _row,
            _headerMap,
            _defaultValue,
            _logPrefix,
            _rowIndex,
            _columnNames);
    }

    public static string GetOptionalStringByHeaders(
        List<string> _row,
        Dictionary<string, int> _headerMap,
        string _defaultValue = "",
        params string[] _columnNames)
    {
        return CSVParserUtility.GetOptionalStringByHeaders(_row, _headerMap, _defaultValue, _columnNames);
    }

    private static void ApplyAbilityColumns(
        BuildingData _building,
        List<string> _row,
        Dictionary<string, int> _headerMap,
        int _rowIndex,
        BuildingCategory _category,
        string _logPrefix)
    {
        if (_building == null)
            return;

        int abilityIdFallbackIndex = _category == BuildingCategory.Factory ? 10 : 11;
        int triggerTypeFallbackIndex = _category == BuildingCategory.Factory ? 11 : 12;
        int abilityValueFallbackIndex = _category == BuildingCategory.Factory ? 12 : 13;

        string abilityId = CSVParserUtility.GetColumnValue(_row, _headerMap, "AbilityID", abilityIdFallbackIndex).Trim();
        if (IsNullToken(abilityId))
        {
            _building.ability = null;
            return;
        }

        string triggerType = NormalizeNullableValue(
            CSVParserUtility.GetColumnValue(_row, _headerMap, "AbilityTriggerType", triggerTypeFallbackIndex));

        int abilityValue = CSVParserUtility.ParseInt(
            CSVParserUtility.GetColumnValue(_row, _headerMap, "AbilityValue", abilityValueFallbackIndex),
            _logPrefix,
            _rowIndex,
            "AbilityValue");

        _building.ability = BuildingAbilityFactory.Create(abilityId, triggerType, abilityValue);
        if (_building.ability == null)
        {
            Debug.LogWarning(
                $"{_logPrefix} : Unknown AbilityID '{abilityId}' at row {_rowIndex} for building '{_building.ID}'.");
        }
    }

    public static bool IsNullToken(string _value)
    {
        if (string.IsNullOrWhiteSpace(_value))
            return true;

        string normalized = _value.Trim().ToLowerInvariant();
        if (normalized == "-" || normalized == "\uC5C6\uC74C")
            return true;
        return normalized == "0" || normalized == "none" || normalized == "없음" || normalized == "?놁쓬" || normalized == "null";
    }

    private static string NormalizeNullableValue(string _value)
    {
        return IsNullToken(_value) ? string.Empty : _value.Trim();
    }
}
