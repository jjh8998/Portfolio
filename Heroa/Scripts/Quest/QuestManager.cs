
#define DEBUG // DEBUG가 정의되어 있으면 (save 경로) Log를 출력합니다.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class QuestManager : MonoBehaviour
{
    public static QuestManager instance;

    public DailyQuestData dailyQuestData;
    public WeekQuestData weekQuestData;
    public AchievementData achievementData;

    public Parser parser;
    public GameObject alarmSpot;
    public GameObject questInfoContent;
    public List<Quest> questList;

    private int nowClearQuestCount = 0; // 클리어 된 퀘스트 갯수를 세서 alarm on/off 판단
    private bool canNowClearCounting;

    private QuestInfoScript[] infoes;

    private void Awake()
    {
        if (instance == null)
            instance = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        infoes = questInfoContent.GetComponentsInChildren<QuestInfoScript>();

        StartCoroutine(CheckAllQuestAlarm()); // 퀘스트 깰때마다 한번 더 넣야할거같음
    }

    #region Json

    // * 세이브 폴더 만들어서 거기다 저장하기.
    // * 암호화

    public void SaveDailyQuestDataToJson()
    {
        string jsonData = JsonUtility.ToJson(dailyQuestData, true);
        string path = Path.Combine(Application.persistentDataPath, "DailyQuestData.json");
        File.WriteAllText(path, jsonData);

        // Debug.Log("Saving dailyQuest Data to Json is finished in " + path);
    }

    public void LoadDailyQuestDataFromJson()
    {
        string path = Path.Combine(Application.persistentDataPath, "DailyQuestData.json");

        if (File.Exists(path))
        {
            string jsonData = File.ReadAllText(path);
            dailyQuestData = JsonUtility.FromJson<DailyQuestData>(jsonData);

#if DEBUG
            // Debug.Log("Loading Data from Json is finished from " + path);
#endif
        }
        else
        {
            Debug.LogError("QuestManager : No Path to load DailyQuestData");
        }
    }

    public void SaveWeekQuestDataToJson()
    {
        string jsonData = JsonUtility.ToJson(weekQuestData, true);
        string path = Path.Combine(Application.persistentDataPath, "WeekQuestData.json");
        File.WriteAllText(path, jsonData);

#if DEBUG
        // Debug.Log("Saving weekQuest Data to Json is finished in " + path);
#endif
    }

    public void LoadWeekQuestDataFromJson()
    {
        string path = Path.Combine(Application.persistentDataPath, "WeekQuestData.json");

        if (File.Exists(path))
        {
            string jsonData = File.ReadAllText(path);
            weekQuestData = JsonUtility.FromJson<WeekQuestData>(jsonData);

#if DEBUG
            // Debug.Log("Loading Data from Json is finished from " + path);
#endif
        }
        else
        {
            Debug.LogError("QuestManger : No Path to load WeekQuestData");
        }
    }


    public void SaveAchievementDataToJson()
    {
        string jsonData = JsonUtility.ToJson(achievementData, true);
        string path = Path.Combine(Application.persistentDataPath, "AchievementData.json");
        File.WriteAllText(path, jsonData);

#if DEBUG
        // Debug.Log("Saving Achievement Data to Json is finished in " + path);
#endif
    }

    public void LoadAchievementDataFromJson()
    {
        string path = Path.Combine(Application.persistentDataPath, "AchievementData.json");

        if (File.Exists(path))
        {
            string jsonData = File.ReadAllText(path);
            achievementData = JsonUtility.FromJson<AchievementData>(jsonData);

#if DEBUG
            // Debug.Log("Loading Data from Json is finished from " + path);
#endif
        }
        else
        {
            Debug.LogError("QuestManager : No Path to load AchievementData");
        }
    }

    #endregion

    // questInfoScript가 켜져야 clear 여부를 확인할 수 있어서 만든 꼼수
    IEnumerator CheckAllQuestAlarm()
    {
        /*
         * Quest Panel을 켜서 questList에 각 퀘스트들을 넣음.
         * 자동으로 클리어 된 것이 있으면 info 스크립트에서 onAlarm
         * ** 기다리는 시간은 좀 고민해봐야될듯
         */

        canNowClearCounting = true;

        OnClickedAchievementBtn(); // 퀘스트 리스트에 achievement quest들 넣기
        MainMenuManager.instance.OnQuestPanel();
        MainMenuManager.instance.SetQuestPanelAlpha(0f);
        yield return new WaitForSeconds(0.3f);

        OnClickedWeekQuestBtn(); // 퀘스트 리스트에 week quest들 넣기
        yield return new WaitForSeconds(0.3f);

        OnClickedDailyQuestBtn(); // 퀘스트 리스트에 daily quest들 넣기
        yield return new WaitForSeconds(0.3f);

        MainMenuManager.instance.SetQuestPanelAlpha(1f);
        MainMenuManager.instance.OffQuestPanel();

        canNowClearCounting = false;
        // 순서 daily로 해서 가장 먼저 daily가 켜질 수 있도록
    }

    #region Insert QuestData to QuestList

    private void InsertDailyQuestDataToQuestList()
    {
        /*
         * pre :
         * post : questList를 Clear 후 DailyQuestData.Json이 존재하지 않을경우, DailyQuest_List를 파싱후 dailyQuestData 데이터에 넣고 저장. parser에서 questList에 데이터 넣어줌
         *         파일 있으면, 로드,  questList에 dailyQuestData 넣음.
         */

        //파일경로
        string strFile = Path.Combine(Application.persistentDataPath, "DailyQuestData.Json"); ;
        FileInfo fileInfo = new FileInfo(strFile);

        if (fileInfo.Exists == false)
        {
            // DailyQuest_List.Json 파일이 존재하지 않으면 파싱 후 생성
            parser.QuestParser("DailyQuest_List");

            for (int i = 0; i < questList.Count; i++)
            {
                dailyQuestData.dailyQuests.Add(questList[i]);
            }

            SaveDailyQuestDataToJson();
        }
        else
        {
            // DailyQuestData.Json 파일 있으면 파일 로드
            LoadDailyQuestDataFromJson();
            questList.Clear();
            questList = dailyQuestData.dailyQuests;
        }
    }

    private void InsertWeekQuestDataToQuestList()
    {
        /*
         * pre :
         * post : questList를 Clear 후 WeekQuestData.Json이 존재하지 않을경우, WeekQuest_List를 파싱후 weekQuestData 데이터에 넣고 저장. parser에서 questList에 데이터 넣어줌
         *         파일 있으면, 로드,  questList에 weekQuestData 넣음.
         */

        //파일경로
        string strFile = Path.Combine(Application.persistentDataPath, "WeekQuestData.Json"); ;
        FileInfo fileInfo = new FileInfo(strFile);

        if (fileInfo.Exists == false)
        {
            // WeekQuest_List.Json 파일이 존재하지 않으면 파싱 후 생성
            parser.QuestParser("WeekQuest_List");

            for (int i = 0; i < questList.Count; i++)
            {
                weekQuestData.weekQuests.Add(questList[i]);
            }

            SaveWeekQuestDataToJson();
        }
        else
        {
            // WeekQuestData_List.Json 파일 있으면 파일 로드
            LoadWeekQuestDataFromJson();
            questList.Clear();
            questList = weekQuestData.weekQuests;
        }
    }

    private void InsertAchievementDataToQuestList()
    {
        /*
         * pre :
         * post : questList를 Clear 후 AchievementData.Json이 존재하지 않을경우, Achievement_List를 파싱후 AchievementData 데이터에 넣고 저장. parser에서 questList에 데이터 넣어줌
         *         파일 있으면, 로드,  questList에 AchievementData 넣음.
         */

        //파일경로
        string strFile = Path.Combine(Application.persistentDataPath, "AchievementData.Json"); ;
        FileInfo fileInfo = new FileInfo(strFile);

        if (fileInfo.Exists == false)
        {
            // Achievement_List.Json 파일이 존재하지 않으면 파싱 후 생성
            parser.QuestParser("Achievement_List");

            for (int i = 0; i < questList.Count; i++)
            {
                achievementData.achievements.Add(questList[i]);
            }

            SaveAchievementDataToJson();
        }
        else
        {
            // AchievementData.Json 파일 있으면 파일 로드
            LoadAchievementDataFromJson();
            questList.Clear();
            questList = achievementData.achievements;
        }
    }

    #endregion

    #region OnClicked

    public void OnClickedDailyQuestBtn()
    {
        /*
         * pre :
         * post : questList를 Clear 후 (parser or insert 스크립트에서) DailyQuest를 questList에 넣고 InitializeQuestinfoes()
         */

        InsertDailyQuestDataToQuestList();
        InitializeQuestInfoes();
    }

    public void OnClickedWeekQuestBtn()
    {
        /*
         * pre :
         * post : questList를 Clear 후 (parser or insert 스크립트에서) WeekQuestData를 questList에 넣고 InitializeQuestInfoes()
         *         InitializeQuest()
         */

        InsertWeekQuestDataToQuestList();
        InitializeQuestInfoes();
    }

    public void OnClickedAchievementBtn()
    {
        /*
         * pre :
         * post : questList를 Clear
         *         Achievement_List.Json이 존재하지 않을경우, Achievement_List를 파싱후 achievementData 데이터에 넣고 저장. parser에서 questList에 데이터 넣어줌
         *         파일 있으면, 로드,  questList에 achievements 넣음.
         *         InitializeQuest()
         */

        InsertAchievementDataToQuestList();
        InitializeQuestInfoes();
    }

    #endregion

    private void InitializeQuestInfoes()
    {
        /*
         * pre :
         * post : questList setActive true
         *         questList의 count만큼 infoes를 활성화 및 QuestInfoScript를 설정하고 나머지는 비활성화
         */

        for (int i = 0; i < infoes.Length; i++)
            infoes[i].gameObject.SetActive(true);

        for (int j = 0; j < questList.Count; j++)
            infoes[j].SetQuestInfo(questList[j]);

        for (int k = questList.Count; k < infoes.Length; k++)
            infoes[k].gameObject.SetActive(false);
    }

    public void ResetDailyQuest()
    {
        /*
         * 모든 dailyQuest의 nowClear, isCleared = false
         */

        InsertDailyQuestDataToQuestList();

        for (int i = 0; i < questList.Count; i++)
        {
            questList[i].nowClear = false;
            questList[i].isCleared = false;
        }

        Debug.Log("ResetDailyQuest");

        dailyQuestData.dailyQuests = questList;

        SaveDailyQuestDataToJson();
    }

    #region On/Off

    public void OnAlarmSpot()
    {
        if (canNowClearCounting == true)
        {
            nowClearQuestCount++;
            alarmSpot.SetActive(true);
        }
    }

    public void OffAlarmSpot()
    {
        nowClearQuestCount--;

        if (nowClearQuestCount <= 0)
            alarmSpot.SetActive(false);
    }

    #endregion

    #region Set

    public void SetDailyQuestNowClear(int _index, bool _bool)
    {
        dailyQuestData.dailyQuests[_index].nowClear = _bool;

        SaveDailyQuestDataToJson();
    }

    public void SetDailyQuestIsCleared(int _index, bool _bool)
    {
        dailyQuestData.dailyQuests[_index].isCleared = _bool;

        SaveDailyQuestDataToJson();
    }

    public void SetWeekQuestNowClear(int _index, bool _bool)
    {
        weekQuestData.weekQuests[_index].nowClear = _bool;

        SaveWeekQuestDataToJson();
    }

    public void SetWeekQuestIsCleared(int _index, bool _bool)
    {
        weekQuestData.weekQuests[_index].isCleared = _bool;

        SaveWeekQuestDataToJson();
    }

    public void SetAchievementNowClear(int _index, bool _bool)
    {
        achievementData.achievements[_index].nowClear = _bool;

        SaveAchievementDataToJson();
    }

    public void SetAchievementIsCleared(int _index, bool _bool)
    {
        achievementData.achievements[_index].isCleared = _bool;

        SaveAchievementDataToJson();
    }

    #endregion
}