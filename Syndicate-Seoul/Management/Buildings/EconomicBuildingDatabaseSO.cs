using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EconomicBuildingDatabase", menuName = "City/Economic Building Database")]
public class EconomicBuildingDatabaseSO : ScriptableObject
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

        ParserScript.EnsureUpgradeLinksFromIds(allBuildings, "EconomicBuildingDatabaseSO");
    }

    [ContextMenu("Load CSV Now")]
    public void LoadCSV()
    {
        if (allBuildings == null)
            allBuildings = new List<BuildingData>();

        string[] resourcePaths =
        {
            "CSVs/Syndicate_EconomyBuildingList",
            "CSVs/Syndicate_economyBuildingList"
        };

        ParserScript.LoadStandardBuildings(
            ref csvFile,
            "EconomicBuildingDatabaseSO",
            resourcePaths,
            BuildingCategory.Economy,
            allBuildings);

        ParserScript.EnsureUpgradeLinksFromIds(allBuildings, "EconomicBuildingDatabaseSO");

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }

    public BuildingData GetBuildingByID(string _id)
    {
        LoadCSVIfEmpty();

        return allBuildings.Find(x => x != null && x.ID == _id);
    }
}
