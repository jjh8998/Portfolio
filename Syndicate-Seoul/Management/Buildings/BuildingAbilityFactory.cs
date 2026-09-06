using System;

public static class BuildingAbilityFactory
{
    public static BuildingAbility Create(string _abilityId)
    {
        if (string.IsNullOrWhiteSpace(_abilityId))
            return null;

        switch (_abilityId.Trim())
        {
            case "Ability_CardIncinerationPower":
                return new CardIncinerationPowerAbility();
            case "Ability_battle_maxHpUp":
            case "Ability_Support_Battle_MaxHpUp":
                return new BattleMaxHpUpAbility();
            case "Ability_Support_Battle_FirstTurnShield":
                return new BattleFirstTurnShieldAbility();
            case "Ability_Support_Battle_EnemyCardCostUp":
                return new BattleEnemyCardCostUpAbility();
            case "Ability_Support_Battle_InsertRansomware":
                return new BattleInsertRansomwareAbility();

            default:
                return null;
        }
    }

    public static BuildingAbility Create(string _abilityId, string _triggerType, int _abilityValue)
    {
        BuildingAbility ability = Create(_abilityId);
        ApplyCsvValues(ability, _triggerType, _abilityValue);
        return ability;
    }

    public static void ApplyCsvValues(BuildingAbility _ability, string _triggerType, int _abilityValue)
    {
        if (_ability == null)
            return;

        string triggerType = string.IsNullOrWhiteSpace(_triggerType) ? string.Empty : _triggerType.Trim();

        if (_ability is CardIncinerationPowerAbility cardIncinerationPowerAbility)
        {
            cardIncinerationPowerAbility.triggerType = triggerType;
            if (_abilityValue > 0)
                cardIncinerationPowerAbility.powerGain = _abilityValue;
            return;
        }

        if (_ability is BattleMaxHpUpAbility battleMaxHpUpAbility)
        {
            battleMaxHpUpAbility.triggerType = triggerType;
            if (_abilityValue > 0)
                battleMaxHpUpAbility.maxHpBonus = _abilityValue;
            return;
        }

        if (_ability is BattleFirstTurnShieldAbility battleFirstTurnShieldAbility)
        {
            battleFirstTurnShieldAbility.triggerType = triggerType;
            if (_abilityValue > 0)
                battleFirstTurnShieldAbility.shieldAmount = _abilityValue;
            return;
        }

        if (_ability is BattleEnemyCardCostUpAbility battleEnemyCardCostUpAbility)
        {
            battleEnemyCardCostUpAbility.triggerType = triggerType;
            if (_abilityValue > 0)
                battleEnemyCardCostUpAbility.affectedCardCount = _abilityValue;
            return;
        }

        if (_ability is BattleInsertRansomwareAbility battleInsertRansomwareAbility)
        {
            battleInsertRansomwareAbility.triggerType = triggerType;
            if (_abilityValue > 0)
                battleInsertRansomwareAbility.ransomwareInsertCount = _abilityValue;
        }
    }

    public static bool TryGetCardIncinerationValues(
        BuildingAbility _ability,
        out int _powerGain,
        out int _durationMonths,
        out int _cardCost)
    {
        _powerGain = 0;
        _durationMonths = 0;
        _cardCost = 0;

        if (_ability is not CardIncinerationPowerAbility ability)
            return false;

        _powerGain = ability.powerGain;
        _durationMonths = ability.durationMonths;
        _cardCost = ability.cardCost;
        return true;
    }
}

[Serializable]
public class BattleMaxHpUpAbility : BuildingAbility
{
    public string triggerType = string.Empty;
    public int maxHpBonus = 10;
}

[Serializable]
public class BattleFirstTurnShieldAbility : BuildingAbility
{
    public string triggerType = string.Empty;
    public int shieldAmount = 1;
}

[Serializable]
public class BattleEnemyCardCostUpAbility : BuildingAbility
{
    public string triggerType = string.Empty;
    public int affectedCardCount = 1;
}

[Serializable]
public class BattleInsertRansomwareAbility : BuildingAbility
{
    public string triggerType = string.Empty;
    public int ransomwareInsertCount = 1;
}
