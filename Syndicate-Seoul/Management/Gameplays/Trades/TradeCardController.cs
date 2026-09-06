using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class TradeCardController : MonoBehaviour
{
    private const string NegativeCardCountMessage = "Card counts cannot be negative.";
    private const string MissingPlayerCardIdMessage = "Enter a player card ID when offering cards.";
    private const string NoSelectionLabel = "None";

    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_InputField playerGiveCardCountInput;
    [SerializeField] private TMP_Text playerSelectedCardText;
    [SerializeField] private ShowCardListPanel showCardListPanel;
    [SerializeField] private Button confirmButton;

    private string selectedCardId = string.Empty;
    private string selectedCardName = NoSelectionLabel;
    private UnityAction<string> offerChangedCallback;

    private void OnEnable()
    {
        BindInputEvents();
        BindButtonEvents();
    }

    private void OnDisable()
    {
        UnbindInputEvents();
        UnbindButtonEvents();
    }

    public void OpenCardSelect(FactionManager _faction)
    {
        if (showCardListPanel == null)
        {
            LogMissingReference(nameof(showCardListPanel));
            return;
        }

        List<string> cardIds = new List<string>();

        if (_faction != null)
        {
            CardInventory inventory = _faction.GetCardInventory();
            if (inventory != null)
            {
                var cardStacks = inventory.GetAll();
                if (cardStacks != null)
                {
                    for (int i = 0; i < cardStacks.Count; i++)
                    {
                        CardStack stack = cardStacks[i];
                        if (stack == null || stack.count <= 0 || string.IsNullOrWhiteSpace(stack.cardId))
                            continue;

                        CardData card = CardDatabase.Instance != null ? CardDatabase.Instance.GetById(stack.cardId) : null;
                        if (card == null)
                        {
                            Debug.LogWarning($"{nameof(TradeCardController)}: Card data not found. cardId={stack.cardId}", this);
                            continue;
                        }

                        cardIds.Add(stack.cardId);
                    }
                }
            }
        }

        OpenPanel();
        showCardListPanel.Open(cardIds, true, OnCardSelected);
    }

    public void OpenPanel()
    {
        SetPanelVisible(true);
    }

    public void Close()
    {
        CloseCardSelect();
        SetPanelVisible(false);
    }

    public void CloseCardSelect()
    {
        if (showCardListPanel == null)
        {
            LogMissingReference(nameof(showCardListPanel));
            return;
        }

        showCardListPanel.Close();
    }

    public void ResetSelection()
    {
        selectedCardId = string.Empty;
        selectedCardName = NoSelectionLabel;
        SetInputTextWithoutNotify(playerGiveCardCountInput, nameof(playerGiveCardCountInput), string.Empty);

        if (playerSelectedCardText != null)
        {
            playerSelectedCardText.text = NoSelectionLabel;
        }
        else
        {
            LogMissingReference(nameof(playerSelectedCardText));
        }

        CloseCardSelect();
        NotifyOfferChanged(string.Empty);
    }

    public void RefreshCardSelectList(FactionManager _faction)
    {
        if (showCardListPanel == null)
        {
            LogMissingReference(nameof(showCardListPanel));
            return;
        }

        if (!showCardListPanel.IsOpen)
            return;

        OpenCardSelect(_faction);
    }

    public bool TryGetOffer(out string _cardId, out int _count, out string _message)
    {
        _cardId = selectedCardId;
        _count = 0;

        if (playerGiveCardCountInput == null)
        {
            LogMissingReference(nameof(playerGiveCardCountInput));
            _message = string.Empty;
            return true;
        }

        if (!string.IsNullOrWhiteSpace(playerGiveCardCountInput.text)
            && !int.TryParse(playerGiveCardCountInput.text, out _count))
        {
            _message = "Enter valid whole numbers.";
            return false;
        }

        if (_count < 0)
        {
            _message = NegativeCardCountMessage;
            return false;
        }

        if (_count > 0 && string.IsNullOrWhiteSpace(_cardId))
        {
            _message = MissingPlayerCardIdMessage;
            return false;
        }

        if (_count <= 0)
            _cardId = string.Empty;

        _message = string.Empty;
        return true;
    }

    public bool HasOffer()
    {
        return TryGetOffer(out _, out int count, out _) && count > 0;
    }

    public void BindOfferChanged(UnityAction<string> _callback)
    {
        if (_callback == null)
            return;

        offerChangedCallback -= _callback;
        offerChangedCallback += _callback;
    }

    public void UnbindOfferChanged(UnityAction<string> _callback)
    {
        if (_callback == null)
            return;

        offerChangedCallback -= _callback;
    }

    public string GetSelectedCardLabel()
    {
        if (!string.IsNullOrWhiteSpace(selectedCardName) && selectedCardName != NoSelectionLabel)
            return selectedCardName;

        return !string.IsNullOrWhiteSpace(selectedCardId) ? selectedCardId : NoSelectionLabel;
    }

    public bool IsPanelOpen()
    {
        return showCardListPanel != null && showCardListPanel.IsOpen;
    }

    public void OnClickConfirm()
    {
        if (!TryGetOffer(out string cardId, out _, out string message))
        {
            Debug.LogWarning($"{nameof(TradeCardController)}: {message}", this);
            return;
        }

        NotifyOfferChanged(cardId);
        Close();
    }

    private void OnCardSelected(string _cardId)
    {
        if (string.IsNullOrWhiteSpace(_cardId))
            return;

        selectedCardId = _cardId;
        CardData card = CardDatabase.Instance != null ? CardDatabase.Instance.GetById(_cardId) : null;
        selectedCardName = card != null && !string.IsNullOrWhiteSpace(card.cardName)
            ? card.cardName
            : _cardId;

        if (playerSelectedCardText != null)
        {
            playerSelectedCardText.text = selectedCardName;
        }
        else
        {
            LogMissingReference(nameof(playerSelectedCardText));
        }

        CloseCardSelect();
        NotifyOfferChanged(_cardId);
    }

    private void BindInputEvents()
    {
        if (playerGiveCardCountInput != null)
        {
            playerGiveCardCountInput.onValueChanged.RemoveListener(OnCardInputValueChanged);
            playerGiveCardCountInput.onValueChanged.AddListener(OnCardInputValueChanged);
        }
        else
        {
            LogMissingReference(nameof(playerGiveCardCountInput));
        }
    }

    private void UnbindInputEvents()
    {
        if (playerGiveCardCountInput != null)
            playerGiveCardCountInput.onValueChanged.RemoveListener(OnCardInputValueChanged);
    }

    private void BindButtonEvents()
    {
        if (confirmButton == null)
            return;

        confirmButton.onClick.RemoveListener(OnClickConfirm);
        confirmButton.onClick.AddListener(OnClickConfirm);
    }

    private void UnbindButtonEvents()
    {
        if (confirmButton == null)
            return;

        confirmButton.onClick.RemoveListener(OnClickConfirm);
    }

    private void OnCardInputValueChanged(string _value)
    {
        NotifyOfferChanged(_value);
    }

    private void NotifyOfferChanged(string _value)
    {
        offerChangedCallback?.Invoke(_value);
    }

    private void SetPanelVisible(bool _isVisible)
    {
        if (panelRoot == null)
            LogMissingReference(nameof(panelRoot));

        GameObject root = panelRoot != null ? panelRoot : gameObject;
        if (root != null)
            root.SetActive(_isVisible);
    }

    private void SetInputTextWithoutNotify(TMP_InputField inputField, string fieldName, string value)
    {
        if (inputField == null)
        {
            LogMissingReference(fieldName);
            return;
        }

        inputField.SetTextWithoutNotify(value);
    }

    private void LogMissingReference(string fieldName)
    {
        Debug.LogWarning($"{nameof(TradeCardController)}: {fieldName} is not assigned.", this);
    }
}
