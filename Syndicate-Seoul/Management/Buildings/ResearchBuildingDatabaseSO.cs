using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ResearchBuildingDatabase", menuName = "City/Research Building Database")]
public class ResearchBuildingDatabaseSO : ScriptableObject
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

        ParserScript.EnsureUpgradeLinksFromIds(allBuildings, "ResearchBuildingDatabaseSO");
    }

    [ContextMenu("Load CSV Now")]
    public void LoadCSV()
    {
        if (allBuildings == null)
            allBuildings = new List<BuildingData>();

        string[] resourcePaths =
        {
            "CSVs/Syndicate_ResearchBuildingList"
        };

        ParserScript.LoadStandardBuildings(
            ref csvFile,
            "ResearchBuildingDatabaseSO",
            resourcePaths,
            BuildingCategory.Research,
            allBuildings);

        ParserScript.EnsureUpgradeLinksFromIds(allBuildings, "ResearchBuildingDatabaseSO");

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
