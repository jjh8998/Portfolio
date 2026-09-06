using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class AchievementData
{

    public List<Quest> achievements;

    AchievementData()
    {
        achievements = new List<Quest>();
    }
}
