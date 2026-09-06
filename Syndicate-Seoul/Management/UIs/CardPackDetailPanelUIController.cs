using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardPackDetailPanelUIController : MonoBehaviour, IEscapeClosable
{
    [SerializeField] private GameObject detailPanel;
    [SerializeField] private TMP_Text packNameText;
    [SerializeField] private TMP_Text packCountText;
    [SerializeField] private Image packImage;
    [SerializeField] private TMP_Text rateText;
    [SerializeField] private Button open1PackButton;
    [SerializeField] private Button openXPackButton;
    [SerializeField] private TMP_InputField openCountInputField;
    [SerializeField] private ShowCardListPanel showCardListPanel;
    [Tooltip("비어 있으면 연출 없이 즉시 결과 그리드를 연다.")]
    [SerializeField] private CardPackOpenVfx packOpenVfx;

    private FactionManager owner;
    private CardPackDatabaseSO cardPackDatabase;
    private CardPackData selectedPackData;
    private int drawCount = CardPackOpenService.DefaultDrawCount;
    private Action<string, int> onOpenedCallback;

    public bool IsOpen => IsPanelOpen(detailPanel) ||
                          (showCardListPanel != null && showCardListPanel.IsOpen);

    /// <summary>획득 카드 목록(ShowCardListPanel)이 열려 있는지 여부.</summary>
    public bool IsShowCardListOpen => showCardListPanel != null && showCardListPanel.IsOpen;

    private void Awake()
    {
        ClearPackImage();

        if (open1PackButton != null)
            open1PackButton.onClick.AddListener(OnOpen1PackButtonClicked);

        if (openXPackButton != null)
            openXPackButton.onClick.AddListener(OnOpenXPackButtonClicked);

        if (openCountInputField != null)
            openCountInputField.onValueChanged.AddListener(OnOpenCountInputChanged);
    }

    private void OnDestroy()
    {
        if (open1PackButton != null)
            open1PackButton.onClick.RemoveListener(OnOpen1PackButtonClicked);

        if (openXPackButton != null)
            openXPackButton.onClick.RemoveListener(OnOpenXPackButtonClicked);

        if (openCountInputField != null)
            openCountInputField.onValueChanged.RemoveListener(OnOpenCountInputChanged);
    }

    public void Open(
        FactionManager _owner,
        CardPackDatabaseSO _cardPackDatabase,
        CardPackData _packData,
        int _currentCount,
        int _drawCount,
        Action<string, int> _onOpenedCallback)
    {
        owner = _owner;
        cardPackDatabase = _cardPackDatabase;
        selectedPackData = _packData;
        drawCount = Mathf.Max(1, _drawCount > 0 ? _drawCount : CardPackOpenService.DefaultDrawCount);
        onOpenedCallback = _onOpenedCallback;

        if (detailPanel != null)
            detailPanel.SetActive(true);
        else
            Debug.LogWarning("[CardPackDetailPanelUI] Detail panel is null.");

        UpdatePackNameText();
        UpdatePackCountText(_currentCount);
        UpdatePackImage();
        UpdateRateText();
        SetOpenCountInputFieldToCurrentCount(_currentCount);
        UpdateButtonInteractable(_currentCount);
        UpdateOpenXPackButtonText();
    }

    public void Close()
    {
        if (detailPanel != null)
            detailPanel.SetActive(false);

        ClearPackImage();
    }

    public void CloseAll()
    {
        if (showCardListPanel != null)
            showCardListPanel.Close();

        Close();
    }

    public bool CloseByEscape()
    {
        return CloseTopmost();
    }

    public bool CloseTopmost()
    {
        if (showCardListPanel != null && showCardListPanel.IsOpen)
        {
            showCardListPanel.Close();
            return true;
        }

        if (!IsPanelOpen(detailPanel))
            return false;

        Close();
        return true;
    }

    private void OnOpen1PackButtonClicked()
    {
        OpenPacks(1);
    }

    private void OnOpenXPackButtonClicked()
    {
        int currentCount = owner != null && selectedPackData != null && !string.IsNullOrWhiteSpace(selectedPackData.id)
            ? owner.GetCardPackCount(selectedPackData.id)
            : 0;
        int openCount = ResolveOpenCount(currentCount);
        OpenPacks(openCount);
    }

    private void OnOpenCountInputChanged(string _value)
    {
        UpdateOpenXPackButtonText();
    }

    private void OpenPacks(int _openCount)
    {
        if (!CanTryOpenPack(out string packId))
            return;

        int currentCount = owner != null ? owner.GetCardPackCount(packId) : 0;
        int openCount = currentCount > 0
            ? Mathf.Clamp(_openCount, 1, currentCount)
            : 0;

        if (openCount < 1)
        {
            UpdatePackCountText(currentCount);
            UpdateOpenCountInputField(currentCount);
            UpdateButtonInteractable(currentCount);

            return;
        }

        List<string> gainedCardIds = new List<string>();
        int successCount = 0;

        for (int i = 0; i < openCount; i++)
        {
            bool opened = CardPackOpenService.TryOpenPack(
                owner,
                cardPackDatabase,
                packId,
                out CardPackOpenResult result,
                drawCount);

            if (!opened)
            {
                string reason = result != null && !string.IsNullOrWhiteSpace(result.errorMessage)
                    ? result.errorMessage
                    : "Unknown error.";

                Debug.LogWarning($"[CardPackDetailPanelUI] Failed to open pack. packId={packId}, succeeded={successCount}, reason={reason}");
                break;
            }

            successCount++;

            if (result != null && result.gainedCardIds != null && result.gainedCardIds.Count > 0)
                gainedCardIds.AddRange(result.gainedCardIds);
        }

        if (successCount > 0)
        {
            if (packOpenVfx != null)
            {
                // 개봉을 누른 순간부터 연출(개봉 VFX + 카드 등장)이 끝날 때까지 ESC 입력을 차단한다.
                ShowCardListPanel.SetEscBlocked(true);
                packOpenVfx.Play(selectedPackData, gainedCardIds, showCardListPanel);
            }
            else if (showCardListPanel != null)
            {
                ShowCardListPanel.SetEscBlocked(true);
                showCardListPanel.Open(gainedCardIds);
            }
            else
                Debug.LogWarning("[CardPackDetailPanelUI] ShowCardListPanel is null.");
        }

        currentCount = owner != null ? owner.GetCardPackCount(packId) : 0;
        UpdatePackCountText(currentCount);
        UpdateOpenCountInputField(currentCount);
        UpdateButtonInteractable(currentCount);
        UpdateOpenXPackButtonText();

        onOpenedCallback?.Invoke(packId, currentCount);
    }

    private bool CanTryOpenPack(out string _packId)
    {
        _packId = selectedPackData != null ? selectedPackData.id : string.Empty;

        if (owner == null)
        {
            Debug.LogWarning("[CardPackDetailPanelUI] Failed to open pack. reason=Owner is null.");
            return false;
        }

        if (cardPackDatabase == null)
        {
            Debug.LogWarning("[CardPackDetailPanelUI] Failed to open pack. reason=Card pack database is null.");
            return false;
        }

        if (selectedPackData == null)
        {
            Debug.LogWarning("[CardPackDetailPanelUI] Failed to open pack. reason=Selected pack data is null.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(_packId))
        {
            Debug.LogWarning("[CardPackDetailPanelUI] Failed to open pack. reason=Pack id is empty.");
            return false;
        }

        return true;
    }

    private void UpdatePackNameText()
    {
        if (packNameText == null)
            return;

        string packName = selectedPackData != null && !string.IsNullOrWhiteSpace(selectedPackData.name)
            ? selectedPackData.name
            : selectedPackData != null ? selectedPackData.id : string.Empty;

        packNameText.text = packName;
    }

    private void UpdatePackCountText(int _currentCount)
    {
        if (packCountText == null)
            return;

        packCountText.text = $"보유: {_currentCount}";
    }

    private void UpdatePackImage()
    {
        if (packImage == null)
            return;

        Sprite sprite = selectedPackData != null ? selectedPackData.GetCardPackSprite() : null;
        if (sprite == null)
        {
            ClearPackImage();
            return;
        }

        packImage.gameObject.SetActive(true);
        packImage.sprite = sprite;
        packImage.preserveAspect = true;
    }

    private void ClearPackImage()
    {
        if (packImage == null)
            return;

        packImage.sprite = null;
        packImage.gameObject.SetActive(false);
    }

    private void UpdateRateText()
    {
        if (rateText == null)
            return;

        if (selectedPackData == null)
        {
            rateText.text = string.Empty;
            return;
        }

        rateText.text =
            $"1티어: {selectedPackData.tier1Rate}%\n" +
            $"2티어: {selectedPackData.tier2Rate}%\n" +
            $"3티어: {selectedPackData.tier3Rate}%\n" +
            $"4티어: {selectedPackData.tier4Rate}%\n" +
            $"5티어: {selectedPackData.tier5Rate}%";
    }

    private int ResolveOpenCount(int currentCount)
    {
        int openCount = 5;

        if (openCountInputField != null)
        {
            string rawValue = openCountInputField.text;
            if (!int.TryParse(rawValue, out openCount) || openCount < 1)
                openCount = 5;
        }

        if (currentCount > 0)
            openCount = Mathf.Clamp(openCount, 1, currentCount);
        else
            openCount = 0;

        return openCount;
    }

    private void UpdateOpenCountInputField(int currentCount)
    {
        if (openCountInputField == null)
            return;

        if (currentCount <= 0)
        {
            openCountInputField.SetTextWithoutNotify("0");
            UpdateOpenXPackButtonText();
            return;
        }

        int openCount = 5;
        string rawValue = openCountInputField.text;
        if (!string.IsNullOrWhiteSpace(rawValue) && int.TryParse(rawValue, out int parsedValue) && parsedValue > 0)
            openCount = parsedValue;

        openCount = Mathf.Clamp(openCount, 1, currentCount);

        openCountInputField.SetTextWithoutNotify(openCount.ToString());
        UpdateOpenXPackButtonText();
    }

    private void SetOpenCountInputFieldToCurrentCount(int currentCount)
    {
        if (openCountInputField == null)
            return;

        int openCount = Mathf.Max(0, currentCount);
        openCountInputField.SetTextWithoutNotify(openCount.ToString());
        UpdateOpenXPackButtonText();
    }

    private void UpdateButtonInteractable(int _currentCount)
    {
        bool canOpen = _currentCount > 0;

        if (open1PackButton != null)
            open1PackButton.interactable = canOpen;

        if (openXPackButton != null)
            openXPackButton.interactable = canOpen;
    }

    private void UpdateOpenXPackButtonText()
    {
        if (openXPackButton == null)
            return;

        TMP_Text text = openXPackButton.GetComponentInChildren<TMP_Text>(true);
        if (text == null)
            return;

        int openCount = 5;
        if (openCountInputField != null)
        {
            string rawValue = openCountInputField.text;
            if (!int.TryParse(rawValue, out openCount) || openCount < 1)
                openCount = 5;
        }

        text.text = $"{openCount}개 개봉";
    }

    private static bool IsPanelOpen(GameObject panelObject)
    {
        return panelObject != null && panelObject.activeInHierarchy;
    }
}
