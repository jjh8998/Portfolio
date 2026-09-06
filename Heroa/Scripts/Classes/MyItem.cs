using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 플레이어가 소지한 아이템 저장용
[System.Serializable]
public class MyItem
{
    public string uniqueId;
    public int itemID;
    public int itemCount;
    public int itemLV;
    public float itemNowExp;


    public MyItem(int _ItemID, int _ItemCount = 1, int _ItemLV = 1, float _ItemExp = 0)
    {
        uniqueId = System.Guid.NewGuid().ToString();
        itemID = _ItemID;
        itemCount = _ItemCount;
        itemLV = _ItemLV;
        itemNowExp = _ItemExp;
    }

    public void PlusItemLV(int _Plus)
    {
        itemLV += _Plus;
    }

    public void PlusItemNowExp(float _Plus)
    {
        itemNowExp += _Plus;
    }
}
