using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 선택한 건물의 업그레이드 UI를 구성하고, 업그레이드 및 긴급 수주 입력을 처리하는 스크립트.
public class PossibleUpgradeBuildingUIController : MonoBehaviour
{
    private const string NullBuildingLabel = "(null)";
    private const string UnnamedBuildingLabel = "Unnamed Building";
    private const string BuildingNameTextObjectName = "BuildingName_Text";
    private const string NeedCreditTextObjectName = "NeedCredit_Text";
    private const string BuildingDescTextObjectName = "BuildingDesc_Text";
    private const string NeedPowerTextObjectName = "NeedPower_Text";
    private const string NeedLabelTextObjectName = "Need_Text";
    private const string UpgradeTargetRootName = "UpgradeTargetPreview";
    private const string UpgradeTargetPanelName = "UpgradeTargetPreviewPanel";
    private const string UpgradeTargetPreviewName = "UpgradeTargetHologram";
    private const string UpgradeTargetTitleName = "UpgradeTargetTitle_Text";
    private const string UpgradeTargetDescriptionName = "UpgradeTargetDesc_Text";
    private const string UpgradeTargetStatusName = "UpgradeTargetStatus_Text";
    private const string UpgradeTargetCostName = "UpgradeTargetCost_Text";
    private const string UpgradeTargetRequirementName = "UpgradeTargetRequirement_Text";
    private const string UpgradeTargetIncomeName = "UpgradeTargetIncome_Text";
    private const string UpgradeTargetRPName = "UpgradeTargetRP_Text";
    private const string UpgradeTargetPowerName = "UpgradeTargetPower_Text";
    private const string UpgradeEmptyTextName = "UpgradeEmpty_Text";
    private const string UpgradeHeaderTextObjectName = "Header_Text";
    private const string UpgradeHeaderText = "// BUILD / UPGRADE BLUEPRINTS";
    private const string ConfirmBuildButtonAutoBindName = "ConfirmBuildButton";
    private const string DefaultCategoryIconMapResourcePath = "Databases/BuildingCategoryIconMap";

    [Header("Selection State")]
    [SerializeField] private CityScript selectedCity;
    [SerializeField] private BuildingData myBuilding;
    [SerializeField] private int buildingIndex;
    [SerializeField] private FactionManager playerFac;

    [Header("Upgrade Option List")]
    public Button upgradeButtonPrefab;
    public Transform upgradeButtonsParent;
    public TMP_Text myBuildingText;

    private List<Button> activeUpgradeButtons = new List<Button>();
    private List<BuildingData> visibleUpgradeOptions = new List<BuildingData>();

    [Header("External Controllers")]
    [SerializeField] private CityUIController cityUIController;
    [SerializeField] private BuildingActionButtonUIController buildingActionButtonUIController;

    [Header("Selected Building Detail")]
    [SerializeField] private BuildingDetailUIController buildingDetailUIController;

    [Header("Upgrade Target Detail")]
    [SerializeField] private RectTransform upgradeTargetPanel;
    [SerializeField] private BuildingHologramPreviewUI upgradeTargetPreview;
    [SerializeField] private TMP_Text upgradeTargetTitleText;
    [SerializeField] private TMP_Text upgradeTargetDescriptionText;
    [SerializeField] private TMP_Text upgradeTargetStatusText;
    [SerializeField] private TMP_Text upgradeTargetCostText;
    [SerializeField] private TMP_Text upgradeTargetRequirementText;
    [SerializeField] private TMP_Text upgradeTargetIncomeText;
    [SerializeField] private TMP_Text upgradeTargetRPText;
    [SerializeField] private TMP_Text upgradeTargetPowerText;

    [Header("Confirm Build Button")]
    [SerializeField] private Button confirmBuildButton;

    [Header("Upgrade Button Colors")]
    [SerializeField] private BuildingCategoryIconMapSO categoryIconMap;
    [SerializeField] private Color normalUpgradeButtonColor = new Color32(0x03, 0x0F, 0x1C, 0xE0);
    [SerializeField] private Color selectedUpgradeButtonColor = new Color32(0x05, 0x1C, 0x2D, 0xF0);
    [SerializeField] private Color disabledUpgradeButtonColor = new Color32(0x03, 0x0F, 0x1C, 0x73);

    private TMP_Text upgradeEmptyText;
    private int selectedUpgradeOptionIndex = -1;

    // 상위 UI 참조를 초기화하고 긴급 수주 버튼 이벤트를 연결한다.
    private void Awake()
    {
        if (cityUIController == null)
            cityUIController = GetComponentInParent<CityUIController>();

        ResolveCategoryIconMap();
        ResolveBuildingActionButtonUIController();
        ResolveBuildingDetailUIController();
    }

    private void OnDestroy()
    {
        if (confirmBuildButton != null)
            confirmBuildButton.onClick.RemoveListener(OnClickConfirmBuild);
    }

    private void OnEnable()
    {
        ResolveCategoryIconMap();
        ResolveBuildingActionButtonUIController();
        BindSceneReferences();
        RefreshUpgradeButtonStates();
        RefreshConfirmBuildButtonUI();
    }

    private void LateUpdate()
    {
        RefreshUpgradeButtonStates();
    }

    // 선택한 업그레이드 항목을 검사한 뒤 실제 업그레이드를 요청한다.
    public void OnclickedBuild(int optionIndex)
    {
        RebuildVisibleUpgradeOptions();

        if (visibleUpgradeOptions.Count == 0)
        {
            Debug.LogWarning("UpgradeBuildingUIController : no available upgrades");
            return;
        }

        if (optionIndex < 0 || optionIndex >= visibleUpgradeOptions.Count)
        {
            Debug.LogWarning("UpgradeBuildingUIController : invalid upgrade choice");
            return;
        }

        selectedUpgradeOptionIndex = optionIndex;
        BuildingData selectedUpgrade = visibleUpgradeOptions[optionIndex];
        BuildingConstructionValidationResult validation = ValidateUpgrade(selectedUpgrade);
        if (!validation.canBuild)
        {
            Debug.LogWarning("UpgradeBuildingUIController : " + validation.message);
            return;
        }

        bool success = selectedCity.BuildBuilding(buildingIndex, selectedUpgrade);

        if (!success)
        {
            Debug.LogWarning("UpgradeBuildingUIController : failed to build");
            return;
        }

        if (TutorialManager.Instance != null)
            TutorialManager.Instance.NotifyCondition(TutorialCondition.BuildBuilding);

        if (cityUIController == null)
            cityUIController = GetComponentInParent<CityUIController>();

        if (cityUIController != null)
        {
            cityUIController.ClosePossibleBuildingPanel();
            cityUIController.RefreshSelectedCityUI();
        }
    }

    // 상위 도시 UI 컨트롤러를 외부에서 연결한다.
    public void AssignCityController(CityUIController controller)
    {
        cityUIController = controller;
    }

    public int BuildingIndex
    {
        get { return buildingIndex; }
    }

    public void RefreshForCity(CityScript city, FactionManager playerFaction)
    {
        BuildingInstance buildingInstance = city != null ? city.GetBuildingInstance(buildingIndex) : null;
        SetUpgradeBuildings(city, buildingInstance != null ? buildingInstance.data : null, buildingIndex, playerFaction);
    }

    // 현재 건물 기준으로 업그레이드 버튼 목록과 긴급 수주 UI를 갱신한다.
    public void SetUpgradeBuildings(CityScript _city, BuildingData _building, int _index, FactionManager _playerFac)
    {
        if (cityUIController == null)
            cityUIController = GetComponentInParent<CityUIController>();

        selectedCity = _city;
        playerFac = _playerFac;
        buildingIndex = _index;
        myBuilding = _building;

        BuildingInstance selectedBuilding = GetSelectedBuildingInstance();
        if (selectedBuilding != null)
            myBuilding = selectedBuilding.data;

        RebuildVisibleUpgradeOptions();
        selectedUpgradeOptionIndex = ShouldAutoSelectFirstOption() ? 0 : -1;
        BindSceneReferences();
        RefreshSelectedBuildingDetail(selectedBuilding);
        ClearUpgradeButtons();
        RefreshUpgradeListVisibility();

        if (ShouldShowBlueprintList() && visibleUpgradeOptions.Count > 0 && upgradeButtonPrefab != null && upgradeButtonsParent != null)
        {
            for (int i = 0; i < visibleUpgradeOptions.Count; i++)
            {
                BuildingData building = visibleUpgradeOptions[i];
                Button newButton = Instantiate(upgradeButtonPrefab, upgradeButtonsParent);
                newButton.gameObject.name = $"BuildingBtn_{i}";
                bool canExecuteUpgrade = CanExecuteUpgrade(building);
                SetUpgradeButtonTexts(newButton, building, canExecuteUpgrade);
                LayoutUpgradeButton(newButton);
                newButton.interactable = canExecuteUpgrade;
                ApplyUpgradeButtonVisualState(newButton, building, canExecuteUpgrade, i == selectedUpgradeOptionIndex);
                newButton.onClick = new Button.ButtonClickedEvent();

                int index = i;
                newButton.onClick.AddListener(() => SelectUpgradeOption(index));
                activeUpgradeButtons.Add(newButton);
            }
        }

        RefreshUpgradeEmptyState();
        RefreshConfirmBuildButtonUI();

        if (myBuildingText != null)
        {
            myBuildingText.text = GetSelectedBuildingTitle(selectedBuilding, GetSelectedUpgradeOption());
            ManagementUIDesignSystem.StyleText(myBuildingText);
        }

        RefreshBuildingActionButtons();
    }

    // 현재 건물에서 표시할 수 있는 업그레이드 후보 목록을 다시 만든다.
    private void RebuildVisibleUpgradeOptions()
    {
        visibleUpgradeOptions.Clear();

        if (IsReadOnlyCity())
            return;

        if (myBuilding == null || myBuilding.nextUpgradeBuildings == null)
            return;

        for (int i = 0; i < myBuilding.nextUpgradeBuildings.Count; i++)
        {
            BuildingData building = myBuilding.nextUpgradeBuildings[i];
            if (building == null)
                continue;

            if (selectedCity == null || selectedCity.cityData == null || selectedCity.cityData.owner == null)
            {
                if (!string.IsNullOrWhiteSpace(building.requiredResearchId))
                    continue;
            }

            visibleUpgradeOptions.Add(building);
        }
    }

    // 이전에 만든 업그레이드 버튼들을 모두 정리한다.
    private void ClearUpgradeButtons()
    {
        HashSet<GameObject> clearedObjects = new HashSet<GameObject>();
        for (int i = 0; i < activeUpgradeButtons.Count; i++)
        {
            if (activeUpgradeButtons[i] == null)
                continue;

            GameObject buttonObject = activeUpgradeButtons[i].gameObject;
            clearedObjects.Add(buttonObject);
            DestroyUpgradeButtonObject(buttonObject);
        }

        activeUpgradeButtons.Clear();

        if (upgradeButtonsParent == null)
            return;

        Button[] staleButtons = upgradeButtonsParent.GetComponentsInChildren<Button>(true);
        for (int i = staleButtons.Length - 1; i >= 0; i--)
        {
            Button staleButton = staleButtons[i];
            if (staleButton != null
                && staleButton.gameObject.name.StartsWith("BuildingBtn_")
                && !clearedObjects.Contains(staleButton.gameObject))
            {
                DestroyUpgradeButtonObject(staleButton.gameObject);
            }
        }
    }

    private static void DestroyUpgradeButtonObject(GameObject buttonObject)
    {
        if (buttonObject == null)
            return;

        buttonObject.SetActive(false);

        if (Application.isPlaying)
            Destroy(buttonObject);
        else
            DestroyImmediate(buttonObject);
    }

    private void SelectUpgradeOption(int optionIndex)
    {
        if (optionIndex < 0 || optionIndex >= visibleUpgradeOptions.Count)
        {
            selectedUpgradeOptionIndex = -1;
        }
        else
        {
            selectedUpgradeOptionIndex = optionIndex;
        }

        BuildingInstance selectedBuilding = GetSelectedBuildingInstance();
        RefreshSelectedBuildingDetail(selectedBuilding);
        RefreshUpgradeListVisibility();
        RefreshUpgradeButtonStates();
        RefreshConfirmBuildButtonUI();
    }

    private void OnClickConfirmBuild()
    {
        OnclickedBuild(selectedUpgradeOptionIndex);
    }

    private BuildingData GetSelectedUpgradeOption()
    {
        if (selectedUpgradeOptionIndex < 0 || selectedUpgradeOptionIndex >= visibleUpgradeOptions.Count)
            return null;

        return visibleUpgradeOptions[selectedUpgradeOptionIndex];
    }

    private bool ShouldAutoSelectFirstOption()
    {
        return !IsCurrentSelectionEmptySlot()
            && visibleUpgradeOptions.Count == 1;
    }

    private bool ShouldShowBlueprintList()
    {
        bool isEmptySlot = IsCurrentSelectionEmptySlot();
        return !IsReadOnlyCity()
            && (isEmptySlot || visibleUpgradeOptions.Count > 1);
    }

    private bool ShouldShowUpgradeTargetPanel()
    {
        return !IsReadOnlyCity()
            && !IsCurrentSelectionEmptySlot()
            && GetSelectedUpgradeOption() != null;
    }

    private bool IsCurrentSelectionEmptySlot()
    {
        BuildingInstance selectedBuilding = GetSelectedBuildingInstance();
        if (selectedBuilding != null)
            return selectedBuilding.IsEmptySlot();

        return myBuilding == null || myBuilding.IsEmptySlot();
    }

    private bool IsReadOnlyCity()
    {
        return selectedCity != null
            && selectedCity.cityData != null
            && selectedCity.cityData.owner != playerFac;
    }

    private BuildingConstructionValidationResult ValidateUpgrade(BuildingData building)
    {
        return BuildingConstructionValidator.Validate(
            selectedCity,
            buildingIndex,
            building,
            playerFac,
            true);
    }

    private bool CanExecuteUpgrade(BuildingData building)
    {
        return ValidateUpgrade(building).canBuild;
    }

    private BuildingConstructionFailReason GetUpgradeFailReason(BuildingData building)
    {
        return ValidateUpgrade(building).failReason;
    }

    private void ApplyUpgradeButtonVisualState(Button button, BuildingData building, bool canExecuteUpgrade, bool isSelected)
    {
        if (button == null)
            return;

        BuildingCategoryIconMapSO.IconMapping mapping = GetCategoryMapping(building);
        Graphic graphic = button.targetGraphic != null ? button.targetGraphic : button.GetComponent<Image>();
        if (graphic != null)
            graphic.color = isSelected
                ? GetMappedButtonColor(mapping, ButtonVisualState.Selected)
                : (canExecuteUpgrade
                    ? GetMappedButtonColor(mapping, ButtonVisualState.Normal)
                    : GetMappedButtonColor(mapping, ButtonVisualState.Disabled));

        TMP_Text[] texts = button.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] == null)
                continue;

            ManagementUIDesignSystem.StyleText(texts[i]);
            Color mappedTextColor = GetMappedButtonTextColor(mapping);

            if (texts[i].gameObject.name == BuildingNameTextObjectName)
                texts[i].color = canExecuteUpgrade
                    ? mappedTextColor
                    : WithAlpha(mappedTextColor, isSelected ? 0.82f : 0.54f);
            else
                texts[i].color = canExecuteUpgrade
                    ? WithAlpha(mappedTextColor, isSelected ? 0.92f : 0.74f)
                    : WithAlpha(mappedTextColor, 0.48f);
        }
    }

    public void RefreshUpgradeButtonStates()
    {
        for (int i = 0; i < activeUpgradeButtons.Count; i++)
        {
            if (i >= visibleUpgradeOptions.Count)
                break;

            Button button = activeUpgradeButtons[i];
            if (button == null)
                continue;

            bool canExecute = CanExecuteUpgrade(visibleUpgradeOptions[i]);
            button.interactable = canExecute;
            ApplyUpgradeButtonVisualState(button, visibleUpgradeOptions[i], canExecute, i == selectedUpgradeOptionIndex);
        }

        RefreshBuildingActionButtons();
        RefreshConfirmBuildButtonUI();
    }

    private void ResolveBuildingActionButtonUIController()
    {
        if (buildingActionButtonUIController == null)
            buildingActionButtonUIController = GetComponentInChildren<BuildingActionButtonUIController>(true);
    }

    private void RefreshBuildingActionButtons()
    {
        ResolveBuildingActionButtonUIController();

        if (buildingActionButtonUIController == null)
            return;

        buildingActionButtonUIController.Refresh(
            selectedCity,
            buildingIndex,
            playerFac,
            myBuilding,
            cityUIController,
            RefreshAfterBuildingAction);
    }

    private void RefreshAfterBuildingAction()
    {
        BuildingInstance selectedBuilding = GetSelectedBuildingInstance();
        if (selectedBuilding != null)
            myBuilding = selectedBuilding.data;

        RebuildVisibleUpgradeOptions();
        if (selectedUpgradeOptionIndex < 0 || selectedUpgradeOptionIndex >= visibleUpgradeOptions.Count)
            selectedUpgradeOptionIndex = ShouldAutoSelectFirstOption() ? 0 : -1;

        RefreshSelectedBuildingDetail(selectedBuilding);
        RefreshUpgradeListVisibility();
        RefreshUpgradeButtonStates();
        RefreshUpgradeEmptyState();
        RefreshConfirmBuildButtonUI();
    }

    private void BindSceneReferences()
    {
        RectTransform panelRect = transform as RectTransform;
        if (panelRect == null)
            return;

        ResolveBuildingDetailUIController();
        BindUpgradeTargetReferences(panelRect);
        BindExistingInfoBlock(panelRect);
        BindUpgradeList(panelRect);
        BindActionButtons(panelRect);
    }

    private void BindUpgradeTargetReferences(RectTransform panelRect)
    {
        RectTransform preferredTargetPanel = FindFirstRect(panelRect, UpgradeTargetRootName, "UpgradeTargetPanel", "UpgradeTarget", "TargetPanel", UpgradeTargetPanelName);
        if (preferredTargetPanel != null)
            upgradeTargetPanel = preferredTargetPanel;
        else if (upgradeTargetPanel == null)
            upgradeTargetPanel = FindFirstRect(panelRect, UpgradeTargetPanelName);

        Transform searchRoot = upgradeTargetPanel != null ? upgradeTargetPanel : panelRect;

        Transform previewTransform = FindFirstDescendant(searchRoot, UpgradeTargetPreviewName, "TargetHologram", "UpgradeHologram", "HologramPreview", "Preview");
        BuildingHologramPreviewUI preferredPreview = previewTransform != null
            ? previewTransform.GetComponent<BuildingHologramPreviewUI>()
            : searchRoot.GetComponentInChildren<BuildingHologramPreviewUI>(true);
        if (preferredPreview != null)
        {
            upgradeTargetPreview = preferredPreview;
        }

        upgradeTargetTitleText = ResolvePreferredText(upgradeTargetTitleText, searchRoot, panelRect, UpgradeTargetTitleName, "Target_Text", "TargetName_Text", "TargetName", "BuildingName_Text");
        upgradeTargetDescriptionText = ResolvePreferredText(upgradeTargetDescriptionText, searchRoot, panelRect, UpgradeTargetDescriptionName, "TargetDesc_Text", "Description_Text", "BuildingDesc_Text");
        upgradeTargetStatusText = ResolvePreferredText(upgradeTargetStatusText, searchRoot, panelRect, UpgradeTargetStatusName, "TargetStatus_Text", "Status_Text");
        upgradeTargetCostText = ResolvePreferredText(upgradeTargetCostText, searchRoot, panelRect, UpgradeTargetCostName, "TargetCost_Text", "Cost_Text");
        upgradeTargetRequirementText = ResolvePreferredText(upgradeTargetRequirementText, searchRoot, panelRect, UpgradeTargetRequirementName, "TargetRequirement_Text", "Requirement_Text");
        upgradeTargetIncomeText = ResolvePreferredText(upgradeTargetIncomeText, searchRoot, panelRect, UpgradeTargetIncomeName, "TargetIncome_Text", "Income_Text");
        upgradeTargetRPText = ResolvePreferredText(upgradeTargetRPText, searchRoot, panelRect, UpgradeTargetRPName, "TargetRP_Text", "RP_Text");
        upgradeTargetPowerText = ResolvePreferredText(upgradeTargetPowerText, searchRoot, panelRect, UpgradeTargetPowerName, "TargetPower_Text", "Power_Text");
    }

    private void BindExistingInfoBlock(RectTransform panelRect)
    {
        RectTransform infoGroup = FindDescendant(transform, "BuildingInfoes") as RectTransform;
        if (infoGroup == null)
            return;

        // Scene-authored TMP alignment/material values are preserved.
    }

    private void BindUpgradeList(RectTransform panelRect)
    {
        RectTransform listRect = GetUpgradeListRect();

        ScrollRect scrollRect = listRect != null ? listRect.GetComponent<ScrollRect>() : null;
        if (scrollRect != null)
        {
            RectTransform viewport = scrollRect.viewport;
            EnsureViewportMask(viewport);
        }

        TMP_Text headerText = listRect != null ? FindChildText(listRect, UpgradeHeaderTextObjectName) : null;
        if (headerText != null)
        {
            headerText.text = UpgradeHeaderText;
        }
    }

    private RectTransform GetUpgradeListRect()
    {
        return upgradeButtonsParent != null
            ? upgradeButtonsParent.GetComponentInParent<ScrollRect>(true)?.transform as RectTransform
            : FindDescendant(transform, "UpgradeListPanel") as RectTransform;
    }

    private static void EnsureViewportMask(RectTransform viewport)
    {
        if (viewport == null)
            return;

        RectMask2D rectMask = viewport.GetComponent<RectMask2D>();
        bool addedMaskComponent = rectMask == null;
        if (rectMask == null)
            rectMask = viewport.gameObject.AddComponent<RectMask2D>();

        Image image = viewport.GetComponent<Image>();
        if (image == null)
        {
            image = viewport.gameObject.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.001f);
            image.raycastTarget = false;
        }

        if (addedMaskComponent && rectMask != null)
            rectMask.enabled = true;
    }

    private void RefreshUpgradeListVisibility()
    {
        bool showTarget = ShouldShowUpgradeTargetPanel();
        RectTransform listRect = GetUpgradeListRect();
        if (listRect != null)
            listRect.gameObject.SetActive(ShouldShowBlueprintList() && !showTarget);

        if (upgradeTargetPanel != null)
            upgradeTargetPanel.gameObject.SetActive(showTarget);
    }

    private void BindActionButtons(RectTransform panelRect)
    {
        ResolveConfirmBuildButton();
    }

    private void RefreshConfirmBuildButtonUI()
    {
        ResolveConfirmBuildButton();

        if (confirmBuildButton == null)
            return;

        BuildingInstance selectedBuilding = GetSelectedBuildingInstance();
        BuildingData selectedOption = GetSelectedUpgradeOption();
        bool isReadOnly = IsReadOnlyCity();
        bool hasSelectedOption = selectedOption != null;
        bool canExecute = !isReadOnly && hasSelectedOption && CanExecuteUpgrade(selectedOption);
        BuildingCategoryIconMapSO.IconMapping mapping = GetCategoryMapping(selectedOption);

        confirmBuildButton.gameObject.SetActive(!isReadOnly && hasSelectedOption);
        confirmBuildButton.interactable = canExecute;
        ApplyConfirmButtonVisualState(confirmBuildButton, mapping, canExecute);

        TMP_Text label = confirmBuildButton.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            if (!hasSelectedOption)
            {
                label.text = "SELECT BLUEPRINT";
            }
            else
            {
                label.text = selectedBuilding == null || selectedBuilding.IsEmptySlot()
                    ? "BUILD"
                    : "UPGRADE";
            }

            ManagementUIDesignSystem.StyleText(label);
            label.color = canExecute
                ? GetMappedButtonTextColor(mapping)
                : WithAlpha(GetMappedButtonTextColor(mapping), 0.52f);
        }
    }

    private void ResolveCategoryIconMap()
    {
        if (categoryIconMap != null)
            return;

        categoryIconMap = Resources.Load<BuildingCategoryIconMapSO>(DefaultCategoryIconMapResourcePath);
    }

    private void ResolveBuildingDetailUIController()
    {
        if (buildingDetailUIController != null)
            return;

        buildingDetailUIController = GetComponentInChildren<BuildingDetailUIController>(true);
    }

    private BuildingCategoryIconMapSO.IconMapping GetCategoryMapping(BuildingData building)
    {
        if (building == null || categoryIconMap == null)
            return null;

        return categoryIconMap.TryGetMapping(building.category, out BuildingCategoryIconMapSO.IconMapping mapping)
            ? mapping
            : null;
    }

    private enum ButtonVisualState
    {
        Normal,
        Selected,
        Disabled
    }

    private Color GetMappedButtonColor(BuildingCategoryIconMapSO.IconMapping mapping, ButtonVisualState state)
    {
        if (mapping == null)
        {
            switch (state)
            {
                case ButtonVisualState.Selected:
                    return selectedUpgradeButtonColor;
                case ButtonVisualState.Disabled:
                    return disabledUpgradeButtonColor;
                default:
                    return normalUpgradeButtonColor;
            }
        }

        switch (state)
        {
            case ButtonVisualState.Selected:
                return mapping.buttonSelectedColor;
            case ButtonVisualState.Disabled:
                return mapping.buttonDisabledColor;
            default:
                return mapping.buttonNormalColor;
        }
    }

    private Color GetMappedButtonTextColor(BuildingCategoryIconMapSO.IconMapping mapping)
    {
        return mapping != null
            ? mapping.buttonTextColor
            : new Color32(0xD5, 0xFB, 0xFF, 0xFF);
    }

    private void ApplyConfirmButtonVisualState(Button button, BuildingCategoryIconMapSO.IconMapping mapping, bool canExecute)
    {
        if (button == null)
            return;

        Graphic graphic = button.targetGraphic != null ? button.targetGraphic : button.GetComponent<Image>();
        if (graphic != null)
            graphic.color = canExecute
                ? GetMappedButtonColor(mapping, ButtonVisualState.Selected)
                : GetMappedButtonColor(mapping, ButtonVisualState.Disabled);
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a *= alpha;
        return color;
    }

    private void ResolveConfirmBuildButton()
    {
        if (confirmBuildButton == null)
        {
            RectTransform confirm = FindDescendant(transform, ConfirmBuildButtonAutoBindName) as RectTransform;
            confirmBuildButton = confirm != null ? confirm.GetComponent<Button>() : null;
        }

        if (confirmBuildButton == null)
            return;

        confirmBuildButton.onClick.RemoveListener(OnClickConfirmBuild);
        confirmBuildButton.onClick.AddListener(OnClickConfirmBuild);
    }

    private void RefreshSelectedBuildingDetail(BuildingInstance selectedBuilding)
    {
        ResolveBuildingDetailUIController();

        BuildingData selectedOption = GetSelectedUpgradeOption();
        BuildingData detailBuilding = GetSelectedPanelBuilding(selectedBuilding, selectedOption);

        if (myBuildingText != null)
        {
            myBuildingText.text = GetSelectedBuildingTitle(selectedBuilding, selectedOption);
            ManagementUIDesignSystem.StyleText(myBuildingText);
        }

        if (buildingDetailUIController != null)
        {
            int bonusIncome = detailBuilding != null ? detailBuilding.bonusIncome : 0;
            int rpOutput = detailBuilding != null ? detailBuilding.rpOutput : 0;
            int powerOutput = detailBuilding != null ? detailBuilding.powerOutput : 0;
            int powerConsumption = detailBuilding != null ? detailBuilding.powerConsumption : 0;

            buildingDetailUIController.Refresh(
                detailBuilding,
                GetSelectedDescriptionLabel(selectedBuilding, selectedOption, detailBuilding),
                GetSelectedStatusLabel(selectedBuilding, selectedOption),
                GetSelectedCostLabel(selectedBuilding, selectedOption),
                GetRequirementLabel(detailBuilding),
                $"CR {bonusIncome:+#;-#;+0}",
                $"RP {rpOutput:+#;-#;+0}",
                GetPowerDeltaLabel(powerOutput, powerConsumption),
                IsReadOnlyCity() ? 0.72f : 1f);
        }

        RefreshUpgradeTargetDetail(selectedBuilding, selectedOption);
    }

    private void RefreshUpgradeTargetDetail(BuildingInstance selectedBuilding, BuildingData selectedOption)
    {
        if (upgradeTargetPanel == null)
            return;

        bool showTarget = ShouldShowUpgradeTargetPanel();
        upgradeTargetPanel.gameObject.SetActive(showTarget);
        if (!showTarget)
        {
            if (upgradeTargetPreview != null)
                upgradeTargetPreview.ClearPreview();
            return;
        }

        if (upgradeTargetPreview != null)
        {
            if (selectedOption != null && selectedOption.LoadHologramPrefab() != null)
                upgradeTargetPreview.Show(selectedOption, CanExecuteUpgrade(selectedOption) ? 1f : 0.62f);
            else
                upgradeTargetPreview.ClearPreview();
        }

        if (upgradeTargetTitleText != null)
        {
            upgradeTargetTitleText.text = GetBuildingNameLabel(selectedOption);
            ManagementUIDesignSystem.StyleText(upgradeTargetTitleText);
        }

        if (upgradeTargetDescriptionText != null)
        {
            upgradeTargetDescriptionText.text = GetBuildingDescriptionLabel(selectedOption);
            ManagementUIDesignSystem.StyleText(upgradeTargetDescriptionText);
        }

        if (upgradeTargetStatusText != null)
        {
            upgradeTargetStatusText.text = GetSelectedOptionStatusLabel(selectedBuilding, selectedOption);
            ManagementUIDesignSystem.StyleText(upgradeTargetStatusText);
        }

        if (upgradeTargetCostText != null)
        {
            upgradeTargetCostText.text = GetSelectedCostLabel(selectedBuilding, selectedOption);
            ManagementUIDesignSystem.StyleText(upgradeTargetCostText);
        }

        if (upgradeTargetRequirementText != null)
        {
            upgradeTargetRequirementText.text = GetRequirementLabel(selectedOption);
            ManagementUIDesignSystem.StyleText(upgradeTargetRequirementText);
        }

        SetMetricText(upgradeTargetIncomeText, $"CR {selectedOption.bonusIncome:+#;-#;+0}");
        SetMetricText(upgradeTargetRPText, $"RP {selectedOption.rpOutput:+#;-#;+0}");
        SetMetricText(upgradeTargetPowerText, GetPowerDeltaLabel(selectedOption.powerOutput, selectedOption.powerConsumption));
    }

    private static void SetMetricText(TMP_Text text, string value)
    {
        if (text == null)
            return;

        text.text = value;
        ManagementUIDesignSystem.StyleResourceValue(text);
    }

    private void RefreshUpgradeEmptyState()
    {
        RefreshUpgradeListVisibility();

        if (upgradeButtonsParent == null)
            return;

        if (!ShouldShowBlueprintList())
        {
            if (upgradeEmptyText != null)
                upgradeEmptyText.gameObject.SetActive(false);
            return;
        }

        if (upgradeEmptyText == null)
        {
            upgradeEmptyText = ResolveText(null, upgradeButtonsParent, UpgradeEmptyTextName);
            if (upgradeEmptyText == null)
                return;
        }

        bool showEmpty = activeUpgradeButtons.Count == 0;
        upgradeEmptyText.gameObject.SetActive(showEmpty);
        if (!showEmpty)
            return;

        BuildingInstance selectedBuilding = GetSelectedBuildingInstance();
        upgradeEmptyText.text = selectedBuilding != null && selectedBuilding.IsUnderConstruction()
            ? "CONSTRUCTION QUEUE ACTIVE\nNO BLUEPRINT AVAILABLE"
            : "NO AVAILABLE BLUEPRINT\nRESEARCH OR RESOURCE REQUIREMENT MAY BE MISSING";
        ManagementUIDesignSystem.StyleText(upgradeEmptyText);
    }

    private void LayoutUpgradeButton(Button button)
    {
        if (button == null)
            return;

        // Keep prefab-authored anchors, pivot, and size. Layout is controlled by
        // the button prefab or scene hierarchy, not reset by the data binder.
        LayoutElement layoutElement = button.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = button.gameObject.AddComponent<LayoutElement>();
            layoutElement.minHeight = 156f;
            layoutElement.preferredHeight = 164f;
            layoutElement.flexibleWidth = 1f;
        }
    }

    private BuildingData GetDetailBuildingForPanel()
    {
        return GetSelectedPanelBuilding(GetSelectedBuildingInstance(), GetSelectedUpgradeOption());
    }

    private BuildingData GetSelectedPanelBuilding(BuildingInstance selectedBuilding, BuildingData selectedOption)
    {
        if ((selectedBuilding == null || selectedBuilding.IsEmptySlot())
            && selectedOption != null
            && !selectedOption.IsEmptySlot())
        {
            return selectedOption;
        }

        if (selectedBuilding != null && selectedBuilding.data != null && !selectedBuilding.data.IsEmptySlot())
            return selectedBuilding.data;

        if (myBuilding != null && !myBuilding.IsEmptySlot())
            return myBuilding;

        return null;
    }

    private string GetSelectedBuildingTitle(BuildingInstance selectedBuilding, BuildingData selectedOption)
    {
        if (selectedOption != null)
        {
            if (selectedBuilding == null || selectedBuilding.IsEmptySlot())
                return GetBuildingNameLabel(selectedOption);

            return GetBuildingNameLabel(selectedBuilding.data);
        }

        if (selectedBuilding == null || selectedBuilding.IsEmptySlot())
            return "EMPTY SLOT";

        string name = GetBuildingNameLabel(selectedBuilding.data);
        if (selectedBuilding.IsUpgrading() && selectedBuilding.upgradeTargetData != null)
            return $"{name}  >  {GetBuildingNameLabel(selectedBuilding.upgradeTargetData)}";

        return name;
    }

    private string GetSelectedDescriptionLabel(BuildingInstance selectedBuilding, BuildingData selectedOption, BuildingData detailBuilding)
    {
        if (selectedOption != null && (selectedBuilding == null || selectedBuilding.IsEmptySlot()))
            return GetBuildingDescriptionLabel(selectedOption);

        if (IsReadOnlyCity())
            return selectedBuilding == null || selectedBuilding.IsEmptySlot()
                ? "This city is not under your control. Building slots are read-only."
                : GetBuildingDescriptionLabel(detailBuilding);

        if (selectedBuilding == null || selectedBuilding.IsEmptySlot())
        {
            if (visibleUpgradeOptions.Count == 0)
                return "No available blueprint for this build slot.";

            return "Select a blueprint to inspect construction details.";
        }

        return GetBuildingDescriptionLabel(detailBuilding);
    }

    private string GetSelectedStatusLabel(BuildingInstance selectedBuilding, BuildingData selectedOption)
    {
        if (IsReadOnlyCity())
            return "STATUS: READ ONLY";

        if (selectedOption != null && (selectedBuilding == null || selectedBuilding.IsEmptySlot()))
            return GetSelectedOptionStatusLabel(selectedBuilding, selectedOption);

        if (selectedBuilding == null || selectedBuilding.IsEmptySlot())
            return visibleUpgradeOptions.Count > 0 ? "STATUS: SELECT BLUEPRINT" : "STATUS: NO BLUEPRINT";

        if (selectedBuilding.IsUpgrading() && selectedBuilding.upgradeTargetData != null)
            return $"STATUS: UPGRADING  {selectedBuilding.remainConstructionDay} DAY";

        if (selectedBuilding.IsUnderConstruction())
            return $"STATUS: CONSTRUCTION  {selectedBuilding.remainConstructionDay} DAY";

        if (selectedBuilding.IsDisabled())
            return "STATUS: OFFLINE";

        return "STATUS: OPERATIONAL";
    }

    private string GetSelectedOptionStatusLabel(BuildingInstance selectedBuilding, BuildingData selectedOption)
    {
        BuildingConstructionFailReason failReason = GetUpgradeFailReason(selectedOption);
        switch (failReason)
        {
            case BuildingConstructionFailReason.None:
                break;
            case BuildingConstructionFailReason.ResearchLocked:
                return "STATUS: RESEARCH LOCKED";
            case BuildingConstructionFailReason.NotEnoughCredit:
                return "STATUS: INSUFFICIENT GEAR";
            case BuildingConstructionFailReason.NotEnoughPower:
                return "STATUS: INSUFFICIENT POWER";
            case BuildingConstructionFailReason.SlotUnderConstruction:
            case BuildingConstructionFailReason.CityUnderConstruction:
                return "STATUS: CONSTRUCTION QUEUE BUSY";
            case BuildingConstructionFailReason.NotOwner:
                return "STATUS: READ ONLY";
            default:
                return "STATUS: UNAVAILABLE";
        }

        return selectedBuilding == null || selectedBuilding.IsEmptySlot()
            ? "STATUS: READY TO BUILD"
            : "STATUS: READY TO UPGRADE";
    }

    private Color GetSelectedStatusColor(BuildingInstance selectedBuilding, BuildingData selectedOption)
    {
        if (IsReadOnlyCity())
            return new Color32(0x5D, 0x91, 0xA4, 0xFF);

        if (selectedOption != null)
            return CanExecuteUpgrade(selectedOption)
                ? new Color32(0x00, 0xE5, 0xFF, 0xFF)
                : new Color32(0xE8, 0xC3, 0x5A, 0xFF);

        if (selectedBuilding == null || selectedBuilding.IsEmptySlot())
            return visibleUpgradeOptions.Count > 0
                ? new Color32(0x00, 0xE5, 0xFF, 0xFF)
                : new Color32(0xE8, 0xC3, 0x5A, 0xFF);

        if (selectedBuilding.IsDisabled())
            return new Color32(0xFF, 0x4B, 0x63, 0xFF);

        if (selectedBuilding.IsUnderConstruction())
            return new Color32(0xE8, 0xC3, 0x5A, 0xFF);

        return new Color32(0x00, 0xE5, 0xFF, 0xFF);
    }

    private string GetSelectedCostLabel(BuildingInstance selectedBuilding, BuildingData selectedOption)
    {
        if (IsReadOnlyCity())
            return "ACTION: VIEW ONLY";

        if (selectedBuilding != null && !selectedBuilding.IsEmptySlot())
        {
            if (selectedOption == null)
                return visibleUpgradeOptions.Count > 0 ? "UPGRADE: SELECT TARGET" : "UPGRADE: NO AVAILABLE TARGET";

            return "UPGRADE TARGET DISPLAYED";
        }

        if (selectedOption == null)
        {
            string emptyAction = selectedBuilding == null || selectedBuilding.IsEmptySlot() ? "BUILD" : "UPGRADE";
            return visibleUpgradeOptions.Count == 0 ? $"{emptyAction}: NO AVAILABLE BLUEPRINT" : "COST: --";
        }

        string action = selectedBuilding == null || selectedBuilding.IsEmptySlot() ? "BUILD" : "UPGRADE";
        return $"{action}: GEAR {selectedOption.constructionCost} / {selectedOption.constructionDay} DAY";
    }

    private static string GetRequirementLabel(BuildingData building)
    {
        if (building == null)
            return "REQ: --";

        return string.IsNullOrWhiteSpace(building.requiredResearchId)
            ? "REQ: NONE"
            : $"REQ: {building.requiredResearchId.ToUpperInvariant()}";
    }

    private static string GetPowerDeltaLabel(int powerOutput, int powerConsumption)
    {
        if (powerOutput > 0 && powerConsumption > 0)
            return $"PWR +{powerOutput}/-{powerConsumption}";

        if (powerOutput > 0)
            return $"PWR +{powerOutput}";

        if (powerConsumption > 0)
            return $"PWR -{powerConsumption}";

        return "PWR +0";
    }

    // 현재 선택된 건물 인스턴스를 인덱스로 조회한다.
    private BuildingInstance GetSelectedBuildingInstance()
    {
        if (selectedCity == null)
            return null;

        return selectedCity.GetBuildingInstance(buildingIndex);
    }

    // 업그레이드 버튼에 건물 이름과 비용 텍스트를 채운다.
    private void SetUpgradeButtonTexts(Button button, BuildingData building, bool canExecuteUpgrade)
    {
        if (button == null)
            return;

        TMP_Text buildingNameText = FindChildText(button.transform, BuildingNameTextObjectName);
        TMP_Text creditText = FindChildText(button.transform, NeedCreditTextObjectName);
        TMP_Text buildingDescText = FindChildText(button.transform, BuildingDescTextObjectName);
        TMP_Text powerText = FindChildText(button.transform, NeedPowerTextObjectName);
        TMP_Text headerText = FindChildText(button.transform, UpgradeHeaderTextObjectName);
        TMP_Text needLabelText = FindChildText(button.transform, NeedLabelTextObjectName);
        BuildingHologramPreviewUI hologramPreview = button.GetComponentInChildren<BuildingHologramPreviewUI>(true);

        EnsureUpgradeButtonContentVisible(button.transform);

        if (headerText != null)
        {
            headerText.text = $"// {GetCategoryLabel(building != null ? building.category : BuildingCategory.Empty)} BLUEPRINT";
            ManagementUIDesignSystem.StyleText(headerText);
        }

        if (buildingNameText != null)
        {
            buildingNameText.text = GetBuildingNameLabel(building);
            ManagementUIDesignSystem.StyleText(buildingNameText);
        }

        if (buildingDescText != null)
        {
            buildingDescText.text = GetBuildingListDescriptionLabel(building);
            ManagementUIDesignSystem.StyleText(buildingDescText);
        }

        if (needLabelText != null)
        {
            needLabelText.text = "BUILD";
            ManagementUIDesignSystem.StyleText(needLabelText);
        }

        if (creditText != null)
        {
            creditText.text = GetCreditLabel(building);
            ManagementUIDesignSystem.StyleResourceValue(creditText);
        }

        if (powerText != null)
        {
            powerText.text = GetPowerLabel(building);
            ManagementUIDesignSystem.StyleResourceValue(powerText);
        }

        if (hologramPreview != null && building != null && !building.IsEmptySlot())
        {
            if (!hologramPreview.gameObject.activeSelf)
                hologramPreview.gameObject.SetActive(true);

            hologramPreview.Show(building, canExecuteUpgrade ? 1f : 0.78f);
        }
        else if (hologramPreview != null)
        {
            hologramPreview.ClearPreview();
        }
    }

    private static void EnsureUpgradeButtonContentVisible(Transform buttonTransform)
    {
        if (buttonTransform == null)
            return;

        SetChildActive(buttonTransform, UpgradeHeaderTextObjectName, true);
        SetChildActive(buttonTransform, BuildingNameTextObjectName, true);
        SetChildActive(buttonTransform, BuildingDescTextObjectName, true);
        SetChildActive(buttonTransform, NeedLabelTextObjectName, true);
        SetChildActive(buttonTransform, NeedCreditTextObjectName, true);
        SetChildActive(buttonTransform, NeedPowerTextObjectName, true);
        SetChildActive(buttonTransform, "HologramPreview", true);
        SetChildActive(buttonTransform, "HologramModelPreview", true);
        SetChildActive(buttonTransform, "TopDivider", true);
        SetChildActive(buttonTransform, "CostDivider", true);
    }

    private static void SetChildActive(Transform parent, string objectName, bool active)
    {
        Transform child = FindDescendant(parent, objectName);
        if (child != null && child.gameObject.activeSelf != active)
            child.gameObject.SetActive(active);
    }

    private static RectTransform FindFirstRect(Transform parent, params string[] objectNames)
    {
        Transform found = FindFirstDescendant(parent, objectNames);
        return found as RectTransform;
    }

    private static Transform FindFirstDescendant(Transform parent, params string[] objectNames)
    {
        if (parent == null || objectNames == null)
            return null;

        for (int i = 0; i < objectNames.Length; i++)
        {
            Transform found = FindDescendant(parent, objectNames[i]);
            if (found != null)
                return found;
        }

        return null;
    }

    private static TMP_Text ResolveText(TMP_Text current, Transform parent, params string[] objectNames)
    {
        if (current != null)
            return current;

        Transform found = FindFirstDescendant(parent, objectNames);
        if (found != null)
            return found.GetComponent<TMP_Text>();

        return null;
    }

    private static TMP_Text ResolveText(TMP_Text current, Transform primaryParent, Transform fallbackParent, params string[] objectNames)
    {
        TMP_Text result = ResolveText(current, primaryParent, objectNames);
        if (result != null || fallbackParent == primaryParent)
            return result;

        return ResolveText(null, fallbackParent, objectNames);
    }

    private static TMP_Text ResolvePreferredText(TMP_Text current, Transform preferredParent, Transform fallbackParent, params string[] objectNames)
    {
        TMP_Text preferred = ResolveText(null, preferredParent, objectNames);
        if (preferred != null)
            return preferred;

        return ResolveText(current, fallbackParent, objectNames);
    }

    private static void CopyTextStyle(TMP_Text source, TMP_Text target)
    {
        if (source == null || target == null)
            return;

        target.font = source.font;
        target.fontSharedMaterial = source.fontSharedMaterial;
    }

    private static Transform FindDescendant(Transform parent, string objectName)
    {
        if (parent == null || string.IsNullOrWhiteSpace(objectName))
            return null;

        if (parent.name == objectName)
            return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform result = FindDescendant(parent.GetChild(i), objectName);
            if (result != null)
                return result;
        }

        return null;
    }

    // 버튼 하위 오브젝트에서 이름으로 TMP 텍스트를 찾는다.
    private static TMP_Text FindChildText(Transform parent, string objectName)
    {
        if (parent == null || string.IsNullOrWhiteSpace(objectName))
            return null;

        TMP_Text[] texts = parent.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];
            if (text != null && text.gameObject.name == objectName)
                return text;
        }

        return null;
    }

    // 건물 이름 표시용 문자열을 안전하게 반환한다.
    private static string GetBuildingNameLabel(BuildingData building)
    {
        if (building == null)
            return NullBuildingLabel;

        return string.IsNullOrWhiteSpace(building.name) ? UnnamedBuildingLabel : building.name.ToUpperInvariant();
    }

    private static string GetBuildingDescriptionLabel(BuildingData building)
    {
        if (building == null)
            return "STRUCTURE DATA MISSING";

        if (HasMeaningfulDescription(building))
            return building.description.Trim();

        return BuildGeneratedBuildingDescription(building);
    }

    private static string GetBuildingListDescriptionLabel(BuildingData building)
    {
        if (building == null)
            return "NO STRUCTURE DATA";

        List<string> parts = new List<string>();
        if (building.bonusIncome != 0)
            parts.Add($"CR {building.bonusIncome:+#;-#;+0}");

        if (building.rpOutput != 0)
            parts.Add($"RP {building.rpOutput:+#;-#;+0}");

        if (building.powerOutput != 0)
            parts.Add($"PWR {building.powerOutput:+#;-#;+0}");

        if (building.powerConsumption != 0)
            parts.Add($"USE {building.powerConsumption}");

        string output = parts.Count > 0 ? string.Join("  /  ", parts) : "NO PASSIVE OUTPUT";
        return $"{GetCategoryLabel(building.category)} STRUCTURE\n{output}";
    }

    private static bool HasMeaningfulDescription(BuildingData building)
    {
        if (building == null || string.IsNullOrWhiteSpace(building.description))
            return false;

        string description = building.description.Trim();
        if (string.Equals(description, building.name, System.StringComparison.OrdinalIgnoreCase))
            return false;

        return !int.TryParse(description, out _);
    }

    private static string BuildGeneratedBuildingDescription(BuildingData building)
    {
        List<string> outputParts = new List<string>();

        if (building.bonusIncome != 0)
            outputParts.Add($"CREDIT {building.bonusIncome:+#;-#;+0} / DAY");

        if (building.rpOutput != 0)
            outputParts.Add($"RESEARCH {building.rpOutput:+#;-#;+0} / DAY");

        if (building.powerOutput != 0)
            outputParts.Add($"POWER {building.powerOutput:+#;-#;+0} / DAY");

        if (building.powerConsumption != 0)
            outputParts.Add($"POWER USE {building.powerConsumption} / DAY");

        string output = outputParts.Count > 0
            ? string.Join("\n", outputParts)
            : "NO PASSIVE OUTPUT";

        return $"{GetCategoryLabel(building.category)} STRUCTURE\n{output}\nBUILD COST GEAR {building.constructionCost} / {building.constructionDay} DAY";
    }

    private static string GetCategoryLabel(BuildingCategory category)
    {
        switch (category)
        {
            case BuildingCategory.Economy:
                return "ECONOMY";
            case BuildingCategory.Power:
                return "POWER";
            case BuildingCategory.Research:
                return "RESEARCH";
            case BuildingCategory.Factory:
                return "FACTORY";
            case BuildingCategory.Support:
                return "SUPPORT";
            case BuildingCategory.Empty:
                return "EMPTY";
            default:
                return "GENERAL";
        }
    }

    // 건물의 골드 표시 문자열을 만든다.
    private static string GetCreditLabel(BuildingData building)
    {
        int constructionCost = building != null ? building.constructionCost : 0;
        return $"GEAR {constructionCost}";
    }

    // 건물의 전력 표시 문자열을 만든다.
    private static string GetPowerLabel(BuildingData building)
    {
        int powerConsumption = building != null ? building.powerConsumption : 0;
        return $"PWR {powerConsumption}";
    }
}
