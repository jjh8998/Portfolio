using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DrawSelectScript : MonoBehaviour
{
    [SerializeField]
    private GameObject alarmObj;
    [SerializeField]
    private GameObject woodBoxDrawPanel;
    [SerializeField]
    private GameObject woodChestDrawPanel;
    [SerializeField]
    private GameObject silverChestDrawPanel;
    [SerializeField]
    private GameObject goldChestDrawPanel;

    private TextMeshProUGUI alarmText;

    // Start is called before the first frame update
    void Start()
    {
        alarmText = alarmObj.GetComponent<TextMeshProUGUI>();
        alarmObj.SetActive(false);
    }

    #region OnClicked

    public void OnClickedWoodBoxBtn()
    {
        OffAllChestDrawPanel();
        OnWoodBoxDrawPanel();
    }

    public void OnClickedWoodChestBtn()
    {
        if (DatabaseManager.instance.myStageProgressData.isStage_3_Clear == true)
        {
            OffAllChestDrawPanel();
            OnWoodChestDrawPanel();
        }
        else
        {
            StartCoroutine(ShowAlarmText("늪지대 클리어시 개방"));
        }
    }

    public void OnClickedSilverChestBtn()
    {
        if (DatabaseManager.instance.myStageProgressData.isStage_5_Clear == true)
        {
            OffAllChestDrawPanel();
            OnWoodChestDrawPanel();
        }
        else
        {
            StartCoroutine(ShowAlarmText("5스테이지 (이름적어줘요) 클리어시 개방"));
        }
    }

    public void OnClickedGoldChestBtn()
    {
        if (DatabaseManager.instance.myStageProgressData.isStage_7_Clear == true)
        {
            OffAllChestDrawPanel();
            OnWoodChestDrawPanel();
        }
        else
        {
            StartCoroutine(ShowAlarmText("7스테이지 (이름적어줘요) 클리어시 개방"));
        }
    }

    #endregion

    IEnumerator ShowAlarmText(string _Text)
    {
        alarmObj.SetActive(true);
        alarmText.text = _Text;

        yield return new WaitForSeconds(3f);

        alarmObj.SetActive(false);
    }

    public string GetNowActivePanelToString()
    {
        if (woodBoxDrawPanel.activeSelf == true)
            return "woodBoxDrawPanel";
        if (woodChestDrawPanel.activeSelf == true)
            return "woodChestDrawPanel";
        if (silverChestDrawPanel.activeSelf)
            return "silverChestDrawPanel";
        if (goldChestDrawPanel.activeSelf)
            return "goldChestDrawPanel";


        return "Null";
    }

    #region On/Off

    public void OnWoodBoxDrawPanel()
    {
        woodBoxDrawPanel.SetActive(true);
    }

    public void OffWoodBoxDrawPanel()
    {
        woodBoxDrawPanel.SetActive(false);
    }

    public void OnWoodChestDrawPanel()
    {
        woodChestDrawPanel.SetActive(true);
    }
    public void OffWoodChestDrawPanel()
    {
        woodChestDrawPanel.SetActive(false);
    }

    public void OnSilverChestDrawPanel()
    {
        silverChestDrawPanel.SetActive(true);
    }
    public void OffSilverChestDrawPanel()
    {
        silverChestDrawPanel.SetActive(false);
    }

    public void OnGoldChestDrawPanel()
    {
        goldChestDrawPanel.SetActive(true);
    }
    public void OffGoldChestDrawPanel()
    {
        goldChestDrawPanel.SetActive(false);
    }

    public void OffAllChestDrawPanel()
    {
        OffWoodBoxDrawPanel();
        OffWoodChestDrawPanel();
        OffSilverChestDrawPanel();
        OffGoldChestDrawPanel();
    }

    #endregion
}
