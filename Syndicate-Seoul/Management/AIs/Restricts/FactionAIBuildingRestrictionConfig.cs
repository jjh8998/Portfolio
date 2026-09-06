using System;
using System.Collections.Generic;
using UnityEngine;

public class FactionAIBuildingRestrictionConfig : MonoBehaviour
{
    [SerializeField] private List<string> disabledBuildingIds = new List<string>();

    private HashSet<string> disabledBuildingIdSet;

    public bool IsDisabled(BuildingData building)
    {
        return building != null && IsDisabled(building.ID);
    }

    public bool IsDisabled(string buildingId)
    {
        if (string.IsNullOrWhiteSpace(buildingId))
            return false;

        EnsureCache();
        return disabledBuildingIdSet.Contains(buildingId.Trim());
    }

    private void OnValidate()
    {
        RebuildCache();
    }

    private void EnsureCache()
    {
        if (disabledBuildingIdSet == null)
            RebuildCache();
    }

    private void RebuildCache()
    {
        disabledBuildingIdSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (disabledBuildingIds == null)
            return;

        for (int i = 0; i < disabledBuildingIds.Count; i++)
        {
            string buildingId = disabledBuildingIds[i];
            if (string.IsNullOrWhiteSpace(buildingId))
                continue;

            disabledBuildingIdSet.Add(buildingId.Trim());
        }
    }
}
