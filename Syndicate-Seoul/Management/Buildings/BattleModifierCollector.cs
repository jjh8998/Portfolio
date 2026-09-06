using System.Collections.Generic;
using UnityEngine;

public static class BattleModifierCollector
{
    public static BattleModifierSnapshot Collect(FactionManager faction)
    {
        BattleModifierSnapshot snapshot = BattleModifierSnapshot.Empty();
        if (faction == null)
            return snapshot;

        CollectResearchModifiers(faction, snapshot);
        CollectSupportBuildingModifiers(faction, snapshot);
        CollectLegacyBuffBuildingModifiers(faction, snapshot);
        return snapshot;
    }

    private static void CollectResearchModifiers(FactionManager faction, BattleModifierSnapshot snapshot)
    {
        FactionResearchState researchState = faction.GetResearchState;
        if (researchState == null || researchState.modifiers == null)
            return;

        ResearchModifierSet modifiers = researchState.modifiers;
        snapshot.maxHpBonus += Mathf.Max(0, modifiers.ceoBattleMaxHpBonus);
        snapshot.maxEnergyBonus += Mathf.Max(0, modifiers.battleMaxEnergyBonus);
    }

    private static void CollectSupportBuildingModifiers(FactionManager faction, BattleModifierSnapshot snapshot)
    {
        List<BattleSupportEffect> supportEffects = SupportBuildingApplier.Collect(faction);
        if (supportEffects == null || supportEffects.Count == 0)
            return;

        snapshot.supportEffects.AddRange(supportEffects);

        for (int i = 0; i < supportEffects.Count; i++)
        {
            BattleSupportEffect effect = supportEffects[i];
            if (effect == null)
                continue;

            switch (effect.effectType)
            {
                case BattleSupportEffectType.MaxHpBonus:
                    snapshot.maxHpBonus += Mathf.Max(0, effect.value);
                    break;
                case BattleSupportEffectType.FirstTurnShield:
                    snapshot.startShieldBonus += Mathf.Max(0, effect.value);
                    break;
            }
        }
    }

    private static void CollectLegacyBuffBuildingModifiers(FactionManager faction, BattleModifierSnapshot snapshot)
    {
        List<BuffBuildingEffect> legacyEffects = BuffBuildingApplier.Collect(faction);
        if (legacyEffects == null || legacyEffects.Count == 0)
            return;

        snapshot.legacyBuffEffects.AddRange(legacyEffects);
    }
}
