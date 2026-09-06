using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{

    public static MainMenuManager instance;

    [SerializeField]
    private GameObject mainMenuInfoPanel = null;
    [SerializeField]
    private GameObject optionPanel = null;
    [SerializeField]
    private GameObject buttomPanel = null;
    [SerializeField]
    private GameObject stageSelectPanel = null;
    [SerializeField]
    private GameObject wordSelectView = null;
    [SerializeField]
    private GameObject questPanel = null;
    [SerializeField]
    private GameObject drawPanel = null;
    [SerializeField]
    private GameObject inventoryPanel = null;
    [SerializeField]
    private GameObject magicPanel = null;
    [SerializeField]
    private GameObject adPanel = null;

    private CanvasGroup questPanelCanvasGroup; // 퀘스트패널 캔버스 그룹 - 알파값 조정용

    private void Awake()
    {
        if (instance == null)
            instance = this;
    }
    
    private void Start()
    {
        questPanelCanvasGroup = questPanel.GetComponent<CanvasGroup>();
    }

    // * 이름 수정 바람.
    public void OffAllPanel()
    {
        OffOptionPanel();
        OffStageSelectPanel();
        OffInventoryPanel();
        OffQuestPanel();
        OffDrawPanel();
        OffMagicPanel();

        OffAdPanel();

        OnBasicPanel();
    }

    public void OnBasicPanel()
    {
        OnMMInfoPanel();
        OnButtonPanel();

        OnAdPanel();
    }

    public void OffBasicPanel()
    {
        OffMMInfoPanel();
        OffButtonPanel();

        OffAdPanel();
    }

    #region MainMenuInfoPanel

    public void OnMMInfoPanel()
    {
        mainMenuInfoPanel.SetActive(true);
    }

    public void OffMMInfoPanel()
    {
        mainMenuInfoPanel.SetActive(false);
    }

    #endregion

    #region OptionPanel

    public void OnOptionPanel()
    {
        OffButtonPanel();
        optionPanel.SetActive(true);
    }

    public void OffOptionPanel()
    {
        OnButtonPanel();
        optionPanel.SetActive(false);
    }

    #endregion

    #region ButtonPanel

    public void OnButtonPanel()
    {
        buttomPanel.SetActive(true);
    }

    public void OffButtonPanel()
    {
        buttomPanel.SetActive(false);
    }

    #endregion

    #region StageSelect

    public void OnStageSelectPanel()
    {
        OffBasicPanel();

        stageSelectPanel.SetActive(true);
    }

    public void OffStageSelectPanel()
    {
        stageSelectPanel.SetActive(false);

        OffWordSelectView();
    }

    public void OnWordSelectView()
    {
        wordSelectView.SetActive(true);
    }

    public void OffWordSelectView()
    {
        wordSelectView.SetActive(false);
    }

    #endregion

    #region Quest

    public void OnQuestPanel()
    {
        questPanel.SetActive(true);
    }

    public void OffQuestPanel()
    {
        questPanel.SetActive(false);
    }

    public void SetQuestPanelAlpha(float _Float)
    {
        questPanelCanvasGroup.alpha = _Float;
    }

    #endregion

    #region Draw

    public void OnDrawPanel()
    {
        OffBasicPanel();

        drawPanel.SetActive(true);
    }

    public void OffDrawPanel()
    {
        drawPanel.SetActive(false);
    }

    #endregion

    #region Inventory

    public void OnInventoryPanel()
    {
        OffBasicPanel();

        InventoryScript.instance.ShowItem();
        inventoryPanel.SetActive(true);
    }

    public void OffInventoryPanel()
    {
        inventoryPanel.SetActive(false);
        // 위치 바꿔야함
    }

    #endregion

    #region Magic

    public void OnMagicPanel()
    {
        if (DatabaseManager.instance.myStageProgressData.isStage_1_Clear == true)
        {
            magicPanel.SetActive(true);
            MagicSelectManager.instance.ShowAlphabetCount();
        }
        else
        {
            if (GameManager.instance != null)
                GameManager.instance.ShowAlarmPanel("소울루스 평원을 클리어 해야합니다.");
        }
    }

    public void OffMagicPanel()
    {
        magicPanel.SetActive(false);
    }

    #endregion


    #region Ad

    public void OnAdPanel()
    {
        adPanel.SetActive(true);
    }

    public void OffAdPanel()
    {
        adPanel.SetActive(false);
    }

    #endregion
}
