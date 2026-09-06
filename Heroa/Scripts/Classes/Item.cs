using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Item
{

    public int itemID; // 아이템의 고유 ID값, 중복 불가능
    public string itemName; // 아이템의 이름, 중복 가능
    public string abilityDescription; // 아이템의 능력 설명
    public string settingDescription; // 아이템의 설정 설명
    public string itemSpriteName;
    public bool countable; // 아이템이 통합해서 셀 수 있는 아이템인지
    public int itemCount; // 아이템 소지 갯수
    public int itemRate; // 별 갯수
    public Sprite itemSprite; // 아이템 스프라이트
    public ItemType itemType; // 아이템 종류
    public Equipment equipment;

    public enum ItemType
    {
        Weapon, // 무기
        Head, // 머리
        Top, // 상의
        Bottom, // 하의
        Shoes, // 신발
        ETC // 장비 아이템 아님
    }

    public Item(int _ItemID, string _ItemName, string _AbilityDescription, string _SettingDescription, string _ItemSpriteName, ItemType _ItemType, int _Rate, bool _Countable = false, int _ItemCount = 1,
        float _Hp = 0, float _Damage = 0, float _Defense = 0, float _CriticalDamage = 0, float _CriticalRate = 0, Attribute _Attribute = Attribute.ETC, 
        float _UpHp = 0, float _UpDamge = 0, float _UpDefense = 0, float _UpCD = 0, float _UpCR = 0, string _Ability = "None", float _AbilityValue = 0, int _AbilityTurn = 0, float _UpAbilityValue = 0)
    {
        itemID = _ItemID;
        itemName = _ItemName;
        abilityDescription = _AbilityDescription;
        settingDescription = _SettingDescription;
        itemSpriteName = _ItemSpriteName;
        countable = _Countable;
        itemRate = _Rate;
        itemCount = _ItemCount;
        itemType = _ItemType;

        itemSprite = Resources.Load("Images/ItemImages/" + itemSpriteName, typeof(Sprite)) as Sprite;

        if (itemSprite == null && itemSpriteName != "None")
            Debug.LogError("Item : item " +itemID + "," +itemName + " sprite is Null : " + itemSpriteName);

        equipment = new Equipment(_Hp, _Damage, _Defense, _CriticalDamage, _CriticalRate, _Attribute, _UpHp, _UpDamge, _UpDefense, _UpCD, _UpCR, _Ability, _AbilityValue, _AbilityTurn, _UpAbilityValue);
    }

}
