using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class QuestInfoScript : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI t_name;
    [SerializeField]
    private TextMeshProUGUI t_explan;
    [SerializeField]
    private TextMeshProUGUI t_resultGold;
    [SerializeField]
    private TextMeshProUGUI t_resultExp;
    [SerializeField]
    private GameObject receiveBtn;
    [SerializeField]
    private Image questProgressImage;
    [SerializeField]
    private TextMeshProUGUI questProgressText;

    private int sortID;
    private Quest quest;

    // Start is called before the first frame update
    void Start()
    {
        /*
        if (quest.isCleared == false)
            SetNowClear(quest.nowClear);
        */
    }

    private void Update()
    {
        if (quest.nowClear == false && quest.isCleared == false)
            CheckQuestClear();

        CalculateQuestNowFill();
    }

    public void CalculateQuestNowFill()
    {
        /*
         * pre : quest가 존재해야함
         * post : quest condition에 따라 questProgress 설정
         */

        switch (quest.condition)
        {
            // 일퀘
            case Quest.Condition.TodayAccess:
                questProgressImage.fillAmount = 1;
                questProgressText.text = "1" + " / " + quest.numForClear;
                break;

            case Quest.Condition.TodayRightAnswerCount:
                questProgressImage.fillAmount = DatabaseManager.instance.myPlayerData.todayRightAnswerCount / (float)(quest.numForClear);
                questProgressText.text = DatabaseManager.instance.myPlayerData.todayRightAnswerCount + " / " + quest.numForClear;
                break;

            case Quest.Condition.TodayMonsterKill:
                questProgressImage.fillAmount = DatabaseManager.instance.myPlayerData.todayMonsterKill / (float)(quest.numForClear);
                questProgressText.text = DatabaseManager.instance.myPlayerData.todayMonsterKill + " / " + quest.numForClear;
                break; ;

            case Quest.Condition.TodayUseMagic:
                questProgressImage.fillAmount = DatabaseManager.instance.myPlayerData.todayUseMagic / (float)(quest.numForClear);
                questProgressText.text = DatabaseManager.instance.myPlayerData.todayUseMagic + " / " + quest.numForClear;
                break; ;

            case Quest.Condition.TodayUseGold:
                questProgressImage.fillAmount = DatabaseManager.instance.myPlayerData.todayUseGold / (float)(quest.numForClear);
                questProgressText.text = DatabaseManager.instance.myPlayerData.todayUseGold + " / " + quest.numForClear;
                break;

            // 주간퀘
            case Quest.Condition.WeekAccessDayCount:
                questProgressImage.fillAmount = DatabaseManager.instance.myPlayerData.weekAccessDayCount / (float)(quest.numForClear);
                questProgressText.text = DatabaseManager.instance.myPlayerData.weekAccessDayCount + " / " + quest.numForClear;
                break;
            case Quest.Condition.WeekRightAnswerCount:
                questProgressImage.fillAmount = DatabaseManager.instance.myPlayerData.weekRightAnswerCount / (float)(quest.numForClear);
                questProgressText.text = DatabaseManager.instance.myPlayerData.weekRightAnswerCount + " / " + quest.numForClear;
                break;
            case Quest.Condition.WeekMonsterKill:
                questProgressImage.fillAmount = DatabaseManager.instance.myPlayerData.weekMonsterKill / (float)(quest.numForClear);
                questProgressText.text = DatabaseManager.instance.myPlayerData.weekMonsterKill + " / " + quest.numForClear;
                break;
            case Quest.Condition.WeekUseMagic:
                questProgressImage.fillAmount = DatabaseManager.instance.myPlayerData.weekUseMagic / (float)(quest.numForClear);
                questProgressText.text = DatabaseManager.instance.myPlayerData.weekUseMagic + " / " + quest.numForClear;
                break;
            case Quest.Condition.WeekUseGold:
                questProgressImage.fillAmount = DatabaseManager.instance.myPlayerData.weekUseGold / (float)(quest.numForClear);
                questProgressText.text = DatabaseManager.instance.myPlayerData.weekUseGold + " / " + quest.numForClear;
                break;

            // 업적
            case Quest.Condition.Word:
                questProgressImage.fillAmount = DatabaseManager.instance.myPlayerData.totalRightAnswerCount / (float)(quest.numForClear);
                questProgressText.text = DatabaseManager.instance.myPlayerData.totalRightAnswerCount + " / " + quest.numForClear;
                break;

            case Quest.Condition.Monster:
                questProgressImage.fillAmount = DatabaseManager.instance.myPlayerData.totalMonsterKillCount / (float)(quest.numForClear);
                questProgressText.text = DatabaseManager.instance.myPlayerData.totalMonsterKillCount + " / " + quest.numForClear;
                break;

            default:
                Debug.LogError("QuestInfoScript : NO condition (Function - CalculateQuestNowFill)");
                break;
        }
    }

    public void CheckQuestClear()
    {
        /*
         * quest Conditon 에 따라 CheckQuestClear 실행
         */

        switch (quest.condition)
        {
            // 일퀘
            case Quest.Condition.TodayAccess:
                CheckQuestClear_TodayAccess();
                break;
            case Quest.Condition.TodayRightAnswerCount:
                CheckQuestClear_TodayWordRightAnswerCount();
                break;
            case Quest.Condition.TodayMonsterKill:
                CheckQuestClear_TodayMonsterKill();
                break;
            case Quest.Condition.TodayUseMagic:
                CheckQuestClear_TodayUseMagic();
                break;
            case Quest.Condition.TodayUseGold:
                CheckQuestClear_TodayUseGold();
                break;

            // 주간퀘
            case Quest.Condition.WeekAccessDayCount:
                CheckQuestClear_WeekAccessDayCount();
                break;
            case Quest.Condition.WeekRightAnswerCount:
                CheckQuestClear_WeekRightAnswerCount();
                break;
            case Quest.Condition.WeekMonsterKill:
                CheckQuestClear_weekMonsterKill();
                break;
            case Quest.Condition.WeekUseMagic:
                CheckQuestClear_WeekUseMagic();
                break;
            case Quest.Condition.WeekUseGold:
                CheckQuestClear_WeekUseGold();
                break;

            case Quest.Condition.Word:
                CheckQuestClear_TotalRightAnswer();
                break;

            case Quest.Condition.Monster:
                CheckQuestClear_TotalMonsterKillCount();
                break;

            default:
                Debug.LogError("QuestInfoScript : NO condition - ");
                break;
        }
    }

    #region CheckQuest

    #region DailyQuestCheck

    public void CheckQuestClear_TodayAccess()
    {
        if (quest.nowClear == false && quest.isCleared == false)
        {
            DatabaseManager.instance.SetIsGetTodayAccessResult(true);
            SetNowClear(true);
        }
    }

    public void CheckQuestClear_TodayWordRightAnswerCount()
    {
        if (quest.nowClear == false && quest.isCleared == false)
            if (DatabaseManager.instance.myPlayerData.todayRightAnswerCount >= quest.numForClear)
                SetNowClear(true);
    }

    public void CheckQuestClear_TodayMonsterKill()
    {
        if (quest.nowClear == false && quest.isCleared == false)
            if (DatabaseManager.instance.myPlayerData.todayMonsterKill >= quest.numForClear)
                SetNowClear(true);
    }

    public void CheckQuestClear_TodayUseMagic()
    {
        if (quest.nowClear == false && quest.isCleared == false)
            if (DatabaseManager.instance.myPlayerData.todayUseMagic >= quest.numForClear)
                SetNowClear(true);
    }

    public void CheckQuestClear_TodayUseGold()
    {
        if (quest.nowClear == false && quest.isCleared == false)
            if (DatabaseManager.instance.myPlayerData.todayUseGold >= quest.numForClear)
                SetNowClear(true);
    }

    #endregion

    #region WeekQuestCheck

    public void CheckQuestClear_WeekAccessDayCount()
    {
        if (quest.nowClear == false && quest.isCleared == false)
            if (DatabaseManager.instance.myPlayerData.weekAccessDayCount >= quest.numForClear)
                SetNowClear(true);
    }

    public void CheckQuestClear_WeekRightAnswerCount()
    {
        if (quest.nowClear == false && quest.isCleared == false)
            if (DatabaseManager.instance.myPlayerData.weekRightAnswerCount >= quest.numForClear)
                SetNowClear(true);
    }

    public void CheckQuestClear_weekMonsterKill()
    {
        if (quest.nowClear == false && quest.isCleared == false)
            if (DatabaseManager.instance.myPlayerData.weekMonsterKill >= quest.numForClear)
                SetNowClear(true);
    }

    public void CheckQuestClear_WeekUseMagic()
    {
        if (quest.nowClear == false && quest.isCleared == false)
            if (DatabaseManager.instance.myPlayerData.weekUseMagic >= quest.numForClear)
                SetNowClear(true);
    }

    public void CheckQuestClear_WeekUseGold()
    {
        if (quest.nowClear == false && quest.isCleared == false)
            if (DatabaseManager.instance.myPlayerData.weekUseGold >= quest.numForClear)
                SetNowClear(true);
    }

    #endregion

    public void CheckQuestClear_TotalRightAnswer()
    {
        if (quest.nowClear == false && quest.isCleared == false)
            if (DatabaseManager.instance.myPlayerData.totalRightAnswerCount >= quest.numForClear)
                SetNowClear(true);
    }

    public void CheckQuestClear_TotalMonsterKillCount()
    {
        if (quest.nowClear == false && quest.isCleared == false)
            if (DatabaseManager.instance.myPlayerData.totalMonsterKillCount >= quest.numForClear)
                SetNowClear(true);
    }

    #endregion

    public void ReceiveResult()
    {
        /*
         * pre : nowClear가 true.
         * post : result gold, exp 지급 후 nowClear = false, isCleared = true , OffAlarmSpot()
         */

        if (quest.nowClear == true)
        {
            DatabaseManager.instance.PlusGold(quest.resultGold);
            DatabaseManager.instance.PlusNowExp(quest.resultExp);

            SetNowClear(false);
            quest.isCleared = true;

            QuestManager.instance.OffAlarmSpot();

            switch (quest.sortID)
            {
                case 0:
                    QuestManager.instance.SaveDailyQuestDataToJson();
                    break;
                case 1:
                    QuestManager.instance.SaveWeekQuestDataToJson();
                    break;
                case 2:
                    QuestManager.instance.SaveAchievementDataToJson();
                    break;
                default:
                    Debug.LogError("QuestInfoScript : No SortID");
                    break;
            }
        }
    }

    public void SetNowClear(bool _bool)
    {
        /*
         * pre : nowClear의 bool 상태
         * post : true이면, nowClear = true, receiveBtn color = white, OnalarmSpot
         *         false이면, nowClear = false, receiveBtn color = gray
         *         저장
         */

        // Debug.Log("OnAlarm 재고 필요!!");

        if (_bool == true)
        {
            quest.nowClear = true;
            receiveBtn.GetComponent<Image>().color = Color.white;

            QuestManager.instance.OnAlarmSpot();
        }
        else if (_bool == false)
        {
            quest.nowClear = false;
            receiveBtn.GetComponent<Image>().color = Color.gray;
        }

        switch (quest.sortID)
        {
            case 0:
                QuestManager.instance.SaveDailyQuestDataToJson();
                break;
            case 1:
                QuestManager.instance.SaveWeekQuestDataToJson();
                break;
            case 2:
                QuestManager.instance.SaveAchievementDataToJson();
                break;
        }
    }

    public void SetQuestInfo(Quest _quest)
    {
        /*
         * pre : Quest의 정보
         * post : 해당 questInfo의 정보를 받아온 quest의 정보로 수정
         *         UI들에 해당 정보들 표시
         *         만약 클리어가 안됐으면 수령 버튼 회색
         */

        quest = _quest;

        t_name.text = quest.name;
        t_explan.text = quest.explanation;
        t_resultGold.text = quest.resultGold.ToString();
        t_resultExp.text = quest.resultExp.ToString();

        /*
        if (quest.nowClear == false)
            receiveBtn.GetComponent<Image>().color = Color.gray;
        */

        SetNowClear(quest.nowClear);
    }
}
