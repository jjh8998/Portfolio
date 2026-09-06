using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

public class TradeOfferItemView : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_InputField valueInputField;
    [SerializeField] private Button removeButton;

    public void Setup(Sprite _icon, string _name, string _value, UnityAction _onRemove = null, UnityAction<string> _onValueChanged = null)
    {
        if (iconImage != null)
        {
            iconImage.sprite = _icon;
            iconImage.enabled = _icon != null;
        }
        else
        {
            LogMissingReference(nameof(iconImage));
        }

        if (nameText != null)
        {
            nameText.text = _name ?? string.Empty;
        }
        else
        {
            LogMissingReference(nameof(nameText));
        }

        if (valueInputField != null)
        {
            valueInputField.onValueChanged.RemoveAllListeners();
            valueInputField.SetTextWithoutNotify(_value ?? string.Empty);
            valueInputField.interactable = _onValueChanged != null;

            if (_onValueChanged != null)
                valueInputField.onValueChanged.AddListener(_onValueChanged);
        }
        else
        {
            LogMissingReference(nameof(valueInputField));
        }

        if (removeButton == null)
        {
            LogMissingReference(nameof(removeButton));
            return;
        }

        removeButton.onClick.RemoveAllListeners();

        if (_onRemove == null)
        {
            removeButton.gameObject.SetActive(false);
            return;
        }

        removeButton.gameObject.SetActive(true);
        removeButton.onClick.AddListener(_onRemove);
    }

    private void LogMissingReference(string fieldName)
    {
        Debug.LogWarning($"{nameof(TradeOfferItemView)}: {fieldName} is not assigned.", this);
    }
}
