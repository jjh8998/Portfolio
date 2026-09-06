using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StageManager : MonoBehaviour
{
    const int PENALTY = 3;

    public static StageManager instance;

    [SerializeField]
    private string stageName;
    [SerializeField]
    private int stageProcessivity = 0; // 스테이지 진행도
    [SerializeField]
    private int nowStageProcessivity = 0;

    [SerializeField]
    private Image stageProcessivityImage = null;

    [Header ("Boss")]
    [SerializeField]
    private string miniBossName = null;
    [SerializeField]
    private string lastBossName = null;

    private float nowFill = 0f;

    private void Awake()
    {
        if (instance == null)
            instance = this;
    }

    IEnumerator Start()
    {
        yield return new WaitUntil(() => DatabaseManager.instance.isFinishedLoading == true);

        InitializeStageProcessivity();
        CalculateNowFill();
    }

    public void InitializeStageProcessivity()
    {
        /*
         * pre :
         * post : stageName에 따라 myStageProgressData에 저장되 있는 진행도로 nowStageProcessivity 설정
         */
        
        switch (stageName)
        {
            case "Stage_1":
                /*
                if (DatabaseManager.instance.myPlayerData.chpater1_StoryReader == 1)
                {
                    DatabaseManager.instance.myPlayerData.chpater1_StoryReader++;
                    DatabaseManager.instance.SaveMyPlayerDataToJson();
                    DialogueManager.StartDialogue("Heroa_Chapter1_1", "Stage_1_Scene");
                }
                */

                nowStageProcessivity = DatabaseManager.instance.myStageProgressData.stage_1_Processivity;
                break;
            case "Stage_2":
                nowStageProcessivity = DatabaseManager.instance.myStageProgressData.stage_2_Processivity;
                break;
            case "Stage_3":
                nowStageProcessivity = DatabaseManager.instance.myStageProgressData.stage_3_Processivity;
                break;
            case "Stage_4":
                nowStageProcessivity = DatabaseManager.instance.myStageProgressData.stage_4_Processivity;
                break;
            case "Stage_5":
                nowStageProcessivity = DatabaseManager.instance.myStageProgressData.stage_5_Processivity;
                break;
            case "Stage_6":
                nowStageProcessivity = DatabaseManager.instance.myStageProgressData.stage_6_Processivity;
                break;
            case "Stage_7":
                nowStageProcessivity = DatabaseManager.instance.myStageProgressData.stage_7_Processivity;
                break;
            case "Stage_8":
                nowStageProcessivity = DatabaseManager.instance.myStageProgressData.stage_8_Processivity;
                break;
            case "Stage_9":
                nowStageProcessivity = DatabaseManager.instance.myStageProgressData.stage_9_Processivity;
                break;
            case "Stage_10":
                nowStageProcessivity = DatabaseManager.instance.myStageProgressData.stage_10_Processivity;
                break;
            default :
                Debug.LogError("StageManager : Wrong stageName");
                break;
        }

        ProcessivityPenalty();
    }

    public void ProcessivityPenalty()
    {
        int getPenalty = nowStageProcessivity - PENALTY;

        if (getPenalty < 0)
        {
            nowStageProcessivity = 0;
            return;
        }
        else if (nowStageProcessivity > stageProcessivity / 2 &&  getPenalty < stageProcessivity / 2)
        {
            nowStageProcessivity = stageProcessivity / 2 + 1;
            return;
        }

        nowStageProcessivity = getPenalty;
    }

    public void CheckProcssivity()
    {
        // 던전 진행도를 체크 후 몬스터 또는 보스 소환

        // 반 진행시
        if (nowStageProcessivity == stageProcessivity / 2)
        {
            MonsterSpawnScript.instance.MonsterSpawn(miniBossName);
            switch (stageName)
            {
                case "Stage_1":
                    DatabaseManager.instance.myStageProgressData.isStage_1_Half_Clear = true;

                    /*
                    if (DatabaseManager.instance.myPlayerData.chpater1_StoryReader == 2)
                    {
                        DatabaseManager.instance.myPlayerData.chpater1_StoryReader++;
                        DatabaseManager.instance.SaveMyPlayerDataToJson();
                        DialogueManager.StartDialogue("Heroa_Chapter1_2", "Stage_1_Scene");
                    }
                    */

                    break;
                case "Stage_2":
                    DatabaseManager.instance.myStageProgressData.isStage_2_Half_Clear = true;
                    break;
                case "Stage_3":
                    DatabaseManager.instance.myStageProgressData.isStage_3_Half_Clear = true;
                    break;
                case "Stage_4":
                    DatabaseManager.instance.myStageProgressData.isStage_4_Half_Clear = true;
                    break;
                case "Stage_5":
                    DatabaseManager.instance.myStageProgressData.isStage_5_Half_Clear = true;
                    break;
                case "Stage_6":
                    DatabaseManager.instance.myStageProgressData.isStage_6_Half_Clear = true;
                    break;
                case "Stage_7":
                    DatabaseManager.instance.myStageProgressData.isStage_7_Half_Clear = true;
                    break;
                case "Stage_8":
                    DatabaseManager.instance.myStageProgressData.isStage_8_Half_Clear = true;
                    break;
                case "Stage_9":
                    DatabaseManager.instance.myStageProgressData.isStage_9_Half_Clear = true;
                    break;
                case "Stage_10":
                    DatabaseManager.instance.myStageProgressData.isStage_10_Half_Clear = true;
                    break;
            }

            DatabaseManager.instance.SaveStageProgressDataToJson();
        }
        else if (nowStageProcessivity >= stageProcessivity)
        {
            /*
            if (DatabaseManager.instance.myPlayerData.chpater1_StoryReader == 4)
            {
                DatabaseManager.instance.myPlayerData.chpater1_StoryReader++;
                DatabaseManager.instance.SaveMyPlayerDataToJson();
                DialogueManager.StartDialogue("Heroa_Chapter1_4", "Stage_1_Scene");

                // MonsterSpawnScript.instance.MonsterSpawn(lastBossName);
            }
            */

            MonsterSpawnScript.instance.MonsterSpawn(lastBossName);
        }
        else
            MonsterSpawnScript.instance.RandomMonsterSpawn();
    }

    public void ClearBoss()
    {
        if (nowStageProcessivity == stageProcessivity / 2 + 1 )
        {
            if (DatabaseManager.instance.myPlayerData.chpater1_StoryReader == 3)
            {
                /*
                DatabaseManager.instance.myPlayerData.chpater1_StoryReader++;
                DatabaseManager.instance.SaveMyPlayerDataToJson();
                DialogueManager.StartDialogue("Heroa_Chapter1_3", "Stage_1_Scene");
                */
            }
        }
        else if (nowStageProcessivity >= stageProcessivity)
        {
            nowStageProcessivity = 0;

            switch (stageName)
            {
                case "Stage_1":
                    DatabaseManager.instance.myStageProgressData.isStage_1_Half_Clear = false;
                    DatabaseManager.instance.myStageProgressData.isStage_1_Clear = true;
                    DatabaseManager.instance.myStageProgressData.stage_1_Processivity = 0;

                    /*
                    if (DatabaseManager.instance.myPlayerData.chpater1_StoryReader == 5)
                    {
                        DatabaseManager.instance.myPlayerData.chpater1_StoryReader++;
                        DatabaseManager.instance.SaveMyPlayerDataToJson();
                        DialogueManager.StartDialogue("Heroa_Chapter1_5", "Stage_1_Scene");
                    }
                    */

                    break;

                case "Stage_2":
                    DatabaseManager.instance.myStageProgressData.isStage_2_Half_Clear = false;
                    DatabaseManager.instance.myStageProgressData.isStage_2_Clear = true;
                    break;

                case "Stage_3":
                    DatabaseManager.instance.myStageProgressData.isStage_3_Half_Clear = false;
                    DatabaseManager.instance.myStageProgressData.isStage_3_Clear = true;
                    break;

                case "Stage_4":
                    DatabaseManager.instance.myStageProgressData.isStage_4_Half_Clear = false;
                    DatabaseManager.instance.myStageProgressData.isStage_4_Clear = true;
                    break;

                case "Stage_5":
                    DatabaseManager.instance.myStageProgressData.isStage_5_Half_Clear = false;
                    DatabaseManager.instance.myStageProgressData.isStage_5_Clear = true;
                    break;

                case "Stage_6":
                    DatabaseManager.instance.myStageProgressData.isStage_6_Half_Clear = false;
                    DatabaseManager.instance.myStageProgressData.isStage_6_Clear = true;
                    break;

                case "Stage_7":
                    DatabaseManager.instance.myStageProgressData.isStage_7_Half_Clear = false;
                    DatabaseManager.instance.myStageProgressData.isStage_7_Clear = true;
                    break;

                case "Stage_8":
                    DatabaseManager.instance.myStageProgressData.isStage_8_Half_Clear = false;
                    DatabaseManager.instance.myStageProgressData.isStage_8_Clear = true;
                    break;

                case "Stage_9":
                    DatabaseManager.instance.myStageProgressData.isStage_9_Half_Clear = false;
                    DatabaseManager.instance.myStageProgressData.isStage_9_Clear = true;
                    break;

                case "Stage_10":
                    DatabaseManager.instance.myStageProgressData.isStage_10_Half_Clear = false;
                    DatabaseManager.instance.myStageProgressData.isStage_10_Clear = true;
                    break;
            }

            DatabaseManager.instance.SaveStageProgressDataToJson();

            GameManager.instance.OnClearPanel();
        }
    }

    public void CalculateNowFill()
    {
        float f_nowStageProcessivity = (float)nowStageProcessivity; // fiilAmount는 0~1 값이기때문에 소수점 해줘야함

        nowFill = f_nowStageProcessivity / stageProcessivity;
        stageProcessivityImage.fillAmount = nowFill;
    }
    
    public void PlusNowStageProcessivity(int _Plus)
    {
        /*
         * nowStageProcessivity 를 _Plus만큼 증가 후 myStageProgressData에 저장, StageProgressData를 Json으로 저장 후 CalculateNowFill()
         */

        nowStageProcessivity += _Plus;

        switch (stageName)
        {
            case "Stage_1":
                DatabaseManager.instance.myStageProgressData.stage_1_Processivity = nowStageProcessivity;
                break;
            case "Stage_2":
                DatabaseManager.instance.myStageProgressData.stage_2_Processivity = nowStageProcessivity;
                break;
            case "Stage_3":
                DatabaseManager.instance.myStageProgressData.stage_3_Processivity = nowStageProcessivity;
                break;
            case "Stage_4":
                DatabaseManager.instance.myStageProgressData.stage_4_Processivity = nowStageProcessivity;
                break;
            case "Stage_5":
                DatabaseManager.instance.myStageProgressData.stage_5_Processivity = nowStageProcessivity;
                break;
            case "Stage_6":
                DatabaseManager.instance.myStageProgressData.stage_6_Processivity = nowStageProcessivity;
                break;
            case "Stage_7":
                DatabaseManager.instance.myStageProgressData.stage_7_Processivity = nowStageProcessivity;
                break;
            case "Stage_8":
                DatabaseManager.instance.myStageProgressData.stage_8_Processivity = nowStageProcessivity;
                break;
            case "Stage_9":
                DatabaseManager.instance.myStageProgressData.stage_9_Processivity = nowStageProcessivity;
                break;
            case "Stage_10":
                DatabaseManager.instance.myStageProgressData.stage_10_Processivity = nowStageProcessivity;
                break;
        }

        DatabaseManager.instance.SaveStageProgressDataToJson();

        CalculateNowFill();
    }
}
