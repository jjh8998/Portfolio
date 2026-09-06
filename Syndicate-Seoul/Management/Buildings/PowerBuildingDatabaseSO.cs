using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PowerBuildingDatabase", menuName = "City/Power Building Database")]
public class PowerBuildingDatabaseSO : ScriptableObject
{
    public TextAsset csvFile;

    [Header("아래 리스트는 CSV에서 자동 로딩됩니다")]
    public List<BuildingData> allBuildings = new List<BuildingData>();

    private void OnEnable()
    {
        LoadCSV();
    }

    public void LoadCSVIfEmpty()
    {
        if (allBuildings == null || allBuildings.Count == 0)
            LoadCSV();
    }

    [ContextMenu("Load CSV Now")]
    public void LoadCSV()
    {
        if (allBuildings == null)
            allBuildings = new List<BuildingData>();

        string[] resourcePaths =
        {
            "CSVs/Syndicate_PowerBuildingList",
            "CSVs/PowerBuildings"
        };

        ParserScript.LoadStandardBuildings(
            ref csvFile,
            "PowerBuildingDatabaseSO",
            resourcePaths,
            BuildingCategory.Power,
            allBuildings);

        ParserScript.EnsureUpgradeLinksFromIds(allBuildings, "PowerBuildingDatabaseSO");

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
