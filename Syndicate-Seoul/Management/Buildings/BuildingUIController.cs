using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class BuildingUIController : MonoBehaviour
{
    private static readonly Color DisableToggleEnabledColor = new Color32(0x00, 0xE5, 0xFF, 0xFF);
    private static readonly Color DisableToggleDisabledColor = new Color32(0xFF, 0x4B, 0x63, 0xFF);
    private static readonly Color ScavengerSlotBorderColor = new Color32(0xFF, 0x38, 0x55, 0xFF);
    private const string ScavengerPrefabResourcePath = "Buildings/Sca/Scavenger";
    private const string DefaultCategoryIconMapResourcePath = "Databases/BuildingCategoryIconMap";

    private static GameObject cachedScavengerPreviewPrefab;

    private CityScript selectedCity;
    private FactionManager playerFac; // 언젠가는 바꿀꺼
    [SerializeField]
    private BuildingData myBuilding;
    private BuildingInstance myBuildingInstance;
    private int buildingIndex; // 선택된 건물의 인덱스
    private bool isLocked;
    private bool isScavengerLocked;

    /// <summary>스캐빈저 점거 슬롯 클릭 시 호출되는 콜백. (city, slotIndex)</summary>
    public System.Action<CityScript, int> OnScavengerSlotClicked;

    [Header("UI")]
    public CityUIController cityUIController;
    public PossibleUpgradeBuildingUIController possibleUpgradeBuildingUIController;

    public TMP_Text buildingNameText;
    public TMP_Text buildingDescText;
    [SerializeField] private Button buildingButton;
    [SerializeField] private GameObject lockIcon;
    [SerializeField] private TMP_Text lockText;

    [Header("Icon")]
    [SerializeField]
    public Image buildingIconImage;
    [SerializeField] private Image categoryIconImage;
    [SerializeField] private BuildingCategoryIconMapSO categoryIconMap;
    [Header("Hologram Preview")]
    [SerializeField] private BuildingHologramPreviewUI hologramPreview;
    [SerializeField] private UIButtonHoverGlow hoverGlow;
    [SerializeField] private CutCornerPanel slotPanel;
    [SerializeField] private Material normalSlotPanelMaterial;
    [SerializeField] private Material scavengerSlotPanelMaterial;

    [Header("Disable Toggle")]
    [SerializeField] private Button disableToggleButton;

    private bool hasCachedSlotPanelStyle;
    private bool cachedSlotPanelHadBorderOverride;
    private Color cachedSlotPanelBorderColor;
    private Material cachedSlotPanelMaterial;

    private void Awake()
    {
        if (buildingButton == null)
            buildingButton = GetComponent<Button>();

        if (hoverGlow == null)
            hoverGlow = GetComponent<UIButtonHoverGlow>();

        ResolveCategoryIconMap();
        ResolveSlotPanel();
        CacheSlotPanelStyle();
        EnsureButtonHitTarget();

        if (buildingButton != null)
        {
            buildingButton.onClick.RemoveListener(SetUpgradeBuildings);
            buildingButton.onClick.AddListener(SetUpgradeBuildings);
        }

        if (disableToggleButton != null)
        {
            disableToggleButton.onClick.RemoveListener(OnClickDisableToggle);
            disableToggleButton.onClick.AddListener(OnClickDisableToggle);
        }
    }

    private void OnDestroy()
    {
        if (buildingButton != null)
            buildingButton.onClick.RemoveListener(SetUpgradeBuildings);

        if (disableToggleButton != null)
            disableToggleButton.onClick.RemoveListener(OnClickDisableToggle);
    }

    public void UpdateSelectedCity(CityScript city, FactionManager player, int index)
    {
        selectedCity = city;
        playerFac = player;
        buildingIndex = index;
        RefreshSelectedBuildingState();

        RefreshUI();
    }

    public void ClearSelectedCity()
    {
        selectedCity = null;
        playerFac = null;
        myBuilding = null;
        myBuildingInstance = null;
        isLocked = false;
        isScavengerLocked = false;
        RefreshUI();
    }

    private void RefreshUI()
    {
        bool hasBuilding = myBuilding != null && !myBuilding.IsEmptySlot();
        bool visuallyLocked = isLocked || isScavengerLocked;

        if (buildingNameText != null)
        {
            string buildingName = hasBuilding
                ? myBuilding.name.ToUpperInvariant()
                : (isScavengerLocked ? "점거됨" : (isLocked ? "LOCKED" : "EMPTY"));
            buildingNameText.text = myBuildingInstance != null && myBuildingInstance.IsDisabled()
                ? $"OFFLINE  {buildingName}"
                : buildingName;
            ManagementUIDesignSystem.StyleText(buildingNameText);
        }

        if (buildingDescText != null)
        {
            buildingDescText.text = isScavengerLocked
                ? "스캐빈저 점거"
                : (isLocked ? "LOCKED SLOT" : (hasBuilding ? "STRUCTURE ONLINE" : "[+] BUILDABLE"));
            ManagementUIDesignSystem.StyleText(buildingDescText);
        }

        RefreshIcon();
        RefreshCategoryIcon();
        RefreshScavengerSlotPanel();
        RefreshBuildingButtonColor(hasBuilding);
        RefreshHoverGlow();
        RefreshDisableToggleButton();
        RefreshLockedState();
        ManagementUIDesignSystem.StyleSlot(this, visuallyLocked, hasBuilding);
    }

    private void RefreshHoverGlow()
    {
        if (hoverGlow == null)
            return;

        if (isScavengerLocked)
            return;

        bool hasActiveBuilding = myBuildingInstance != null
            && !myBuildingInstance.IsEmptySlot();

        hoverGlow.SetActiveBuilding(hasActiveBuilding);
    }

    private void RefreshScavengerSlotPanel()
    {
        ResolveSlotPanel();

        if (slotPanel == null)
            return;

        CacheSlotPanelStyle();

        if (isScavengerLocked)
        {
            if (hoverGlow != null && hoverGlow.enabled)
                hoverGlow.enabled = false;

            if (scavengerSlotPanelMaterial != null)
                slotPanel.SetPanelMaterial(scavengerSlotPanelMaterial);

            slotPanel.SetBorderColor(ScavengerSlotBorderColor);
            return;
        }

        if (normalSlotPanelMaterial != null)
            slotPanel.SetPanelMaterial(normalSlotPanelMaterial);
        else if (cachedSlotPanelMaterial != null)
            slotPanel.SetPanelMaterial(cachedSlotPanelMaterial);

        if (cachedSlotPanelHadBorderOverride)
            slotPanel.SetBorderColor(cachedSlotPanelBorderColor);
        else
            slotPanel.ClearBorderColorOverride();

        if (hoverGlow != null && !hoverGlow.enabled)
            hoverGlow.enabled = true;
    }

    private void ResolveSlotPanel()
    {
        if (slotPanel == null)
            slotPanel = GetComponent<CutCornerPanel>();
    }

    private void ResolveCategoryIconMap()
    {
        if (categoryIconMap != null)
            return;

        categoryIconMap = Resources.Load<BuildingCategoryIconMapSO>(DefaultCategoryIconMapResourcePath);
    }

    private void RefreshBuildingButtonColor(bool hasBuilding)
    {
        if (slotPanel == null || isScavengerLocked || isLocked || !hasBuilding)
            return;

        ResolveCategoryIconMap();
        BuildingCategoryIconMapSO.IconMapping mapping = null;
        if (categoryIconMap == null || !categoryIconMap.TryGetMapping(myBuilding.category, out mapping))
            return;

        Color borderColor = mapping.buttonTextColor;
        if (myBuildingInstance != null && myBuildingInstance.IsDisabled())
            borderColor.a *= 0.45f;

        slotPanel.SetBorderColor(borderColor);
    }

    private void CacheSlotPanelStyle()
    {
        if (hasCachedSlotPanelStyle || slotPanel == null)
            return;

        cachedSlotPanelMaterial = slotPanel.PanelMaterial;
        cachedSlotPanelHadBorderOverride = slotPanel.HasBorderColorOverride;
        cachedSlotPanelBorderColor = slotPanel.BorderColor;
        hasCachedSlotPanelStyle = true;
    }

    private void RefreshDisableToggleButton()
    {
        if (disableToggleButton == null)
            return;

        bool showToggle = myBuildingInstance != null
            && !myBuildingInstance.IsEmptySlot()
            && !myBuildingInstance.IsUnderConstruction()
            && selectedCity != null
            && selectedCity.cityData != null
            && selectedCity.cityData.owner == playerFac
            && !isLocked
            && !isScavengerLocked;

        disableToggleButton.gameObject.SetActive(showToggle);
        disableToggleButton.interactable = showToggle;

        Image buttonImage = disableToggleButton.GetComponent<Image>();
        if (buttonImage != null && myBuildingInstance != null)
            buttonImage.color = myBuildingInstance.IsDisabled()
                ? DisableToggleDisabledColor
                : DisableToggleEnabledColor;
    }

    private void RefreshIcon()
    {
        if (buildingIconImage == null && hologramPreview == null)
            return;

        Sprite icon = null;
        float iconAlpha = isLocked || isScavengerLocked || (myBuildingInstance != null && myBuildingInstance.IsDisabled()) ? 0.45f : 1f;

        if (isScavengerLocked && hologramPreview != null)
        {
            GameObject scavengerPrefab = LoadScavengerPreviewPrefab();
            if (scavengerPrefab != null)
            {
                if (!hologramPreview.gameObject.activeSelf)
                    hologramPreview.gameObject.SetActive(true);

                if (buildingIconImage != null)
                    buildingIconImage.enabled = false;

                hologramPreview.ShowPrefab(scavengerPrefab, iconAlpha);
                return;
            }
        }

        bool showModelPreview = myBuilding != null
            && !myBuilding.IsEmptySlot()
            && myBuilding.LoadHologramPrefab() != null;

        if (showModelPreview && hologramPreview != null)
        {
            if (!hologramPreview.gameObject.activeSelf)
                hologramPreview.gameObject.SetActive(true);

            if (buildingIconImage != null)
                buildingIconImage.enabled = false;

            hologramPreview.Show(myBuilding, iconAlpha);
            return;
        }

        if (hologramPreview != null)
        {
            hologramPreview.ClearPreview();
            if (hologramPreview.gameObject.activeSelf)
                hologramPreview.gameObject.SetActive(false);
        }

        if (buildingIconImage == null)
            return;

        buildingIconImage.sprite = icon;
        buildingIconImage.enabled = icon != null && myBuilding != null && !myBuilding.IsEmptySlot();

        Color color = buildingIconImage.color;
        color.a = iconAlpha;
        buildingIconImage.color = color;
    }

    private static GameObject LoadScavengerPreviewPrefab()
    {
        if (cachedScavengerPreviewPrefab != null)
        {
            return cachedScavengerPreviewPrefab;
        }

        cachedScavengerPreviewPrefab = Resources.Load<GameObject>(ScavengerPrefabResourcePath);
        if (cachedScavengerPreviewPrefab == null)
        {
            Debug.LogWarning($"[BuildingUIController] Scavenger preview prefab load failed. path: Resources/{ScavengerPrefabResourcePath}");
        }

        return cachedScavengerPreviewPrefab;
    }

    private void EnsureButtonHitTarget()
    {
        if (buildingButton == null)
            return;

        if (buildingButton.targetGraphic != null)
        {
            buildingButton.targetGraphic.raycastTarget = true;
            return;
        }

        Image buttonImage = GetComponent<Image>();
        if (buttonImage == null)
        {
            buttonImage = gameObject.AddComponent<Image>();
            buttonImage.color = new Color(1f, 1f, 1f, 0f);
        }

        buttonImage.raycastTarget = true;
        buildingButton.targetGraphic = buttonImage;
    }

    private void RefreshCategoryIcon()
    {
        if (categoryIconImage == null)
            return;

        BuildingCategoryIconMapSO.IconMapping mapping = null;
        bool showCategory = myBuilding != null
            && !myBuilding.IsEmptySlot()
            && !isLocked
            && !isScavengerLocked
            && categoryIconMap != null
            && categoryIconMap.TryGetMapping(myBuilding.category, out mapping);

        categoryIconImage.gameObject.SetActive(showCategory);
        categoryIconImage.enabled = showCategory;

        if (!showCategory)
            return;

        categoryIconImage.material = mapping.material;
        if (mapping.sprite != null)
            categoryIconImage.sprite = mapping.sprite;

        Color color = categoryIconImage.color;
        color.a = myBuildingInstance != null && myBuildingInstance.IsDisabled() ? 0.45f : 1f;
        categoryIconImage.color = color;
    }

    private void RefreshLockedState()
    {
        // 스캐빈저 점거 슬롯은 클릭 가능해야 모달을 띄울 수 있으므로 interactable 유지.
        if (buildingButton != null)
            buildingButton.interactable = !isLocked;

        bool showLockVisual = isLocked || isScavengerLocked;

        if (lockIcon != null)
            lockIcon.SetActive(showLockVisual);

        if (lockText != null)
        {
            lockText.gameObject.SetActive(showLockVisual);
            if (isScavengerLocked)
                lockText.text = "점거됨";
            else if (isLocked)
                lockText.text = "잠김";
        }
    }

    public void SetBuildingIndex(int index)
    {
        buildingIndex = index;
        RefreshSelectedBuildingState();
    }

    public void SetUpgradeBuildings()
    {
        if (isScavengerLocked)
        {
            OnScavengerSlotClicked?.Invoke(selectedCity, buildingIndex);
            return;
        }

        if (isLocked)
            return;

        RefreshSelectedBuildingState();

        if (possibleUpgradeBuildingUIController != null)
            possibleUpgradeBuildingUIController.SetUpgradeBuildings(selectedCity, myBuilding, buildingIndex, playerFac);

        if (cityUIController != null)
            cityUIController.OpenPossibleBuildingPanel();
    }

    private void RefreshSelectedBuildingState()
    {
        myBuildingInstance = GetSelectedBuildingInstance();
        myBuilding = myBuildingInstance != null ? myBuildingInstance.data : null;
    }

    private BuildingInstance GetSelectedBuildingInstance()
    {
        if (selectedCity == null)
            return null;

        return selectedCity.GetBuildingInstance(buildingIndex);
    }

    public void SetLocked(bool _isLocked)
    {
        isLocked = _isLocked;
        RefreshUI();
    }

    public void SetScavengerLocked(bool _isScavengerLocked)
    {
        isScavengerLocked = _isScavengerLocked;
        RefreshUI();
    }

    public void OnClickDisableToggle()
    {
        if (selectedCity == null)
            return;

        if (!selectedCity.ToggleBuildingDisabled(buildingIndex))
            return;

        RefreshSelectedBuildingState();
        RefreshUI();

        if (cityUIController != null)
            cityUIController.RefreshSelectedCityUI();
    }
}
