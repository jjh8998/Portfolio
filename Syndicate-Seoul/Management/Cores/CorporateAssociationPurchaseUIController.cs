using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class CorporateAssociationPurchaseUIController : MonoBehaviour
{
    [SerializeField] private CitySharePurchaseService sharePurchaseService;
    [SerializeField] private TMP_InputField purchaseAmountInput;
    [SerializeField] private Button purchaseButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private TMP_Text availableShareText;
    [SerializeField] private TMP_Text cityNameText;

    private CityScript currentCity;
    private FactionManager playerFac;

    public event Action Purchased;

    private void Awake()
    {
        if (purchaseButton != null)
        {
            purchaseButton.onClick.RemoveListener(OnClickPurchaseButton);
            purchaseButton.onClick.AddListener(OnClickPurchaseButton);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Close);
            closeButton.onClick.AddListener(Close);
        }

        if (purchaseAmountInput != null)
        {
            purchaseAmountInput.onValueChanged.RemoveListener(OnPurchaseAmountChanged);
            purchaseAmountInput.onValueChanged.AddListener(OnPurchaseAmountChanged);
        }
    }

    private void OnDestroy()
    {
        if (purchaseButton != null)
            purchaseButton.onClick.RemoveListener(OnClickPurchaseButton);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);

        if (purchaseAmountInput != null)
            purchaseAmountInput.onValueChanged.RemoveListener(OnPurchaseAmountChanged);
    }

    public void SetPlayerFaction(FactionManager _playerFac)
    {
        playerFac = _playerFac;
    }

    public void Show(CityScript _city)
    {
        currentCity = _city;
        Refresh();
    }

    public void Clear()
    {
        currentCity = null;

        if (purchaseAmountInput != null)
            purchaseAmountInput.text = string.Empty;

        SetPriceText(string.Empty);
        SetAvailableShareText(string.Empty);
        SetResultText(string.Empty);
        SetCityNameText(string.Empty);

        if (purchaseButton != null)
            purchaseButton.interactable = false;
    }

    public void Close()
    {
        Clear();
        gameObject.SetActive(false);
    }

    private void OnClickPurchaseButton()
    {
        if (sharePurchaseService == null)
        {
            SetResultText("지분 구매 서비스를 찾을 수 없습니다.");
            return;
        }

        if (playerFac == null)
        {
            SetResultText("플레이어 세력이 설정되지 않았습니다.");
            return;
        }

        if (currentCity == null)
        {
            SetResultText("도시가 선택되지 않았습니다.");
            return;
        }

        if (purchaseAmountInput == null)
        {
            SetResultText("구매 수량 입력 필드가 없습니다.");
            return;
        }

        if (!int.TryParse(purchaseAmountInput.text, out int amount) || amount <= 0)
        {
            SetResultText("올바른 지분 수량을 입력하세요.");
            return;
        }

        bool success = sharePurchaseService.TryPurchaseShare(currentCity, playerFac, amount, out string message);
        SetResultText(message);

        if (!success)
            return;

        Refresh();
        Purchased?.Invoke();

        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.NotifyCondition(TutorialCondition.PurchaseCorporateAssociationShare);
            if (TutorialManager.Instance.IsActive)
                Close();
        }
    }

    private void Refresh()
    {
        if (currentCity == null || currentCity.cityData == null || currentCity.cityData.shareData == null)
        {
            SetCityNameText(string.Empty);
            SetPriceText(string.Empty);
            SetAvailableShareText(string.Empty);

            if (purchaseButton != null)
                purchaseButton.interactable = false;

            return;
        }

        string cityName = CityDisplayNameUtility.ToKoreanDisplayName(currentCity.cityData.cityName);
        SetCityNameText(string.IsNullOrWhiteSpace(cityName) ? string.Empty : $"{cityName}");

        int amount = 0;
        bool hasValidAmount = purchaseAmountInput != null
            && int.TryParse(purchaseAmountInput.text, out amount)
            && amount > 0;

        bool canPurchase = false;
        if (sharePurchaseService != null && hasValidAmount)
        {
            SetPriceText($"예상 가격: {sharePurchaseService.GetPurchasePrice(amount)}");
            canPurchase = sharePurchaseService.CanPurchaseShare(currentCity, playerFac, amount, out _);
        }
        else
        {
            SetPriceText(string.Empty);
        }

        SetAvailableShareText($"기업협회 보유 지분: {currentCity.cityData.shareData.UnassignedSharePercent}%");

        if (purchaseButton != null)
            purchaseButton.interactable = canPurchase;
    }

    private void OnPurchaseAmountChanged(string value)
    {
        Refresh();
    }

    private void SetResultText(string value)
    {
        if (resultText != null)
            resultText.text = value;
    }

    private void SetPriceText(string value)
    {
        if (priceText != null)
            priceText.text = value;
    }

    private void SetAvailableShareText(string value)
    {
        if (availableShareText != null)
            availableShareText.text = value;
    }

    private void SetCityNameText(string value)
    {
        if (cityNameText != null)
            cityNameText.text = value;
    }
}
