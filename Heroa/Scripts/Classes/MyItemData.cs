using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Json에 저장하기 위한
[System.Serializable]
public class MyItemData
{

    public List<MyItem> myitemData;

    public void PlusMyItemDataCount(int _ItemID, int _Count)
    {

        for (int i = 0; i < myitemData.Count; i++)
        {
            if (_ItemID == myitemData[i].itemID)
            {
                myitemData[i].itemCount += _Count;
                return;
            }
        }

        Debug.LogError("MyItemData : Can't find item to increase.");
    }

    public void FindRemoveMyItemData(int _ItemID, int _ItemCount, int _ItemLV, float _ItemExp)
    {
        for (int i = 0; i < myitemData.Count; i++)
        {
            if (_ItemID == myitemData[i].itemID)
            {
                if (_ItemLV == myitemData[i].itemLV)
                {
                    if (_ItemExp == myitemData[i].itemNowExp)
                    {
                        myitemData.RemoveAt(i);
                        return;
                    }
                }

                Debug.LogError("MyItemData : Can't delete item");
            }
        }
    }

    public void AddMyItemData(MyItem _MyItem)
    {
        myitemData.Add(_MyItem);
    }

    public void InsertMyItemData(int _Index, MyItem _MyItem)
    {
        myitemData.Insert(_Index, _MyItem);
    }

    public void RemoveMyItemData(MyItem _MyItem)
    {
        myitemData.Remove(_MyItem);
    }
    public void RemoveAtMyItemData(int _Index)
    {
        myitemData.RemoveAt(_Index);
    }

    public int GetItemID(int _Index)
    {
        return myitemData[_Index].itemID;
    }

    public int GetMyItemDataCount()
    {
        return myitemData.Count;
    }

}
