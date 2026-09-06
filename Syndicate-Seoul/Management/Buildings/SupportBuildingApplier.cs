using System.Collections.Generic;
using UnityEngine;

public static class SupportBuildingApplier
{
    public static List<BattleSupportEffect> Collect(FactionManager _faction)
    {
        List<BattleSupportEffect> effects = new List<BattleSupportEffect>();

        if (_faction == null || _faction.ownedCities == null)
            return effects;

        float powerSatisfactionRate = ManagementResourceCalculator.CalculatePowerSatisfactionRate(_faction);

        for (int i = 0; i < _faction.ownedCities.Count; i++)
        {
            CityScript city = _faction.ownedCities[i];
            if (city == null || city.cityData == null || city.cityData.buildings == null)
                continue;

            List<BuildingInstance> buildings = city.cityData.buildings;
            for (int j = 0; j < buildings.Count; j++)
            {
                BuildingInstance building = buildings[j];
                if (building == null || building.IsEmptySlot() || building.IsUnderConstruction() || building.IsDisabled())
                    continue;

                if (building.data == null || building.data.category != BuildingCategory.Support)
                    continue;

                if (building.data.ability is BattleMaxHpUpAbility maxHpAbility)
                {
                    effects.Add(new BattleSupportEffect
                    {
                        effectId = "support_max_hp_up",
                        sourceBuildingId = building.data.ID,
                        sourceBuildingName = building.data.name,
                        effectType = BattleSupportEffectType.MaxHpBonus,
                        value = Mathf.Max(0, Mathf.FloorToInt(maxHpAbility.maxHpBonus * powerSatisfactionRate))
                    });
                    continue;
                }

                if (building.data.ability is BattleFirstTurnShieldAbility firstTurnShieldAbility)
                {
                    int value = Mathf.Max(0, Mathf.FloorToInt(firstTurnShieldAbility.shieldAmount * powerSatisfactionRate));
                    if (value > 0)
                    {
                        effects.Add(new BattleSupportEffect
                        {
                            effectId = "support_first_turn_shield",
                            sourceBuildingId = building.data.ID,
                            sourceBuildingName = building.data.name,
                            effectType = BattleSupportEffectType.FirstTurnShield,
                            value = value
                        });
                    }
                    continue;
                }

                if (building.data.ability is BattleEnemyCardCostUpAbility enemyCardCostUpAbility)
                {
                    int value = Mathf.Max(0, Mathf.FloorToInt(enemyCardCostUpAbility.affectedCardCount * powerSatisfactionRate));
                    if (value > 0)
                    {
                        effects.Add(new BattleSupportEffect
                        {
                            effectId = "support_enemy_card_cost_up",
                            sourceBuildingId = building.data.ID,
                            sourceBuildingName = building.data.name,
                            effectType = BattleSupportEffectType.EnemyCardCostUp,
                            value = value
                        });
                    }
                    continue;
                }

                if (building.data.ability is BattleInsertRansomwareAbility insertRansomwareAbility)
                {
                    effects.Add(new BattleSupportEffect
                    {
                        effectId = "support_insert_ransomware",
                        sourceBuildingId = building.data.ID,
                        sourceBuildingName = building.data.name,
                        effectType = BattleSupportEffectType.InsertRansomware,
                        value = Mathf.Max(0, Mathf.FloorToInt(insertRansomwareAbility.ransomwareInsertCount * powerSatisfactionRate))
                    });
                }
            }
        }

        return effects;
    }
}
