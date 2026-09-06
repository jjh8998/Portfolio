using UnityEngine;

public enum BuildingConstructionFailReason
{
    None,
    InvalidCity,
    InvalidOwner,
    NotOwner,
    InvalidBuilding,
    InvalidBuildingList,
    InvalidSlot,
    SlotLocked,
    SlotNotBuildable,
    SlotUnderConstruction,
    CityUnderConstruction,
    ResearchLocked,
    NotEnoughCredit,
    NotEnoughPower,
    DisabledForAI
}

public struct BuildingConstructionValidationResult
{
    public bool canBuild;
    public BuildingConstructionFailReason failReason;
    public string message;
    public int requiredCredit;
    public int requiredAdditionalPower;

    public static BuildingConstructionValidationResult Success(int requiredCredit, int requiredAdditionalPower)
    {
        return new BuildingConstructionValidationResult
        {
            canBuild = true,
            failReason = BuildingConstructionFailReason.None,
            message = string.Empty,
            requiredCredit = requiredCredit,
            requiredAdditionalPower = requiredAdditionalPower
        };
    }

    public static BuildingConstructionValidationResult Fail(
        BuildingConstructionFailReason failReason,
        string message,
        int requiredCredit = 0,
        int requiredAdditionalPower = 0)
    {
        return new BuildingConstructionValidationResult
        {
            canBuild = false,
            failReason = failReason,
            message = message,
            requiredCredit = requiredCredit,
            requiredAdditionalPower = requiredAdditionalPower
        };
    }
}

public static class BuildingConstructionValidator
{
    public static BuildingConstructionValidationResult Validate(
        CityScript city,
        int slotIndex,
        BuildingData building,
        FactionManager actorFaction,
        bool requireActorOwnsCity = true,
        bool checkAIRestriction = false)
    {
        if (city == null || city.cityData == null)
            return BuildingConstructionValidationResult.Fail(
                BuildingConstructionFailReason.InvalidCity,
                "City data is missing.");

        FactionManager owner = city.cityData.owner;
        if (owner == null)
            return BuildingConstructionValidationResult.Fail(
                BuildingConstructionFailReason.InvalidOwner,
                "City owner is missing.");

        if (building == null)
            return BuildingConstructionValidationResult.Fail(
                BuildingConstructionFailReason.InvalidBuilding,
                "Building data is missing.");

        int requiredCredit = Mathf.Max(0, building.constructionCost);

        if (checkAIRestriction && FactionAIBuildingRestrictionService.IsDisabledForAI(building))
            return BuildingConstructionValidationResult.Fail(
                BuildingConstructionFailReason.DisabledForAI,
                "Building is disabled for AI.",
                requiredCredit);

        if (requireActorOwnsCity && actorFaction != owner)
            return BuildingConstructionValidationResult.Fail(
                BuildingConstructionFailReason.NotOwner,
                "Actor faction does not own this city.",
                requiredCredit);

        if (city.cityData.buildings == null)
            return BuildingConstructionValidationResult.Fail(
                BuildingConstructionFailReason.InvalidBuildingList,
                "Building list is missing.",
                requiredCredit);

        if (slotIndex < 0 || slotIndex >= city.cityData.buildings.Count)
            return BuildingConstructionValidationResult.Fail(
                BuildingConstructionFailReason.InvalidSlot,
                "Invalid building slot.",
                requiredCredit);

        if (slotIndex >= city.GetAvailableBuildingSlotCount())
            return BuildingConstructionValidationResult.Fail(
                BuildingConstructionFailReason.SlotLocked,
                "Building slot is locked.",
                requiredCredit);

        if (!city.IsBuildingSlotBuildable(slotIndex))
            return BuildingConstructionValidationResult.Fail(
                BuildingConstructionFailReason.SlotNotBuildable,
                "Building slot is not buildable.",
                requiredCredit);

        if (city.IsSlotScavengerLocked(slotIndex))
            return BuildingConstructionValidationResult.Fail(
                BuildingConstructionFailReason.SlotLocked,
                "Building slot is occupied by scavengers.",
                requiredCredit);

        BuildingInstance instance = city.GetBuildingInstance(slotIndex);
        if (instance != null && instance.IsUnderConstruction())
            return BuildingConstructionValidationResult.Fail(
                BuildingConstructionFailReason.SlotUnderConstruction,
                "Selected building slot is already under construction.",
                requiredCredit);

        if (city.HasAnyBuildingUnderConstruction())
            return BuildingConstructionValidationResult.Fail(
                BuildingConstructionFailReason.CityUnderConstruction,
                "A building is under construction or upgrade.",
                requiredCredit);

        if (!owner.CanUseBuilding(building))
            return BuildingConstructionValidationResult.Fail(
                BuildingConstructionFailReason.ResearchLocked,
                "Building is locked by research.",
                requiredCredit);

        int requiredAdditionalPower = city.GetRequiredAdditionalPowerForBuild(slotIndex, building);
        if (owner.GetNetPower < requiredAdditionalPower)
            return BuildingConstructionValidationResult.Fail(
                BuildingConstructionFailReason.NotEnoughPower,
                "Not enough power.",
                requiredCredit,
                requiredAdditionalPower);

        if (owner.GetCredit < requiredCredit)
            return BuildingConstructionValidationResult.Fail(
                BuildingConstructionFailReason.NotEnoughCredit,
                "Not enough credit.",
                requiredCredit,
                requiredAdditionalPower);

        return BuildingConstructionValidationResult.Success(requiredCredit, requiredAdditionalPower);
    }
}
