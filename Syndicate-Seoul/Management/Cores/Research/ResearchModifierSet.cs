using System;
using UnityEngine.Serialization;

[Serializable]
public class ResearchModifierSet
{
    public int creditIncomePercentBonus;
    public int researchOutputPercentBonus;
    public int powerOutputPercentBonus;
    public int buildingPowerReduction;
    public int factoryOutputPercentBonus;
    public int addBuildingSlot;
    public int ceoBattleMaxHpBonus;
    public int battleMaxEnergyBonus;
    public int maxEmployeeSlotBonus;
    public int cardPackOpenCardCountBonus;
    public int rpIncomeBonus;

    public void Clear()
    {
        creditIncomePercentBonus = 0;
        researchOutputPercentBonus = 0;
        powerOutputPercentBonus = 0;
        buildingPowerReduction = 0;
        factoryOutputPercentBonus = 0;
        addBuildingSlot = 0;
        ceoBattleMaxHpBonus = 0;
        battleMaxEnergyBonus = 0;
        maxEmployeeSlotBonus = 0;
        cardPackOpenCardCountBonus = 0;
        rpIncomeBonus = 0;
    }
}
