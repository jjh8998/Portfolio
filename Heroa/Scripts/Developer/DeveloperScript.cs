using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeveloperScript : MonoBehaviour
{

    public string DialogueCSVName;
    public ShowSelectedMagicInfoScript s;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {


        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            DatabaseManager.instance.PlusGold(1000);
            Debug.Log("1000골드 증정");
        }
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            if (GameManager.instance != null)
            {
                DatabaseManager.instance.myStageProgressData.stage_1_Processivity++;
            }
        }
        else if (Input.GetKeyDown(KeyCode.R))
        {
            DeleteAllData();
            Debug.Log("DeveloperScrpt : 데이터 초기화");
        }
        else if (Input.GetKeyDown(KeyCode.Q))
        {
            MyItem myItem = new MyItem(1);
            InventoryScript.instance.GetAnItem(myItem);
            Debug.Log("모음 1개 흭득");
        }
        else if (Input.GetKeyDown(KeyCode.W))
        {
            MyItem myItem = new MyItem(2);
            InventoryScript.instance.GetAnItem(myItem);
            Debug.Log("자음 1개 흭득");
        }
        else if (Input.GetKeyDown(KeyCode.E))
        {
            InventoryScript.instance.RemoveItem(new MyItem(2, 0, 0, 0));
            Debug.Log("모음 1개 제거");
        }
        else if (Input.GetKeyDown(KeyCode.Z))
        {
            DialogueManager.StartDialogue(DialogueCSVName, "MainMenu");
            Debug.Log("다이알로그 실행");
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            StageManager.instance.PlusNowStageProcessivity(1);
            Debug.Log("현재 스테이지 진행도 +1");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            DatabaseManager.instance.myStageProgressData.isStage_1_Clear = true;
            DatabaseManager.instance.SaveStageProgressDataToJson();
            Debug.Log("DeveloperScrpt : 1스테이지 클리어");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            DatabaseManager.instance.myStageProgressData.isStage_2_Clear = true;
            DatabaseManager.instance.SaveStageProgressDataToJson();
            Debug.Log("DeveloperScrpt : 2스테이지 클리어");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            DatabaseManager.instance.myStageProgressData.isStage_3_Clear = true;
            DatabaseManager.instance.SaveStageProgressDataToJson();
            Debug.Log("DeveloperScrpt : 3스테이지 클리어");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            DatabaseManager.instance.myStageProgressData.isStage_4_Clear = true;
            DatabaseManager.instance.SaveStageProgressDataToJson();
            Debug.Log("DeveloperScrpt : 4스테이지 클리어");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha5))
        {
            DatabaseManager.instance.myStageProgressData.isStage_5_Clear = true;
            DatabaseManager.instance.SaveStageProgressDataToJson();
            Debug.Log("DeveloperScrpt : 5스테이지 클리어");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha6))
        {
            DatabaseManager.instance.myStageProgressData.isStage_6_Clear = true;
            DatabaseManager.instance.SaveStageProgressDataToJson();
            Debug.Log("DeveloperScrpt : 6스테이지 클리어");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            DatabaseManager.instance.myStageProgressData.isStage_7_Clear = true;
            DatabaseManager.instance.SaveStageProgressDataToJson();
            Debug.Log("DeveloperScrpt : 7스테이지 클리어");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            DatabaseManager.instance.myStageProgressData.isStage_8_Clear = true;
            DatabaseManager.instance.SaveStageProgressDataToJson();
            Debug.Log("DeveloperScrpt : 8스테이지 클리어");
        }
        else if (Input.GetKeyDown(KeyCode.Alpha9))
        {
            DatabaseManager.instance.myStageProgressData.isStage_9_Clear = true;
            DatabaseManager.instance.SaveStageProgressDataToJson();
            Debug.Log("DeveloperScrpt : 9스테이지 클리어");
        }
        else if (Input.GetKey(KeyCode.Alpha1) && Input.GetKey(KeyCode.Alpha0))
        {
            DatabaseManager.instance.myStageProgressData.isStage_10_Clear = true;
            DatabaseManager.instance.SaveStageProgressDataToJson();
            Debug.Log("DeveloperScrpt : 10스테이지 클리어");
        }
    }


    public void DeleteAllData()
    {
        PlayerPrefs.DeleteAll();
    }

}