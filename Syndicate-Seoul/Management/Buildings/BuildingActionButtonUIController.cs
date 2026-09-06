using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuildingActionButtonUIController : MonoBehaviour
{
    [Header("Ability")]
    [SerializeField] private Button abilityButton;
    [SerializeField] private TMP_Text abilityDescText;
    [SerializeField] private CardIncinerationPanelUIController cardIncinerationPanelUIController;

    [Header("Disable Toggle")]
    [SerializeField] private Button disableToggleButton;
    [SerializeField] private TMP_Text disableToggleText;

    [Header("Destroy Building")]
    [SerializeField] private Button destroyBuildingButton;
    [SerializeField] private TMP_Text destroyRefundText;
    [SerializeField, Range(0f, 1f)] private float destroyRefundRate = 0.5f;

    [Header("Close")]
    [SerializeField] private Button closeButton;

    private CityScript selectedCity;
    private BuildingData myBuilding;
    private int buildingIndex;
    private FactionManager playerFac;
    private CityUIController cityUIController;
    private Action refreshed;

    private void Awake()
    {
        BindButtons();
    }

    private void OnDestroy()
    {
        UnbindButtons();
    }

    public void Refresh(
        CityScript _selectedCity,
        int _buildingIndex,
        FactionManager _playerFac,
        BuildingData _myBuilding,
        CityUIController _cityUIController,
        Action _refreshed)
    {
        selectedCity = _selectedCity;
        buildingIndex = _buildingIndex;
        playerFac = _playerFac;
        myBuilding = _myBuilding;
        cityUIController = _cityUIController != null ? _cityUIController : GetComponentInParent<CityUIController>();
        refreshed = _refreshed;

        RefreshAbilityButtonUI();
        RefreshDisableToggleUI();
        RefreshDestroyBuildingUI();
        RefreshCloseButtonUI();
    }

    public void Clear()
    {
        selectedCity = null;
        myBuilding = null;
        buildingIndex = -1;
        playerFac = null;
        refreshed = null;

        SetVisible(abilityButton, false);
        SetVisible(disableToggleButton, false);
        SetVisible(destroyBuildingButton, false);
        RefreshCloseButtonUI();

        if (abilityDescText != null)
        {
            abilityDescText.text = string.Empty;
            abilityDescText.gameObject.SetActive(false);
        }

        if (destroyRefundText != null)
            destroyRefundText.gameObject.SetActive(false);
    }

    private void BindButtons()
    {
        BindButton(abilityButton, OnClickAbilityButton);
        BindButton(disableToggleButton, OnClickDisableToggle);
        BindButton(destroyBuildingButton, OnClickDestroyBuilding);
        BindButton(closeButton, OnClickCloseButton);
    }

    private void UnbindButtons()
    {
        UnbindButton(abilityButton, OnClickAbilityButton);
        UnbindButton(disableToggleButton, OnClickDisableToggle);
        UnbindButton(destroyBuildingButton, OnClickDestroyBuilding);
        UnbindButton(closeButton, OnClickCloseButton);
    }

    private void BindButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
            return;

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private void UnbindButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
            return;

        button.onClick.RemoveListener(action);
    }

    private void OnClickEmergencyOrder()
    {
        if (!EmergencyOrderService.TryExecute(selectedCity, buildingIndex, playerFac, out string failReason))
        {
            if (string.IsNullOrWhiteSpace(failReason))
                failReason = "Emergency order failed.";

            Debug.LogWarning("UpgradeBuildingUIController : emergency order failed - " + failReason);
            RefreshAbilityButtonUI();
            return;
        }

        if (cityUIController != null)
            cityUIController.RefreshSelectedCityUI();

        RefreshAbilityButtonUI();
        refreshed?.Invoke();
    }

    private void OnClickAbilityButton()
    {
        EmergencyOrderState emergencyOrderState = EmergencyOrderService.GetState(selectedCity, buildingIndex, playerFac);
        if (CanShowEmergencyOrderButton(emergencyOrderState))
        {
            OnClickEmergencyOrder();
            return;
        }

        if (!CanShowCardIncinerationButton())
            return;

        if (cardIncinerationPanelUIController == null)
        {
            Debug.LogWarning("UpgradeBuildingUIController : card incineration panel is not assigned.");
            return;
        }

        cardIncinerationPanelUIController.Open(selectedCity);
    }

    private void OnClickDisableToggle()
    {
        if (selectedCity == null)
            return;

        if (!selectedCity.ToggleBuildingDisabled(buildingIndex))
        {
            Debug.LogWarning("UpgradeBuildingUIController : failed to toggle building disabled state.");
            refreshed?.Invoke();
            return;
        }

        BuildingInstance selectedBuilding = GetSelectedBuildingInstance();
        myBuilding = selectedBuilding != null ? selectedBuilding.data : null;

        if (cityUIController == null)
            cityUIController = GetComponentInParent<CityUIController>();

        refreshed?.Invoke();

        if (cityUIController != null)
            cityUIController.RefreshSelectedCityUI();
    }

    private void OnClickDestroyBuilding()
    {
        if (selectedCity == null)
        {
            Debug.LogWarning("UpgradeBuildingUIController : no selected city");
            return;
        }

        if (!CanShowDestroyBuildingButton())
        {
            Debug.LogWarning("UpgradeBuildingUIController : building cannot be destroyed");
            RefreshDestroyBuildingUI();
            return;
        }

        if (!selectedCity.DestroyBuilding(buildingIndex, destroyRefundRate, out int refundCredit, out string failReason))
        {
            if (string.IsNullOrWhiteSpace(failReason))
                failReason = "Destroy building failed.";

            Debug.LogWarning("UpgradeBuildingUIController : destroy building failed - " + failReason);
            RefreshDestroyBuildingUI();
            return;
        }

        Debug.Log($"UpgradeBuildingUIController : building destroyed. refundCredit={refundCredit}");

        if (cityUIController == null)
            cityUIController = GetComponentInParent<CityUIController>();

        if (cityUIController != null)
        {
            cityUIController.RefreshSelectedCityUI();
            cityUIController.ClosePossibleBuildingPanel();
        }
        else
        {
            refreshed?.Invoke();
        }
    }

    private void OnClickCloseButton()
    {
        if (IsBuildTutorialSelectionStep())
            return;

        if (cityUIController == null)
            cityUIController = GetComponentInParent<CityUIController>();

        if (cityUIController != null)
            cityUIController.ClosePossibleBuildingPanel();
    }

    private void RefreshAbilityButtonUI()
    {
        if (abilityButton == null)
        {
            SetAbilityDescText(false, string.Empty);
            return;
        }

        EmergencyOrderState emergencyOrderState = EmergencyOrderService.GetState(selectedCity, buildingIndex, playerFac);
        bool showEmergencyOrder = CanShowEmergencyOrderButton(emergencyOrderState);
        bool showCardIncineration = !showEmergencyOrder && CanShowCardIncinerationButton();
        bool isVisible = showEmergencyOrder || showCardIncineration;

        abilityButton.gameObject.SetActive(isVisible);
        abilityButton.interactable = showEmergencyOrder ? emergencyOrderState.canExecute : isVisible;

        if (showEmergencyOrder)
        {
            SetAbilityButtonText("긴급 수주");
            SetAbilityDescText(true, $"긴급 수주 비용: {emergencyOrderState.cost}");
        }
        else if (showCardIncineration)
        {
            SetAbilityButtonText("카드 소각");
            SetAbilityDescText(false, string.Empty);
        }
        else
        {
            SetAbilityDescText(false, string.Empty);
        }
    }

    private bool CanShowEmergencyOrderButton(EmergencyOrderState _state)
    {
        if (_state == null)
            return false;

        return _state.isVisible && !IsReadOnlyCity();
    }

    private bool CanShowCardIncinerationButton()
    {
        if (myBuilding == null || myBuilding.ability == null)
            return false;

        if (myBuilding.ability is not CardIncinerationPowerAbility)
            return false;

        if (selectedCity == null || selectedCity.cityData == null || selectedCity.cityData.owner == null)
            return false;

        if (selectedCity.cityData.owner != playerFac)
            return false;

        BuildingInstance selectedBuilding = GetSelectedBuildingInstance();
        if (selectedBuilding == null || selectedBuilding.IsUnderConstruction() || selectedBuilding.IsUpgrading() || selectedBuilding.IsDisabled())
            return false;

        return true;
    }

    private void SetAbilityButtonText(string _text)
    {
        TMP_Text abilityButtonText = abilityButton.GetComponentInChildren<TMP_Text>(true);
        if (abilityButtonText == null)
            return;

        abilityButtonText.text = _text;
        ManagementUIDesignSystem.StyleText(abilityButtonText);
    }

    private void SetAbilityDescText(bool _isVisible, string _text)
    {
        if (abilityDescText == null)
            return;

        abilityDescText.gameObject.SetActive(_isVisible);
        abilityDescText.text = _isVisible ? _text : string.Empty;
    }

    private void RefreshDisableToggleUI()
    {
        bool canShow = CanShowDisableToggleButton();

        if (disableToggleButton != null)
        {
            disableToggleButton.gameObject.SetActive(canShow);
            disableToggleButton.interactable = canShow;
        }

        if (!canShow)
            return;

        BuildingInstance selectedBuilding = GetSelectedBuildingInstance();
        TMP_Text text = disableToggleText != null
            ? disableToggleText
            : disableToggleButton != null ? disableToggleButton.GetComponentInChildren<TMP_Text>(true) : null;

        if (text != null)
            text.text = selectedBuilding != null && selectedBuilding.IsDisabled() ? "재가동" : "비활성화";
    }

    private bool CanShowDisableToggleButton()
    {
        if (selectedCity == null || selectedCity.cityData == null)
            return false;

        if (selectedCity.cityData.owner == null || selectedCity.cityData.owner != playerFac)
            return false;

        if (buildingIndex < 0 || buildingIndex >= selectedCity.GetAvailableBuildingSlotCount())
            return false;

        BuildingInstance selectedBuilding = GetSelectedBuildingInstance();
        if (selectedBuilding == null || selectedBuilding.IsEmptySlot())
            return false;

        if (selectedBuilding.IsUnderConstruction() || selectedBuilding.IsUpgrading())
            return false;

        return true;
    }

    private void RefreshDestroyBuildingUI()
    {
        bool canShow = CanShowDestroyBuildingButton();

        if (destroyBuildingButton != null)
        {
            destroyBuildingButton.gameObject.SetActive(canShow);
            destroyBuildingButton.interactable = canShow;
        }

        if (destroyRefundText != null)
        {
            destroyRefundText.gameObject.SetActive(canShow);
            destroyRefundText.text = canShow ? $"파괴 환급: {GetDestroyRefundCredit()}" : string.Empty;
        }
    }

    private void RefreshCloseButtonUI()
    {
        if (closeButton != null)
            closeButton.interactable = !IsBuildTutorialSelectionStep();
    }

    private bool CanShowDestroyBuildingButton()
    {
        if (selectedCity == null || selectedCity.cityData == null)
            return false;

        if (selectedCity.cityData.owner == null || selectedCity.cityData.owner != playerFac)
            return false;

        if (buildingIndex < 0 || buildingIndex >= selectedCity.GetAvailableBuildingSlotCount())
            return false;

        BuildingInstance selectedBuilding = GetSelectedBuildingInstance();
        if (selectedBuilding == null || selectedBuilding.IsEmptySlot())
            return false;

        if (selectedBuilding.IsUnderConstruction() || selectedBuilding.IsUpgrading())
            return false;

        if (selectedCity.HasAnyBuildingUnderConstruction())
            return false;

        return true;
    }

    private int GetDestroyRefundCredit()
    {
        BuildingInstance selectedBuilding = GetSelectedBuildingInstance();
        BuildingData building = selectedBuilding != null ? selectedBuilding.data : null;
        if (building == null)
            return 0;

        return Mathf.Max(0, Mathf.RoundToInt(building.constructionCost * Mathf.Clamp01(destroyRefundRate)));
    }

    private BuildingInstance GetSelectedBuildingInstance()
    {
        return selectedCity != null ? selectedCity.GetBuildingInstance(buildingIndex) : null;
    }

    private bool IsReadOnlyCity()
    {
        return selectedCity != null
            && selectedCity.cityData != null
            && selectedCity.cityData.owner != playerFac;
    }

    private static void SetVisible(Button button, bool isVisible)
    {
        if (button != null)
            button.gameObject.SetActive(isVisible);
    }

    private static bool IsBuildTutorialSelectionStep()
    {
        return TutorialManager.Instance != null
            && TutorialManager.Instance.IsManagementStepActive("mgmt_build");
    }
}
