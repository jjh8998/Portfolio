using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MyMagicData
{
    public int[] selectedMagicID;
    public List<bool> isHave; // 계산하기 쉽게 인덱스 1에서부터 시작

    MyMagicData()
    {
        selectedMagicID = new int[3];
    }
}
