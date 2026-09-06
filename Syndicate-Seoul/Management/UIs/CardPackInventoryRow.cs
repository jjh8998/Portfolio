using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CardPackInventoryRow : MonoBehaviour
{
    [SerializeField] private TMP_Text packNameText;
    [SerializeField] private TMP_Text countText;
    [SerializeField] private Image packImage;
    [SerializeField] private Button openButton;

    private string packId;
    private Action<string> onOpenClicked;

    private void Awake()
    {
        if (openButton != null)
            openButton.onClick.AddListener(OnOpenButtonClicked);
    }

    private void OnDestroy()
    {
        if (openButton != null)
            openButton.onClick.RemoveListener(OnOpenButtonClicked);
    }

    public void Setup(CardPackData _packData, int _count, Action<string> _onOpenClicked)
    {
        packId = _packData != null ? _packData.id : string.Empty;
        onOpenClicked = _onOpenClicked;

        string packName = _packData != null && !string.IsNullOrWhiteSpace(_packData.name)
            ? _packData.name
            : packId;

        if (packNameText != null)
            packNameText.text = packName;

        if (countText != null)
            countText.text = _count > 0 ? $"x{_count}" : string.Empty;

        SetPackImage(_packData);

        if (openButton != null)
            openButton.interactable = _count > 0 && !string.IsNullOrWhiteSpace(packId);
    }

    private void SetPackImage(CardPackData packData)
    {
        Image targetImage = packImage;
        if (targetImage == null && openButton != null)
            targetImage = openButton.image;

        if (targetImage == null)
            return;

        Sprite sprite = packData != null ? packData.GetCardPackSprite() : null;
        if (sprite == null)
            return;

        targetImage.sprite = sprite;
        targetImage.preserveAspect = true;
    }

    private void OnOpenButtonClicked()
    {
        if (string.IsNullOrWhiteSpace(packId))
            return;

        onOpenClicked?.Invoke(packId);
    }
}
