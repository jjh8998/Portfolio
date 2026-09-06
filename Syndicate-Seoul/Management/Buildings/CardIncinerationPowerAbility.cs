using System;

[Serializable]
public class CardIncinerationPowerAbility : BuildingAbility
{
    public string triggerType = string.Empty;
    public int powerGain = 30;
    public int durationMonths = 3;
    public int cardCost = 1;
}
