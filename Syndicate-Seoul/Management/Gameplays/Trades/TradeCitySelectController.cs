using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public enum TradeCitySelectMode
{
    PlayerGiveCity,
    PlayerGiveShare,
    AiGiveCity,
    AiGiveShare
}

public class TradeCitySelectController : MonoBehaviour, IEscapeClosable
{
    private const string NoCityOptionLabel = "선택 취소";
    private const string NoTradableShareOptionLabel = "거래 가능한 지분 없음";
    private const string UnnamedCityLabel = "Unnamed city";
    private const string MissingShareManagerMessage = "CityShareManager.instance is missing.";

    [SerializeField] private GameObject citySelectPanelRoot;
    [SerializeField] private float playerOpenOffsetX;
    [SerializeField] private float targetOpenOffsetX;
    [SerializeField] private Transform cityContent;
    [SerializeField] private TradeCitySelectListItem cityListItemPrefab;

    private readonly List<CityScript> playerGiveCityOptions = new List<CityScript>();
    private readonly List<CityScript> aiGiveCityOptions = new List<CityScript>();
    private readonly List<CityScript> playerGiveShareCityOptions = new List<CityScript>();
    private readonly List<CityScript> aiGiveShareCityOptions = new List<CityScript>();
    private readonly List<UnityAction<string>> selectionChangedCallbacks = new List<UnityAction<string>>();

    private CityScript selectedPlayerGiveCity;
    private CityScript selectedAiGiveCity;
    private CityScript selectedPlayerGiveShareCity;
    private CityScript selectedAiGiveShareCity;
    private FactionManager currentPlayerFaction;
    private FactionManager currentTargetFaction;
    private TradeCitySelectMode currentMode;

    public bool IsOpen => IsPanelOpen();

    public void Refresh(FactionManager _playerFaction, FactionManager _targetFaction)
    {
        currentPlayerFaction = _playerFaction;
        currentTargetFaction = _targetFaction;

        RefreshPlayerGiveCityOptions(_playerFaction);
        RefreshAiGiveCityOptions(_targetFaction);
        RefreshShareCityOptions(playerGiveShareCityOptions, ref selectedPlayerGiveShareCity, _playerFaction);
        RefreshShareCityOptions(aiGiveShareCityOptions, ref selectedAiGiveShareCity, _targetFaction);
        RefreshCurrentModeList();
    }

    public void SetMode(TradeCitySelectMode _mode)
    {
        currentMode = _mode;
        RefreshCurrentModeList();
    }

    public TradeCitySelectMode GetCurrentMode()
    {
        return currentMode;
    }

    public bool IsPanelOpen()
    {
        return citySelectPanelRoot != null && citySelectPanelRoot.activeInHierarchy;
    }

    public bool CloseByEscape()
    {
        if (!IsOpen)
            return false;

        SetPanelVisible(false);
        return true;
    }

    public void SetPanelVisible(bool _isVisible)
    {
        if (citySelectPanelRoot != null)
        {
            citySelectPanelRoot.SetActive(_isVisible);
        }
        else
        {
            LogMissingReference(nameof(citySelectPanelRoot));
        }
    }

    public void SetPanelVisible(bool _isVisible, RectTransform _sourceButtonRect)
    {
        SetPlayerPanelVisible(_isVisible, _sourceButtonRect);
    }

    public void SetPlayerPanelVisible(bool _isVisible, RectTransform _sourceButtonRect)
    {
        if (_isVisible)
            ApplyOpenPosition(_sourceButtonRect, playerOpenOffsetX);

        SetPanelVisible(_isVisible);
    }

    public void SetTargetPanelVisible(bool _isVisible, RectTransform _sourceButtonRect)
    {
        if (_isVisible)
            ApplyOpenPosition(_sourceButtonRect, targetOpenOffsetX);

        SetPanelVisible(_isVisible);
    }

    public CityScript GetSelectedPlayerGiveCity()
    {
        return selectedPlayerGiveCity;
    }

    public CityScript GetSelectedPlayerGiveShareCity()
    {
        return selectedPlayerGiveShareCity;
    }

    public CityScript GetSelectedAiGiveCity()
    {
        return selectedAiGiveCity;
    }

    public CityScript GetSelectedAiGiveShareCity()
    {
        return selectedAiGiveShareCity;
    }

    public int GetSelectedAiGiveSharePercent()
    {
        return GetFactionSharePercent(selectedAiGiveShareCity, currentTargetFaction);
    }

    public int GetFactionSharePercent(CityScript _city, FactionManager _faction)
    {
        return TryGetFactionSharePercent(_city, _faction, out int sharePercent)
            ? sharePercent
            : 0;
    }

    public string GetCityLabel(CityScript _city)
    {
        if (_city == null || _city.cityData == null || string.IsNullOrWhiteSpace(_city.cityData.cityName))
            return UnnamedCityLabel;

        return CityDisplayNameUtility.ToKoreanDisplayName(_city.cityData.cityName);
    }

    public void ResetSelections()
    {
        ResetPlayerCitySelection();
        ResetAiCitySelection();
        ResetPlayerShareSelection();
        ResetAiShareSelection();
    }

    public void ResetPlayerCitySelection()
    {
        selectedPlayerGiveCity = null;
    }

    public void ResetPlayerShareSelection()
    {
        selectedPlayerGiveShareCity = null;
    }

    public void ResetAiCitySelection()
    {
        selectedAiGiveCity = null;
    }

    public void ResetAiShareSelection()
    {
        selectedAiGiveShareCity = null;
    }

    public void BindSelectionChanged(UnityAction<string> _callback)
    {
        if (_callback == null || selectionChangedCallbacks.Contains(_callback))
            return;

        selectionChangedCallbacks.Add(_callback);
    }

    public void UnbindSelectionChanged(UnityAction<string> _callback)
    {
        if (_callback == null)
            return;

        selectionChangedCallbacks.Remove(_callback);
    }

    private void RefreshPlayerGiveCityOptions(FactionManager playerFaction)
    {
        RefreshOwnedCityOptions(playerGiveCityOptions, ref selectedPlayerGiveCity, playerFaction);
    }

    private void RefreshAiGiveCityOptions(FactionManager targetFaction)
    {
        RefreshOwnedCityOptions(aiGiveCityOptions, ref selectedAiGiveCity, targetFaction);
    }

    private void RefreshOwnedCityOptions(
        List<CityScript> optionsBuffer,
        ref CityScript selectedCity,
        FactionManager faction)
    {
        optionsBuffer.Clear();

        if (faction != null && faction.ownedCities != null)
        {
            for (int i = 0; i < faction.ownedCities.Count; i++)
            {
                CityScript city = faction.ownedCities[i];
                if (city == null)
                    continue;

                optionsBuffer.Add(city);
            }
        }

        if (selectedCity != null && !optionsBuffer.Contains(selectedCity))
            selectedCity = null;
    }

    private void RefreshShareCityOptions(
        List<CityScript> optionsBuffer,
        ref CityScript selectedCity,
        FactionManager faction)
    {
        optionsBuffer.Clear();

        if (faction == null)
        {
            selectedCity = null;
            return;
        }

        if (CityShareManager.instance == null)
        {
            Debug.LogWarning($"{nameof(TradeCitySelectController)}: {MissingShareManagerMessage}", this);
            selectedCity = null;
            return;
        }

        CityScript[] cities = FindObjectsByType<CityScript>(FindObjectsSortMode.None);
        for (int i = 0; i < cities.Length; i++)
        {
            CityScript city = cities[i];
            if (!TryGetFactionSharePercent(city, faction, out int sharePercent))
                continue;

            optionsBuffer.Add(city);
        }

        if (selectedCity != null && !optionsBuffer.Contains(selectedCity))
            selectedCity = null;
    }

    private void RefreshCurrentModeList()
    {
        ClearContent(cityContent, nameof(cityContent));

        if (!EnsureContent(cityContent, nameof(cityContent)))
            return;

        switch (currentMode)
        {
            case TradeCitySelectMode.PlayerGiveShare:
                AddShareCityItems(playerGiveShareCityOptions, currentPlayerFaction, SelectPlayerGiveShareCity);
                break;
            case TradeCitySelectMode.AiGiveShare:
                AddShareCityItems(aiGiveShareCityOptions, currentTargetFaction, SelectAiGiveShareCity);
                break;
            case TradeCitySelectMode.AiGiveCity:
                AddAiGiveCityItems();
                break;
            default:
                AddPlayerGiveCityItems();
                break;
        }
    }

    private void AddPlayerGiveCityItems()
    {
        AddCityItems(playerGiveCityOptions, SelectPlayerGiveCity);
    }

    private void AddAiGiveCityItems()
    {
        AddCityItems(aiGiveCityOptions, SelectAiGiveCity);
    }

    private void AddCityItems(List<CityScript> options, UnityAction<CityScript> onSelect)
    {
        AddListItem(cityContent, NoCityOptionLabel, () => onSelect(null));

        for (int i = 0; i < options.Count; i++)
        {
            CityScript city = options[i];
            if (city == null)
                continue;

            AddListItem(cityContent, GetCityLabel(city), () => onSelect(city));
        }
    }

    private void AddShareCityItems(List<CityScript> options, FactionManager faction, UnityAction<CityScript> onSelect)
    {
        AddListItem(cityContent, NoCityOptionLabel, () => onSelect(null));

        if (options.Count == 0)
        {
            AddListItem(cityContent, NoTradableShareOptionLabel, () => { });
            return;
        }

        for (int i = 0; i < options.Count; i++)
        {
            CityScript city = options[i];
            if (city == null)
                continue;

            int sharePercent = GetFactionSharePercent(city, faction);

            AddListItem(cityContent, $"{GetCityLabel(city)} ({sharePercent}%)", () => onSelect(city));
        }
    }

    private void SelectPlayerGiveCity(CityScript city)
    {
        selectedPlayerGiveCity = city;
        NotifySelectionChanged(city);
    }

    private void SelectAiGiveCity(CityScript city)
    {
        selectedAiGiveCity = city;
        NotifySelectionChanged(city);
    }

    private void SelectPlayerGiveShareCity(CityScript city)
    {
        selectedPlayerGiveShareCity = city;
        NotifySelectionChanged(city);
    }

    private void SelectAiGiveShareCity(CityScript city)
    {
        selectedAiGiveShareCity = city;
        NotifySelectionChanged(city);
    }

    private void NotifySelectionChanged(CityScript city)
    {
        string value = city != null ? GetCityLabel(city) : NoCityOptionLabel;
        for (int i = selectionChangedCallbacks.Count - 1; i >= 0; i--)
            selectionChangedCallbacks[i]?.Invoke(value);
    }

    private void AddListItem(Transform content, string label, UnityAction onClick)
    {
        if (cityListItemPrefab == null)
        {
            LogMissingReference(nameof(cityListItemPrefab));
            return;
        }

        TradeCitySelectListItem item = Instantiate(cityListItemPrefab, content);
        if (item == null)
            return;

        item.Setup(label, onClick);
    }

    private void ClearContent(Transform content, string contentFieldName)
    {
        if (content == null)
        {
            LogMissingReference(contentFieldName);
            return;
        }

        // cityContent should contain only generated list items because every child is removed here.
        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);
    }

    private bool EnsureContent(Transform content, string contentFieldName)
    {
        if (content != null)
            return true;

        LogMissingReference(contentFieldName);
        return false;
    }

    private void ApplyOpenPosition(RectTransform sourceButtonRect)
    {
        ApplyOpenPosition(sourceButtonRect, playerOpenOffsetX);
    }

    private void ApplyOpenPosition(RectTransform sourceButtonRect, float _openOffsetX)
    {
        if (citySelectPanelRoot == null || sourceButtonRect == null)
            return;

        RectTransform panelRect = citySelectPanelRoot.GetComponent<RectTransform>();
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

    private bool TryGetFactionSharePercent(CityScript city, FactionManager faction, out int sharePercent)
    {
        sharePercent = 0;

        if (city == null || faction == null || CityShareManager.instance == null)
            return false;

        if (!CityShareManager.instance.TryPrepareCityShares(city, out _))
            return false;

        if (city.cityData == null || city.cityData.shareData == null || city.cityData.shareData.GetTotalShare() != 100)
            return false;

        sharePercent = city.cityData.shareData.GetShare(faction);
        return sharePercent > 0;
    }

    private void LogMissingReference(string fieldName)
    {
        Debug.LogWarning($"{nameof(TradeCitySelectController)}: {fieldName} is not assigned.", this);
    }
}
