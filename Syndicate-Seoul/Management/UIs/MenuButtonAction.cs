using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 메뉴 버튼 프리팹에 부착되어, 클릭 시 씬의 해당 컨트롤러를 런타임에 찾아 패널을 연다.
/// 프리팹이 씬 오브젝트를 직접 참조하지 않으므로(인스펙터 onClick 미사용)
/// 프리팹을 다시 Apply 해도 연결이 끊기지 않는다.
/// </summary>
[RequireComponent(typeof(Button))]
public class MenuButtonAction : MonoBehaviour
{
    private const string EmployeeLockedTitle = "직원 기능 잠김";
    private const string EmployeeLockedDescription = "직원 기능을 사용하려면 한 도시에 완공된 건물을 3개 이상 지어야 합니다.";

    public enum MenuActionType
    {
        ToggleResearchPanel,
        OpenDeckBuilder,
        OpenCardPackInventory,
        OpenPauseMenu,
        OpenEmployeePanel
    }

    [SerializeField] private MenuActionType action;
    [SerializeField] private ManagementPopupUIController managementPopupUIController;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.RemoveListener(Execute);
        button.onClick.AddListener(Execute);
        ConfigureEmployeeDisabledColor();
    }

    private void OnEnable()
    {
        SubscribeEmployeeUnlockEvents();
        RefreshInteractable();
    }

    private void Start()
    {
        RefreshInteractable();
    }

    private void OnDisable()
    {
        UnsubscribeEmployeeUnlockEvents();
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(Execute);

        UnsubscribeEmployeeUnlockEvents();
    }

    private void Execute()
    {
        switch (action)
        {
            case MenuActionType.ToggleResearchPanel:
                Invoke<ResearchPanelController>(c => c.ToggleResearchPanel());
                break;
            case MenuActionType.OpenDeckBuilder:
                Invoke<CityUIController>(c => c.OnClickDeckButton());
                break;
            case MenuActionType.OpenCardPackInventory:
                Invoke<CardPackInventoryUIController>(c => c.OpenCardPackInventoryPanel());
                break;
            case MenuActionType.OpenPauseMenu:
                Invoke<PauseMenuController>(c => c.Open());
                break;
            case MenuActionType.OpenEmployeePanel:
                if (!IsEmployeeUnlocked())
                {
                    ShowEmployeeLockedPopup();
                    return;
                }

                Invoke<EmployeeManagementUIController>(c => c.OpenPanel());
                break;
        }
    }

    private void SubscribeEmployeeUnlockEvents()
    {
        if (action != MenuActionType.OpenEmployeePanel)
            return;

        FactionManager.InitialEmployeeUnlocked -= OnInitialEmployeeUnlocked;
        FactionManager.InitialEmployeeUnlocked += OnInitialEmployeeUnlocked;
        SaveManager.LoadCompleted -= OnLoadCompleted;
        SaveManager.LoadCompleted += OnLoadCompleted;
    }

    private void UnsubscribeEmployeeUnlockEvents()
    {
        if (action != MenuActionType.OpenEmployeePanel)
            return;

        FactionManager.InitialEmployeeUnlocked -= OnInitialEmployeeUnlocked;
        SaveManager.LoadCompleted -= OnLoadCompleted;
    }

    private void OnInitialEmployeeUnlocked(FactionManager _faction)
    {
        RefreshInteractable();
    }

    private void OnLoadCompleted()
    {
        RefreshInteractable();
    }

    private void RefreshInteractable()
    {
        if (action != MenuActionType.OpenEmployeePanel || button == null)
            return;

        button.interactable = true;
    }

    private bool IsEmployeeUnlocked()
    {
        FactionManager playerFaction = FindPlayerFaction();
        return playerFaction != null && playerFaction.IsEmployeeHiringUnlocked;
    }

    private void ShowEmployeeLockedPopup()
    {
        if (managementPopupUIController == null)
            managementPopupUIController = FindFirstObjectByType<ManagementPopupUIController>(FindObjectsInactive.Include);

        if (managementPopupUIController == null)
        {
            Debug.LogWarning("[MenuButtonAction] ManagementPopupUIController를 찾을 수 없습니다.");
            return;
        }

        managementPopupUIController.Open(EmployeeLockedTitle, EmployeeLockedDescription);
    }

    private void ConfigureEmployeeDisabledColor()
    {
        if (action != MenuActionType.OpenEmployeePanel || button == null)
            return;

        ColorBlock colors = button.colors;
        colors.disabledColor = new Color(0.45f, 0.45f, 0.45f, 0.6f);
        button.colors = colors;
    }

    private FactionManager FindPlayerFaction()
    {
        FactionManager[] factions = FindObjectsByType<FactionManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < factions.Length; i++)
        {
            FactionManager faction = factions[i];
            if (faction != null && faction.IsPlayerFaction)
                return faction;
        }

        for (int i = 0; i < factions.Length; i++)
        {
            FactionManager faction = factions[i];
            if (faction != null && faction.GetComponent<NationAIController>() == null)
                return faction;
        }

        return factions != null && factions.Length > 0 ? factions[0] : null;
    }

    private static void Invoke<T>(System.Action<T> _call) where T : MonoBehaviour
    {
        T target = FindFirstObjectByType<T>(FindObjectsInactive.Include);
        if (target == null)
        {
            Debug.LogWarning($"[MenuButtonAction] 씬에서 {typeof(T).Name} 를 찾을 수 없습니다.");
            return;
        }

        _call(target);
    }
}
