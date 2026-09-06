using System.Collections.Generic;
using System.Data;
using UnityEngine;

public class EmptySc : BuildingData
{
    public EmptySc()
    {
        ID = "EMPTY";
        category = BuildingCategory.Empty;
        name = "Empty Space";
        description = "빈 공간입니다. 무엇이든 지을 수 있습니다.";
        constructionCost = 0;
        constructionDay = 0;
        bonusIncome = 0;
        nextUpgradeBuildings = new List<BuildingData>();
    }
}
