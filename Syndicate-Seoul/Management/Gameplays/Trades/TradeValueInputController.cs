using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TradeValueInputController : MonoBehaviour, IEscapeClosable
{
    private const string InvalidNumberMessage = "Enter valid whole numbers.";

    [SerializeField] private GameObject panelRoot;
    [SerializeField] private float playerOpenOffsetX;
    [SerializeField] private float targetOpenOffsetX;
    [SerializeField] private TMP_InputField creditInputField;
    [SerializeField] private Button confirmButton;
    [SerializeField] private TMP_Text messageText;

    private Action<int> confirmCallback;

    public bool IsOpen => panelRoot != null && panelRoot.activeInHierarchy;

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

    public void Open(int _currentValue, Action<int> _onConfirm)
    {
        confirmCallback = _onConfirm;

        if (creditInputField != null)
        {
            creditInputField.SetTextWithoutNotify(_currentValue > 0 ? _currentValue.ToString() : string.Empty);
        }
        else
        {
            LogMissingReference(nameof(creditInputField));
        }

        SetMessage(string.Empty);
        SetPanelVisible(true);

        if (creditInputField != null)
        {
            creditInputField.Select();
            creditInputField.ActivateInputField();
        }
    }

    public void Open(int _currentValue, Action<int> _onConfirm, RectTransform _sourceButtonRect)
    {
        OpenForPlayer(_currentValue, _onConfirm, _sourceButtonRect);
    }

    public void OpenForPlayer(int _currentValue, Action<int> _onConfirm, RectTransform _sourceButtonRect)
    {
        ApplyOpenPosition(_sourceButtonRect, playerOpenOffsetX);
        Open(_currentValue, _onConfirm);
    }

    public void OpenForTarget(int _currentValue, Action<int> _onConfirm, RectTransform _sourceButtonRect)
    {
        ApplyOpenPosition(_sourceButtonRect, targetOpenOffsetX);
        Open(_currentValue, _onConfirm);
    }

    public void Close()
    {
        confirmCallback = null;
        SetMessage(string.Empty);
        SetPanelVisible(false);
    }

    public bool CloseByEscape()
    {
        if (!IsOpen)
            return false;

        Close();
        return true;
    }

    public void OnClickConfirm()
    {
        if (creditInputField == null)
            LogMissingReference(nameof(creditInputField));

        string inputText = creditInputField != null ? creditInputField.text.Trim() : string.Empty;
        if (string.IsNullOrWhiteSpace(inputText))
        {
            confirmCallback?.Invoke(0);
            Close();
            return;
        }

        if (!int.TryParse(inputText, out int creditValue))
        {
            SetMessage(InvalidNumberMessage);
            return;
        }

        if (creditValue < 0)
        {
            SetMessage(InvalidNumberMessage);
            return;
        }

        confirmCallback?.Invoke(creditValue);
        Close();
    }

    public void OnClickCancel()
    {
        Close();
    }

    private void OnCreditInputSubmit(string _submittedText)
    {
        OnClickConfirm();
    }

    private void BindInputEvents()
    {
        if (creditInputField == null)
        {
            LogMissingReference(nameof(creditInputField));
            return;
        }

        creditInputField.onSubmit.RemoveListener(OnCreditInputSubmit);
        creditInputField.onSubmit.AddListener(OnCreditInputSubmit);
    }

    private void UnbindInputEvents()
    {
        if (creditInputField == null)
        {
            LogMissingReference(nameof(creditInputField));
            return;
        }

        creditInputField.onSubmit.RemoveListener(OnCreditInputSubmit);
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

    private void SetMessage(string message)
    {
        if (messageText != null)
        {
            messageText.text = message;
        }
        else
        {
            LogMissingReference(nameof(messageText));
        }
    }

    private void SetPanelVisible(bool _isVisible)
    {
        if (panelRoot == null)
            LogMissingReference(nameof(panelRoot));

        GameObject root = panelRoot != null ? panelRoot : gameObject;
        if (root != null)
            root.SetActive(_isVisible);
    }

    private void ApplyOpenPosition(RectTransform sourceButtonRect)
    {
        ApplyOpenPosition(sourceButtonRect, playerOpenOffsetX);
    }

    private void ApplyOpenPosition(RectTransform sourceButtonRect, float _openOffsetX)
    {
        if (panelRoot == null || sourceButtonRect == null)
            return;

        RectTransform panelRect = panelRoot.GetComponent<RectTransform>();
        if (panelRect == null)
            return;

        RectTransform parentRect = panelRect.parent as RectTransform;
        if (parentRect == null)
            return;

        Vector3 localPosition = parentRect.InverseTransformPoint(sourceButtonRect.position);
        Vector2 anchoredPosition = panelRect.anchoredPosition;
        anchoredPosition.x = localPosition.x + _openOffsetX;
        anchoredPosition.y = localPosition.y;
        panelRect.anchoredPosition = anchoredPosition;
    }

    private void LogMissingReference(string fieldName)
    {
        Debug.LogWarning($"{nameof(TradeValueInputController)}: {fieldName} is not assigned.", this);
    }
}
