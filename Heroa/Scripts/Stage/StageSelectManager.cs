using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StageSelectManager : MonoBehaviour
{

    public static StageSelectManager instance;

    [SerializeField]
    private GameObject showWordPanel;

    [SerializeField]
    private GameObject alarmObj;
    private TextMeshProUGUI alarmText;

    [SerializeField]
    private Image stage_1_Icon;
    [SerializeField]
    private Image stage_2_Icon;
    [SerializeField]
    private Image stage_3_Icon;
    [SerializeField]
    private Image stage_4_Icon;
    [SerializeField]
    private Image stage_5_Icon;
    [SerializeField]
    private Image stage_6_Icon;
    [SerializeField]
    private Image stage_7_Icon;
    [SerializeField]
    private Image stage_8_Icon;
    [SerializeField]
    private Image stage_9_Icon;
    [SerializeField]
    private Image stage_10_Icon;

    private StageProgressData myStageData;

    private void Awake()
    {
        if (instance == null)
            instance = this;
    }

    private void Start()
    {
        alarmText = alarmObj.GetComponent<TextMeshProUGUI>();
        alarmObj.SetActive(false);

        myStageData = DatabaseManager.instance.myStageProgressData;

        ShowNowStageIcon();
    }

    public bool CheckStageClear(string _StageNmae)
    {
        switch (_StageNmae)
        {
            case "SampleScene": // 수정바람
                return true;

            case "Stage_1_Scene":
                return true;

            case "Stage_2_Scene":
                if (myStageData.isStage_1_Clear == true)
                {
                    return true;
                }
                StartCoroutine(ShowAlarmText("해당 장소가 잠겨 있습니다. 선행 장소를 클리어 해주세요."));
                break;

            case "Stage_3_Scene":
                if (myStageData.isStage_2_Clear == true)
                {
                    return true;
                }
                StartCoroutine(ShowAlarmText("해당 장소가 잠겨 있습니다. 선행 장소를 클리어 해주세요."));
                break;

            case "Stage_4_Scene":
                /*
                if (myStageData.isStage_3_Clear == true)
                {
                    return true;
                }
                */
                StartCoroutine(ShowAlarmText("개발중입니다. 다음 업데이트를 기대해주세요."));
                break;

            case "Stage_5_Scene":
                if (myStageData.isStage_4_Clear == true)
                    return true;

                StartCoroutine(ShowAlarmText("개발중입니다. 다음 업데이트를 기대해주세요."));
                break;

            case "Stage_6_Scene":
                if (myStageData.isStage_5_Clear == true)
                    return true;

                StartCoroutine(ShowAlarmText("개발중입니다. 다음 업데이트를 기대해주세요."));
                break;

            case "Stage_7_Scene":
                if (myStageData.isStage_6_Clear == true)
                    return true;

                StartCoroutine(ShowAlarmText("개발중입니다. 다음 업데이트를 기대해주세요."));
                break;

            case "Stage_8_Scene":
                if (myStageData.isStage_7_Clear == true)
                    return true;

                StartCoroutine(ShowAlarmText("개발중입니다. 다음 업데이트를 기대해주세요."));
                break;

            case "Stage_9_Scene":
                if (myStageData.isStage_9_Clear == true)
                    return true;

                StartCoroutine(ShowAlarmText("개발중입니다. 다음 업데이트를 기대해주세요."));
                break;

            case "Stage_10_Scene":
                if (myStageData.isStage_10_Clear == true)
                    return true;

                StartCoroutine(ShowAlarmText("개발중입니다. 다음 업데이트를 기대해주세요."));
                break;
        }

        return false;
    }

    IEnumerator ShowAlarmText(string _string)
    {
        alarmObj.SetActive(true);
        alarmText.text = _string;
        yield return new WaitForSeconds(3f);
        alarmObj.SetActive(false);
    }

    // * 이름 바꾸기?
    public void ShowNowStageIcon()
    {
        if (myStageData.isStage_1_Clear == false)
        {
            stage_2_Icon.color = Color.gray;
        }
        if (myStageData.isStage_2_Clear == false)
        {
            stage_3_Icon.color = Color.gray;
        }
        if (myStageData.isStage_3_Clear == false)
        {
            stage_4_Icon.color = Color.gray;
        }
        if (myStageData.isStage_4_Clear == false)
        {
            stage_5_Icon.color = Color.gray;
        }
        if (myStageData.isStage_5_Clear == false)
        {
            stage_6_Icon.color = Color.gray;
        }
        if (myStageData.isStage_6_Clear == false)
        {
            stage_7_Icon.color = Color.gray;
        }
        if (myStageData.isStage_7_Clear == false)
        {
            stage_8_Icon.color = Color.gray;
        }
        if (myStageData.isStage_8_Clear == false)
        {
            stage_9_Icon.color = Color.gray;
        }
        if (myStageData.isStage_9_Clear == false)
        {
            stage_10_Icon.color = Color.gray;
        }
    }

    public void OnShowWordPanel()
    {
        showWordPanel.SetActive(true);
    }
    public void OffShowWordPanel()
    {
        showWordPanel.SetActive(false);
    }

}
