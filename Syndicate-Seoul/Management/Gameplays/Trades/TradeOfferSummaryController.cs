using UnityEngine;
using UnityEngine.Events;

public partial class TradeOfferSummaryController : MonoBehaviour
{
    private const string CreditLabel = "\uD06C\uB808\uB527";
    private const string PowerLabel = "\uC804\uB825";
    private const string CardLabel = "\uCE74\uB4DC";
    private const string CityLabel = "\uB3C4\uC2DC";
    private const string ShareLabel = "\uC9C0\uBD84";
    private const string PatentLicenseLabel = "\uD2B9\uD5C8 \uB77C\uC774\uC120\uC2A4";

    [SerializeField] private Transform playerOfferContent;
    [SerializeField] private Transform aiOfferContent;
    [SerializeField] private TradeOfferItemView offerItemPrefab;
    [SerializeField] private Sprite creditIcon;
    [SerializeField] private Sprite powerIcon;
    [SerializeField] private Sprite cardIcon;
    [SerializeField] private Sprite cityIcon;
    [SerializeField] private Sprite shareIcon;
    [SerializeField] private Sprite patentLicenseIcon;

    public void Clear()
    {
        ClearContent(playerOfferContent, nameof(playerOfferContent));
        ClearContent(aiOfferContent, nameof(aiOfferContent));
    }

    public void AddPlayerCredit(string _value, UnityAction _onRemove, UnityAction<string> _onValueChanged = null)
    {
        LogMissingIcon(creditIcon, nameof(creditIcon));
        AddPlayerItem(creditIcon, CreditLabel, _value, _onRemove, _onValueChanged);
    }

    public void AddPlayerPower(string _value, UnityAction _onRemove, UnityAction<string> _onValueChanged = null)
    {
        LogMissingIcon(powerIcon, nameof(powerIcon));
        AddPlayerItem(powerIcon, PowerLabel, _value, _onRemove, _onValueChanged);
    }

    public void AddPlayerCard(string _value, UnityAction _onRemove)
    {
        LogMissingIcon(cardIcon, nameof(cardIcon));
        AddPlayerItem(cardIcon, CardLabel, _value, _onRemove, null);
    }

    public void AddPlayerCity(string _value, UnityAction _onRemove)
    {
        LogMissingIcon(cityIcon, nameof(cityIcon));
        AddPlayerItem(cityIcon, CityLabel, _value, _onRemove, null);
    }

    public void AddPlayerShare(string _value, UnityAction _onRemove)
    {
        LogMissingIcon(shareIcon, nameof(shareIcon));
        AddPlayerItem(shareIcon, ShareLabel, _value, _onRemove, null);
    }

    public void AddPlayerShare(string _cityName, string _value, UnityAction _onRemove, UnityAction<string> _onValueChanged)
    {
        LogMissingIcon(shareIcon, nameof(shareIcon));
        AddPlayerItem(shareIcon, GetShareLabel(_cityName), _value, _onRemove, _onValueChanged);
    }

    public void AddPlayerPatentLicense(string _value, UnityAction _onRemove)
    {
        Sprite icon = GetPatentLicenseIcon();
        LogMissingIcon(icon, nameof(patentLicenseIcon));
        AddPlayerItem(icon, PatentLicenseLabel, _value, _onRemove, null);
    }

    public void AddAiCredit(string _value, UnityAction _onRemove, UnityAction<string> _onValueChanged = null)
    {
        LogMissingIcon(creditIcon, nameof(creditIcon));
        AddAiItem(creditIcon, CreditLabel, _value, _onRemove, _onValueChanged);
    }

    public void AddAiPower(string _value, UnityAction _onRemove, UnityAction<string> _onValueChanged = null)
    {
        LogMissingIcon(powerIcon, nameof(powerIcon));
        AddAiItem(powerIcon, PowerLabel, _value, _onRemove, _onValueChanged);
    }

    public void AddAiCity(string _value, UnityAction _onRemove)
    {
        LogMissingIcon(cityIcon, nameof(cityIcon));
        AddAiItem(cityIcon, CityLabel, _value, _onRemove, null);
    }

    public void AddAiShare(string _value, UnityAction _onRemove)
    {
        LogMissingIcon(shareIcon, nameof(shareIcon));
        AddAiItem(shareIcon, ShareLabel, _value, _onRemove, null);
    }

    public void AddAiShare(string _cityName, string _value, UnityAction _onRemove, UnityAction<string> _onValueChanged)
    {
        LogMissingIcon(shareIcon, nameof(shareIcon));
        AddAiItem(shareIcon, GetShareLabel(_cityName), _value, _onRemove, _onValueChanged);
    }

    public void AddAiPatentLicense(string _value, UnityAction _onRemove)
    {
        Sprite icon = GetPatentLicenseIcon();
        LogMissingIcon(icon, nameof(patentLicenseIcon));
        AddAiItem(icon, PatentLicenseLabel, _value, _onRemove, null);
    }

    private void AddPlayerItem(Sprite icon, string name, string value, UnityAction onRemove, UnityAction<string> onValueChanged)
    {
        AddItem(playerOfferContent, nameof(playerOfferContent), icon, name, value, onRemove, onValueChanged);
    }

    private void AddAiItem(Sprite icon, string name, string value, UnityAction onRemove, UnityAction<string> onValueChanged)
    {
        AddItem(aiOfferContent, nameof(aiOfferContent), icon, name, value, onRemove, onValueChanged);
    }

    private void AddItem(Transform content, string contentFieldName, Sprite icon, string name, string value, UnityAction onRemove, UnityAction<string> onValueChanged)
    {
        if (content == null)
        {
            LogMissingReference(contentFieldName);
            return;
        }

        if (offerItemPrefab == null)
        {
            LogMissingReference(nameof(offerItemPrefab));
            return;
        }

        TradeOfferItemView itemView = Instantiate(offerItemPrefab, content);
        if (itemView == null)
        {
            Debug.LogWarning($"{nameof(TradeOfferSummaryController)}: Failed to instantiate {nameof(offerItemPrefab)}.", this);
            return;
        }

        itemView.Setup(icon, name, value, onRemove, onValueChanged);
    }

    private void ClearContent(Transform content, string contentFieldName)
    {
        if (content == null)
        {
            LogMissingReference(contentFieldName);
            return;
        }

        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);
    }

    private string GetShareLabel(string cityName)
    {
        return string.IsNullOrWhiteSpace(cityName) ? ShareLabel : $"{ShareLabel} - {cityName}";
    }

    private Sprite GetPatentLicenseIcon()
    {
        return patentLicenseIcon != null ? patentLicenseIcon : cardIcon;
    }

    private void LogMissingIcon(Sprite icon, string fieldName)
    {
        if (icon != null)
            return;

        LogMissingReference(fieldName);
    }

    private void LogMissingReference(string fieldName)
    {
        Debug.LogWarning($"{nameof(TradeOfferSummaryController)}: {fieldName} is not assigned.", this);
    }
}
