using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Quest
{
    public int sortID; // 0 = 일퀘, 1 = 주간퀘, 2 = 업적
    public int ID;
    public string name;
    public string explanation;
    public int resultGold;
    public int resultExp;

    public enum d
    {

    }
    public enum Condition 
    { 
        TodayAccess, TodayRightAnswerCount, TodayMonsterKill, TodayUseMagic, TodayUseGold,
        WeekAccessDayCount, WeekRightAnswerCount, WeekMonsterKill, WeekUseMagic, WeekUseGold,
        Word, Monster, Item, Accum_Gold, Consume_Gold, ETC 
    }
    public Condition condition;
    public int numForClear; // 퀘스트 클리어를 위해서 필요한 횟수
    public bool nowClear; // 클리어는 됐고 보상은 안받은 상태
    public bool isCleared; // 클리어도 됐고 보상도 받은 상태

    public Quest(int _sortID, int _ID, string _name, string _explan, int _resultGold, int _resultExp, Condition _condition, int _numForClear, bool _nowClear = false, bool _isClear = false)
    {
        sortID = _sortID;
        ID = _ID;
        name = _name;
        explanation = _explan;
        resultGold = _resultGold;
        resultExp = _resultExp;
        condition = _condition;
        numForClear = _numForClear;
        nowClear = _nowClear;
        isCleared = _isClear;
    }
}
