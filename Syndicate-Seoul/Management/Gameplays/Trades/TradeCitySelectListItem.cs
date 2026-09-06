using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class TradeCitySelectListItem : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text labelText;

    public void Setup(string _label, UnityAction _onClick)
    {
        if (labelText != null)
            labelText.text = _label ?? string.Empty;

        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        if (_onClick != null)
            button.onClick.AddListener(_onClick);
    }
}
