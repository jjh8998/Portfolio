using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 장비템 속성
[System.Serializable]
public class Equipment
{
    public float equipment_Hp;
    public float equipment_Damage;
    public float equipment_Defense;
    public float equipment_CriticalDamage;
    public float equipment_CriticalRate;

    public Attribute itemAttribute; // 아이템 속성

    public int itemLV;
    public float itemNowExp;

    // 아이템 레벨업에 따른 성장 능력치
    public float upHp;
    public float upDamage;
    public float upDefense;
    public float upCD;
    public float upCR;
    public EquipmentAbility equipmentAbility;
    public float abilityValue;
    public int abilityTurn;
    public float upAbilityValue;

    // 아이템의 진짜 능력치 (강화 계산 포함)
    public float realHp = 0f;
    public float realDamage = 0f;
    public float realDefense = 0f;
    public float realCriticalDamage = 0f;
    public float realCriticalRate = 0f;
    
    public Equipment(float _Hp, float _Damage, float _Defense, float _CriticalDamage, float _CriticalRate, Attribute _Attribute, 
        float _UpHp, float _UpDamage, float _UpDefense, float _UpCD, float _UpCR, string _Ability, float _AbilityValue, int _AbilityTurn, float _UpAbilityValue)
    {
        equipment_Hp = _Hp;
        equipment_Damage = _Damage;
        equipment_Defense = _Defense;
        equipment_CriticalDamage = _CriticalDamage;
        equipment_CriticalRate = _CriticalRate;
        itemAttribute = _Attribute;
        upHp = _UpHp;
        upDamage = _UpDamage;
        upDefense = _UpDefense;
        upCD = _UpCD;
        upCR = _UpCR;

        switch (_Ability)
        {
            case "IncreaseMagicDamage": equipmentAbility = EquipmentAbility.IncreaseMagicDamage; break;
            case "DecreaseAttributeDamage": equipmentAbility = EquipmentAbility.DecreaseAttributeDamage; break;
            case "ChargeAttack": equipmentAbility = EquipmentAbility.ChargeAttack; break;
        }

        abilityValue = _AbilityValue;
        abilityTurn = _AbilityTurn;
        upAbilityValue = _UpAbilityValue;
    }
}
