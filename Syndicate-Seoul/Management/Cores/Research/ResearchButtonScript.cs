using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum ResearchVisualState
{
    Locked,
    Available,
    Queued,
    InProgress,
    Completed
}

/// <summary>
/// 연구 ID를 기준으로 데이터를 조회해 버튼 UI를 갱신하고 클릭 이벤트를 전달하는 스크립트.
/// </summary>
public class ResearchButtonScript : MonoBehaviour
{
    [SerializeField] private String researchId;
    [SerializeField] private ResearchDatabaseSO researchDatabase;
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Graphic backgroundGraphic;
    [SerializeField] private Image lockedImage;
    [SerializeField] private GameObject queueOrderObject;

    private Action<ResearchData> clickAction;
    private ResearchData researchData;
    private TMP_Text queueOrderText;

    public string ResearchId => researchId;
    public ResearchData CurrentResearchData => researchData;

    private void Awake()
    {
        CacheQueueOrderText();
        BindButton();
        ResolveResearchData();
        RefreshBasicInfo();
    }

    private void OnValidate()
    {
        if (Application.isPlaying)
            return;

        if (string.IsNullOrEmpty(researchId))
            researchId = "0";

        ResolveResearchData();
    }

    public void Setup(ResearchData _researchData, Action<ResearchData> _clickAction)
    {
        clickAction = _clickAction;
        SetResearch(_researchData);
        BindButton();
        RefreshBasicInfo();
    }

    public void Setup(string _researchId, Action<ResearchData> _clickAction)
    {
        clickAction = _clickAction;
        SetResearchId(_researchId);
        BindButton();
    }

    public void SetResearchId(string _researchId)
    {
        researchId = string.IsNullOrEmpty(_researchId) ? "0" : _researchId;
        ResolveResearchData();
        RefreshBasicInfo();
    }

    public void RefreshState(ResearchVisualState _state, Color _backgroundColor)
    {
        RefreshState(_state, _backgroundColor, _state == ResearchVisualState.Locked);
    }

    public void RefreshState(ResearchVisualState _state, Color _backgroundColor, bool _showLockedImage)
    {
        Color nameTextColor = nameText != null ? nameText.color : Color.white;
        RefreshState(_state, _backgroundColor, _showLockedImage, nameTextColor);
    }

    public void RefreshState(ResearchVisualState _state, Color _backgroundColor, bool _showLockedImage, Color _nameTextColor)
    {
        bool canClick = researchData != null;

        if (backgroundGraphic != null)
            backgroundGraphic.color = _backgroundColor;

        if (nameText != null)
            nameText.color = _nameTextColor;

        SetLockedImageVisible(_showLockedImage);

        if (button != null)
            button.interactable = canClick;
    }

    public void SetQueueOrder(int _order)
    {
        if (queueOrderObject == null)
            return;

        if (queueOrderText == null)
            CacheQueueOrderText();

        bool showOrder = _order > 0;
        if (queueOrderText != null)
            queueOrderText.text = showOrder ? _order.ToString() : string.Empty;

        queueOrderObject.SetActive(showOrder);
    }

    private void RefreshBasicInfo()
    {
        if (nameText != null)
            nameText.text = researchData != null ? researchData.name : "Unknown Research";

        if (iconImage != null)
        {
            Sprite sprite = null;

            if (researchData != null)
            {
                if (researchData.iconImage != null)
                    sprite = researchData.iconImage.sprite;

                if (sprite == null)
                    sprite = researchData.iconSprite;
            }

            iconImage.sprite = sprite;
            iconImage.enabled = sprite != null;
        }

        if (button != null)
            button.interactable = researchData != null;

        SetLockedImageVisible(false);

        SetQueueOrder(0);
    }

    private void OnClickButton()
    {
        if (researchData == null || clickAction == null)
            return;

        clickAction.Invoke(researchData);
    }

    private void BindButton()
    {
        if (button == null)
            return;

        button.onClick.RemoveListener(OnClickButton);
        button.onClick.AddListener(OnClickButton);
    }

    private void CacheQueueOrderText()
    {
        queueOrderText = queueOrderObject != null
            ? queueOrderObject.GetComponentInChildren<TMP_Text>(true)
            : null;
    }

    private void SetLockedImageVisible(bool _visible)
    {
        if (lockedImage != null)
            lockedImage.gameObject.SetActive(_visible);
    }

    private void SetResearch(ResearchData _researchData)
    {
        researchData = _researchData;

        if (researchData == null)
        {
            researchId = "0";
            return;
        }

        researchId = researchData.id ?? "0";
    }

    private void ResolveResearchData()
    {
        if (string.IsNullOrEmpty(researchId))
        {
            researchData = null;
            return;
        }

        ResearchDatabaseSO db = GetResearchDatabase();
        researchData = db != null ? db.GetResearchById(researchId) : null;
    }

    private ResearchDatabaseSO GetResearchDatabase()
    {
        if (researchDatabase == null)
            researchDatabase = Resources.Load<ResearchDatabaseSO>("Databases/ResearchDatabase");

        if (researchDatabase == null)
        {
            researchDatabase = ScriptableObject.CreateInstance<ResearchDatabaseSO>();
            researchDatabase.LoadCSV();
        }

        return researchDatabase;
    }
}
