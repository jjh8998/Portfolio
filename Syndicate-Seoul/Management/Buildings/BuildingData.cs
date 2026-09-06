using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BuildingData
{
    public string ID;
    public BuildingCategory category;
    public string name;
    public string description;

    [Header("자원 생산")]
    public int bonusIncome;
    public int powerOutput;
    public int powerConsumption;
    public int rpOutput;

    [Header("건설")]
    public int constructionCost;
    public int constructionDay;
    [Tooltip("인스펙터에서 직접 연결하는 건물 프리팹입니다. 지정되어 있으면 문자열 Resources 경로보다 우선 사용됩니다.")]
    public GameObject prefab;
    [Tooltip("Resources 기준 건물 프리팹 경로입니다. 비워두면 ID/카테고리로 기본 경로를 추론합니다.")]
    public string prefabResourcePath;

    [Header("업그레이드")]
    [NonSerialized]
    public List<BuildingData> nextUpgradeBuildings = new List<BuildingData>();
    public List<string> nextUpgradeBuildingIds = new List<string>();
    public string requiredResearchId;

    [Header("능력")]
    public BuildingAbility ability;

    [Header("공장 전용 정적 설정")]
    public int activationIntervalDays;
    public int cardYieldAmount = 1;
    public int emergencyOrderCost = 100;
    public string rewardPackFilter;
    public int minRewardTier;
    public int maxRewardTier;

    [Header("공장 티어 확률")]
    public int tier1Rate;
    public int tier2Rate;
    public int tier3Rate;
    public int tier4Rate;
    public int tier5Rate;

    public bool IsEmptySlot()
    {
        return category == BuildingCategory.Empty;
    }

    public bool IsFactory()
    {
        return category == BuildingCategory.Factory;
    }

    public GameObject LoadPrefab()
    {
        if (prefab != null)
        {
            return prefab;
        }

        string path = GetPrefabResourcePath();
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        GameObject loadedPrefab = Resources.Load<GameObject>(path);
        if (loadedPrefab == null)
        {
            Debug.LogWarning($"[BuildingData] Prefab load failed. ID: {ID}, path: Resources/{path}");
        }

        return loadedPrefab;
    }

    public GameObject LoadHologramPrefab()
    {
        string path = GetHologramPrefabResourcePath();
        if (!string.IsNullOrWhiteSpace(path))
        {
            GameObject hologramPrefab = Resources.Load<GameObject>(path);
            if (hologramPrefab != null)
            {
                return hologramPrefab;
            }
        }

        return LoadPrefab();
    }

    public void AutoAssignPrefabReference()
    {
        if (prefab != null)
        {
            return;
        }

        string path = GetPrefabResourcePath();
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        prefab = Resources.Load<GameObject>(path);
    }

    public string GetPrefabResourcePath()
    {
        if (!string.IsNullOrWhiteSpace(prefabResourcePath))
        {
            return prefabResourcePath.Trim();
        }

        if (!TryGetBuildingTier(out int tier))
        {
            return string.Empty;
        }

        string folderPrefix = GetPrefabFolderPrefix();
        if (string.IsNullOrWhiteSpace(folderPrefix))
        {
            return string.Empty;
        }

        string prefabName = $"{folderPrefix}{tier}";
        return $"Buildings/{folderPrefix}/{prefabName}/{prefabName}";
    }

    public string GetHologramPrefabResourcePath()
    {
        string basePath = GetPrefabResourcePath();
        if (string.IsNullOrWhiteSpace(basePath))
        {
            return string.Empty;
        }

        int slashIndex = basePath.LastIndexOf('/');
        if (slashIndex < 0 || slashIndex >= basePath.Length - 1)
        {
            return string.Empty;
        }

        string folderPath = basePath.Substring(0, slashIndex);
        string prefabName = basePath.Substring(slashIndex + 1);
        if (!folderPath.StartsWith("Buildings/", StringComparison.Ordinal))
        {
            return string.Empty;
        }

        return $"BuildingsHologram/{folderPath.Substring("Buildings/".Length)}/{prefabName}_Hologram";
    }

    private bool TryGetBuildingTier(out int tier)
    {
        tier = 0;
        if (string.IsNullOrWhiteSpace(ID))
        {
            return false;
        }

        string cleanId = ID.Trim();
        int digitStart = cleanId.Length;
        while (digitStart > 0 && char.IsDigit(cleanId[digitStart - 1]))
        {
            digitStart--;
        }

        return digitStart < cleanId.Length
            && int.TryParse(cleanId.Substring(digitStart), out tier)
            && tier > 0;
    }

    public int GetTierOrDefault()
    {
        return TryGetBuildingTier(out int tier) ? tier : 1;
    }

    private string GetPrefabFolderPrefix()
    {
        switch (category)
        {
            case BuildingCategory.Economy:
                return "Bank";
            case BuildingCategory.Power:
                return "Ener";
            case BuildingCategory.Research:
                return "Aca";
            case BuildingCategory.Factory:
                return "Fac";
            case BuildingCategory.Support:
                return "Tactic";
        }

        if (string.IsNullOrWhiteSpace(ID))
        {
            return string.Empty;
        }

        switch (char.ToUpperInvariant(ID.Trim()[0]))
        {
            case 'E':
                return "Bank";
            case 'P':
                return "Ener";
            case 'R':
                return "Aca";
            case 'F':
                return "Fac";
            default:
                return string.Empty;
        }
    }

    public virtual BuildingData Clone()
    {
        BuildingData clone = (BuildingData)MemberwiseClone();
        clone.nextUpgradeBuildingIds = nextUpgradeBuildingIds != null
            ? new List<string>(nextUpgradeBuildingIds)
            : new List<string>();
        clone.nextUpgradeBuildings = nextUpgradeBuildings != null
            ? new List<BuildingData>(nextUpgradeBuildings)
            : new List<BuildingData>();
        return clone;
    }
}
