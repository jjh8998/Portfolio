using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class MyPlayerData
{
    // 튜토리얼
    public bool clearStageTutorial;
    public bool clearBattleTutorial;
    public bool clearDrawTutorial;
    public bool clearItemTutorial;

    public int lastAccessDay; // 마지막 접속일

    // 퀘스트용 데이터
    public bool isGetTodayAccessResult; // 최초 접속 보상 받았는지, 문제없으면 삭제요망
    public int todayRightAnswerCount;
    public int todayMonsterKill;
    public int todayUseMagic; // 안만드
    public float todayUseGold;

    public int weekAccessDayCount; // 1주일동안 접속한 날짜수
    public int weekRightAnswerCount;
    public int weekMonsterKill;
    public int weekUseMagic; // 안만드
    public float weekUseGold;

    // 업적용 데이터
    public int totalRightAnswerCount;
    public int totalMonsterKillCount;
    public int totalItemCount;
    public float totalGold;
    public float totalUsedGold;

    // 플레이어 데이터
    public float gold;
    public int LV = 1;
    public float LevelUpExp = 0;
    public float nowExp = 0;

    // 플레이어 스텟
    public float hp = 0;
    public float player_AttackDamage = 0;
    public float player_Defense = 0;
    public float player_CriticalDamage = 0;
    public float player_CriticalRate = 0;

    // 스토리 진행도
    public int chpater1_StoryReader; // 스토리 읽을때마다 +1
}
