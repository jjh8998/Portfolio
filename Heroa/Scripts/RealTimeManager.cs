using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System; // 로컬 시간 함수

public class RealTimeManager : MonoBehaviour
{
    DatabaseManager databaseManager;

    int nowDay = int.Parse(DateTime.Now.ToString("dd"));
    int nowHour = int.Parse(DateTime.Now.ToString("hh"));
    DayOfWeek nowDayOfWeek = DateTime.Now.DayOfWeek; // 요일

    // Start is called before the first frame update
    void Start()
    {
        databaseManager = DatabaseManager.instance;

        StartCoroutine(WaitLoading());
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ResetToday()
    {
        /*
         * pre : 
         * post : 만약 로컬시간이 lastAcessDay와 다르고 4시가 지났다면,
         *         today data, today quest 모두 초기화
         *         
         *         만약 월요일이라면, week data, week quest 모두 초기화
         *         weekAccessDayCount +1
         */

        int nowDay = int.Parse(DateTime.Now.ToString("dd"));
        int nowHour = int.Parse(DateTime.Now.ToString("hh"));
        int accessDayInterval = Mathf.Abs(nowDay - databaseManager.myPlayerData.lastAccessDay); // 2일 이상 미접시 **31일 넘어갈때 계산필요

        if (databaseManager.myPlayerData.lastAccessDay != nowDay)
        {
            if (nowHour > 4 || accessDayInterval >= 2)
            {
                databaseManager.SetIsGetTodayAccessResult(false);
                databaseManager.SetTodayRightAnswerCount(0);
                databaseManager.SetTodayMonsterKill(0);
                databaseManager.SetTodayUseMagic(0);
                databaseManager.SetTodayUseGold(0f);

                if (QuestManager.instance != null)
                    QuestManager.instance.ResetDailyQuest();
                else
                    Debug.LogError("RealTimeManager : NO QuestManager");

                // 한주 초기화
                if (nowDayOfWeek == DayOfWeek.Monday)
                {
                    databaseManager.SetWeekAccessDayCount(0);
                    databaseManager.SetWeekRightAnswerCount(0);
                    databaseManager.SetWeekMonsterKill(0);
                    databaseManager.SetWeekUseMagic(0);
                    databaseManager.SetWeekUseGold(0f);
                }

                databaseManager.PlusWeekAccessDayCount(1);

                databaseManager.SetLastAccessDay(nowDay);
                Debug.Log("RealTimeManager : ResetToday" + databaseManager.myPlayerData.lastAccessDay + " " + nowDay);
            }
        }
    }

    IEnumerator WaitLoading()
    {
        yield return new WaitUntil(() => databaseManager.isFinishedLoading == true);

        ResetToday();
    }
}
