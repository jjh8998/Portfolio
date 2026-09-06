using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WeekQuestData
{
    public List<Quest> weekQuests;

    WeekQuestData()
    {
        weekQuests = new List<Quest>();
    }
}
