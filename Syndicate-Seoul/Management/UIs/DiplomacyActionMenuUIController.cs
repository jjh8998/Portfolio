using UnityEngine;
using UnityEngine.UI;

public class DiplomacyActionMenuUIController : MonoBehaviour
{
    [SerializeField] private GameObject actionMenuRoot;
    [SerializeField] private Button talkButton;
    [SerializeField] private Button denounceButton;
    [SerializeField] private Button tradeButton;
    [SerializeField] private Button giftButton;
    [SerializeField] private FactionManager playerFac;
    [SerializeField] private DiplomacyTradePanelController diplomacyTradePanel;

    private CityScript currentCity;
    private FactionManager targetFaction;

    public bool IsOpen => actionMenuRoot != null && actionMenuRoot.activeInHierarchy;

    private void Awake()
    {
        BindButtons();
        Close();
    }

    private void OnEnable()
    {
        BindButtons();
    }

    private void OnDestroy()
    {
        UnbindButtons();
    }

    public void Open(CityScript _city, FactionManager _targetFaction)
    {
        if (_city == null || _targetFaction == null)
            return;

        currentCity = _city;
        targetFaction = _targetFaction;

        if (actionMenuRoot != null)
            actionMenuRoot.SetActive(true);

        if (TutorialManager.Instance != null)
            TutorialManager.Instance.NotifyCondition(TutorialCondition.OpenDiplomacyActionMenu);
    }

    public void Close()
    {
        if (actionMenuRoot != null)
            actionMenuRoot.SetActive(false);

        currentCity = null;
        targetFaction = null;
    }

    public void Toggle(CityScript _city, FactionManager _targetFaction)
    {
        if (_city == null || _targetFaction == null)
            return;

        if (IsSameTarget(_city, _targetFaction) && IsOpen)
        {
            Close();
            return;
        }

        Open(_city, _targetFaction);
    }

    private void OnClickTalkButton()
    {
        if (!HasValidTarget())
            return;

        Debug.Log($"[DiplomacyActionMenuUI] Talk clicked. city={GetCityName(currentCity)}, targetFaction={targetFaction.factionName}");
        Close();
    }

    private void OnClickDenounceButton()
    {
        if (!HasValidTarget())
            return;

        Debug.Log($"[DiplomacyActionMenuUI] Denounce clicked. city={GetCityName(currentCity)}, targetFaction={targetFaction.factionName}");
        Close();
    }

    private void OnClickTradeButton()
    {
        if (!HasValidTarget())
            return;

        if (!ResolvePlayerFaction())
        {
            Debug.LogWarning("[DiplomacyActionMenuUI] Player faction is missing.");
            return;
        }

        if (targetFaction == playerFac)
        {
            Debug.LogWarning("[DiplomacyActionMenuUI] Cannot trade with player faction.");
            return;
        }

        if (diplomacyTradePanel == null)
            diplomacyTradePanel = Object.FindAnyObjectByType<DiplomacyTradePanelController>(FindObjectsInactive.Include);

        if (diplomacyTradePanel == null)
        {
            Debug.LogError("[DiplomacyActionMenuUI] DiplomacyTradePanelController was not found.");
            return;
        }

        diplomacyTradePanel.OpenTradeWith(targetFaction);
        Close();
    }

    private void OnClickGiftButton()
    {
        if (!HasValidTarget())
            return;

        Debug.Log($"[DiplomacyActionMenuUI] Gift clicked. city={GetCityName(currentCity)}, targetFaction={targetFaction.factionName}");
        Close();
    }

    private void BindButtons()
    {
        BindButton(talkButton, OnClickTalkButton);
        BindButton(denounceButton, OnClickDenounceButton);
        BindButton(tradeButton, OnClickTradeButton);
        BindButton(giftButton, OnClickGiftButton);
    }

    private void UnbindButtons()
    {
        UnbindButton(talkButton, OnClickTalkButton);
        UnbindButton(denounceButton, OnClickDenounceButton);
        UnbindButton(tradeButton, OnClickTradeButton);
        UnbindButton(giftButton, OnClickGiftButton);
    }

    private void BindButton(Button button, UnityEngine.Events.UnityAction onClick)
    {
        if (button == null)
            return;

        button.onClick.RemoveListener(onClick);
        button.onClick.AddListener(onClick);
    }

    private void UnbindButton(Button button, UnityEngine.Events.UnityAction onClick)
    {
        if (button == null)
            return;

        button.onClick.RemoveListener(onClick);
    }

    private bool HasValidTarget()
    {
        return currentCity != null && currentCity.cityData != null && targetFaction != null;
    }

    private bool ResolvePlayerFaction()
    {
        if (playerFac != null)
            return true;

        FactionManager[] factions = Object.FindObjectsByType<FactionManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < factions.Length; i++)
        {
            FactionManager faction = factions[i];
            if (faction == null)
                continue;

            if (faction.IsPlayerFaction)
            {
                playerFac = faction;
                return true;
            }
        }

        return false;
    }

    private bool IsSameTarget(CityScript city, FactionManager faction)
    {
        return ReferenceEquals(currentCity, city) && ReferenceEquals(targetFaction, faction);
    }

    private string GetCityName(CityScript city)
    {
        return city != null && city.cityData != null
            ? CityDisplayNameUtility.ToKoreanDisplayName(city.cityData.cityName)
            : "None";
    }
}
