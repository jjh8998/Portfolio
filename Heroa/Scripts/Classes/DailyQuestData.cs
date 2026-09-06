using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DailyQuestData
{
    public List<Quest> dailyQuests;

    DailyQuestData()
    {
        dailyQuests = new List<Quest>();
    }
}
