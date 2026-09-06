using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class SupportBuildingApplierTests
{
    private readonly List<GameObject> createdObjects = new List<GameObject>();

    [TearDown]
    public void TearDown()
    {
        for (int i = 0; i < createdObjects.Count; i++)
        {
            if (createdObjects[i] != null)
                Object.DestroyImmediate(createdObjects[i]);
        }

        createdObjects.Clear();
    }

    [Test]
    public void Collect_RansomwareSupportBuilding_ReturnsDeckInfectionEffect()
    {
        FactionManager faction = CreateFaction();
        CityScript city = CreateCity();
        faction.ownedCities.Add(city);
        city.cityData.buildings = new List<BuildingInstance>
        {
            new BuildingInstance(new BuildingData
            {
                ID = "SUPPORT_RANSOMWARE_1",
                name = "랜섬웨어 지원 건물",
                category = BuildingCategory.Support,
                ability = new BattleInsertRansomwareAbility { ransomwareInsertCount = 2 }
            })
        };

        List<BattleSupportEffect> effects = SupportBuildingApplier.Collect(faction);

        Assert.AreEqual(1, effects.Count);
        Assert.AreEqual(BattleSupportEffectType.InsertRansomware, effects[0].effectType);
        Assert.AreEqual("support_insert_ransomware", effects[0].effectId);
        Assert.AreEqual("SUPPORT_RANSOMWARE_1", effects[0].sourceBuildingId);
        Assert.AreEqual(2, effects[0].value);
    }

    [Test]
    public void Create_InsertRansomwareAbility_AppliesCsvValue()
    {
        BuildingAbility ability = BuildingAbilityFactory.Create("Ability_Support_Battle_InsertRansomware", "battle_start", 3);

        Assert.IsInstanceOf<BattleInsertRansomwareAbility>(ability);
        var insertRansomwareAbility = (BattleInsertRansomwareAbility)ability;
        Assert.AreEqual("battle_start", insertRansomwareAbility.triggerType);
        Assert.AreEqual(3, insertRansomwareAbility.ransomwareInsertCount);
    }

    [Test]
    public void Create_SupportBattleMaxHpUpAbility_AppliesCsvValue()
    {
        BuildingAbility ability = BuildingAbilityFactory.Create("Ability_Support_Battle_MaxHpUp", "Passive", 20);

        Assert.IsInstanceOf<BattleMaxHpUpAbility>(ability);
        var maxHpAbility = (BattleMaxHpUpAbility)ability;
        Assert.AreEqual("Passive", maxHpAbility.triggerType);
        Assert.AreEqual(20, maxHpAbility.maxHpBonus);
    }

    [Test]
    public void Create_SupportBattleFirstTurnShieldAbility_AppliesCsvValue()
    {
        BuildingAbility ability = BuildingAbilityFactory.Create("Ability_Support_Battle_FirstTurnShield", "Passive", 15);

        Assert.IsInstanceOf<BattleFirstTurnShieldAbility>(ability);
        var shieldAbility = (BattleFirstTurnShieldAbility)ability;
        Assert.AreEqual("Passive", shieldAbility.triggerType);
        Assert.AreEqual(15, shieldAbility.shieldAmount);
    }

    [Test]
    public void Create_SupportBattleEnemyCardCostUpAbility_AppliesCsvValue()
    {
        BuildingAbility ability = BuildingAbilityFactory.Create("Ability_Support_Battle_EnemyCardCostUp", "Passive", 2);

        Assert.IsInstanceOf<BattleEnemyCardCostUpAbility>(ability);
        var enemyCostAbility = (BattleEnemyCardCostUpAbility)ability;
        Assert.AreEqual("Passive", enemyCostAbility.triggerType);
        Assert.AreEqual(2, enemyCostAbility.affectedCardCount);
    }

    [Test]
    public void Create_SupportBattleInsertRansomwareAbility_AppliesCsvValue()
    {
        BuildingAbility ability = BuildingAbilityFactory.Create("Ability_Support_Battle_InsertRansomware", "Passive", 4);

        Assert.IsInstanceOf<BattleInsertRansomwareAbility>(ability);
        var insertRansomwareAbility = (BattleInsertRansomwareAbility)ability;
        Assert.AreEqual("Passive", insertRansomwareAbility.triggerType);
        Assert.AreEqual(4, insertRansomwareAbility.ransomwareInsertCount);
    }

    [Test]
    public void Collect_SupportBattleAbilities_ReturnsSupportEffects()
    {
        FactionManager faction = CreateFaction();
        CityScript city = CreateCity();
        faction.ownedCities.Add(city);
        city.cityData.buildings = new List<BuildingInstance>
        {
            CreateSupportBuilding("SUPPORT_MAX_HP", new BattleMaxHpUpAbility { maxHpBonus = 20 }),
            CreateSupportBuilding("SUPPORT_SHIELD", new BattleFirstTurnShieldAbility { shieldAmount = 15 }),
            CreateSupportBuilding("SUPPORT_ENEMY_COST", new BattleEnemyCardCostUpAbility { affectedCardCount = 2 }),
            CreateSupportBuilding("SUPPORT_RANSOMWARE", new BattleInsertRansomwareAbility { ransomwareInsertCount = 3 })
        };

        List<BattleSupportEffect> effects = SupportBuildingApplier.Collect(faction);

        Assert.AreEqual(4, effects.Count);
        AssertSupportEffect(effects[0], BattleSupportEffectType.MaxHpBonus, "support_max_hp_up", "SUPPORT_MAX_HP", 20);
        AssertSupportEffect(effects[1], BattleSupportEffectType.FirstTurnShield, "support_first_turn_shield", "SUPPORT_SHIELD", 15);
        AssertSupportEffect(effects[2], BattleSupportEffectType.EnemyCardCostUp, "support_enemy_card_cost_up", "SUPPORT_ENEMY_COST", 2);
        AssertSupportEffect(effects[3], BattleSupportEffectType.InsertRansomware, "support_insert_ransomware", "SUPPORT_RANSOMWARE", 3);
    }

    [Test]
    public void Collect_FirstTurnShieldSupport_AddsStartShieldBonus()
    {
        FactionManager faction = CreateFaction();
        CityScript city = CreateCity();
        faction.ownedCities.Add(city);
        city.cityData.buildings = new List<BuildingInstance>
        {
            CreateSupportBuilding("SUPPORT_SHIELD_A", new BattleFirstTurnShieldAbility { shieldAmount = 15 }),
            CreateSupportBuilding("SUPPORT_SHIELD_B", new BattleFirstTurnShieldAbility { shieldAmount = 25 })
        };

        BattleModifierSnapshot snapshot = BattleModifierCollector.Collect(faction);

        Assert.AreEqual(40, snapshot.startShieldBonus);
    }

    private FactionManager CreateFaction()
    {
        var go = new GameObject("Test Faction");
        createdObjects.Add(go);
        return go.AddComponent<FactionManager>();
    }

    private CityScript CreateCity()
    {
        var go = new GameObject("Test City");
        createdObjects.Add(go);
        var city = go.AddComponent<CityScript>();
        city.cityData = new CityData { buildings = new List<BuildingInstance>() };
        return city;
    }

    private BuildingInstance CreateSupportBuilding(string _id, BuildingAbility _ability)
    {
        return new BuildingInstance(new BuildingData
        {
            ID = _id,
            name = _id,
            category = BuildingCategory.Support,
            ability = _ability
        });
    }

    private void AssertSupportEffect(
        BattleSupportEffect _effect,
        BattleSupportEffectType _effectType,
        string _effectId,
        string _sourceBuildingId,
        int _value)
    {
        Assert.NotNull(_effect);
        Assert.AreEqual(_effectType, _effect.effectType);
        Assert.AreEqual(_effectId, _effect.effectId);
        Assert.AreEqual(_sourceBuildingId, _effect.sourceBuildingId);
        Assert.AreEqual(_value, _effect.value);
    }
}
