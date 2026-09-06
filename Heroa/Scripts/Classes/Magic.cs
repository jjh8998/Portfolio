using System.Collections;
using System.Collections.Generic;
// using UnityEditor.U2D.Path;
using UnityEngine;

[System.Serializable]
public class Magic
{
    public int ID;
    public string name;
    public string abilityDescription;
    public string settingDescription;
    public Sprite magicSprite;
    public MagicType magicTpye;
    public Attribute magicAttribute;
    public int cooldown;
    public float fixedDamage;
    public float percentDamage;
    public float criticalDamage;
    public float criticalRate;
    public string ability;
    public int abilityAmount;
    public float abilityPercent;
    public int abilityMaintainTurn;
    public string idiom;
    public string idiomMeaning;
    public int needVower;
    public int needConsanant;
    public string saying;
    public string sayingMeaning;

    public enum MagicType
    {
        Attack,
        Heal,
        ETC // 
    }

    public Magic(int _ID, string _Name, string _Ability, string _Description, string _ImageName, string _MagicTpye, string _MagicAttribute, int _Cooldown, float _FixedDamage, float _PercentDamage, 
        float _CriticalDamage, float _CriticalRate,  string ability, int _AbitliyAmount, float _AbilityPercent, int _AbilityMaintainTurn, string _Idiom, string _IdiomMeaning, int _NeedVower, int _NeedConsanant, string _Saying, string _SayingMeaning)
    {
        ID = _ID;
        name = _Name;
        abilityDescription = _Ability;
        settingDescription = _Description;

        magicSprite = Resources.Load("Images/MagicImages/" + _ImageName, typeof(Sprite)) as Sprite;

        if (magicSprite == null && _ImageName != "None")
        {
            magicSprite = Resources.Load("Images/Null_Image", typeof(Sprite)) as Sprite;
            Debug.LogError("Magic : Magic " + ID + "," + name + " sprite is Null");
        }

        switch (_MagicTpye)
        {
            case "Attack":
                magicTpye = MagicType.Attack;
                break;
            case "Heal":
                magicTpye = MagicType.Heal;
                break;
            default:
                Debug.LogError("Magic - " + name + " have No Type!");
                magicTpye = MagicType.ETC;
                break;
        }

        switch (_MagicAttribute)
        {
            case "Fire":
                magicAttribute = Attribute.Fire;
                break;
            case "Water":
                magicAttribute = Attribute.Water;
                break;
            case "Grass":
                magicAttribute = Attribute.Grass;
                break;
            case "ETC" :
                magicAttribute = Attribute.ETC;
                    break;
            default:
                Debug.LogError("Magic : " + name + " has no Atrribute");
                break;
        }

        fixedDamage = _FixedDamage;
        percentDamage = _PercentDamage;
        criticalDamage = _CriticalDamage;
        criticalRate = _CriticalRate;
        cooldown = _Cooldown;
        ability = _Ability;
        abilityAmount = _AbitliyAmount;
        abilityPercent = _AbilityPercent;
        abilityMaintainTurn = _AbilityMaintainTurn;
        idiom = _Idiom;
        idiomMeaning = _IdiomMeaning;
        needVower = _NeedVower;
        needConsanant = _NeedConsanant;
        saying = _Saying;
        sayingMeaning = _SayingMeaning;
    }
}
