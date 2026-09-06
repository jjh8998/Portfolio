using TMPro;
using UnityEngine;

public class BuildingDetailUIController : MonoBehaviour
{
    private const string DetailPreviewRootName = "SelectedBuildingPreview";
    private const string DetailPreviewPanelName = "SelectedBuildingPreviewPanel";
    private const string DetailPreviewName = "SelectedBuildingHologram";
    private const string DetailDescriptionName = "SelectedBuildingDesc_Text";
    private const string DetailStatusName = "SelectedBuildingStatus_Text";
    private const string DetailCostName = "SelectedBuildingCost_Text";
    private const string DetailRequirementName = "SelectedBuildingRequirement_Text";
    private const string DetailIncomeName = "SelectedBuildingIncome_Text";
    private const string DetailRPName = "SelectedBuildingRP_Text";
    private const string DetailPowerName = "SelectedBuildingPower_Text";

    [Header("Selected Building Detail")]
    [SerializeField] private RectTransform selectedBuildingPreviewPanel;
    [SerializeField] private BuildingHologramPreviewUI selectedBuildingPreview;
    [SerializeField] private TMP_Text selectedBuildingDescriptionText;
    [SerializeField] private TMP_Text selectedBuildingStatusText;
    [SerializeField] private TMP_Text selectedBuildingCostText;
    [SerializeField] private TMP_Text selectedBuildingRequirementText;
    [SerializeField] private TMP_Text selectedBuildingIncomeText;
    [SerializeField] private TMP_Text selectedBuildingRPText;
    [SerializeField] private TMP_Text selectedBuildingPowerOutputText;

    private void Awake()
    {
        BindReferences();
    }

    private void OnEnable()
    {
        BindReferences();
    }

    public void Refresh(
        BuildingData detailBuilding,
        string descriptionLabel,
        string statusLabel,
        string costLabel,
        string requirementLabel,
        string incomeLabel,
        string rpLabel,
        string powerLabel,
        float hologramAlpha)
    {
        BindReferences();
        RefreshHologram(detailBuilding, hologramAlpha);
        SetText(selectedBuildingDescriptionText, descriptionLabel);
        SetText(selectedBuildingStatusText, statusLabel);
        SetText(selectedBuildingCostText, costLabel);
        SetText(selectedBuildingRequirementText, requirementLabel);
        SetMetricText(selectedBuildingIncomeText, incomeLabel);
        SetMetricText(selectedBuildingRPText, rpLabel);
        SetMetricText(selectedBuildingPowerOutputText, powerLabel);
    }

    private void RefreshHologram(BuildingData detailBuilding, float hologramAlpha)
    {
        if (selectedBuildingPreview == null)
            return;

        if (detailBuilding != null && !detailBuilding.IsEmptySlot() && detailBuilding.LoadHologramPrefab() != null)
            selectedBuildingPreview.Show(detailBuilding, hologramAlpha);
        else
            selectedBuildingPreview.ClearPreview();
    }

    private void SetText(TMP_Text text, string value)
    {
        if (text == null)
            return;

        text.text = value;
        ManagementUIDesignSystem.StyleText(text);
    }

    private void SetMetricText(TMP_Text text, string value)
    {
        if (text == null)
            return;

        text.text = value;
        ManagementUIDesignSystem.StyleResourceValue(text);
    }

    private void BindReferences()
    {
        RectTransform panelRect = transform as RectTransform;
        if (panelRect == null)
            return;

        RectTransform selectedRoot = FindFirstRect(panelRect, DetailPreviewRootName, DetailPreviewPanelName);
        Transform textRoot = selectedRoot != null ? selectedRoot : panelRect;

        RectTransform preferredPreviewPanel = selectedRoot != null && selectedRoot.name == DetailPreviewPanelName
            ? selectedRoot
            : FindDescendant(selectedRoot, DetailPreviewPanelName) as RectTransform;

        if (preferredPreviewPanel != null)
            selectedBuildingPreviewPanel = preferredPreviewPanel;
        else if (selectedBuildingPreviewPanel == null)
            selectedBuildingPreviewPanel = FindDescendant(panelRect, DetailPreviewPanelName) as RectTransform;

        Transform previewTransform = FindDescendant(textRoot, DetailPreviewName);
        BuildingHologramPreviewUI preferredPreview = previewTransform != null
            ? previewTransform.GetComponent<BuildingHologramPreviewUI>()
            : (selectedBuildingPreviewPanel != null ? selectedBuildingPreviewPanel.GetComponentInChildren<BuildingHologramPreviewUI>(true) : null);

        if (preferredPreview != null)
            selectedBuildingPreview = preferredPreview;
        else if (selectedBuildingPreview == null)
            selectedBuildingPreview = selectedBuildingPreviewPanel != null
                ? selectedBuildingPreviewPanel.GetComponentInChildren<BuildingHologramPreviewUI>(true)
                : null;

        selectedBuildingDescriptionText = ResolvePreferredText(selectedBuildingDescriptionText, textRoot, panelRect, DetailDescriptionName);
        selectedBuildingStatusText = ResolvePreferredText(selectedBuildingStatusText, textRoot, panelRect, DetailStatusName);
        selectedBuildingCostText = ResolvePreferredText(selectedBuildingCostText, textRoot, panelRect, DetailCostName);
        selectedBuildingRequirementText = ResolvePreferredText(selectedBuildingRequirementText, textRoot, panelRect, DetailRequirementName);
        selectedBuildingIncomeText = ResolvePreferredText(selectedBuildingIncomeText, textRoot, panelRect, DetailIncomeName, "Income_Text");
        selectedBuildingRPText = ResolvePreferredText(selectedBuildingRPText, textRoot, panelRect, DetailRPName, "RP_Text");
        selectedBuildingPowerOutputText = ResolvePreferredText(selectedBuildingPowerOutputText, textRoot, panelRect, DetailPowerName, "Power_Text");
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

    private static TMP_Text ResolvePreferredText(TMP_Text current, Transform preferredParent, Transform fallbackParent, params string[] objectNames)
    {
        TMP_Text preferred = ResolveText(null, preferredParent, objectNames);
        if (preferred != null)
            return preferred;

        return ResolveText(current, fallbackParent, objectNames);
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
}
