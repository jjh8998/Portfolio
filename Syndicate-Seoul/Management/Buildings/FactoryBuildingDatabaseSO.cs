using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "FactoryBuildingDatabase", menuName = "City/Factory Building Database")]
public class FactoryBuildingDatabaseSO : ScriptableObject
{
    public TextAsset csvFile;

    [Header("아래 리스트는 CSV에서 자동 로딩됩니다")]
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

        ParserScript.EnsureUpgradeLinksFromIds(allBuildings, "FactoryBuildingDatabaseSO");
    }

    [ContextMenu("Load CSV Now")]
    public void LoadCSV()
    {
        if (allBuildings == null)
            allBuildings = new List<BuildingData>();

        string[] resourcePaths =
        {
            "CSVs/Syndicate_FactoryBuildingList",
            "CSVs/Syndicate_FactoryBuildingList - 시트1",
        };

        ParserScript.LoadStandardBuildings(
            ref csvFile,
            "FactoryBuildingDatabaseSO",
            resourcePaths,
            BuildingCategory.Factory,
            allBuildings,
            _parseExtraWithHeaders: ParseFactoryColumns);

        ParserScript.EnsureUpgradeLinksFromIds(allBuildings, "FactoryBuildingDatabaseSO");

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    private void ParseFactoryColumns(BuildingData _building, List<string> _row, Dictionary<string, int> _headerMap, int _rowIndex)
    {
        const string logPrefix = "FactoryBuildingDatabaseSO";

        _building.activationIntervalDays = ParserScript.GetOptionalIntByHeaders(
            _row,
            _headerMap,
            90,
            logPrefix,
            _rowIndex,
            "CardPackProductionDay",
            "CardProductionDay",
            "activationIntervalDays");

        _building.cardYieldAmount = ParserScript.GetOptionalIntByHeaders(
            _row,
            _headerMap,
            1,
            logPrefix,
            _rowIndex,
            "CardPackDrawCount",
            "CardDrawCount",
            "cardYieldAmount");

        _building.emergencyOrderCost = ParserScript.GetOptionalIntByHeaders(
            _row,
            _headerMap,
            100,
            logPrefix,
            _rowIndex,
            "EmergencyOrderCost",
            "emergencyOrderCost");

        _building.rewardPackFilter = ParserScript.GetOptionalStringByHeaders(
            _row,
            _headerMap,
            string.Empty,
            "rewardPackFilter",
            "RewardPackFilter");

        _building.minRewardTier = ParserScript.GetOptionalIntByHeaders(
            _row,
            _headerMap,
            0,
            logPrefix,
            _rowIndex,
            "minRewardTier",
            "MinRewardTier");

        _building.maxRewardTier = ParserScript.GetOptionalIntByHeaders(
            _row,
            _headerMap,
            0,
            logPrefix,
            _rowIndex,
            "maxRewardTier",
            "MaxRewardTier");

        _building.tier1Rate = ParserScript.GetOptionalIntByHeaders(
            _row,
            _headerMap,
            0,
            logPrefix,
            _rowIndex,
            "1TierPackRate",
            "Tier1Rate",
            "1TierRate",
            "tier1Rate");

        _building.tier2Rate = ParserScript.GetOptionalIntByHeaders(
            _row,
            _headerMap,
            0,
            logPrefix,
            _rowIndex,
            "2TierPackRate",
            "Tier2Rate",
            "2TierRate",
            "tier2Rate");

        _building.tier3Rate = ParserScript.GetOptionalIntByHeaders(
            _row,
            _headerMap,
            0,
            logPrefix,
            _rowIndex,
            "3TierPackRate",
            "Tier3Rate",
            "3TierRate",
            "tier3Rate");

        _building.tier4Rate = ParserScript.GetOptionalIntByHeaders(
            _row,
            _headerMap,
            0,
            logPrefix,
            _rowIndex,
            "4TierPackRate",
            "Tier4Rate",
            "4TierRate",
            "4Tier",
            "tier4Rate");

        _building.tier5Rate = ParserScript.GetOptionalIntByHeaders(
            _row,
            _headerMap,
            0,
            logPrefix,
            _rowIndex,
            "5TierPackRate",
            "Tier5Rate",
            "5TierRate",
            "5Tier",
            "tier5Rate");
    }

    public BuildingData GetBuildingByID(string _id)
    {
        LoadCSVIfEmpty();

        return allBuildings.Find(x => x != null && x.ID == _id);
    }
}
