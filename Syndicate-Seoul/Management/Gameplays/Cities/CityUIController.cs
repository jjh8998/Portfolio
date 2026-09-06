using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CityUIController : MonoBehaviour, IEscapeClosable
{
    [Header("Main Panel")]
    public GameObject cityPanel;

    [Header("City Info")]
    [SerializeField] private CEODatabaseSO ceoDatabase;
    public Image ceoIconImage;
    public TMP_Text nameText;
    public TMP_Text ownerText;
    public TMP_Text incomeText;
    public TMP_Text rpText;
    public TMP_Text powerText;

    [Header("Building Slots")]
    public GameObject myBuildingUIParent;
    public List<BuildingUIController> myBuildingUIControllers;

    [Header("Building Panels")]
    public GameObject possibleBuildingPanel;
    [SerializeField] private PossibleUpgradeBuildingUIController possibleUpgradeBuildingUIController;

    [Header("Sub Panels")]
    [SerializeField] private GameObject buildingListPanel;
    [SerializeField] private GameObject citySharePanel;
    [SerializeField] private GameObject corporateAssociationPurchasePanel;

    [Header("Sub Panel Controllers")]
    [SerializeField] private CityShareUIController cityShareUIController;
    [SerializeField] private CorporateAssociationPurchaseUIController corporateAssociationPurchaseUIController;

    [Header("Buttons")]
    [SerializeField] private Button buildingListButton;
    [SerializeField] private Button cityShareButton;
    [SerializeField] private Button corporateAssociationButton;
    [SerializeField] private Button diplomacyActionButton;
    [SerializeField] private Button employeeDeployButton;
    [SerializeField] private Button warButton;
    [SerializeField] private Button closeButton;

    [Header("External UI")]
    [SerializeField] private DiplomacyActionMenuUIController diplomacyActionMenu;
    [SerializeField] private WarManager warManager;
    [SerializeField] private ScavengerWarningPopUpUIController scavengerWarningPopup;

    [Header("Faction")]
    [SerializeField]
    private FactionManager playerFac; // 언젠가는 바꿀꺼

    private Sprite robotImage;
    private static readonly Color powerWarningColor = new Color(1f, 0.55f, 0f);

    private Color defaultPowerTextColor;
    private bool hasDefaultPowerTextColor;
    private CityScript selectedCity;

    public bool IsOpen => cityPanel != null && cityPanel.activeSelf;

    private void Awake()
    {
        if (buildingListPanel == null)
            buildingListPanel = myBuildingUIParent;

        if (citySharePanel == null && cityShareUIController != null)
            citySharePanel = cityShareUIController.gameObject;

        if (corporateAssociationPurchasePanel == null && corporateAssociationPurchaseUIController != null)
            corporateAssociationPurchasePanel = corporateAssociationPurchaseUIController.gameObject;

        BindSubPanelButtons();

        if (scavengerWarningPopup != null)
        {
            scavengerWarningPopup.Confirmed -= OnScavengerBattleConfirmed;
            scavengerWarningPopup.Confirmed += OnScavengerBattleConfirmed;
        }
    }

    private void OnEnable()
    {
        if (CityOwnershipManager.instance != null)
            CityOwnershipManager.instance.AnyCityOwnerChanged += OnCityOwnerChanged;

        if (playerFac != null)
            playerFac.ResearchStateChanged += OnPlayerResearchStateChanged;

        if (corporateAssociationPurchaseUIController != null)
        {
            corporateAssociationPurchaseUIController.Purchased -= RefreshCitySharePanel;
            corporateAssociationPurchaseUIController.Purchased += RefreshCitySharePanel;
        }
    }

    private void OnDisable()
    {
        if (CityOwnershipManager.instance != null)
            CityOwnershipManager.instance.AnyCityOwnerChanged -= OnCityOwnerChanged;

        if (playerFac != null)
            playerFac.ResearchStateChanged -= OnPlayerResearchStateChanged;

        if (corporateAssociationPurchaseUIController != null)
            corporateAssociationPurchaseUIController.Purchased -= RefreshCitySharePanel;
    }

    private void OnDestroy()
    {
        UnbindSubPanelButtons();

        if (scavengerWarningPopup != null)
            scavengerWarningPopup.Confirmed -= OnScavengerBattleConfirmed;
    }

    private void OnCityOwnerChanged(CityScript _city, FactionManager _oldOwner, FactionManager _newOwner)
    {
        if (selectedCity == null) return;
        if (!ReferenceEquals(_city, selectedCity)) return;

        RefreshSelectedCityUI();
    }

    private void OnPlayerResearchStateChanged()
    {
        if (selectedCity == null || selectedCity.cityData == null || selectedCity.cityData.owner != playerFac)
            return;

        RefreshSelectedCityUI();
    }

    public void OnClickDeckButton()
    {
        var deckBuilder = DeckBuilderUI.Instance;

        if (deckBuilder == null)
        {
            // Instance가 없다면 비활성화된 오브젝트 중에서 찾음 (유니티 2023+ 기준 FindFirstObjectByType 사용 권장)
            deckBuilder = Object.FindAnyObjectByType<DeckBuilderUI>(FindObjectsInactive.Include);
        }

        if (deckBuilder != null)
        {
            deckBuilder.Open(playerFac);
            if (TutorialManager.Instance != null)
                TutorialManager.Instance.NotifyCondition(TutorialCondition.OpenDeckBuilder);
        }
        else
        {
            Debug.LogError("CityUIController: DeckBuilderUI를 씬 어디에서도 찾을 수 없습니다.");
        }
    }
    private void PopulateBuildingUIControllers()
    {
        if (myBuildingUIParent == null) return;

        var comps = myBuildingUIParent.GetComponentsInChildren<BuildingUIController>(true);
        myBuildingUIControllers = new List<BuildingUIController>(comps.Length);

        for (int i = 0; i < comps.Length; i++)
        {
            BuildingUIController ui = comps[i];
            if (ui == null) continue;

            ui.SetBuildingIndex(i);
            myBuildingUIControllers.Add(ui);
        }
    }

    private void UpdateCEOIconImage(FactionManager _owner)
    {
        if (ceoIconImage == null) return;

        if (_owner == null)
        {
            ceoIconImage.sprite = null;
            ceoIconImage.enabled = false;
            return;
        }

        if (_owner.IsPlayerFaction)
        {
            PlayerPortraitDatabaseSO portraitDatabase = PlayerPortraitDatabaseSO.Load();
            Sprite playerSprite = portraitDatabase != null ? portraitDatabase.GetSprite(_owner.portraitId) : null;
            ceoIconImage.sprite = playerSprite != null ? playerSprite : (robotImage != null ? robotImage : (robotImage = Resources.Load<Sprite>("Images/CEOIcons/RobotIcon_Image")));
            ceoIconImage.enabled = ceoIconImage.sprite != null;
            return;
        }

        Sprite sprite = null;

        if (!string.IsNullOrWhiteSpace(_owner.ceoId))
        {
            if (ceoDatabase == null)
                ceoDatabase = ScriptableObject.CreateInstance<CEODatabaseSO>();

            CEOData ceo = ceoDatabase.GetCEOByID(_owner.ceoId);
            if (ceo != null)
                sprite = ceo.iconSprite;
        }

        ceoIconImage.sprite = sprite != null ? sprite : (robotImage != null ? robotImage : (robotImage = Resources.Load<Sprite>("Images/CEOIcons/RobotIcon_Image")));
        ceoIconImage.enabled = ceoIconImage.sprite != null;
    }

    public void RefreshSelectedCityUI()
    {
        if (selectedCity == null || selectedCity.cityData == null) return;

        RefreshSelectedCityInfo();
        RefreshBuildingSlotUIs();
        RefreshOpenedSubPanels();
        ManagementUIDesignSystem.ApplyNow();

        if (possibleBuildingPanel != null && possibleBuildingPanel.activeSelf && possibleUpgradeBuildingUIController != null)
            possibleUpgradeBuildingUIController.RefreshUpgradeButtonStates();
    }

    public void OpenBuildingListPanel()
    {
        SetCitySubPanel(true);
    }

    public void OpenCitySharePanel()
    {
        SetCitySubPanel(false);

        if (TutorialManager.Instance != null)
            TutorialManager.Instance.NotifyCondition(TutorialCondition.OpenCitySharePanel);

        if (selectedCity != null)
        {
            if (cityShareUIController != null)
                cityShareUIController.ShowCityShares(selectedCity);
        }
        else
        {
            if (cityShareUIController != null)
                cityShareUIController.Clear();
        }

        if (cityShareUIController != null)
            cityShareUIController.PlaySharePieOpenEffect();
    }

    public void OpenCorporateAssociationPurchasePanel()
    {
        if (selectedCity == null)
        {
            Debug.LogWarning("CityUIController: 선택된 도시가 없어 기업협회 구매 패널을 열 수 없습니다.");
            CloseCorporateAssociationPurchasePanel();
            return;
        }

        if (corporateAssociationPurchasePanel != null)
            corporateAssociationPurchasePanel.SetActive(true);

        if (corporateAssociationPurchaseUIController != null)
        {
            corporateAssociationPurchaseUIController.SetPlayerFaction(playerFac);
            corporateAssociationPurchaseUIController.Show(selectedCity);
        }

        if (TutorialManager.Instance != null)
            TutorialManager.Instance.NotifyCondition(TutorialCondition.OpenCorporateAssociationPurchasePanel);
    }

    public void OpenDiplomacyActionMenu()
    {
        if (selectedCity == null || selectedCity.cityData == null)
        {
            Debug.LogWarning("CityUIController: 선택된 도시가 없습니다.");
            return;
        }

        FactionManager targetFaction = selectedCity.cityData.owner;
        if (targetFaction == null)
        {
            Debug.LogWarning("CityUIController: 선택된 도시의 소유자가 없습니다.");
            return;
        }

        if (targetFaction == playerFac)
        {
            Debug.LogWarning("CityUIController: 플레이어 자신의 도시에는 외교 메뉴를 열 수 없습니다.");
            return;
        }

        if (diplomacyActionMenu == null)
            diplomacyActionMenu = Object.FindAnyObjectByType<DiplomacyActionMenuUIController>(FindObjectsInactive.Include);

        if (diplomacyActionMenu == null)
        {
            Debug.LogError("CityUIController: DiplomacyActionMenuUIController를 찾을 수 없습니다.");
            return;
        }

        diplomacyActionMenu.Toggle(selectedCity, targetFaction);
    }

    private void OnClickEmployeeDeployButton()
    {
        if (selectedCity == null || selectedCity.cityData == null)
        {
            Debug.LogWarning("CityUIController: 선택된 도시가 없습니다.");
            return;
        }

        if (!ResolvePlayerFaction())
        {
            Debug.LogWarning("CityUIController: 플레이어 세력을 찾을 수 없습니다.");
            return;
        }

        EmployeeManagementUIController employeeManagementUIController = Object.FindAnyObjectByType<EmployeeManagementUIController>(FindObjectsInactive.Include);
        if (employeeManagementUIController == null)
        {
            Debug.LogError("CityUIController: EmployeeManagementUIController를 찾을 수 없습니다.");
            return;
        }

        Debug.Log($"CityUIController: 직원 배치 패널을 엽니다. city={GetCityName(selectedCity)}");
        employeeManagementUIController.OpenAssignmentPanel(playerFac, selectedCity);
    }

    private void OnClickWarButton()
    {
        if (selectedCity == null || selectedCity.cityData == null)
        {
            Debug.LogWarning("CityUIController: 선택된 도시가 없습니다.");
            return;
        }

        FactionManager targetFaction = selectedCity.cityData.owner;
        if (targetFaction == null)
        {
            Debug.LogWarning("CityUIController: 선택된 도시의 소유자가 없습니다.");
            return;
        }

        if (!ResolvePlayerFaction())
        {
            Debug.LogWarning("CityUIController: 플레이어 세력을 찾을 수 없습니다.");
            return;
        }

        if (targetFaction == playerFac)
        {
            Debug.LogWarning("CityUIController: 플레이어 자신의 도시에는 전쟁을 선포할 수 없습니다.");
            return;
        }

        DeckBuilderUI deckBuilder = DeckBuilderUI.Instance
            ?? Object.FindAnyObjectByType<DeckBuilderUI>(FindObjectsInactive.Include);

        if (deckBuilder == null)
        {
            Debug.LogError("CityUIController: DeckBuilderUI를 찾을 수 없습니다.");
            return;
        }

        if (warManager == null)
            warManager = Object.FindAnyObjectByType<WarManager>(FindObjectsInactive.Include);

        if (warManager == null)
        {
            Debug.LogError("CityUIController: WarManager를 찾을 수 없습니다.");
            return;
        }

        deckBuilder.OpenForWar(playerFac, selectedCity, warManager);
    }

    private void OnScavengerSlotClicked(CityScript _city, int _slotIndex)
    {
        if (_city == null || _slotIndex < 0)
            return;

        if (!_city.IsSlotScavengerLocked(_slotIndex))
            return;

        if (scavengerWarningPopup == null)
        {
            Debug.LogError("CityUIController: ScavengerWarningPopUpUIController가 연결되지 않았습니다.");
            return;
        }

        scavengerWarningPopup.Open(_city, _slotIndex);
    }

    private void OnScavengerBattleConfirmed(CityScript _city, int _slotIndex)
    {
        if (_city == null || _slotIndex < 0)
            return;

        if (!_city.IsSlotScavengerLocked(_slotIndex))
            return;

        if (!ResolvePlayerFaction())
        {
            Debug.LogWarning("CityUIController: 플레이어 세력을 찾을 수 없습니다.");
            return;
        }

        DeckBuilderUI deckBuilder = DeckBuilderUI.Instance
            ?? Object.FindAnyObjectByType<DeckBuilderUI>(FindObjectsInactive.Include);

        if (deckBuilder == null)
        {
            Debug.LogError("CityUIController: DeckBuilderUI를 찾을 수 없습니다.");
            return;
        }

        if (warManager == null)
            warManager = Object.FindAnyObjectByType<WarManager>(FindObjectsInactive.Include);

        if (warManager == null)
        {
            Debug.LogError("CityUIController: WarManager를 찾을 수 없습니다.");
            return;
        }

        deckBuilder.OpenForScavenger(playerFac, _city, _slotIndex, warManager);
    }

    public void CloseCorporateAssociationPurchasePanel()
    {
        if (corporateAssociationPurchaseUIController != null)
            corporateAssociationPurchaseUIController.Close();

        if (corporateAssociationPurchasePanel != null)
            corporateAssociationPurchasePanel.SetActive(false);
    }

    #region Open/Close

    public void OpenCityPanel(CityScript _citySc)
    {
        if (_citySc == null || _citySc.cityData == null) return;

        selectedCity = _citySc;
        PopulateBuildingUIControllers();
        CityData cityData = _citySc.cityData;

        cityPanel.SetActive(true);
        RefreshSelectedCityInfo();

        if (TutorialManager.Instance != null)
        {
            if (cityData.owner == playerFac)
                TutorialManager.Instance.NotifyCondition(TutorialCondition.SelectCity);
            else if (cityData.owner != null)
                TutorialManager.Instance.NotifyCondition(TutorialCondition.AttackCity);
        }

        OpenBuildingListPanel();
        ManagementUIDesignSystem.ApplyNow();
        RefreshBuildingSlotUIs();
        RefreshOpenPossibleBuildingPanel();
    }

    private void RefreshSelectedCityInfo()
    {
        if (selectedCity == null || selectedCity.cityData == null)
            return;

        CityData cityData = selectedCity.cityData;
        CityResourceSnapshot snapshot = ManagementResourceCalculator.CalculateCity(selectedCity);

        nameText.text = CityDisplayNameUtility.ToKoreanDisplayName(cityData.cityName);
        ManagementUIDesignSystem.StyleText(nameText);

        incomeText.text = $"CR +{snapshot.creditIncome:N0}";
        ManagementUIDesignSystem.StyleResourceValue(incomeText);

        if (rpText != null)
        {
            rpText.text = $"RP +{snapshot.rpProduction:N0}";
            ManagementUIDesignSystem.StyleResourceValue(rpText);
        }

        ownerText.text = cityData.owner == null
            ? "OWNER  NONE"
            : $"OWNER  {cityData.owner.factionName.ToUpperInvariant()}";
        ManagementUIDesignSystem.StyleText(ownerText);

        UpdateCEOIconImage(cityData.owner);

        if (powerText != null)
        {
            powerText.text = $"PWR {snapshot.powerProduction} / {snapshot.powerConsumption}";
            ManagementUIDesignSystem.StyleResourceValue(powerText);
            ApplyPowerTextColor(snapshot.powerProduction, snapshot.powerConsumption);
        }

        RefreshWarButtonState();
    }

    private void RefreshBuildingSlotUIs()
    {
        if (selectedCity == null || myBuildingUIControllers == null)
            return;

        if (myBuildingUIControllers != null)
        {
            int maxSlotCount = selectedCity.GetMaxBuildingSlotCount();
            int availableSlotCount = selectedCity.GetAvailableBuildingSlotCount();

            for (int i = 0; i < myBuildingUIControllers.Count; i++)
            {
                BuildingUIController ui = myBuildingUIControllers[i];
                if (ui == null) continue;

                if (i < maxSlotCount)
                {
                    ui.gameObject.SetActive(true);
                    bool scavengerLocked = selectedCity.IsSlotScavengerLocked(i);
                    ui.OnScavengerSlotClicked = OnScavengerSlotClicked;
                    ui.UpdateSelectedCity(selectedCity, playerFac, i);
                    // 스캐빈저 점거 슬롯은 연구 잠금보다 우선해서 표시한다.
                    ui.SetLocked(!scavengerLocked && i >= availableSlotCount);
                    ui.SetScavengerLocked(scavengerLocked);
                }
                else
                {
                    ui.ClearSelectedCity();
                    ui.gameObject.SetActive(false);
                }
            }
        }
    }

    private void RefreshOpenedSubPanels()
    {
        if (selectedCity == null)
            return;

        if (cityShareUIController != null && citySharePanel != null && citySharePanel.activeSelf)
            cityShareUIController.ShowCityShares(selectedCity);

        if (corporateAssociationPurchaseUIController != null
            && corporateAssociationPurchasePanel != null
            && corporateAssociationPurchasePanel.activeSelf)
        {
            corporateAssociationPurchaseUIController.SetPlayerFaction(playerFac);
            corporateAssociationPurchaseUIController.Show(selectedCity);
        }
    }

    private void RefreshOpenPossibleBuildingPanel()
    {
        if (possibleBuildingPanel == null
            || !possibleBuildingPanel.activeSelf
            || possibleUpgradeBuildingUIController == null
            || selectedCity == null)
        {
            return;
        }

        int index = possibleUpgradeBuildingUIController.BuildingIndex;
        if (index < 0 || index >= selectedCity.GetMaxBuildingSlotCount())
        {
            ClosePossibleBuildingPanel();
            return;
        }

        possibleUpgradeBuildingUIController.RefreshForCity(selectedCity, playerFac);
    }


    public void CloseCityPanel()
    {
        if (diplomacyActionMenu != null)
            diplomacyActionMenu.Close();

        if (cityPanel != null)
            cityPanel.SetActive(false);

        if (buildingListPanel != null)
            buildingListPanel.SetActive(false);

        if (citySharePanel != null)
            citySharePanel.SetActive(false);

        if (corporateAssociationPurchasePanel != null)
            corporateAssociationPurchasePanel.SetActive(false);

        if (buildingListButton != null)
            buildingListButton.interactable = true;

        if (cityShareButton != null)
            cityShareButton.interactable = true;
        selectedCity = null;

        if (cityShareUIController != null)
            cityShareUIController.Clear();

        if (corporateAssociationPurchaseUIController != null)
            corporateAssociationPurchaseUIController.Clear();

        if (myBuildingUIControllers != null)
        {
            foreach (BuildingUIController ui in myBuildingUIControllers)
            {
                if (ui != null)
                    ui.ClearSelectedCity();
            }
        }
    }

    public bool CloseByEscape()
    {
        if (diplomacyActionMenu == null)
            diplomacyActionMenu = Object.FindAnyObjectByType<DiplomacyActionMenuUIController>(FindObjectsInactive.Include);

        if (diplomacyActionMenu != null && diplomacyActionMenu.IsOpen)
        {
            diplomacyActionMenu.Close();
            return true;
        }

        if (possibleBuildingPanel != null && possibleBuildingPanel.activeSelf)
        {
            ClosePossibleBuildingPanel();
            return true;
        }

        if (corporateAssociationPurchasePanel != null && corporateAssociationPurchasePanel.activeSelf)
        {
            CloseCorporateAssociationPurchasePanel();
            return true;
        }

        CloseCityPanel();
        return true;
    }

    public void OpenPossibleBuildingPanel()
    {
        possibleBuildingPanel.SetActive(true);

        if (TutorialManager.Instance != null)
            TutorialManager.Instance.NotifyCondition(TutorialCondition.OpenBuildingPanel);
    }

    public void ClosePossibleBuildingPanel()
    {
        possibleBuildingPanel.SetActive(false);
    }

    #endregion

    private void SetCitySubPanel(bool _showBuildingList)
    {
        if (buildingListPanel != null)
            buildingListPanel.SetActive(_showBuildingList);

        if (citySharePanel != null)
            citySharePanel.SetActive(!_showBuildingList);

        UpdateSubPanelButtonState(_showBuildingList);
    }

    private void UpdateSubPanelButtonState(bool showBuildingList)
    {
        if (buildingListButton != null)
            buildingListButton.interactable = true;

        if (cityShareButton != null)
            cityShareButton.interactable = true;
    }

    private void RefreshCitySharePanel()
    {
        if (selectedCity == null)
            return;

        if (cityShareUIController != null && citySharePanel != null && citySharePanel.activeSelf)
            cityShareUIController.ShowCityShares(selectedCity);

        if (corporateAssociationPurchaseUIController != null
            && corporateAssociationPurchasePanel != null
            && corporateAssociationPurchasePanel.activeSelf)
        {
            corporateAssociationPurchaseUIController.SetPlayerFaction(playerFac);
            corporateAssociationPurchaseUIController.Show(selectedCity);
        }
    }

    private void BindSubPanelButtons()
    {
        if (buildingListButton != null)
        {
            buildingListButton.onClick.RemoveListener(OpenBuildingListPanel);
            buildingListButton.onClick.AddListener(OpenBuildingListPanel);
        }

        if (cityShareButton != null)
        {
            cityShareButton.onClick.RemoveListener(OpenCitySharePanel);
            cityShareButton.onClick.AddListener(OpenCitySharePanel);
        }

        if (corporateAssociationButton != null)
        {
            corporateAssociationButton.onClick.RemoveListener(OpenCorporateAssociationPurchasePanel);
            corporateAssociationButton.onClick.AddListener(OpenCorporateAssociationPurchasePanel);
        }

        if (diplomacyActionButton != null)
        {
            diplomacyActionButton.onClick.RemoveListener(OpenDiplomacyActionMenu);
            diplomacyActionButton.onClick.AddListener(OpenDiplomacyActionMenu);
        }

        if (employeeDeployButton != null)
        {
            employeeDeployButton.onClick.RemoveListener(OnClickEmployeeDeployButton);
            employeeDeployButton.onClick.AddListener(OnClickEmployeeDeployButton);
        }

        if (warButton != null)
        {
            warButton.onClick.RemoveListener(OnClickWarButton);
            warButton.onClick.AddListener(OnClickWarButton);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(CloseCityPanel);
            closeButton.onClick.AddListener(CloseCityPanel);
        }
    }

    private void UnbindSubPanelButtons()
    {
        if (buildingListButton != null)
            buildingListButton.onClick.RemoveListener(OpenBuildingListPanel);

        if (cityShareButton != null)
            cityShareButton.onClick.RemoveListener(OpenCitySharePanel);

        if (corporateAssociationButton != null)
            corporateAssociationButton.onClick.RemoveListener(OpenCorporateAssociationPurchasePanel);

        if (diplomacyActionButton != null)
            diplomacyActionButton.onClick.RemoveListener(OpenDiplomacyActionMenu);

        if (employeeDeployButton != null)
            employeeDeployButton.onClick.RemoveListener(OnClickEmployeeDeployButton);

        if (warButton != null)
            warButton.onClick.RemoveListener(OnClickWarButton);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(CloseCityPanel);
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

    private void RefreshWarButtonState()
    {
        if (warButton == null)
            return;

        FactionManager targetFaction = selectedCity != null && selectedCity.cityData != null
            ? selectedCity.cityData.owner
            : null;

        bool canClickWar = selectedCity != null
            && selectedCity.cityData != null
            && targetFaction != null
            && ResolvePlayerFaction()
            && targetFaction != playerFac;

        warButton.interactable = canClickWar;
    }

    private void ApplyPowerTextColor(int _production, int _consumption)
    {
        if (powerText == null)
            return;

        if (!hasDefaultPowerTextColor)
        {
            defaultPowerTextColor = powerText.color;
            hasDefaultPowerTextColor = true;
        }

        if (_consumption <= 0 || _production >= _consumption)
        {
            powerText.color = defaultPowerTextColor;
            return;
        }

        float shortageRatio = (float)(_consumption - _production) / _consumption;
        powerText.color = shortageRatio > 0.5f ? Color.red : powerWarningColor;
    }

    private string GetCityName(CityScript city)
    {
        return city != null && city.cityData != null
            ? CityDisplayNameUtility.ToKoreanDisplayName(city.cityData.cityName)
            : "None";
    }

}
