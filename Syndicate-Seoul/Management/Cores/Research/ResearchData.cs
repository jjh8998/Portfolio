using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public enum ResearchEffectType
{
    None,
    ResearchEffect_AddBuildingSlot,
    ResearchEffect_Battle_MaxHpUp,
    ResearchEffect_BuildingPowerReduction,
    ResearchEffect_MaxEnergyUp,
    ResearchEffect_MaxEmployeeSlotUp,
    ResearchEffect_CardPack_CardCountUp,
    ResearchEffect_RpIncomeUp
}

[Serializable]
public class ResearchCategoryRequirement
{
    public string category;
    public int count;
}

[Serializable]
public class ResearchData
{
    public string id;
    public string name;
    public string category;
    public string description;
    public string abilityDescription;
    public string iconImageId;
    public Image iconImage;
    public Sprite iconSprite;
    public int costRP;
    public bool isPatentResearch;
    public List<string> prerequisiteResearchIds = new List<string>();
    public List<ResearchCategoryRequirement> unlockCategoryRequirements = new List<ResearchCategoryRequirement>();
    public ResearchEffectType effectType;
    public string effectTargetId;
    public int effectValue;

    public void EnsureInitialized()
    {
        if (prerequisiteResearchIds == null)
            prerequisiteResearchIds = new List<string>();

        if (unlockCategoryRequirements == null)
            unlockCategoryRequirements = new List<ResearchCategoryRequirement>();
    }
}
