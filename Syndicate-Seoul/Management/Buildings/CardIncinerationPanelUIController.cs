using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardIncinerationPanelUIController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button incinerateButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private TMP_Text selectedCardText;
    [SerializeField] private TMP_Text emptyText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private ShowCardListPanel showCardListPanel;
    [SerializeField] private Transform activeIncinerationCardRoot;
    [SerializeField] private HandCardUI activeIncinerationCardPrefab;
    [SerializeField] private TMP_Text incinerationTimeText;

    [Header("Service")]
    [SerializeField] private CardIncinerationPowerService defaultService;

    private CityScript currentCity;
    private FactionManager currentOwner;
    private CardIncinerationPowerService currentService;
    private string selectedCardId = string.Empty;

    private void Awake()
    {
        if (incinerateButton != null)
            incinerateButton.onClick.AddListener(OnClickIncinerate);

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        ResetSelection();
    }

    private void OnDisable()
    {
        UnsubscribeCurrentOwner();
    }

    private void OnDestroy()
    {
        if (incinerateButton != null)
            incinerateButton.onClick.RemoveListener(OnClickIncinerate);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);

        UnsubscribeCurrentOwner();
    }

    public void Open(CityScript _city)
    {
        if (_city == null || _city.cityData == null)
        {
            Debug.LogWarning("[CardIncinerationPanelUIController] City or city data is missing.");
            return;
        }

        if (_city.cityData.owner == null)
        {
            Debug.LogWarning("[CardIncinerationPanelUIController] City owner is missing.");
            return;
        }

        Open(_city.cityData.owner, _city);
    }

    public void Open(FactionManager _owner)
    {
        Open(_owner, null);
    }

    private void Open(FactionManager _owner, CityScript _city)
    {
        if (_owner == null)
        {
            Debug.LogWarning("[CardIncinerationPanelUIController] City owner is missing.");
            return;
        }

        UnsubscribeCurrentOwner();

        currentCity = _city;
        currentOwner = _owner;
        selectedCardId = string.Empty;

        SetPanelVisible(true);
        SubscribeCurrentOwner();
        Refresh();
        SetMessage(IsIncinerationRunning()
            ? "이미 카드 소각 발전이 진행 중입니다."
            : "소각할 카드를 선택하세요.");
    }

    public void Close()
    {
        SetPanelVisible(false);
        UnsubscribeCurrentOwner();
        currentCity = null;
        currentOwner = null;
        ResetSelection();
    }

    public void Refresh()
    {
        ResetSelection();

        if (currentOwner == null)
        {
            SetEmptyTextVisible(true, "보유 카드가 없습니다.");
            RefreshActiveIncinerationCard();
            return;
        }

        CardInventory inventory = currentOwner.GetCardInventory();
        if (inventory == null)
        {
            if (showCardListPanel != null)
                showCardListPanel.Open(new List<string>(), true, OnCardSelected);

            SetEmptyTextVisible(true, "보유 카드가 없습니다.");
            RefreshActiveIncinerationCard();
            SetMessage("카드 목록을 찾을 수 없습니다.");
            return;
        }

        List<string> cardIds = new List<string>();
        List<CardStack> cardStacks = inventory.GetAll();
        if (cardStacks == null)
        {
            if (showCardListPanel != null)
                showCardListPanel.Open(new List<string>(), true, OnCardSelected);

            SetEmptyTextVisible(true, "보유 카드가 없습니다.");
            RefreshActiveIncinerationCard();
            return;
        }

        CardDatabase cardDatabase = CardDatabase.Instance;
        for (int i = 0; i < cardStacks.Count; i++)
        {
            CardStack stack = cardStacks[i];
            if (stack == null || stack.count <= 0 || string.IsNullOrWhiteSpace(stack.cardId))
                continue;

            CardData cardData = cardDatabase != null ? cardDatabase.GetById(stack.cardId) : null;
            if (cardData == null)
            {
                Debug.LogWarning($"[CardIncinerationPanelUIController] Card '{stack.cardId}' not found.");
                continue;
            }

            cardIds.Add(stack.cardId);
        }

        if (showCardListPanel == null)
        {
            Debug.LogWarning("[CardIncinerationPanelUIController] ShowCardListPanel is missing.");
            SetEmptyTextVisible(cardIds.Count == 0, "보유 카드가 없습니다.");
            RefreshActiveIncinerationCard();
            SetMessage("카드 목록 패널을 찾을 수 없습니다.");
            return;
        }

        SetEmptyTextVisible(cardIds.Count == 0, "보유 카드가 없습니다.");
        showCardListPanel.Open(cardIds, true, OnCardSelected);

        if (cardIds.Count == 0)
            SetMessage("소각할 수 있는 카드가 없습니다.");

        RefreshActiveIncinerationCard();
    }

    private void OnCardSelected(string _cardId)
    {
        if (string.IsNullOrWhiteSpace(_cardId))
            return;

        selectedCardId = _cardId;
        CardDatabase cardDatabase = CardDatabase.Instance;
        CardData cardData = cardDatabase != null ? cardDatabase.GetById(_cardId) : null;

        if (selectedCardText != null)
            selectedCardText.text = cardData == null || string.IsNullOrWhiteSpace(cardData.cardName)
                ? _cardId
                : $"{cardData.cardName} ({_cardId})";

        bool isIncinerationRunning = IsIncinerationRunning();
        if (incinerateButton != null)
            incinerateButton.interactable = !isIncinerationRunning;

        SetMessage(isIncinerationRunning
            ? "이미 카드 소각 발전이 진행 중입니다."
            : "소각할 카드를 선택했습니다.");
    }

    private void OnClickIncinerate()
    {
        if (string.IsNullOrWhiteSpace(selectedCardId))
        {
            Debug.LogWarning("[CardIncinerationPanelUIController] No card selected.");
            SetMessage("소각할 카드를 선택하세요.");
            return;
        }

        CardIncinerationPowerService service = FindServiceForCurrentOwner();
        if (service == null)
        {
            Debug.LogWarning("[CardIncinerationPanelUIController] CardIncinerationPowerService for current owner was not found.");
            SetMessage("소각할 수 없습니다.");
            return;
        }

        bool wasIncinerationRunning = service.HasActiveIncinerationEffect();
        bool success = currentCity != null
            ? service.TryIncinerateSelectedCard(selectedCardId, currentCity)
            : service.TryIncinerateSelectedCard(selectedCardId);
        if (success)
        {
            selectedCardId = string.Empty;
            Refresh();
            SetMessage("카드를 소각했습니다.");
            return;
        }

        SetMessage(wasIncinerationRunning || service.HasActiveIncinerationEffect()
            ? "현재 소각 발전이 끝날 때까지 추가 소각할 수 없습니다."
            : "소각할 수 없습니다.");
    }

    private CardIncinerationPowerService FindServiceForCurrentOwner()
    {
        if (currentOwner == null)
        {
            currentService = null;
            return null;
        }

        if (currentService != null && currentService.IsOwnedBy(currentOwner))
            return currentService;

        if (defaultService != null && currentCity != null)
        {
            defaultService.SetFactionManager(currentOwner);
            currentService = defaultService;
            return currentService;
        }

        if (defaultService != null && defaultService.IsOwnedBy(currentOwner))
        {
            currentService = defaultService;
            return currentService;
        }

        CardIncinerationPowerService[] services = FindObjectsByType<CardIncinerationPowerService>(FindObjectsSortMode.None);
        for (int i = 0; i < services.Length; i++)
        {
            CardIncinerationPowerService service = services[i];
            if (service != null && service.IsOwnedBy(currentOwner))
            {
                currentService = service;
                return currentService;
            }
        }

        currentService = null;
        return null;
    }

    private bool IsIncinerationRunning()
    {
        CardIncinerationPowerService service = FindServiceForCurrentOwner();
        return service != null && service.HasActiveIncinerationEffect();
    }

    private void OnCardInventoryChanged(string _cardId, int _currentCount)
    {
        Refresh();
    }

    private void OnActiveEffectsChanged()
    {
        Refresh();
    }

    private void SubscribeCurrentOwner()
    {
        if (currentOwner == null)
            return;

        currentOwner.CardInventoryChanged -= OnCardInventoryChanged;
        currentOwner.CardInventoryChanged += OnCardInventoryChanged;

        CardIncinerationPowerService service = FindServiceForCurrentOwner();
        if (service == null)
            return;

        service.ActiveEffectsChanged -= OnActiveEffectsChanged;
        service.ActiveEffectsChanged += OnActiveEffectsChanged;
    }

    private void UnsubscribeCurrentOwner()
    {
        if (currentOwner != null)
            currentOwner.CardInventoryChanged -= OnCardInventoryChanged;

        if (currentService != null)
            currentService.ActiveEffectsChanged -= OnActiveEffectsChanged;

        currentService = null;
    }

    private void ResetSelection()
    {
        selectedCardId = string.Empty;

        if (selectedCardText != null)
            selectedCardText.text = "소각할 카드를 선택하세요.";

        if (incinerateButton != null)
            incinerateButton.interactable = false;
    }

    private void SetMessage(string _message)
    {
        if (messageText != null)
            messageText.text = _message;
    }

    private void SetEmptyTextVisible(bool _isVisible, string _message)
    {
        if (emptyText == null)
            return;

        emptyText.gameObject.SetActive(_isVisible);
        if (_isVisible)
            emptyText.text = _message;
    }

    private void SetPanelVisible(bool _isVisible)
    {
        GameObject root = panelRoot != null ? panelRoot : gameObject;
        if (root != null)
            root.SetActive(_isVisible);
    }

    private void RefreshActiveIncinerationCard()
    {
        ClearActiveIncinerationCard();

        CardIncinerationPowerService service = FindServiceForCurrentOwner();
        if (service == null)
        {
            SetIncinerationTimeText("소각 중인 카드 없음");
            return;
        }

        if (service.HasActiveIncinerationEffect() && incinerateButton != null)
            incinerateButton.interactable = false;

        List<CardIncinerationPowerService.ActiveIncinerationEffectViewData> activeEffects = service.GetActiveEffectsViewData();
        if (activeEffects == null || activeEffects.Count == 0)
        {
            SetIncinerationTimeText("소각 중인 카드 없음");
            return;
        }

        CardIncinerationPowerService.ActiveIncinerationEffectViewData selectedEffect = activeEffects[0];
        for (int i = 1; i < activeEffects.Count; i++)
        {
            CardIncinerationPowerService.ActiveIncinerationEffectViewData effect = activeEffects[i];
            if (effect == null)
                continue;

            if (selectedEffect == null || effect.remainingMonths < selectedEffect.remainingMonths)
                selectedEffect = effect;
        }

        if (selectedEffect == null)
        {
            SetIncinerationTimeText("소각 중인 카드 없음");
            return;
        }

        SetIncinerationTimeText($"남은 기간: {Mathf.Max(0, selectedEffect.remainingMonths)}개월");

        if (activeIncinerationCardRoot == null || string.IsNullOrWhiteSpace(selectedEffect.cardId))
            return;

        CardDatabase cardDatabase = CardDatabase.Instance;
        CardData cardData = cardDatabase != null ? cardDatabase.GetById(selectedEffect.cardId) : null;
        if (cardData == null)
        {
            Debug.LogWarning($"[CardIncinerationPanelUIController] Active incineration card '{selectedEffect.cardId}' not found.");
            return;
        }

        HandCardUI cardPrefab = ResolveActiveIncinerationCardPrefab();
        if (cardPrefab == null)
            return;

        HandCardUI cardUI = Instantiate(cardPrefab, activeIncinerationCardRoot);
        cardUI.Setup(cardData, -1, 1);
        cardUI.SetGhostMode(false);
        cardUI.SetClickCallback(null);
        cardUI.gameObject.SetActive(true);
    }

    private void ClearActiveIncinerationCard()
    {
        if (activeIncinerationCardRoot == null)
            return;

        for (int i = activeIncinerationCardRoot.childCount - 1; i >= 0; i--)
            Destroy(activeIncinerationCardRoot.GetChild(i).gameObject);
    }

    private HandCardUI ResolveActiveIncinerationCardPrefab()
    {
        if (activeIncinerationCardPrefab != null)
            return activeIncinerationCardPrefab;

        activeIncinerationCardPrefab = Resources.Load<HandCardUI>("Prefabs/card");
        if (activeIncinerationCardPrefab != null)
            return activeIncinerationCardPrefab;

        GameObject prefabObject = Resources.Load<GameObject>("Prefabs/card");
        if (prefabObject != null)
            activeIncinerationCardPrefab = prefabObject.GetComponent<HandCardUI>();

        return activeIncinerationCardPrefab;
    }

    private void SetIncinerationTimeText(string message)
    {
        if (incinerationTimeText != null)
            incinerationTimeText.text = message;
    }
}
