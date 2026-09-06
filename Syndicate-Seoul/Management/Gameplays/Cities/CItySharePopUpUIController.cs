using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CitySharePopUpUIController : MonoBehaviour, IEscapeClosable
{
    private enum CitySharePopupAlertType
    {
        None,
        LowShareLead,
        LostTopShare,
        WarAvailable
    }

    [SerializeField] private FactionManager playerFac;
    [SerializeField] private CityUIController cityUIController;
    [SerializeField] private GameObject lowShareLeadWarningRoot;
    [SerializeField] private TMP_Text lowShareLeadTitleText;
    [SerializeField] private TMP_Text lowShareLeadDescriptionText;
    [SerializeField] private GameObject lostTopShareWarningRoot;
    [SerializeField] private TMP_Text lostTopShareTitleText;
    [SerializeField] private TMP_Text lostTopShareDescriptionText;
    [SerializeField] private GameObject warAvailableWarningRoot;
    [SerializeField] private TMP_Text warAvailableTitleText;
    [SerializeField] private TMP_Text warAvailableDescriptionText;

    private const string LowShareLeadTitle = "도시 지분 경고";
    private const int LowShareLeadWarningThreshold = 10;

    private const string LostTopShareTitle = "도시 지분 경고";
    private const string WarAvailableTitle = "전쟁 선포 가능";
    private const string UnknownCityName = "알 수 없는 도시";
    private const string UnknownFactionName = "알 수 없는 세력";

    private CityScript targetCity;
    private CityScript lastLowShareLeadAlertCity;
    private FactionManager lastLowShareLeadAlertFaction;
    private int lastLowShareLeadAlertGap;
    private CityScript lastLostTopShareAlertCity;
    private FactionManager lastLostTopShareAlertFaction;
    private CityScript lastWarAvailableAlertCity;
    private CitySharePopupAlertType currentAlertType;
    private bool shareEventsBound;

    public bool IsOpen => currentAlertType != CitySharePopupAlertType.None
        || IsWarningOpen(lowShareLeadWarningRoot)
        || IsWarningOpen(lostTopShareWarningRoot)
        || IsWarningOpen(warAvailableWarningRoot);

    private void Awake()
    {
        ClosePopup();
    }

    private void OnEnable()
    {
        SubscribeShareEvents();
    }

    private void OnDisable()
    {
        UnsubscribeShareEvents();
    }

    private void Start()
    {
        SubscribeShareEvents();
        CheckInitialWarAvailableCity();
    }

    private void OnDestroy()
    {
        UnsubscribeShareEvents();
    }

    public void ClosePopup()
    {
        SetWarningActive(CitySharePopupAlertType.LowShareLead, false);
        SetWarningActive(CitySharePopupAlertType.LostTopShare, false);
        SetWarningActive(CitySharePopupAlertType.WarAvailable, false);

        if (currentAlertType != CitySharePopupAlertType.None)
            currentAlertType = CitySharePopupAlertType.None;
    }

    public bool CloseByEscape()
    {
        if (!IsOpen)
            return false;

        ClosePopup();
        return true;
    }

    public void OpenTargetCityFromPopup()
    {
        if (targetCity == null || targetCity.cityData == null || cityUIController == null)
            return;

        ClosePopup();
        cityUIController.OpenCityPanel(targetCity);
    }

    private void SubscribeShareEvents()
    {
        if (shareEventsBound || CityShareManager.instance == null)
            return;

        CityShareManager.instance.AnyCityShareChanged -= OnCityShareChanged;
        CityShareManager.instance.AnyCityShareChanged += OnCityShareChanged;
        shareEventsBound = true;
    }

    private void UnsubscribeShareEvents()
    {
        if (!shareEventsBound || CityShareManager.instance == null)
        {
            shareEventsBound = false;
            return;
        }

        CityShareManager.instance.AnyCityShareChanged -= OnCityShareChanged;
        shareEventsBound = false;
    }

    private void OnCityShareChanged(CityScript _city, FactionManager _fromFaction, FactionManager _toFaction)
    {
        if (_city == null || _city.cityData == null || playerFac == null)
            return;

        if (_city.cityData.owner == null)
        {
            ResetLostTopShareAlertCacheForCity(_city);
            ResetWarAvailableAlertCacheForCity(_city);
            ResetLowShareLeadAlertCacheForCity(_city);
            return;
        }

        if (_city.cityData.owner == playerFac)
        {
            ResetWarAvailableAlertCacheForCity(_city);
            TryOpenLowShareLeadPopup(_city);
            TryOpenLostTopSharePopup(_city);
            return;
        }

        ResetLostTopShareAlertCacheForCity(_city);
        ResetLowShareLeadAlertCacheForCity(_city);
        TryOpenWarAvailablePopup(_city);
    }

    private void CheckInitialWarAvailableCity()
    {
        if (playerFac == null)
            return;

        CityScript[] cities = FindObjectsByType<CityScript>(FindObjectsSortMode.None);
        for (int i = 0; i < cities.Length; i++)
        {
            if (TryOpenWarAvailablePopup(cities[i]))
                return;
        }
    }

    private bool TryOpenLostTopSharePopup(CityScript city)
    {
        if (city == null || city.cityData == null || city.cityData.owner != playerFac)
            return false;

        CityShareData shareData = city.cityData.shareData;
        if (shareData == null)
            return false;

        if (shareData.HasTieForHighestShare())
        {
            ResetLostTopShareAlertCacheForCity(city);
            return false;
        }

        FactionManager highestShareHolder = shareData.GetHighestShareHolder();
        if (highestShareHolder == null || highestShareHolder == playerFac)
        {
            ResetLostTopShareAlertCacheForCity(city);
            return false;
        }

        ResetLowShareLeadAlertCacheForCity(city);

        if (lastLostTopShareAlertCity == city && lastLostTopShareAlertFaction == highestShareHolder)
            return false;

        lastLostTopShareAlertCity = city;
        lastLostTopShareAlertFaction = highestShareHolder;
        OpenPopup(CitySharePopupAlertType.LostTopShare, city, highestShareHolder);
        return true;
    }

    private bool TryOpenLowShareLeadPopup(CityScript city)
    {
        if (city == null || city.cityData == null || playerFac == null)
            return false;

        if (city.cityData.owner != playerFac)
            return false;

        CityShareData shareData = city.cityData.shareData;
        if (shareData == null)
            return false;

        int playerShare = shareData.GetShare(playerFac);
        if (playerShare <= 0)
        {
            ResetLowShareLeadAlertCacheForCity(city);
            return false;
        }

        FactionManager threatFaction = null;
        int threatShare = 0;

        foreach (CityShareData.FactionShareEntry entry in shareData.Shares)
        {
            if (entry == null || entry.faction == null || entry.faction == playerFac)
                continue;

            if (entry.sharePercent > threatShare)
            {
                threatFaction = entry.faction;
                threatShare = entry.sharePercent;
            }
        }

        if (threatFaction == null)
        {
            ResetLowShareLeadAlertCacheForCity(city);
            return false;
        }

        if (threatShare >= playerShare)
        {
            ResetLowShareLeadAlertCacheForCity(city);
            return false;
        }

        int gap = playerShare - threatShare;
        if (gap <= 0)
            return false;

        if (gap > LowShareLeadWarningThreshold)
        {
            ResetLowShareLeadAlertCacheForCity(city);
            return false;
        }

        if (lastLowShareLeadAlertCity == city && lastLowShareLeadAlertFaction == threatFaction)
            return false;

        lastLowShareLeadAlertCity = city;
        lastLowShareLeadAlertFaction = threatFaction;
        lastLowShareLeadAlertGap = gap;
        OpenPopup(CitySharePopupAlertType.LowShareLead, city, threatFaction, playerShare, threatShare);
        return true;
    }

    private bool TryOpenWarAvailablePopup(CityScript city)
    {
        if (city == null || city.cityData == null || playerFac == null)
            return false;

        FactionManager owner = city.cityData.owner;
        if (owner == null || owner == playerFac)
        {
            ResetWarAvailableAlertCacheForCity(city);
            return false;
        }

        CityShareData shareData = city.cityData.shareData;
        if (shareData == null)
            return false;

        if (shareData.HasTieForHighestShare())
        {
            ResetWarAvailableAlertCacheForCity(city);
            return false;
        }

        if (shareData.GetHighestShareHolder() != playerFac)
        {
            ResetWarAvailableAlertCacheForCity(city);
            return false;
        }

        CityShareManager shareManager = CityShareManager.instance != null
            ? CityShareManager.instance
            : FindFirstObjectByType<CityShareManager>();

        if (shareManager == null || !shareManager.CanClaimOwnership(city, playerFac, out _))
        {
            ResetWarAvailableAlertCacheForCity(city);
            return false;
        }

        if (lastWarAvailableAlertCity == city)
            return false;

        lastWarAvailableAlertCity = city;
        OpenPopup(CitySharePopupAlertType.WarAvailable, city, playerFac);
        return true;
    }

    private void OpenPopup(CitySharePopupAlertType alertType, CityScript city, FactionManager highestShareHolder, int playerShare = 0, int threatShare = 0)
    {
        currentAlertType = alertType;
        targetCity = city;

        SetWarningActive(CitySharePopupAlertType.LowShareLead, alertType == CitySharePopupAlertType.LowShareLead);
        SetWarningActive(CitySharePopupAlertType.LostTopShare, alertType == CitySharePopupAlertType.LostTopShare);
        SetWarningActive(CitySharePopupAlertType.WarAvailable, alertType == CitySharePopupAlertType.WarAvailable);

        GetWarningViews(alertType, out _, out TMP_Text selectedTitleText, out TMP_Text selectedDescriptionText);

        if (selectedTitleText != null)
        {
            switch (alertType)
            {
                case CitySharePopupAlertType.LowShareLead:
                    selectedTitleText.text = LowShareLeadTitle;
                    break;
                case CitySharePopupAlertType.WarAvailable:
                    selectedTitleText.text = WarAvailableTitle;
                    break;
                default:
                    selectedTitleText.text = LostTopShareTitle;
                    break;
            }
        }

        if (selectedDescriptionText != null)
        {
            switch (alertType)
            {
                case CitySharePopupAlertType.LowShareLead:
                    selectedDescriptionText.text = BuildLowShareLeadDescription(city, highestShareHolder, playerShare, threatShare);
                    break;
                case CitySharePopupAlertType.WarAvailable:
                    selectedDescriptionText.text = BuildWarAvailableDescription(city);
                    break;
                default:
                    selectedDescriptionText.text = BuildLostTopShareDescription(city, highestShareHolder);
                    break;
            }
        }
    }

    private string BuildLostTopShareDescription(CityScript city, FactionManager highestShareHolder)
    {
        string cityName = GetCityName(city);
        string factionName = highestShareHolder != null
            && !string.IsNullOrWhiteSpace(highestShareHolder.factionName)
            ? highestShareHolder.factionName
            : UnknownFactionName;

        return $"{cityName}에서 {factionName}이 단독 최고 지분자가 되었습니다.\n해당 세력은 이 도시의 소유권을 주장하고 전쟁을 선포할 수 있습니다.";
    }

    private string BuildLowShareLeadDescription(CityScript city, FactionManager threatFaction, int playerShare, int threatShare)
    {
        string cityName = GetCityName(city);
        string factionName = threatFaction != null
            && !string.IsNullOrWhiteSpace(threatFaction.factionName)
            ? threatFaction.factionName
            : UnknownFactionName;
        int gap = Mathf.Max(0, playerShare - threatShare);

        return $"{cityName}에서 {factionName}이 플레이어 지분을 위협하고 있습니다.\n현재 지분 차이는 {gap}%입니다.\n이 세력이 {gap}%만 더 확보하면 최고 지분자가 될 수 있습니다.";
    }

    private string BuildWarAvailableDescription(CityScript city)
    {
        string cityName = GetCityName(city);
        return $"{cityName}에서 플레이어가 단독 최고 지분자가 되었습니다.\n이제 이 도시의 소유권을 주장하고 전쟁을 선포할 수 있습니다.";
    }

    private string GetCityName(CityScript city)
    {
        if (city != null && city.cityData != null && !string.IsNullOrWhiteSpace(city.cityData.cityName))
            return CityDisplayNameUtility.ToKoreanDisplayName(city.cityData.cityName);

        return UnknownCityName;
    }

    private void ResetLostTopShareAlertCacheForCity(CityScript city)
    {
        if (lastLostTopShareAlertCity != city)
            return;

        if (targetCity == city && currentAlertType == CitySharePopupAlertType.LostTopShare)
        {
            targetCity = null;
            ClosePopup();
        }

        lastLostTopShareAlertCity = null;
        lastLostTopShareAlertFaction = null;
    }

    private void ResetLowShareLeadAlertCacheForCity(CityScript city)
    {
        if (lastLowShareLeadAlertCity != city)
            return;

        if (targetCity == city && currentAlertType == CitySharePopupAlertType.LowShareLead)
        {
            targetCity = null;
            ClosePopup();
        }

        lastLowShareLeadAlertCity = null;
        lastLowShareLeadAlertFaction = null;
        lastLowShareLeadAlertGap = 0;
    }

    private void ResetWarAvailableAlertCacheForCity(CityScript city)
    {
        if (lastWarAvailableAlertCity != city)
            return;

        if (targetCity == city && currentAlertType == CitySharePopupAlertType.WarAvailable)
        {
            targetCity = null;
            ClosePopup();
        }

        lastWarAvailableAlertCity = null;
    }


    private void BindButtonPair(Button confirmButton, Button closeButton)
    {
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveListener(OpenTargetCityFromPopup);
            confirmButton.onClick.AddListener(OpenTargetCityFromPopup);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(ClosePopup);
            closeButton.onClick.AddListener(ClosePopup);
        }
    }

    private void UnbindButtonPair(Button confirmButton, Button closeButton)
    {
        if (confirmButton != null)
            confirmButton.onClick.RemoveListener(OpenTargetCityFromPopup);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(ClosePopup);
    }

    private void SetWarningActive(CitySharePopupAlertType alertType, bool isActive)
    {
        GetWarningViews(alertType, out GameObject root, out _, out _);
        if (root != null)
            root.SetActive(isActive);
    }

    private static bool IsWarningOpen(GameObject root)
    {
        return root != null && root.activeInHierarchy;
    }

    private void GetWarningViews(CitySharePopupAlertType alertType, out GameObject root, out TMP_Text title, out TMP_Text description)
    {
        switch (alertType)
        {
            case CitySharePopupAlertType.LowShareLead:
                root = lowShareLeadWarningRoot;
                title = lowShareLeadTitleText;
                description = lowShareLeadDescriptionText;
                break;
            case CitySharePopupAlertType.WarAvailable:
                root = warAvailableWarningRoot;
                title = warAvailableTitleText;
                description = warAvailableDescriptionText;
                break;
            case CitySharePopupAlertType.LostTopShare:
                root = lostTopShareWarningRoot;
                title = lostTopShareTitleText;
                description = lostTopShareDescriptionText;
                break;
            default:
                root = null;
                title = null;
                description = null;
                break;
        }
    }
}
