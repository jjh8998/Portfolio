using System;

[Serializable]
public class BattleSupportEffect
{
    public string effectId;
    public string sourceBuildingId;
    public string sourceBuildingName;
    public BattleSupportEffectType effectType;
    public int value;
}

public enum BattleSupportEffectType
{
    MaxHpBonus,
    FirstTurnShield,
    EnemyCardCostUp,
    InsertRansomware
}
