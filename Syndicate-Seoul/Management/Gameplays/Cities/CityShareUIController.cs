using System.Collections.Generic;
using System.Text;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CityShareUIController : MonoBehaviour
{
    public TextMeshProUGUI shareText;
    [SerializeField] private PIChartUIController sharePieChart;
    [SerializeField] private Image highestShareHolderIconImage;
    [SerializeField] private Button highestShareHolderTradeButton;
    [SerializeField] private DiplomacyTradePanelController diplomacyTradePanel;
    [SerializeField] private CEODatabaseSO ceoDatabase;
    [Header("Open Effect")]
    [SerializeField] private UIHologramGlitchEffect sharePieOpenEffect;
    private CityScript currentCity;
    private FactionManager highestShareHolderFaction;
    private bool diplomacyTradePanelSearched;

    private void OnEnable()
    {
        BindHighestShareHolderTradeButton();

        if (CityShareManager.instance == null)
            return;

        CityShareManager.instance.AnyCityShareChanged -= OnAnyCityShareChanged;
        CityShareManager.instance.AnyCityShareChanged += OnAnyCityShareChanged;
    }

    private void OnDisable()
    {
        UnbindHighestShareHolderTradeButton();

        if (CityShareManager.instance == null)
            return;

        CityShareManager.instance.AnyCityShareChanged -= OnAnyCityShareChanged;
    }

    private const string CorporateAssociationLabel = "기업협회";

    public void ShowCityShares(CityScript _city)
    {
        if (_city == null)
        {
            currentCity = null;
            SetShareText("No city selected.");
            if (sharePieChart != null)
                sharePieChart.Clear();
            ClearHighestShareHolderIcon();
            return;
        }

        currentCity = _city;
        ShowCitySharesInternal(_city.cityData);
    }

    public void ShowCityShares(CityData _cityData)
    {
        currentCity = null;
        ShowCitySharesInternal(_cityData);
    }

    private void ShowCitySharesInternal(CityData _cityData)
    {

        if (_cityData == null)
        {
            SetShareText("City data is missing.");
            if (sharePieChart != null)
                sharePieChart.Clear();
            ClearHighestShareHolderIcon();
            return;
        }

        if (_cityData.shareData == null)
        {
            SetShareText($"{CityDisplayNameUtility.ToKoreanDisplayName(_cityData.cityName)}\nShares not initialized.");
            if (sharePieChart != null)
                sharePieChart.Clear();
            ClearHighestShareHolderIcon();
            return;
        }

        StringBuilder builder = new StringBuilder();
        List<PIChartUIController.PIChartEntry> chartEntries = new List<PIChartUIController.PIChartEntry>();
        builder.AppendLine(string.IsNullOrWhiteSpace(_cityData.cityName)
            ? "Unknown City"
            : CityDisplayNameUtility.ToKoreanDisplayName(_cityData.cityName));

        List<CityShareData.FactionShareEntry> entries = new List<CityShareData.FactionShareEntry>(_cityData.shareData.Shares);
        entries.Sort((left, right) => right.sharePercent.CompareTo(left.sharePercent));

        for (int i = 0; i < entries.Count; i++)
        {
            CityShareData.FactionShareEntry entry = entries[i];
            if (entry == null || entry.faction == null)
                continue;

            builder.AppendLine($"{entry.faction.factionName}: {entry.sharePercent}%");
            chartEntries.Add(new PIChartUIController.PIChartEntry(entry.faction.factionName, entry.sharePercent, entry.faction));
        }

        int unassignedSharePercent = _cityData.shareData.UnassignedSharePercent;
        builder.Append($"{CorporateAssociationLabel}: {unassignedSharePercent}%");
        if (unassignedSharePercent > 0)
            chartEntries.Add(new PIChartUIController.PIChartEntry(CorporateAssociationLabel, unassignedSharePercent));

        SetShareText(builder.ToString());

        if (sharePieChart != null)
        {
            if (chartEntries.Count > 0)
                sharePieChart.SetChartData(chartEntries);
            else
                sharePieChart.Clear();
        }

        RefreshHighestShareHolderIcon(_cityData.shareData);
    }

    public void Clear()
    {
        currentCity = null;
        SetShareText("");

        if (sharePieChart != null)
            sharePieChart.Clear();

        ClearHighestShareHolderIcon();
    }

    public void PlaySharePieOpenEffect()
    {
        if (sharePieOpenEffect == null)
            ResolveSharePieOpenEffect();

        if (sharePieOpenEffect != null)
            sharePieOpenEffect.Play();
    }

    private void OnAnyCityShareChanged(CityScript _city, FactionManager _fromFaction, FactionManager _toFaction)
    {
        if (currentCity == null || !ReferenceEquals(currentCity, _city))
            return;

        ShowCityShares(currentCity);
    }

    private void SetShareText(string value)
    {
        if (shareText != null)
            shareText.text = value;
    }

    private void RefreshHighestShareHolderIcon(CityShareData shareData)
    {
        highestShareHolderFaction = shareData != null ? shareData.GetHighestShareHolder() : null;
        RefreshHighestShareHolderTradeButton();

        if (highestShareHolderFaction == null || string.IsNullOrWhiteSpace(highestShareHolderFaction.ceoId))
        {
            ClearHighestShareHolderImage();
            return;
        }

        if (highestShareHolderIconImage == null)
            return;

        if (ceoDatabase == null)
            ceoDatabase = ScriptableObject.CreateInstance<CEODatabaseSO>();

        CEOData ceo = ceoDatabase.GetCEOByID(highestShareHolderFaction.ceoId);
        if (ceo == null || ceo.iconSprite == null)
        {
            ClearHighestShareHolderImage();
            return;
        }

        highestShareHolderIconImage.sprite = ceo.iconSprite;
        highestShareHolderIconImage.enabled = true;
    }

    private void ClearHighestShareHolderIcon()
    {
        highestShareHolderFaction = null;
        ClearHighestShareHolderImage();
        RefreshHighestShareHolderTradeButton();
    }

    private void ClearHighestShareHolderImage()
    {
        if (highestShareHolderIconImage == null)
            return;

        highestShareHolderIconImage.sprite = null;
        highestShareHolderIconImage.enabled = false;
    }

    private void BindHighestShareHolderTradeButton()
    {
        if (highestShareHolderTradeButton == null)
            return;

        highestShareHolderTradeButton.onClick.RemoveListener(OnClickHighestShareHolderTradeButton);
        highestShareHolderTradeButton.onClick.AddListener(OnClickHighestShareHolderTradeButton);
        RefreshHighestShareHolderTradeButton();
    }

    private void UnbindHighestShareHolderTradeButton()
    {
        if (highestShareHolderTradeButton == null)
            return;

        highestShareHolderTradeButton.onClick.RemoveListener(OnClickHighestShareHolderTradeButton);
    }

    private void RefreshHighestShareHolderTradeButton()
    {
        if (highestShareHolderTradeButton == null)
            return;

        highestShareHolderTradeButton.interactable = highestShareHolderFaction != null
            && !highestShareHolderFaction.IsPlayerFaction;
    }

    private void OnClickHighestShareHolderTradeButton()
    {
        if (highestShareHolderFaction == null)
        {
            Debug.LogWarning("[CityShareUIController] Highest share holder is missing. Trade panel was not opened.");
            return;
        }

        if (highestShareHolderFaction.IsPlayerFaction)
        {
            Debug.LogWarning("[CityShareUIController] Highest share holder is the player faction. Trade panel was not opened.");
            return;
        }

        DiplomacyTradePanelController tradePanel = ResolveDiplomacyTradePanel();
        if (tradePanel == null)
        {
            Debug.LogError("[CityShareUIController] DiplomacyTradePanelController was not found. Trade panel was not opened.");
            return;
        }

        tradePanel.OpenTradeWith(highestShareHolderFaction);
    }

    private DiplomacyTradePanelController ResolveDiplomacyTradePanel()
    {
        if (diplomacyTradePanel == null && !diplomacyTradePanelSearched)
        {
            diplomacyTradePanel = Object.FindAnyObjectByType<DiplomacyTradePanelController>(FindObjectsInactive.Include);
            diplomacyTradePanelSearched = true;
        }

        return diplomacyTradePanel;
    }

    private void ResolveSharePieOpenEffect()
    {
        if (sharePieChart == null)
            return;

        sharePieOpenEffect = sharePieChart.GetComponentInChildren<UIHologramGlitchEffect>(true);
        if (sharePieOpenEffect == null)
            sharePieOpenEffect = sharePieChart.GetComponentInParent<UIHologramGlitchEffect>(true);
    }
}
