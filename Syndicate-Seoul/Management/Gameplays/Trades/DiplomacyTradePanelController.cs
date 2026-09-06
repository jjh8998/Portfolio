using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class DiplomacyTradePanelController : MonoBehaviour, IEscapeClosable
{
    private const string ManagerMissingMessage = "Diplomacy manager is missing.";
    private const string InvalidNumberMessage = "Enter valid whole numbers.";
    private const string NegativeSharePercentMessage = "Share percents cannot be negative.";
    private const string InvalidPatentLicenseMessage = "Select a valid patent research and license duration.";
    private const string NoCeoLabel = "No CEO";
    private const string UnknownCeoLabel = "Unknown CEO";

    [Header("Core References")]
    [SerializeField] private DiplomacyManager diplomacyManager;
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private CEODatabaseSO ceoDatabase;

    [Header("Sub Panels")]
    [SerializeField] private TradeCitySelectController tradeCitySelectController;
    [SerializeField] private TradeCardController tradeCardController;
    [SerializeField] private TradeValueInputController tradeCreditInputController;
    [SerializeField] private TradeOfferSummaryController tradeOfferSummaryController;

    [Header("Warning Popups")]
    [SerializeField] private GameObject warningPopup;

    [Header("Target Info UI")]
    [SerializeField] private TMP_Text targetCeoNameText;
    [SerializeField] private Image targetCeoIconImage;
    [SerializeField] private TMP_Text playerCeoNameText;
    [SerializeField] private Image playerCeoIconImage;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text playerInfoText;
    [SerializeField] private TMP_Text targetInfoText;
    [SerializeField] private TMP_Text debugText;
    [SerializeField] private TMP_Text chatText;

    [Header("Common Buttons")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private Button suggestTermsButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button closeCitySelectButton;

    [Header("Suggest Terms")]
    [SerializeField] private int suggestTermsExtraValue = 5;

    [Header("Player Buttons")]
    [SerializeField] private Button playerCreditButton;
    [SerializeField] private Button playerPowerButton;
    [SerializeField] private Button playerCardButton;
    [SerializeField] private Button playerShareCityButton;
    [SerializeField] private Button playerCityButton;

    [Header("Target Buttons")]
    [SerializeField] private Button aiCreditButton;
    [SerializeField] private Button aiPowerButton;
    [SerializeField] private Button aiShareCityButton;
    [SerializeField] private Button aiCityButton;

    [Header("AI Patent License UI")]
    [SerializeField] private Button aiPatentLicenseButton;
    [SerializeField] private GameObject aiPatentLicensePanelRoot;
    [SerializeField] private TMP_Dropdown aiPatentLicenseDropdown;
    [SerializeField] private Button aiPatentLicenseApplyButton;
    [SerializeField] private Button closeAiPatentLicensePanelButton;
    [SerializeField] private TMP_Dropdown playerPatentLicenseDurationDropdown;
    [SerializeField] private TMP_Dropdown aiPatentLicenseDurationDropdown;

    private FactionManager subscribedPlayerFaction;
    private FactionManager subscribedTargetFaction;
    private int playerGiveCredit;
    private int aiGiveCredit;
    private int playerGivePower;
    private int aiGivePower;
    private int playerGiveSharePercent;
    private int aiGiveSharePercent;
    private string playerGivePatentResearchId = string.Empty;
    private int playerGivePatentLicenseMonths;
    private string aiGivePatentResearchId = string.Empty;
    private int aiGivePatentLicenseMonths;
    private int playerPatentLicenseDurationMonths = PatentResearchManager.OneYearLicenseMonths;
    private int aiPatentLicenseDurationMonths = PatentResearchManager.OneYearLicenseMonths;
    private ResearchDatabaseSO researchDatabase;
    private readonly List<string> aiPatentLicenseResearchIds = new List<string>();
    private string latestStatusMessage = string.Empty;
    private Sprite fallbackCeoIcon;

    public bool IsOpen => IsPanelVisible() || IsTransientTradePanelOpen();

    private void Awake()
    {
        ResolveManager();
        ResolveTradeCitySelectController();
        ResolveTradeCardController();
        InitializePatentLicenseDurationDropdowns();
    }

    private void OnEnable()
    {
        ResolveManager();
        ResolveTradeCitySelectController();
        ResolveTradeCardController();
        BindMainButtonEvents();
        InitializePatentLicenseDurationDropdowns();
        BindPatentLicenseDurationDropdowns();
        SubscribeManagerEvents();
        SubscribeFactionEvents();
        SubscribeShareEvents();
        BindOfferInputEvents();
        RefreshView();
    }

    private void OnDisable()
    {
        CloseOnlyCityWarningPopup();
        UnbindMainButtonEvents();
        UnbindPatentLicenseDurationDropdowns();
        UnbindOfferInputEvents();
        UnsubscribeManagerEvents();
        UnsubscribeFactionEvents();
        UnsubscribeShareEvents();
    }

    public void OpenTradeWith(FactionManager _targetFaction)
    {
        ResolveManager();

        if (diplomacyManager != null)
            diplomacyManager.SelectTargetFaction(_targetFaction);

        latestStatusMessage = string.Empty;
        CloseOnlyCityWarningPopup();
        ResetInputs();
        SetPanelVisible(true);
        SubscribeFactionEvents();
        RefreshView();
    }

    public void OnClickConfirm()
    {
        ResolveManager();

        if (diplomacyManager == null)
        {
            SetStatus(ManagerMissingMessage);
            return;
        }

        if (!TryBuildTradeRequest(out DiplomacyTradeRequest request, out string message))
        {
            SetStatus(message);
            HandleTradeFailureMessage(message);
            return;
        }

        if (diplomacyManager.TryExecuteTrade(request, out message))
        {
            latestStatusMessage = message;
            ResetInputs();
            RefreshView();
            return;
        }

        SetStatus(message);
        HandleTradeFailureMessage(message);
    }

    public void OnClickSuggestTerms()
    {
        ResolveManager();

        if (diplomacyManager == null)
        {
            ShowSuggestTermsMessage(ManagerMissingMessage);
            SetStatus(ManagerMissingMessage);
            return;
        }

        FactionManager targetFaction = diplomacyManager.SelectedTargetFaction;
        if (targetFaction == null)
        {
            string targetMessage = "Select a faction to trade with.";
            ShowSuggestTermsMessage(targetMessage);
            SetStatus(targetMessage);
            return;
        }

        FactionManager playerFaction = diplomacyManager.PlayerFaction;
        if (playerFaction == null)
        {
            string playerMessage = "Player faction is not assigned.";
            ShowSuggestTermsMessage(playerMessage);
            SetStatus(playerMessage);
            return;
        }

        if (!TryBuildTradeRequest(out DiplomacyTradeRequest request, out string buildMessage))
        {
            ShowSuggestTermsMessage(buildMessage);
            SetStatus(buildMessage);
            return;
        }

        if (!HasTradeContent(request))
        {
            string emptyTradeMessage = "Enter at least one trade item.";
            ShowSuggestTermsMessage(emptyTradeMessage);
            SetStatus(emptyTradeMessage);
            return;
        }

        if (!diplomacyManager.TryCalculateAiTradeValues(request, out string message, out int aiReceiveValue, out int aiGiveValue))
        {
            ShowSuggestTermsMessage(message);
            SetStatus(message);
            return;
        }

        int minimumExtraCredit = Mathf.Max(0, aiGiveValue - aiReceiveValue);
        int suggestedExtraCredit = minimumExtraCredit > 0
            ? minimumExtraCredit + Mathf.Max(0, suggestTermsExtraValue)
            : 0;
        string acceptStatus = minimumExtraCredit == 0 ? "Already acceptable." : "Add credit to improve acceptance.";

        if (suggestedExtraCredit > 0)
        {
            int nextPlayerGiveCredit = playerGiveCredit + suggestedExtraCredit;
            int playerCredit = playerFaction.GetCredit;
            if (nextPlayerGiveCredit > playerCredit)
            {
                ShowSuggestTermsMessage(
                    $"Credit: {targetFaction.GetCredit:N0} Power: {targetFaction.GetNetPower:N0}\n"
                    + "Suggest Terms\n"
                    + $"AI Give Value: {aiGiveValue:N0}\n"
                    + $"AI Receive Value: {aiReceiveValue:N0}\n"
                    + $"Minimum Extra Credit: {minimumExtraCredit:N0}\n"
                    + $"Suggested Extra Credit: {suggestedExtraCredit:N0}\n"
                    + $"Not enough credit. Have: {playerCredit:N0}, Needed: {nextPlayerGiveCredit:N0}");
                return;
            }

            playerGiveCredit = nextPlayerGiveCredit;
            RefreshOfferItems();
            acceptStatus = $"Applied extra credit. Player Credit Offer: {playerGiveCredit:N0}";
        }

        ShowSuggestTermsMessage(
            $"Credit: {targetFaction.GetCredit:N0} Power: {targetFaction.GetNetPower:N0}\n"
            + "Suggest Terms\n"
            + $"AI Give Value: {aiGiveValue:N0}\n"
            + $"AI Receive Value: {aiReceiveValue:N0}\n"
            + $"Minimum Extra Credit: {minimumExtraCredit:N0}\n"
            + $"Suggested Extra Credit: {suggestedExtraCredit:N0}\n"
            + acceptStatus);
    }

    public void OnClickClose()
    {
        latestStatusMessage = string.Empty;
        CloseOnlyCityWarningPopup();
        CloseTransientTradePanels();
        ResetInputs();
        SetPanelVisible(false);
    }

    public bool CloseByEscape()
    {
        if (!IsOpen)
            return false;

        if (CloseTopmostTransientTradePanel())
            return true;

        OnClickClose();
        return true;
    }

    public void CloseOnlyCityWarningPopup()
    {
        if (warningPopup != null)
            warningPopup.SetActive(false);
    }

    public void OnClickOpenPlayerCardSelect()
    {
        ResolveManager();
        ResolveTradeCardController();

        if (tradeCardController == null)
        {
            LogMissingReference(nameof(tradeCardController));
            return;
        }

        FactionManager playerFaction = diplomacyManager != null ? diplomacyManager.PlayerFaction : null;
        CloseTransientTradePanels(keepCardSelect: true);
        tradeCardController.OpenCardSelect(playerFaction);
    }

    public void OnClickOpenPlayerCreditInputPanel()
    {
        OpenPlayerCreditInputPanel();
    }

    public void OnClickOpenAiCreditInputPanel()
    {
        OpenAiCreditInputPanel();
    }

    public void OnClickOpenPlayerPowerInputPanel()
    {
        OpenPlayerPowerInputPanel();
    }

    public void OnClickOpenAiPowerInputPanel()
    {
        OpenAiPowerInputPanel();
    }

    public void OnClickOpenCitySelectPanel()
    {
        OpenCitySelectPanel(TradeCitySelectMode.PlayerGiveCity, GetButtonRect(playerCityButton, nameof(playerCityButton)));
    }

    public void OnClickOpenPlayerShareCitySelectPanel()
    {
        OpenCitySelectPanel(TradeCitySelectMode.PlayerGiveShare, GetButtonRect(playerShareCityButton, nameof(playerShareCityButton)));
    }

    public void OnClickOpenAiShareCitySelectPanel()
    {
        OpenTargetCitySelectPanel(TradeCitySelectMode.AiGiveShare, GetButtonRect(aiShareCityButton, nameof(aiShareCityButton)));
    }

    public void OnClickOpenAiCitySelectPanel()
    {
        OpenTargetCitySelectPanel(TradeCitySelectMode.AiGiveCity, GetButtonRect(aiCityButton, nameof(aiCityButton)));
    }

    public void OnClickCloseCitySelectPanel()
    {
        SetCitySelectPanelVisible(false);
    }

    public void OnClickCloseCardSelect()
    {
        if (tradeCardController != null)
        {
            tradeCardController.CloseCardSelect();
        }
        else
        {
            LogMissingReference(nameof(tradeCardController));
        }
    }

    public void OnClickOpenAiPatentLicensePanel()
    {
        ResolveManager();

        FactionManager targetFaction = diplomacyManager != null ? diplomacyManager.SelectedTargetFaction : null;
        if (targetFaction == null)
        {
            SetStatus("Select a faction to trade with.");
            return;
        }

        RefreshAiPatentLicenseOptions(targetFaction);
        if (aiPatentLicenseResearchIds.Count == 0)
        {
            SetAiPatentLicensePanelVisible(false);
            SetStatus("Selected AI has no tradable patents.");
            return;
        }

        CloseTransientTradePanels(keepAiPatentLicense: true);
        SetAiPatentLicensePanelVisible(true);
    }

    public void SetPlayerGiveSharePercent(int _percent)
    {
        SetPlayerGiveSharePercent(_percent, true);
    }

    private void SetPlayerGiveSharePercent(int _percent, bool refreshOfferItems)
    {
        playerGiveSharePercent = GetClampedSharePercent(
            _percent,
            tradeCitySelectController != null ? tradeCitySelectController.GetSelectedPlayerGiveShareCity() : null,
            diplomacyManager != null ? diplomacyManager.PlayerFaction : null);

        if (refreshOfferItems)
            RefreshOfferItems();
    }

    public void SetAiGiveSharePercent(int _percent)
    {
        SetAiGiveSharePercent(_percent, true);
    }

    private void SetAiGiveSharePercent(int _percent, bool refreshOfferItems)
    {
        aiGiveSharePercent = GetClampedSharePercent(
            _percent,
            tradeCitySelectController != null ? tradeCitySelectController.GetSelectedAiGiveShareCity() : null,
            diplomacyManager != null ? diplomacyManager.SelectedTargetFaction : null);

        if (refreshOfferItems)
            RefreshOfferItems();
    }

    public void SetPlayerGiveSharePercentFromText(string _text)
    {
        if (string.IsNullOrWhiteSpace(_text))
        {
            playerGiveSharePercent = 0;
            RefreshOfferItems();
            return;
        }

        if (!int.TryParse(_text, out int percent))
        {
            SetStatus(InvalidNumberMessage);
            return;
        }

        SetPlayerGiveSharePercent(percent);
    }

    public void SetAiGiveSharePercentFromText(string _text)
    {
        if (string.IsNullOrWhiteSpace(_text))
        {
            aiGiveSharePercent = 0;
            RefreshOfferItems();
            return;
        }

        if (!int.TryParse(_text, out int percent))
        {
            SetStatus(InvalidNumberMessage);
            return;
        }

        SetAiGiveSharePercent(percent);
    }

    public void SetPlayerGivePatentLicense(string _researchId, int _licenseMonths)
    {
        if (!TryNormalizePatentLicense(_researchId, _licenseMonths, out string researchId, out int licenseMonths))
        {
            ClearPlayerGivePatentLicense();
            SetStatus(InvalidPatentLicenseMessage);
            return;
        }

        playerGivePatentResearchId = researchId;
        playerGivePatentLicenseMonths = licenseMonths;
        playerPatentLicenseDurationMonths = licenseMonths;
        SetPatentLicenseDurationDropdownValueWithoutNotify(playerPatentLicenseDurationDropdown, licenseMonths);
        RefreshOfferItems();
    }

    public void SetAiGivePatentLicense(string _researchId, int _licenseMonths)
    {
        if (!TryNormalizePatentLicense(_researchId, _licenseMonths, out string researchId, out int licenseMonths))
        {
            ClearAiGivePatentLicense(false);
            SetStatus(InvalidPatentLicenseMessage);
            RefreshOfferItems();
            return;
        }

        ResolveManager();
        FactionManager targetFaction = diplomacyManager != null ? diplomacyManager.SelectedTargetFaction : null;
        if (!IsPatentOwnedByFaction(researchId, targetFaction))
        {
            ClearAiGivePatentLicense(false);
            SetStatus("Selected AI does not own this patent.");
            RefreshOfferItems();
            return;
        }

        aiGivePatentResearchId = researchId;
        aiGivePatentLicenseMonths = licenseMonths;
        aiPatentLicenseDurationMonths = licenseMonths;
        SetPatentLicenseDurationDropdownValueWithoutNotify(aiPatentLicenseDurationDropdown, licenseMonths);
        RefreshOfferItems();
    }

    public void SetPlayerGivePatentLicenseResearch(string _researchId)
    {
        playerPatentLicenseDurationMonths = GetPatentLicenseDurationMonths(playerPatentLicenseDurationDropdown, playerPatentLicenseDurationMonths);
        SetPlayerGivePatentLicense(_researchId, playerPatentLicenseDurationMonths);
    }

    public void SetAiGivePatentLicenseResearch(string _researchId)
    {
        aiPatentLicenseDurationMonths = GetPatentLicenseDurationMonths(aiPatentLicenseDurationDropdown, aiPatentLicenseDurationMonths);
        SetAiGivePatentLicense(_researchId, aiPatentLicenseDurationMonths);
    }

    public void ClearPlayerGivePatentLicense()
    {
        playerGivePatentResearchId = string.Empty;
        playerGivePatentLicenseMonths = 0;
        RefreshOfferItems();
    }

    public void ClearAiGivePatentLicense()
    {
        ClearAiGivePatentLicense(true);
    }

    private void ClearAiGivePatentLicense(bool refreshOfferItems)
    {
        aiGivePatentResearchId = string.Empty;
        aiGivePatentLicenseMonths = 0;

        if (refreshOfferItems)
            RefreshOfferItems();
    }

    private void RefreshView()
    {
        UpdatePlayerCeoView();
        UpdateTargetCeoView();
        if (tradeCitySelectController != null)
        {
            FactionManager playerFaction = diplomacyManager != null ? diplomacyManager.PlayerFaction : null;
            FactionManager targetFaction = tradeCitySelectController.IsPanelOpen() && diplomacyManager != null
                ? diplomacyManager.SelectedTargetFaction
                : null;
            tradeCitySelectController.Refresh(playerFaction, targetFaction);
        }
        else
        {
            LogMissingReference(nameof(tradeCitySelectController));
        }

        RefreshStatusText();
        RefreshPlayerInfoText();
        RefreshTargetInfoText();
        RefreshOfferItems();
    }

    private void UpdatePlayerCeoView()
    {
        FactionManager playerFaction = diplomacyManager != null ? diplomacyManager.PlayerFaction : null;
        string displayName = playerFaction != null && !string.IsNullOrWhiteSpace(playerFaction.factionName)
            ? playerFaction.factionName
            : "Player";

        if (playerCeoNameText != null)
        {
            playerCeoNameText.text = displayName;
        }
        else
        {
            LogMissingReference(nameof(playerCeoNameText));
        }

        if (playerCeoIconImage == null)
        {
            LogMissingReference(nameof(playerCeoIconImage));
            return;
        }

        Sprite sprite = null;
        if (playerFaction != null)
        {
            PlayerPortraitDatabaseSO portraitDatabase = PlayerPortraitDatabaseSO.Load();
            sprite = portraitDatabase != null ? portraitDatabase.GetSprite(playerFaction.portraitId) : null;
        }

        playerCeoIconImage.sprite = sprite != null ? sprite : GetFallbackCeoIcon();
        playerCeoIconImage.enabled = playerCeoIconImage.sprite != null;
    }

    private void UpdateTargetCeoView()
    {
        FactionManager targetFaction = diplomacyManager != null ? diplomacyManager.SelectedTargetFaction : null;

        if (targetFaction == null || string.IsNullOrWhiteSpace(targetFaction.ceoId))
        {
            if (targetCeoNameText != null)
            {
                targetCeoNameText.text = NoCeoLabel;
            }
            else
            {
                LogMissingReference(nameof(targetCeoNameText));
            }

            if (targetCeoIconImage != null)
            {
                targetCeoIconImage.sprite = null;
                targetCeoIconImage.enabled = false;
            }
            else
            {
                LogMissingReference(nameof(targetCeoIconImage));
            }

            return;
        }

        if (ceoDatabase == null)
        {
            LogMissingReference(nameof(ceoDatabase));
            ceoDatabase = ScriptableObject.CreateInstance<CEODatabaseSO>();
        }

        CEOData ceo = ceoDatabase != null ? ceoDatabase.GetCEOByID(targetFaction.ceoId) : null;
        if (ceo == null)
        {
            if (targetCeoNameText != null)
            {
                targetCeoNameText.text = !string.IsNullOrWhiteSpace(targetFaction.factionName)
                    ? targetFaction.factionName
                    : UnknownCeoLabel;
            }
            else
            {
                LogMissingReference(nameof(targetCeoNameText));
            }

            if (targetCeoIconImage != null)
            {
                targetCeoIconImage.sprite = null;
                targetCeoIconImage.enabled = false;
            }
            else
            {
                LogMissingReference(nameof(targetCeoIconImage));
            }

            return;
        }

        if (targetCeoNameText != null)
        {
            targetCeoNameText.text = ceo.name;
        }
        else
        {
            LogMissingReference(nameof(targetCeoNameText));
        }

        if (targetCeoIconImage != null)
        {
            targetCeoIconImage.sprite = ceo.iconSprite;
            targetCeoIconImage.enabled = ceo.iconSprite != null;
        }
        else
        {
            LogMissingReference(nameof(targetCeoIconImage));
        }
    }

    private Sprite GetFallbackCeoIcon()
    {
        if (fallbackCeoIcon == null)
            fallbackCeoIcon = Resources.Load<Sprite>("Images/CEOIcons/RobotIcon_Image");

        return fallbackCeoIcon;
    }

    private void RefreshStatusText()
    {
        if (statusText == null)
        {
            LogMissingReference(nameof(statusText));
            return;
        }

        if (diplomacyManager == null)
        {
            statusText.text = ManagerMissingMessage;
            return;
        }

        FactionManager playerFaction = diplomacyManager.PlayerFaction;
        FactionManager targetFaction = diplomacyManager.SelectedTargetFaction;

        if (playerFaction == null)
        {
            statusText.text = "Player faction is not assigned.";
            return;
        }

        if (targetFaction == null)
        {
            statusText.text = string.IsNullOrWhiteSpace(latestStatusMessage)
                ? "Select a faction to trade with."
                : latestStatusMessage;
            return;
        }

        if (ReferenceEquals(targetFaction, playerFaction))
        {
            statusText.text = "Select an AI faction to trade with.";
            return;
        }

        statusText.text = latestStatusMessage;
    }

    private void RefreshTargetInfoText()
    {
        if (targetInfoText == null)
        {
            LogMissingReference(nameof(targetInfoText));
            return;
        }

        if (diplomacyManager == null)
        {
            targetInfoText.text = ManagerMissingMessage;
            return;
        }

        FactionManager targetFaction = diplomacyManager.SelectedTargetFaction;
        if (targetFaction == null)
        {
            targetInfoText.text = "Select a faction to trade with.";
            return;
        }

        targetInfoText.text
            =  $"Credit: {targetFaction.GetCredit:N0} " + $"Power: {targetFaction.GetNetPower:N0}";
    }

    private void RefreshPlayerInfoText()
    {
        if (playerInfoText == null)
        {
            LogMissingReference(nameof(playerInfoText));
            return;
        }

        if (diplomacyManager == null)
        {
            playerInfoText.text = ManagerMissingMessage;
            return;
        }

        FactionManager playerFaction = diplomacyManager.PlayerFaction;
        if (playerFaction == null)
        {
            playerInfoText.text = "Player faction is not assigned.";
            return;
        }

        playerInfoText.text = $"Credit: {playerFaction.GetCredit:N0} Power: {playerFaction.GetNetPower:N0}";
    }

    private void ShowSuggestTermsMessage(string _message)
    {
        if (debugText == null)
        {
            LogMissingReference(nameof(debugText));
            return;
        }

        debugText.text = _message;
    }

    private void RefreshDebugText()
    {
        if (diplomacyManager == null)
        {
            SetTradeDebugText(ManagerMissingMessage);
            SetTradeChatText("거래 조건을 확인할 수 없습니다.");
            return;
        }

        if (diplomacyManager.SelectedTargetFaction == null)
        {
            SetTradeDebugText("거래할 상대를 선택하세요.");
            SetTradeChatText("거래할 상대를 선택하세요.");
            return;
        }

        if (!TryBuildTradeRequest(out DiplomacyTradeRequest request, out string message))
        {
            SetTradeDebugText(message);
            SetTradeChatText("거래 조건을 확인할 수 없습니다.");
            return;
        }

        if (!HasTradeContent(request))
        {
            SetTradeDebugText("거래 조건을 입력하세요.");
            SetTradeChatText("거래 조건을 입력하세요.");
            return;
        }

        bool accepted = diplomacyManager.TryEvaluateAiTrade(
            request,
            out _,
            out int aiReceiveValue,
            out int aiGiveValue);

        SetTradeDebugText(
            $"내 제안 가치: {aiReceiveValue:N0}\n"
            + $"상대 제안 가치: {aiGiveValue:N0}\n"
            + $"차이값: {aiReceiveValue - aiGiveValue:N0}");
        SetTradeChatText(accepted ? "이 조건이면 거래하겠습니다." : "이 조건으로는 거래하지 않겠습니다.");
    }

    private void SetTradeDebugText(string text)
    {
        if (debugText == null)
        {
            LogMissingReference(nameof(debugText));
            return;
        }

        debugText.text = text;
    }

    private void SetTradeChatText(string text)
    {
        if (chatText == null)
            return;

        chatText.text = text;
    }

    private void SubscribeManagerEvents()
    {
        if (diplomacyManager == null)
            return;

        diplomacyManager.SelectedTargetFactionChanged -= OnSelectedTargetFactionChanged;
        diplomacyManager.SelectedTargetFactionChanged += OnSelectedTargetFactionChanged;
    }

    private void UnsubscribeManagerEvents()
    {
        if (diplomacyManager == null)
            return;

        diplomacyManager.SelectedTargetFactionChanged -= OnSelectedTargetFactionChanged;
    }

    private void SubscribeFactionEvents()
    {
        UnsubscribeFactionEvents();

        if (diplomacyManager == null)
            return;

        subscribedPlayerFaction = diplomacyManager.PlayerFaction;
        subscribedTargetFaction = diplomacyManager.SelectedTargetFaction;

        if (subscribedPlayerFaction != null)
        {
            subscribedPlayerFaction.CreditChanged += OnFactionCreditChanged;
            subscribedPlayerFaction.PowerChanged += OnFactionPowerChanged;
            subscribedPlayerFaction.CardInventoryChanged += OnFactionCardInventoryChanged;
        }

        if (subscribedTargetFaction != null)
        {
            subscribedTargetFaction.CreditChanged += OnFactionCreditChanged;
            subscribedTargetFaction.PowerChanged += OnFactionPowerChanged;
            subscribedTargetFaction.CardInventoryChanged += OnFactionCardInventoryChanged;
        }
    }

    private void UnsubscribeFactionEvents()
    {
        if (subscribedPlayerFaction != null)
        {
            subscribedPlayerFaction.CreditChanged -= OnFactionCreditChanged;
            subscribedPlayerFaction.PowerChanged -= OnFactionPowerChanged;
            subscribedPlayerFaction.CardInventoryChanged -= OnFactionCardInventoryChanged;
        }

        if (subscribedTargetFaction != null)
        {
            subscribedTargetFaction.CreditChanged -= OnFactionCreditChanged;
            subscribedTargetFaction.PowerChanged -= OnFactionPowerChanged;
            subscribedTargetFaction.CardInventoryChanged -= OnFactionCardInventoryChanged;
        }

        subscribedPlayerFaction = null;
        subscribedTargetFaction = null;
    }

    private void SubscribeShareEvents()
    {
        if (CityShareManager.instance == null)
            return;

        CityShareManager.instance.AnyCityShareChanged -= OnAnyCityShareChanged;
        CityShareManager.instance.AnyCityShareChanged += OnAnyCityShareChanged;
    }

    private void UnsubscribeShareEvents()
    {
        if (CityShareManager.instance == null)
            return;

        CityShareManager.instance.AnyCityShareChanged -= OnAnyCityShareChanged;
    }

    private void OnFactionCreditChanged(int _credit)
    {
        RefreshStatusText();
        RefreshPlayerInfoText();
        RefreshTargetInfoText();
        RefreshDebugText();
    }

    private void OnFactionPowerChanged(int _production, int _consumption, int _netPower)
    {
        RefreshStatusText();
        RefreshPlayerInfoText();
        RefreshTargetInfoText();
        RefreshDebugText();
    }

    private void OnFactionCardInventoryChanged(string _cardId, int _currentCount)
    {
        RefreshStatusText();
        RefreshDebugText();

        if (tradeCardController != null)
            tradeCardController.RefreshCardSelectList(diplomacyManager != null ? diplomacyManager.PlayerFaction : null);
    }

    private void OnSelectedTargetFactionChanged(FactionManager _targetFaction)
    {
        ClearAiGivePatentLicense(false);
        aiPatentLicenseResearchIds.Clear();
        SetAiPatentLicensePanelVisible(false);
        SubscribeFactionEvents();
        RefreshView();

        if (tradeCardController != null)
            tradeCardController.RefreshCardSelectList(diplomacyManager != null ? diplomacyManager.PlayerFaction : null);
    }

    private void OnAnyCityShareChanged(CityScript _city, FactionManager _fromFaction, FactionManager _toFaction)
    {
        RefreshView();
    }

    private void SetStatus(string _message)
    {
        latestStatusMessage = _message;
        RefreshStatusText();
    }

    private void HandleTradeFailureMessage(string message)
    {
        if (!string.Equals(message, DiplomacyManager.OnlyCityTradeBlockedMessage, System.StringComparison.Ordinal))
            return;

        ShowOnlyCityWarningPopup();
    }

    private void ShowOnlyCityWarningPopup()
    {
        if (warningPopup == null)
        {
            LogMissingReference(nameof(warningPopup));
            return;
        }

        warningPopup.SetActive(true);
    }

    private void OpenCitySelectPanel(TradeCitySelectMode _mode, RectTransform _sourceButtonRect)
    {
        ResolveManager();
        ResolveTradeCitySelectController();
        CloseTransientTradePanels(keepCitySelect: true);

        if (tradeCitySelectController != null)
        {
            FactionManager playerFaction = diplomacyManager != null ? diplomacyManager.PlayerFaction : null;
            FactionManager targetFaction = diplomacyManager != null ? diplomacyManager.SelectedTargetFaction : null;
            tradeCitySelectController.Refresh(playerFaction, targetFaction);
            tradeCitySelectController.SetMode(_mode);
        }
        else
        {
            LogMissingReference(nameof(tradeCitySelectController));
        }

        SetPlayerCitySelectPanelVisible(true, _sourceButtonRect);
    }

    private void OpenTargetCitySelectPanel(TradeCitySelectMode _mode, RectTransform _sourceButtonRect)
    {
        ResolveManager();
        ResolveTradeCitySelectController();
        CloseTransientTradePanels(keepCitySelect: true);

        if (tradeCitySelectController != null)
        {
            FactionManager playerFaction = diplomacyManager != null ? diplomacyManager.PlayerFaction : null;
            FactionManager targetFaction = diplomacyManager != null ? diplomacyManager.SelectedTargetFaction : null;
            tradeCitySelectController.Refresh(playerFaction, targetFaction);
            tradeCitySelectController.SetMode(_mode);
        }
        else
        {
            LogMissingReference(nameof(tradeCitySelectController));
        }

        SetTargetCitySelectPanelVisible(true, _sourceButtonRect);
    }

    private void OpenPlayerCreditInputPanel()
    {
        if (tradeCreditInputController == null)
        {
            LogMissingReference(nameof(tradeCreditInputController));
            return;
        }

        CloseTransientTradePanels(keepCreditInput: true);
        tradeCreditInputController.OpenForPlayer(
            playerGiveCredit,
            ApplyPlayerCreditInput,
            GetButtonRect(playerCreditButton, nameof(playerCreditButton)));
    }

    private void ApplyPlayerCreditInput(int value)
    {
        playerGiveCredit = value;
        RefreshOfferItems();
    }

    private void OpenAiCreditInputPanel()
    {
        if (tradeCreditInputController == null)
        {
            LogMissingReference(nameof(tradeCreditInputController));
            return;
        }

        CloseTransientTradePanels(keepCreditInput: true);
        tradeCreditInputController.OpenForTarget(
            aiGiveCredit,
            ApplyAiCreditInput,
            GetButtonRect(aiCreditButton, nameof(aiCreditButton)));
    }

    private void ApplyAiCreditInput(int value)
    {
        aiGiveCredit = value;
        RefreshOfferItems();
    }

    private void OpenPlayerPowerInputPanel()
    {
        if (tradeCreditInputController == null)
        {
            LogMissingReference(nameof(tradeCreditInputController));
            return;
        }

        CloseTransientTradePanels(keepCreditInput: true);
        tradeCreditInputController.OpenForPlayer(
            playerGivePower,
            ApplyPlayerPowerInput,
            GetButtonRect(playerPowerButton, nameof(playerPowerButton)));
    }

    private void ApplyPlayerPowerInput(int value)
    {
        playerGivePower = value;
        RefreshOfferItems();
    }

    private void OpenAiPowerInputPanel()
    {
        if (tradeCreditInputController == null)
        {
            LogMissingReference(nameof(tradeCreditInputController));
            return;
        }

        CloseTransientTradePanels(keepCreditInput: true);
        tradeCreditInputController.OpenForTarget(
            aiGivePower,
            ApplyAiPowerInput,
            GetButtonRect(aiPowerButton, nameof(aiPowerButton)));
    }

    private void ApplyAiPowerInput(int value)
    {
        aiGivePower = value;
        RefreshOfferItems();
    }

    private void OpenPlayerShareInputPanel()
    {
        if (tradeCreditInputController == null)
        {
            LogMissingReference(nameof(tradeCreditInputController));
            return;
        }

        CloseTransientTradePanels(keepCreditInput: true);
        tradeCreditInputController.OpenForPlayer(
            playerGiveSharePercent,
            ApplyPlayerShareInput,
            GetButtonRect(playerShareCityButton, nameof(playerShareCityButton)));
    }

    private void ApplyPlayerShareInput(int value)
    {
        SetPlayerGiveSharePercent(value);
    }

    private void OpenAiShareInputPanel()
    {
        if (tradeCreditInputController == null)
        {
            LogMissingReference(nameof(tradeCreditInputController));
            return;
        }

        CloseTransientTradePanels(keepCreditInput: true);
        tradeCreditInputController.OpenForTarget(
            aiGiveSharePercent,
            ApplyAiShareInput,
            GetButtonRect(aiShareCityButton, nameof(aiShareCityButton)));
    }

    private void ApplyAiShareInput(int value)
    {
        SetAiGiveSharePercent(value);
    }

    private void ResetInputs()
    {
        CloseOnlyCityWarningPopup();

        playerGiveCredit = 0;
        aiGiveCredit = 0;
        playerGivePower = 0;
        aiGivePower = 0;
        playerGiveSharePercent = 0;
        aiGiveSharePercent = 0;
        playerGivePatentResearchId = string.Empty;
        playerGivePatentLicenseMonths = 0;
        aiGivePatentResearchId = string.Empty;
        aiGivePatentLicenseMonths = 0;
        playerPatentLicenseDurationMonths = PatentResearchManager.OneYearLicenseMonths;
        aiPatentLicenseDurationMonths = PatentResearchManager.OneYearLicenseMonths;
        aiPatentLicenseResearchIds.Clear();

        if (aiPatentLicenseDropdown != null)
            aiPatentLicenseDropdown.ClearOptions();

        SetPatentLicenseDurationDropdownValueWithoutNotify(playerPatentLicenseDurationDropdown, playerPatentLicenseDurationMonths);
        SetPatentLicenseDurationDropdownValueWithoutNotify(aiPatentLicenseDurationDropdown, aiPatentLicenseDurationMonths);

        if (tradeCardController != null)
        {
            tradeCardController.ResetSelection();
        }
        else
        {
            LogMissingReference(nameof(tradeCardController));
        }

        if (tradeCitySelectController != null)
        {
            tradeCitySelectController.ResetSelections();
        }
        else
        {
            LogMissingReference(nameof(tradeCitySelectController));
        }

        SetCitySelectPanelVisible(false);
        SetAiPatentLicensePanelVisible(false);
        CloseCreditInputPanel();
        RefreshOfferItems();
    }

    private void RefreshOfferItems()
    {
        if (tradeOfferSummaryController == null)
        {
            LogMissingReference(nameof(tradeOfferSummaryController));
            return;
        }

        if (tradeCitySelectController == null)
            LogMissingReference(nameof(tradeCitySelectController));

        tradeOfferSummaryController.Clear();

        if (playerGiveCredit > 0)
            tradeOfferSummaryController.AddPlayerCredit(playerGiveCredit.ToString(), RemovePlayerCreditOffer, OnPlayerCreditOfferValueChanged);

        if (aiGiveCredit > 0)
            tradeOfferSummaryController.AddAiCredit(aiGiveCredit.ToString(), RemoveAiCreditOffer, OnAiCreditOfferValueChanged);

        if (playerGivePower > 0)
            tradeOfferSummaryController.AddPlayerPower(playerGivePower.ToString(), RemovePlayerPowerOffer, OnPlayerPowerOfferValueChanged);

        if (aiGivePower > 0)
            tradeOfferSummaryController.AddAiPower(aiGivePower.ToString(), RemoveAiPowerOffer, OnAiPowerOfferValueChanged);

        if (tradeCardController != null && tradeCardController.TryGetOffer(out _, out int playerCardCount, out _) && playerCardCount > 0)
            tradeOfferSummaryController.AddPlayerCard($"{tradeCardController.GetSelectedCardLabel()} x{playerCardCount:N0}", RemovePlayerCardOffer);

        CityScript playerCity = tradeCitySelectController != null ? tradeCitySelectController.GetSelectedPlayerGiveCity() : null;
        if (playerCity != null)
            tradeOfferSummaryController.AddPlayerCity(tradeCitySelectController.GetCityLabel(playerCity), RemovePlayerCityOffer);

        CityScript aiCity = tradeCitySelectController != null ? tradeCitySelectController.GetSelectedAiGiveCity() : null;
        if (aiCity != null)
            tradeOfferSummaryController.AddAiCity(tradeCitySelectController.GetCityLabel(aiCity), RemoveAiCityOffer);

        if (playerGiveSharePercent > 0)
        {
            CityScript playerShareCity = tradeCitySelectController != null ? tradeCitySelectController.GetSelectedPlayerGiveShareCity() : null;
            if (playerShareCity != null)
                tradeOfferSummaryController.AddPlayerShare(
                    tradeCitySelectController.GetCityLabel(playerShareCity),
                    playerGiveSharePercent.ToString(),
                    RemovePlayerShareOffer,
                    OnPlayerShareOfferValueChanged);
        }

        if (aiGiveSharePercent > 0)
        {
            CityScript aiShareCity = tradeCitySelectController != null ? tradeCitySelectController.GetSelectedAiGiveShareCity() : null;
            if (aiShareCity != null)
                tradeOfferSummaryController.AddAiShare(
                    tradeCitySelectController.GetCityLabel(aiShareCity),
                    aiGiveSharePercent.ToString(),
                    RemoveAiShareOffer,
                    OnAiShareOfferValueChanged);
        }

        if (playerGivePatentLicenseMonths > 0)
            tradeOfferSummaryController.AddPlayerPatentLicense(
                GetPatentLicenseDisplayText(playerGivePatentResearchId, playerGivePatentLicenseMonths),
                ClearPlayerGivePatentLicense);

        if (aiGivePatentLicenseMonths > 0)
            tradeOfferSummaryController.AddAiPatentLicense(
                GetPatentLicenseDisplayText(aiGivePatentResearchId, aiGivePatentLicenseMonths),
                ClearAiGivePatentLicense);

        RefreshDebugText();
    }

    private void OnPlayerCreditOfferValueChanged(string _value)
    {
        if (!TryParseOfferItemValue(_value, InvalidNumberMessage, out int value))
            return;

        playerGiveCredit = value;

        if (value <= 0 || _value.Trim() != value.ToString())
            RefreshOfferItems();
    }

    private void OnAiCreditOfferValueChanged(string _value)
    {
        if (!TryParseOfferItemValue(_value, InvalidNumberMessage, out int value))
            return;

        aiGiveCredit = value;

        if (value <= 0 || _value.Trim() != value.ToString())
            RefreshOfferItems();
    }

    private void OnPlayerPowerOfferValueChanged(string _value)
    {
        if (!TryParseOfferItemValue(_value, InvalidNumberMessage, out int value))
            return;

        playerGivePower = value;

        if (value <= 0 || _value.Trim() != value.ToString())
            RefreshOfferItems();
    }

    private void OnAiPowerOfferValueChanged(string _value)
    {
        if (!TryParseOfferItemValue(_value, InvalidNumberMessage, out int value))
            return;

        aiGivePower = value;

        if (value <= 0 || _value.Trim() != value.ToString())
            RefreshOfferItems();
    }

    private void OnPlayerShareOfferValueChanged(string _value)
    {
        if (!TryParseOfferItemValue(_value, NegativeSharePercentMessage, out int value))
            return;

        SetPlayerGiveSharePercent(value, value <= 0);

        if (value > 0 && (playerGiveSharePercent != value || _value.Trim() != value.ToString()))
            RefreshOfferItems();
    }

    private void OnAiShareOfferValueChanged(string _value)
    {
        if (!TryParseOfferItemValue(_value, NegativeSharePercentMessage, out int value))
            return;

        SetAiGiveSharePercent(value, value <= 0);

        if (value > 0 && (aiGiveSharePercent != value || _value.Trim() != value.ToString()))
            RefreshOfferItems();
    }

    private bool TryParseOfferItemValue(string text, string negativeValueMessage, out int value)
    {
        value = 0;

        if (string.IsNullOrWhiteSpace(text))
            return true;

        if (!int.TryParse(text.Trim(), out value))
        {
            SetStatus(InvalidNumberMessage);
            RefreshOfferItems();
            return false;
        }

        if (value < 0)
        {
            SetStatus(negativeValueMessage);
            RefreshOfferItems();
            return false;
        }

        return true;
    }

    private void RemovePlayerCreditOffer()
    {
        playerGiveCredit = 0;
        RefreshOfferItems();
    }

    private void RemoveAiCreditOffer()
    {
        aiGiveCredit = 0;
        RefreshOfferItems();
    }

    private void RemovePlayerPowerOffer()
    {
        playerGivePower = 0;
        RefreshOfferItems();
    }

    private void RemoveAiPowerOffer()
    {
        aiGivePower = 0;
        RefreshOfferItems();
    }

    private void RemovePlayerCardOffer()
    {
        if (tradeCardController != null)
        {
            tradeCardController.ResetSelection();
        }
        else
        {
            LogMissingReference(nameof(tradeCardController));
        }

        RefreshOfferItems();
    }

    private void RemovePlayerCityOffer()
    {
        if (tradeCitySelectController != null)
        {
            tradeCitySelectController.ResetPlayerCitySelection();
        }
        else
        {
            LogMissingReference(nameof(tradeCitySelectController));
        }

        RefreshOfferItems();
    }

    private void RemoveAiCityOffer()
    {
        if (tradeCitySelectController != null)
        {
            tradeCitySelectController.ResetAiCitySelection();
        }
        else
        {
            LogMissingReference(nameof(tradeCitySelectController));
        }

        RefreshOfferItems();
    }

    private void RemovePlayerShareOffer()
    {
        if (tradeCitySelectController != null)
        {
            tradeCitySelectController.ResetPlayerShareSelection();
        }
        else
        {
            LogMissingReference(nameof(tradeCitySelectController));
        }
        playerGiveSharePercent = 0;
        RefreshOfferItems();
    }

    private void RemoveAiShareOffer()
    {
        if (tradeCitySelectController != null)
        {
            tradeCitySelectController.ResetAiShareSelection();
        }
        else
        {
            LogMissingReference(nameof(tradeCitySelectController));
        }
        aiGiveSharePercent = 0;
        RefreshOfferItems();
    }

    private void BindOfferInputEvents()
    {
        if (tradeCitySelectController != null)
        {
            tradeCitySelectController.BindSelectionChanged(OnCitySelectionChanged);
        }
        else
        {
            LogMissingReference(nameof(tradeCitySelectController));
        }

        if (tradeCardController != null)
        {
            tradeCardController.BindOfferChanged(OnCardOfferChanged);
        }
        else
        {
            LogMissingReference(nameof(tradeCardController));
        }
    }

    private void UnbindOfferInputEvents()
    {
        if (tradeCitySelectController != null)
        {
            tradeCitySelectController.UnbindSelectionChanged(OnCitySelectionChanged);
        }
        else
        {
            LogMissingReference(nameof(tradeCitySelectController));
        }

        if (tradeCardController != null)
        {
            tradeCardController.UnbindOfferChanged(OnCardOfferChanged);
        }
        else
        {
            LogMissingReference(nameof(tradeCardController));
        }
    }

    private void OnCitySelectionChanged(string _value)
    {
        if (tradeCitySelectController == null)
        {
            LogMissingReference(nameof(tradeCitySelectController));
            return;
        }

        TradeCitySelectMode mode = tradeCitySelectController.GetCurrentMode();
        SetCitySelectPanelVisible(false);

        switch (mode)
        {
            case TradeCitySelectMode.PlayerGiveShare:
                if (tradeCitySelectController.GetSelectedPlayerGiveShareCity() == null)
                {
                    playerGiveSharePercent = 0;
                    RefreshOfferItems();
                    return;
                }

                playerGiveSharePercent = 0;
                RefreshOfferItems();
                OpenPlayerShareInputPanel();
                break;

            case TradeCitySelectMode.AiGiveCity:
                RefreshOfferItems();
                break;

            case TradeCitySelectMode.AiGiveShare:
                if (tradeCitySelectController.GetSelectedAiGiveShareCity() == null)
                {
                    aiGiveSharePercent = 0;
                    RefreshOfferItems();
                    return;
                }

                SetAiGiveSharePercent(tradeCitySelectController.GetSelectedAiGiveSharePercent());
                break;

            default:
                RefreshOfferItems();
                break;
        }
    }

    private void OnCardOfferChanged(string _value)
    {
        RefreshOfferItems();
    }

    private int GetClampedSharePercent(int percent, CityScript city, FactionManager faction)
    {
        if (percent < 0)
        {
            SetStatus(NegativeSharePercentMessage);
            return 0;
        }

        int clampedPercent = Mathf.Clamp(percent, 0, 100);
        if (clampedPercent == 0)
            return 0;

        int availablePercent = GetAvailableSharePercent(city, faction);
        if (availablePercent <= 0)
        {
            SetStatus("Selected faction has no shares in that city.");
            return 0;
        }

        if (clampedPercent > availablePercent)
        {
            SetStatus($"Share percent was limited to {availablePercent}%.");
            return availablePercent;
        }

        return clampedPercent;
    }

    private int GetAvailableSharePercent(CityScript city, FactionManager faction)
    {
        if (city == null || faction == null || CityShareManager.instance == null)
            return 0;

        if (!CityShareManager.instance.TryPrepareCityShares(city, out _))
            return 0;

        if (city.cityData == null || city.cityData.shareData == null || city.cityData.shareData.GetTotalShare() != 100)
            return 0;

        return city.cityData.shareData.GetShare(faction);
    }

    private bool TryNormalizePatentLicense(string researchId, int licenseMonths, out string normalizedResearchId, out int normalizedLicenseMonths)
    {
        normalizedResearchId = string.IsNullOrWhiteSpace(researchId) ? string.Empty : researchId.Trim();
        normalizedLicenseMonths = licenseMonths;

        return !string.IsNullOrWhiteSpace(normalizedResearchId) && IsValidPatentLicenseMonths(normalizedLicenseMonths);
    }

    private void RefreshAiPatentLicenseOptions(FactionManager targetFaction)
    {
        aiPatentLicenseResearchIds.Clear();

        if (aiPatentLicenseDropdown == null)
        {
            LogMissingReference(nameof(aiPatentLicenseDropdown));
            return;
        }

        aiPatentLicenseDropdown.ClearOptions();

        FactionManager playerFaction = diplomacyManager != null ? diplomacyManager.PlayerFaction : null;
        PatentResearchManager patentManager = PatentResearchManager.Instance;
        ResearchDatabaseSO database = GetResearchDatabase();
        if (targetFaction == null || playerFaction == null || patentManager == null || database == null)
            return;

        if (database.allResearches == null || database.allResearches.Count == 0)
            database.LoadCSV();

        if (database.allResearches == null)
            return;

        string targetFactionKey = targetFaction.GetSaveKey();
        List<TMP_Dropdown.OptionData> options = new List<TMP_Dropdown.OptionData>();
        for (int i = 0; i < database.allResearches.Count; i++)
        {
            ResearchData research = database.allResearches[i];
            if (research == null || !research.isPatentResearch || string.IsNullOrWhiteSpace(research.id))
                continue;

            string patentOwnerKey = patentManager.GetPatentOwnerKey(research.id);
            if (!string.Equals(patentOwnerKey, targetFactionKey, System.StringComparison.OrdinalIgnoreCase))
                continue;

            if (!patentManager.CanGrantPatentLicense(
                research.id,
                targetFaction,
                playerFaction,
                PatentResearchManager.OneYearLicenseMonths,
                out _))
            {
                continue;
            }

            aiPatentLicenseResearchIds.Add(research.id);
            options.Add(new TMP_Dropdown.OptionData(GetResearchDisplayName(research)));
        }

        aiPatentLicenseDropdown.AddOptions(options);
        if (options.Count > 0)
        {
            aiPatentLicenseDropdown.value = 0;
            aiPatentLicenseDropdown.RefreshShownValue();
        }
    }

    private void ApplySelectedAiPatentLicense(int _licenseMonths)
    {
        if (aiPatentLicenseDropdown == null)
        {
            LogMissingReference(nameof(aiPatentLicenseDropdown));
            return;
        }

        int index = aiPatentLicenseDropdown.value;
        if (index < 0 || index >= aiPatentLicenseResearchIds.Count)
        {
            SetStatus("Select a patent license.");
            return;
        }

        ResolveManager();

        FactionManager targetFaction = diplomacyManager != null ? diplomacyManager.SelectedTargetFaction : null;
        FactionManager playerFaction = diplomacyManager != null ? diplomacyManager.PlayerFaction : null;
        string researchId = aiPatentLicenseResearchIds[index];
        aiPatentLicenseDurationMonths = _licenseMonths;
        SetPatentLicenseDurationDropdownValueWithoutNotify(aiPatentLicenseDurationDropdown, aiPatentLicenseDurationMonths);
        PatentResearchManager patentManager = PatentResearchManager.Instance;
        string message = string.Empty;
        if (patentManager == null
            || !patentManager.CanGrantPatentLicense(researchId, targetFaction, playerFaction, _licenseMonths, out message))
        {
            ClearAiGivePatentLicense(false);
            SetStatus(string.IsNullOrWhiteSpace(message) ? InvalidPatentLicenseMessage : message);
            RefreshOfferItems();
            return;
        }

        SetAiGivePatentLicense(researchId, _licenseMonths);
        SetAiPatentLicensePanelVisible(false);
    }

    private bool IsPatentOwnedByFaction(string researchId, FactionManager faction)
    {
        if (string.IsNullOrWhiteSpace(researchId) || faction == null)
            return false;

        PatentResearchManager patentManager = PatentResearchManager.Instance;
        if (patentManager == null)
            return false;

        return string.Equals(
            patentManager.GetPatentOwnerKey(researchId),
            faction.GetSaveKey(),
            System.StringComparison.OrdinalIgnoreCase);
    }

    private bool IsValidPatentLicenseMonths(int licenseMonths)
    {
        return licenseMonths == PatentResearchManager.OneYearLicenseMonths
            || licenseMonths == PatentResearchManager.ThreeYearLicenseMonths
            || licenseMonths == PatentResearchManager.FiveYearLicenseMonths;
    }

    private string GetPatentLicenseDisplayText(string researchId, int licenseMonths)
    {
        return $"{ResolveResearchName(researchId)} {GetPatentLicenseDurationLabel(licenseMonths)}";
    }

    private string ResolveResearchName(string researchId)
    {
        if (string.IsNullOrWhiteSpace(researchId))
            return string.Empty;

        ResearchDatabaseSO database = GetResearchDatabase();
        ResearchData research = database != null ? database.GetResearchById(researchId) : null;
        return research != null && !string.IsNullOrWhiteSpace(research.name) ? research.name : researchId;
    }

    private ResearchDatabaseSO GetResearchDatabase()
    {
        if (researchDatabase == null)
        {
            researchDatabase = Resources.Load<ResearchDatabaseSO>("Databases/ResearchDatabase");
            if (researchDatabase == null)
                researchDatabase = ScriptableObject.CreateInstance<ResearchDatabaseSO>();
        }

        return researchDatabase;
    }

    private string GetPatentLicenseDurationLabel(int licenseMonths)
    {
        if (licenseMonths == PatentResearchManager.OneYearLicenseMonths)
            return "1\uB144";

        if (licenseMonths == PatentResearchManager.ThreeYearLicenseMonths)
            return "3\uB144";

        if (licenseMonths == PatentResearchManager.FiveYearLicenseMonths)
            return "5\uB144";

        return $"{licenseMonths}\uAC1C\uC6D4";
    }

    private string GetResearchDisplayName(ResearchData research)
    {
        if (research == null)
            return string.Empty;

        return !string.IsNullOrWhiteSpace(research.name) ? research.name : research.id;
    }

    private void InitializePatentLicenseDurationDropdowns()
    {
        InitializePatentLicenseDurationDropdown(playerPatentLicenseDurationDropdown, playerPatentLicenseDurationMonths);
        InitializePatentLicenseDurationDropdown(aiPatentLicenseDurationDropdown, aiPatentLicenseDurationMonths);
    }

    private void InitializePatentLicenseDurationDropdown(TMP_Dropdown dropdown, int licenseMonths)
    {
        if (dropdown == null)
            return;

        dropdown.ClearOptions();
        dropdown.AddOptions(new List<string> { "1\uB144", "3\uB144", "5\uB144" });
        dropdown.SetValueWithoutNotify(GetPatentLicenseDurationDropdownIndex(licenseMonths));
        dropdown.RefreshShownValue();
    }

    private void BindPatentLicenseDurationDropdowns()
    {
        BindDropdown(playerPatentLicenseDurationDropdown, OnPlayerPatentLicenseDurationChanged, nameof(playerPatentLicenseDurationDropdown));
        BindDropdown(aiPatentLicenseDurationDropdown, OnAiPatentLicenseDurationChanged, nameof(aiPatentLicenseDurationDropdown));
    }

    private void UnbindPatentLicenseDurationDropdowns()
    {
        UnbindDropdown(playerPatentLicenseDurationDropdown, OnPlayerPatentLicenseDurationChanged);
        UnbindDropdown(aiPatentLicenseDurationDropdown, OnAiPatentLicenseDurationChanged);
    }

    private void BindDropdown(TMP_Dropdown dropdown, UnityAction<int> action, string fieldName)
    {
        if (dropdown == null)
        {
            LogMissingReference(fieldName);
            return;
        }

        dropdown.onValueChanged.RemoveListener(action);
        dropdown.onValueChanged.AddListener(action);
    }

    private void UnbindDropdown(TMP_Dropdown dropdown, UnityAction<int> action)
    {
        if (dropdown == null)
            return;

        dropdown.onValueChanged.RemoveListener(action);
    }

    private void OnPlayerPatentLicenseDurationChanged(int _index)
    {
        playerPatentLicenseDurationMonths = GetPatentLicenseDurationMonths(_index);
        SetPatentLicenseDurationDropdownValueWithoutNotify(playerPatentLicenseDurationDropdown, playerPatentLicenseDurationMonths);

        if (!string.IsNullOrWhiteSpace(playerGivePatentResearchId))
            SetPlayerGivePatentLicense(playerGivePatentResearchId, playerPatentLicenseDurationMonths);
    }

    private void OnAiPatentLicenseDurationChanged(int _index)
    {
        aiPatentLicenseDurationMonths = GetPatentLicenseDurationMonths(_index);
        SetPatentLicenseDurationDropdownValueWithoutNotify(aiPatentLicenseDurationDropdown, aiPatentLicenseDurationMonths);

        if (!string.IsNullOrWhiteSpace(aiGivePatentResearchId))
            SetAiGivePatentLicense(aiGivePatentResearchId, aiPatentLicenseDurationMonths);
    }

    private int GetPatentLicenseDurationMonths(TMP_Dropdown dropdown, int fallbackMonths)
    {
        if (dropdown == null)
            return IsValidPatentLicenseMonths(fallbackMonths) ? fallbackMonths : PatentResearchManager.OneYearLicenseMonths;

        return GetPatentLicenseDurationMonths(dropdown.value);
    }

    private int GetPatentLicenseDurationMonths(int dropdownIndex)
    {
        switch (dropdownIndex)
        {
            case 1:
                return PatentResearchManager.ThreeYearLicenseMonths;
            case 2:
                return PatentResearchManager.FiveYearLicenseMonths;
            case 0:
            default:
                return PatentResearchManager.OneYearLicenseMonths;
        }
    }

    private int GetPatentLicenseDurationDropdownIndex(int licenseMonths)
    {
        if (licenseMonths == PatentResearchManager.ThreeYearLicenseMonths)
            return 1;

        if (licenseMonths == PatentResearchManager.FiveYearLicenseMonths)
            return 2;

        return 0;
    }

    private void SetPatentLicenseDurationDropdownValueWithoutNotify(TMP_Dropdown dropdown, int licenseMonths)
    {
        if (dropdown == null)
            return;

        dropdown.SetValueWithoutNotify(GetPatentLicenseDurationDropdownIndex(licenseMonths));
        dropdown.RefreshShownValue();
    }

    private bool HasTradeContent(DiplomacyTradeRequest request)
    {
        return request.playerOffer.credit > 0
            || request.targetOffer.credit > 0
            || request.playerOffer.power > 0
            || request.targetOffer.power > 0
            || request.playerOffer.cardCount > 0
            || request.targetOffer.cardCount > 0
            || request.playerOffer.city != null
            || request.targetOffer.city != null
            || request.playerOffer.sharePercent > 0
            || request.targetOffer.sharePercent > 0
            || request.playerOffer.patentLicenseMonths > 0
            || request.targetOffer.patentLicenseMonths > 0;
    }

    private bool TryBuildTradeRequest(out DiplomacyTradeRequest request, out string message)
    {
        request = default;
        int playerToAiCredit = playerGiveCredit;
        int aiToPlayerCredit = aiGiveCredit;
        int playerToAiPower = playerGivePower;
        int aiToPlayerPower = aiGivePower;
        int playerToAiSharePercent = playerGiveSharePercent;
        int aiToPlayerSharePercent = aiGiveSharePercent;

        if (playerToAiCredit < 0)
        {
            message = InvalidNumberMessage;
            return false;
        }

        if (aiToPlayerCredit < 0)
        {
            message = InvalidNumberMessage;
            return false;
        }

        if (playerToAiPower < 0)
        {
            message = InvalidNumberMessage;
            return false;
        }

        if (aiToPlayerPower < 0)
        {
            message = InvalidNumberMessage;
            return false;
        }

        string playerToAiCardId = string.Empty;
        int playerToAiCardCount = 0;
        if (tradeCardController != null)
        {
            if (!tradeCardController.TryGetOffer(out playerToAiCardId, out playerToAiCardCount, out message))
                return false;
        }
        else
        {
            LogMissingReference(nameof(tradeCardController));
        }

        if (playerToAiSharePercent < 0 || aiToPlayerSharePercent < 0)
        {
            message = NegativeSharePercentMessage;
            return false;
        }

        TradeSideOffer playerOffer = new TradeSideOffer(
            playerToAiCredit,
            playerToAiPower,
            playerToAiCardCount > 0 ? playerToAiCardId : string.Empty,
            playerToAiCardCount,
            tradeCitySelectController != null ? tradeCitySelectController.GetSelectedPlayerGiveCity() : null,
            playerToAiSharePercent > 0 && tradeCitySelectController != null ? tradeCitySelectController.GetSelectedPlayerGiveShareCity() : null,
            playerToAiSharePercent,
            playerGivePatentResearchId,
            playerGivePatentLicenseMonths);
        TradeSideOffer targetOffer = new TradeSideOffer(
            aiToPlayerCredit,
            aiToPlayerPower,
            string.Empty,
            0,
            tradeCitySelectController != null ? tradeCitySelectController.GetSelectedAiGiveCity() : null,
            aiToPlayerSharePercent > 0 && tradeCitySelectController != null ? tradeCitySelectController.GetSelectedAiGiveShareCity() : null,
            aiToPlayerSharePercent,
            aiGivePatentResearchId,
            aiGivePatentLicenseMonths);
        request = new DiplomacyTradeRequest(playerOffer, targetOffer);

        message = string.Empty;
        return true;
    }

    private void ResolveManager()
    {
        if (diplomacyManager != null)
            return;

        diplomacyManager = FindAnyObjectByType<DiplomacyManager>(FindObjectsInactive.Include);
        if (diplomacyManager == null)
            LogMissingReference(nameof(diplomacyManager));
    }

    private void ResolveTradeCitySelectController()
    {
        if (tradeCitySelectController != null)
            return;

        tradeCitySelectController = GetComponentInChildren<TradeCitySelectController>(true);
        if (tradeCitySelectController == null)
            LogMissingReference(nameof(tradeCitySelectController));
    }

    private void ResolveTradeCardController()
    {
        if (tradeCardController != null)
            return;

        tradeCardController = GetComponentInChildren<TradeCardController>(true);
        if (tradeCardController == null)
            LogMissingReference(nameof(tradeCardController));
    }

    private void BindMainButtonEvents()
    {
        BindButton(confirmButton, OnClickConfirm, nameof(confirmButton));
        BindButton(suggestTermsButton, OnClickSuggestTerms, nameof(suggestTermsButton));
        BindButton(closeButton, OnClickClose, nameof(closeButton));
        BindButton(playerCardButton, OnClickOpenPlayerCardSelect, nameof(playerCardButton));
        BindButton(playerCreditButton, OnClickOpenPlayerCreditInputPanel, nameof(playerCreditButton));
        BindButton(aiCreditButton, OnClickOpenAiCreditInputPanel, nameof(aiCreditButton));
        BindButton(playerPowerButton, OnClickOpenPlayerPowerInputPanel, nameof(playerPowerButton));
        BindButton(aiPowerButton, OnClickOpenAiPowerInputPanel, nameof(aiPowerButton));
        BindButton(playerCityButton, OnClickOpenCitySelectPanel, nameof(playerCityButton));
        BindButton(playerShareCityButton, OnClickOpenPlayerShareCitySelectPanel, nameof(playerShareCityButton));
        BindButton(aiCityButton, OnClickOpenAiCitySelectPanel, nameof(aiCityButton));
        BindButton(aiShareCityButton, OnClickOpenAiShareCitySelectPanel, nameof(aiShareCityButton));
        BindButton(aiPatentLicenseButton, OnClickOpenAiPatentLicensePanel, nameof(aiPatentLicenseButton));
        BindButton(aiPatentLicenseApplyButton, OnClickApplySelectedAiPatentLicense, nameof(aiPatentLicenseApplyButton));
        BindButton(closeAiPatentLicensePanelButton, OnClickCloseAiPatentLicensePanel, nameof(closeAiPatentLicensePanelButton));
        BindButton(closeCitySelectButton, OnClickCloseCitySelectPanel, nameof(closeCitySelectButton));
    }

    private void UnbindMainButtonEvents()
    {
        UnbindButton(confirmButton, OnClickConfirm);
        UnbindButton(suggestTermsButton, OnClickSuggestTerms);
        UnbindButton(closeButton, OnClickClose);
        UnbindButton(playerCardButton, OnClickOpenPlayerCardSelect);
        UnbindButton(playerCreditButton, OnClickOpenPlayerCreditInputPanel);
        UnbindButton(aiCreditButton, OnClickOpenAiCreditInputPanel);
        UnbindButton(playerPowerButton, OnClickOpenPlayerPowerInputPanel);
        UnbindButton(aiPowerButton, OnClickOpenAiPowerInputPanel);
        UnbindButton(playerCityButton, OnClickOpenCitySelectPanel);
        UnbindButton(playerShareCityButton, OnClickOpenPlayerShareCitySelectPanel);
        UnbindButton(aiCityButton, OnClickOpenAiCitySelectPanel);
        UnbindButton(aiShareCityButton, OnClickOpenAiShareCitySelectPanel);
        UnbindButton(aiPatentLicenseButton, OnClickOpenAiPatentLicensePanel);
        UnbindButton(aiPatentLicenseApplyButton, OnClickApplySelectedAiPatentLicense);
        UnbindButton(closeAiPatentLicensePanelButton, OnClickCloseAiPatentLicensePanel);
        UnbindButton(closeCitySelectButton, OnClickCloseCitySelectPanel);
    }

    private void BindButton(Button button, UnityAction action, string fieldName)
    {
        if (button == null)
        {
            LogMissingReference(fieldName);
            return;
        }

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private void UnbindButton(Button button, UnityAction action)
    {
        if (button == null)
            return;

        button.onClick.RemoveListener(action);
    }

    private RectTransform GetButtonRect(Button _button, string _fieldName)
    {
        if (_button == null)
        {
            LogMissingReference(_fieldName);
            return null;
        }

        return _button.GetComponent<RectTransform>();
    }

    private void SetPanelVisible(bool _isVisible)
    {
        if (panelRoot == null)
            LogMissingReference(nameof(panelRoot));

        GameObject root = panelRoot != null ? panelRoot : gameObject;
        if (root != null)
            root.SetActive(_isVisible);
    }

    private bool IsPanelVisible()
    {
        GameObject root = panelRoot != null ? panelRoot : gameObject;
        return root != null && root.activeInHierarchy;
    }

    private bool IsTransientTradePanelOpen()
    {
        return (warningPopup != null && warningPopup.activeInHierarchy)
            || (tradeCardController != null && tradeCardController.IsPanelOpen())
            || (tradeCreditInputController != null && tradeCreditInputController.IsOpen)
            || (tradeCitySelectController != null && tradeCitySelectController.IsPanelOpen())
            || (aiPatentLicensePanelRoot != null && aiPatentLicensePanelRoot.activeInHierarchy);
    }

    private bool CloseTopmostTransientTradePanel()
    {
        if (warningPopup != null && warningPopup.activeInHierarchy)
        {
            CloseOnlyCityWarningPopup();
            return true;
        }

        if (tradeCardController != null && tradeCardController.IsPanelOpen())
        {
            tradeCardController.CloseCardSelect();
            return true;
        }

        if (aiPatentLicensePanelRoot != null && aiPatentLicensePanelRoot.activeInHierarchy)
        {
            SetAiPatentLicensePanelVisible(false);
            return true;
        }

        if (tradeCreditInputController != null && tradeCreditInputController.IsOpen)
        {
            tradeCreditInputController.Close();
            return true;
        }

        if (tradeCitySelectController != null && tradeCitySelectController.IsPanelOpen())
        {
            SetCitySelectPanelVisible(false);
            return true;
        }

        return false;
    }

    private void CloseTransientTradePanels(bool keepCreditInput = false, bool keepCitySelect = false, bool keepCardSelect = false, bool keepAiPatentLicense = false)
    {
        if (!keepCreditInput)
            CloseCreditInputPanel();

        if (!keepCitySelect)
            SetCitySelectPanelVisible(false);

        if (!keepAiPatentLicense)
            SetAiPatentLicensePanelVisible(false);

        if (keepCardSelect)
            return;

        if (tradeCardController != null)
        {
            tradeCardController.CloseCardSelect();
        }
        else
        {
            LogMissingReference(nameof(tradeCardController));
        }
    }

    private void SetAiPatentLicensePanelVisible(bool _isVisible)
    {
        if (aiPatentLicensePanelRoot != null)
        {
            aiPatentLicensePanelRoot.SetActive(_isVisible);
            return;
        }

        if (_isVisible)
            LogMissingReference(nameof(aiPatentLicensePanelRoot));
    }

    private void OnClickApplySelectedAiPatentLicense()
    {
        aiPatentLicenseDurationMonths = GetPatentLicenseDurationMonths(aiPatentLicenseDurationDropdown, aiPatentLicenseDurationMonths);
        SetPatentLicenseDurationDropdownValueWithoutNotify(aiPatentLicenseDurationDropdown, aiPatentLicenseDurationMonths);
        ApplySelectedAiPatentLicense(aiPatentLicenseDurationMonths);
    }

    private void OnClickCloseAiPatentLicensePanel()
    {
        SetAiPatentLicensePanelVisible(false);
    }

    private void SetCitySelectPanelVisible(bool _isVisible)
    {
        if (tradeCitySelectController != null)
        {
            tradeCitySelectController.SetPanelVisible(_isVisible);
        }
        else
        {
            LogMissingReference(nameof(tradeCitySelectController));
        }
    }

    private void SetPlayerCitySelectPanelVisible(bool _isVisible, RectTransform _sourceButtonRect)
    {
        if (tradeCitySelectController != null)
        {
            tradeCitySelectController.SetPlayerPanelVisible(_isVisible, _sourceButtonRect);
        }
        else
        {
            LogMissingReference(nameof(tradeCitySelectController));
        }
    }

    private void SetTargetCitySelectPanelVisible(bool _isVisible, RectTransform _sourceButtonRect)
    {
        if (tradeCitySelectController != null)
        {
            tradeCitySelectController.SetTargetPanelVisible(_isVisible, _sourceButtonRect);
        }
        else
        {
            LogMissingReference(nameof(tradeCitySelectController));
        }
    }

    private void CloseCreditInputPanel()
    {
        if (tradeCreditInputController == null)
        {
            LogMissingReference(nameof(tradeCreditInputController));
            return;
        }

        tradeCreditInputController.Close();
    }

    private void LogMissingReference(string fieldName)
    {
        Debug.LogWarning($"{nameof(DiplomacyTradePanelController)}: {fieldName} is not assigned.", this);
    }
}
