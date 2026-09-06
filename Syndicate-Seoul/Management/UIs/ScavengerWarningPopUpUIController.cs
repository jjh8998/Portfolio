using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스캐빈저 점거 슬롯을 클릭했을 때 표시되는 경고 모달.
/// "예"를 누르면 Confirmed 이벤트가 (도시, 슬롯 인덱스)와 함께 발생한다.
/// </summary>
public class ScavengerWarningPopUpUIController : MonoBehaviour, IEscapeClosable
{
    private const string DefaultMessage =
        "이 슬롯을 해금하기 위해서는 해당 지역을 점거중인 무리와 싸워야 합니다.\n정말 싸우시겠습니까?";

    [SerializeField] private GameObject popupRoot;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button cancelButton;

    private CityScript pendingCity;
    private int pendingSlotIndex = -1;

    /// <summary>"예" 클릭 시 (도시, 슬롯 인덱스)와 함께 발생.</summary>
    public event Action<CityScript, int> Confirmed;

    public bool IsOpen => popupRoot != null && popupRoot.activeSelf;

    private void Awake()
    {
        ClosePopup();
        BindButtons();
    }

    private void OnDestroy()
    {
        UnbindButtons();
    }

    public void Open(CityScript _city, int _slotIndex)
    {
        if (_city == null || _slotIndex < 0)
            return;

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        CenterPopup();

        pendingCity = _city;
        pendingSlotIndex = _slotIndex;

        if (messageText != null)
        {
            messageText.text = DefaultMessage;
            ManagementUIDesignSystem.StyleText(messageText);
        }

        if (popupRoot != null)
            popupRoot.SetActive(true);
    }

    private void CenterPopup()
    {
        StretchToParent(transform as RectTransform);

        RectTransform popupRect = popupRoot != null ? popupRoot.transform as RectTransform : null;
        StretchToParent(popupRect);

        if (popupRect == null || popupRect.childCount == 0)
            return;

        RectTransform panelRect = popupRect.GetChild(0) as RectTransform;
        if (panelRect == null)
            return;

        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
    }

    private static void StretchToParent(RectTransform rect)
    {
        if (rect == null)
            return;

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
    }

    public void ClosePopup()
    {
        pendingCity = null;
        pendingSlotIndex = -1;

        if (popupRoot != null)
            popupRoot.SetActive(false);
    }

    public bool CloseByEscape()
    {
        if (!IsOpen)
            return false;

        ClosePopup();
        return true;
    }

    private void OnConfirmClicked()
    {
        CityScript city = pendingCity;
        int slotIndex = pendingSlotIndex;

        ClosePopup();

        if (city == null || slotIndex < 0)
            return;

        Confirmed?.Invoke(city, slotIndex);
    }

    private void BindButtons()
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(OnConfirmClicked);
            confirmButton.onClick.AddListener(OnConfirmClicked);
        }

        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveListener(ClosePopup);
            cancelButton.onClick.AddListener(ClosePopup);
        }
    }

    private void UnbindButtons()
    {
        if (confirmButton != null)
            confirmButton.onClick.RemoveListener(OnConfirmClicked);

        if (cancelButton != null)
            cancelButton.onClick.RemoveListener(ClosePopup);
    }
}
