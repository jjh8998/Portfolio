using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class EmployeeManagementUIController : MonoBehaviour
{
    [SerializeField] private FactionManager targetFaction;
    [FormerlySerializedAs("employeeInfoPanel")]
    [SerializeField] private GameObject managementPanel;
    [SerializeField] private Button closeButton;
    [SerializeField] private EmployeeStatusPanelUIController statusController;

    [Header("Employee Slots")]
    [SerializeField] private Transform employeeSlotListRoot;
    [FormerlySerializedAs("employeeSlotPrefab")]
    [SerializeField] private EmployeeDetailPanelUIController employDetailPanel;
    [SerializeField] private EmployeeDetailPanelUIController employEmptyPanel;
    [SerializeField] private EmployeeDetailPanelUIController detailPanel;
    [SerializeField] private EmployeeAssignmentService employeeAssignmentService;

    [Header("Hire")]
    [SerializeField] private EmployeeHireService employeeHireService;
    [SerializeField] private GameObject hirePanelRoot;
    [SerializeField] private Transform candidateListRoot;
    [SerializeField] private GameObject candidateItemPrefab;
    [SerializeField] private Button openHireEmployeeButton;
    [SerializeField] private Button hirePanelCloseButton;
    [SerializeField] private Button rerollButton;
    [SerializeField] private TMP_Text rerollCostText;
    [SerializeField] private TMP_Text candidateRemainText;
    [SerializeField] private TMP_Text hireLockedText;

    private FactionManager subscribedFaction;
    private EmployeeHireService subscribedEmployeeHireService;
    private EmployeeAssignmentService subscribedEmployeeAssignmentService;
    private EmployeeData selectedEmployee;
    private bool isHirePanelManuallyClosed = true;
    private bool isAssignmentMode;
    private CityScript assignmentTargetCity;

    private void Awake()
    {
        ApplyPanelVisibility(false);
        CloseHirePanel();
    }

    private void OnEnable()
    {
        BindCloseButton();
        BindOpenHireEmployeeButton();
        BindHirePanelCloseButton();
        BindRerollButton();
        SaveManager.LoadCompleted -= OnLoadCompleted;
        SaveManager.LoadCompleted += OnLoadCompleted;
        FactionManager.InitialEmployeeUnlocked -= OnInitialEmployeeUnlocked;
        FactionManager.InitialEmployeeUnlocked += OnInitialEmployeeUnlocked;
        RefreshNow();
    }

    private void Start()
    {
        RefreshNow();
    }

    private void OnDisable()
    {
        UnbindCloseButton();
        UnbindOpenHireEmployeeButton();
        UnbindHirePanelCloseButton();
        UnbindRerollButton();
        SaveManager.LoadCompleted -= OnLoadCompleted;
        FactionManager.InitialEmployeeUnlocked -= OnInitialEmployeeUnlocked;
        UnsubscribeFaction();
        UnsubscribeEmployeeHireService();
        UnsubscribeEmployeeAssignmentService();
    }

    private void OnDestroy()
    {
        UnbindCloseButton();
        UnbindOpenHireEmployeeButton();
        UnbindHirePanelCloseButton();
        UnbindRerollButton();
        SaveManager.LoadCompleted -= OnLoadCompleted;
        FactionManager.InitialEmployeeUnlocked -= OnInitialEmployeeUnlocked;
        UnsubscribeFaction();
        UnsubscribeEmployeeHireService();
        UnsubscribeEmployeeAssignmentService();
    }

    public void RefreshNow()
    {
        EnsureTargetFaction();
        ResolveEmployeeHireService();
        ResolveEmployeeAssignmentService();

        SubscribeFaction();
        SubscribeEmployeeHireService();
        SubscribeEmployeeAssignmentService();

        bool hiringUnlocked = targetFaction != null && targetFaction.IsEmployeeHiringUnlocked;

        if (employeeHireService != null)
            employeeHireService.RefreshNow();

        if (!IsPanelVisible())
            return;

        RefreshHireUI(hiringUnlocked);
        RefreshEmployeeSlots();
        RefreshStatusPanel();
    }

    public void OpenPanel()
    {
        ClearAssignmentMode();
        OpenPanelInternal();
    }

    public void OpenAssignmentPanel(FactionManager _faction, CityScript _city)
    {
        if (_faction != null)
            targetFaction = _faction;

        assignmentTargetCity = _city;
        isAssignmentMode = true;
        selectedEmployee = null;
        CloseHirePanel();
        OpenPanelInternal();
    }

    private void OpenPanelInternal()
    {
        EnsureTargetFaction();
        if (targetFaction == null || !targetFaction.IsEmployeeHiringUnlocked)
        {
            Debug.Log("[EmployeeManagementUI] Employee panel is locked.");
            return;
        }

        ResolveEmployeeHireService();
        ResolveEmployeeAssignmentService();
        SubscribeEmployeeHireService();
        SubscribeEmployeeAssignmentService();
        isHirePanelManuallyClosed = true;
        ApplyPanelVisibility(true);

        RefreshEmployeeSlots();
        RefreshStatusPanel();
        RefreshHireUI(targetFaction != null && targetFaction.IsEmployeeHiringUnlocked);
    }

    public void OpenPanel(FactionManager _faction)
    {
        if (_faction != null)
            targetFaction = _faction;

        OpenPanel();
    }

    public void ClosePanel()
    {
        ClearAssignmentMode();
        ApplyPanelVisibility(false);
    }

    public void Close()
    {
        ClosePanel();
    }

    public void CloseHirePanel()
    {
        isHirePanelManuallyClosed = true;

        SetPanelActiveSafely(hirePanelRoot, false, nameof(hirePanelRoot));
    }

    public void OpenHirePanelFromEmptySlot()
    {
        if (isAssignmentMode)
            return;

        OpenHireEmployeePanel();
    }

    public void OpenHireEmployeePanel()
    {
        if (isAssignmentMode)
            return;

        EnsureTargetFaction();
        if (targetFaction == null || !targetFaction.IsEmployeeHiringUnlocked)
            return;

        ResolveEmployeeHireService();
        ResolveEmployeeAssignmentService();
        SubscribeEmployeeHireService();
        SubscribeEmployeeAssignmentService();

        isHirePanelManuallyClosed = false;

        if (employeeHireService != null)
            employeeHireService.RefreshNow();

        RefreshHireUI(true);

        if (TutorialManager.Instance != null)
            TutorialManager.Instance.NotifyEmployeeTutorialAction(TutorialManager.EmployeeActionOpenHire);
    }

    public void RefreshEmployeeSlots()
    {
        ResolveStatusPanel();

        if (targetFaction == null)
            EnsureTargetFaction();

        if (targetFaction == null)
        {
            Debug.LogWarning("[EmployeeManagementUI] Target faction was not found.");
            ClearEmployeeSlotItems();
            RefreshDetailPanel(null);
            return;
        }

        if (employeeSlotListRoot == null)
        {
            Debug.LogWarning("[EmployeeManagementUI] Employee slot list root is missing.");
            return;
        }

        ClearEmployeeSlotItems();

        IReadOnlyList<EmployeeData> employees = targetFaction.GetEmployees();
        int employeeCount = employees != null ? employees.Count : 0;
        int slotCount = targetFaction.GetEmployeeSlotCount();
        int displaySlotCount = Mathf.Max(slotCount, employeeCount);
        EmployeeData refreshedSelectedEmployee = null;

        if (employeeCount > 0 && employDetailPanel == null)
            Debug.LogWarning("[EmployeeManagementUI] Employ detail panel prefab is missing.");

        if (displaySlotCount > employeeCount && employEmptyPanel == null)
            Debug.LogWarning("[EmployeeManagementUI] Employ empty panel prefab is missing.");

        for (int i = 0; i < displaySlotCount; i++)
        {
            if (i < employeeCount)
            {
                if (employDetailPanel == null)
                    continue;

                EmployeeDetailPanelUIController item = Instantiate(employDetailPanel, employeeSlotListRoot);
                if (item == null)
                    continue;

                EmployeeData employee = employees[i];
                if (IsSameEmployee(employee, selectedEmployee))
                    refreshedSelectedEmployee = employee;

                item.SetEmployee(employee);
                if (isAssignmentMode)
                {
                    EmployeeData assignEmployee = employee;
                    if (CanAssignEmployee(assignEmployee))
                        item.SetAssignHandler(() => OnClickAssignEmployee(assignEmployee));
                    else if (IsAlreadyAssignedToTargetCity(assignEmployee))
                    {
                        item.SetAssignHandler(null);
                        item.SetAssignButtonActive(true);
                        item.SetAssignButtonInteractable(false);
                    }
                    else
                    {
                        item.SetAssignHandler(null);
                    }
                }
                else
                {
                    EmployeeData selectedSlotEmployee = employee;
                    item.SetClickHandler(() => OnEmployeeSelected(selectedSlotEmployee));
                }
            }
            else
            {
                if (employEmptyPanel == null)
                    continue;

                EmployeeDetailPanelUIController item = Instantiate(employEmptyPanel, employeeSlotListRoot);
                if (item == null)
                    continue;

                item.SetEmptySlot();
                if (isAssignmentMode)
                    item.SetClickHandler(null);
                else
                {
                    item.SetClickHandler(() =>
                    {
                        OnEmptySlotSelected();
                        OpenHirePanelFromEmptySlot();
                    });
                }
            }
        }

        selectedEmployee = refreshedSelectedEmployee;
        RefreshDetailPanel(selectedEmployee);
    }

    private void ApplyPanelVisibility(bool isVisible)
    {
        ResolveStatusPanel();

        if (isVisible)
        {
            SetPanelActiveSafely(managementPanel, true, nameof(managementPanel));

            return;
        }

        SetPanelActiveSafely(managementPanel, false, nameof(managementPanel));
    }

    private bool IsPanelVisible()
    {
        if (managementPanel != null)
            return managementPanel.activeSelf;

        return false;
    }

    private void SubscribeFaction()
    {
        if (subscribedFaction == targetFaction)
            return;

        UnsubscribeFaction();

        if (targetFaction == null)
            return;

        subscribedFaction = targetFaction;
        subscribedFaction.EmployeeAssignmentChanged += OnEmployeeAssignmentChanged;
    }

    private void UnsubscribeFaction()
    {
        if (subscribedFaction == null)
            return;

        subscribedFaction.EmployeeAssignmentChanged -= OnEmployeeAssignmentChanged;
        subscribedFaction = null;
    }

    private void OnLoadCompleted()
    {
        RefreshNow();
    }

    private void OnEmployeeAssignmentChanged()
    {
        RefreshNow();
    }

    private void OnSpyProgressChanged(EmployeeData _employee)
    {
        if (_employee != null && targetFaction != null && _employee.ownerFaction != targetFaction)
            return;

        RefreshNow();
    }

    private void OnInitialEmployeeUnlocked(FactionManager _faction)
    {
        if (_faction == null || targetFaction == null || _faction != targetFaction)
            return;

        RefreshNow();
    }

    private void OnCandidatesChanged()
    {
        if (!IsPanelVisible())
            return;

        RefreshHireUI(targetFaction != null && targetFaction.IsEmployeeHiringUnlocked);
    }

    private void OnEmployeeHired(EmployeeData _employee)
    {
        RefreshNow();
    }

    private void OnEmployeeSelected(EmployeeData _employee)
    {
        selectedEmployee = _employee;
        RefreshDetailPanel(selectedEmployee);
    }

    private void OnEmptySlotSelected()
    {
        selectedEmployee = null;
        RefreshDetailPanel(null);
    }

    private void EnsureTargetFaction()
    {
        if (targetFaction == null || !targetFaction.IsPlayerFaction)
            targetFaction = FindPlayerFaction();
    }

    private void ResolveStatusPanel()
    {
        if (statusController == null && managementPanel != null)
            statusController = managementPanel.GetComponentInChildren<EmployeeStatusPanelUIController>(true);
    }

    private void ResolveEmployeeAssignmentService()
    {
        if (employeeAssignmentService != null)
            return;

        employeeAssignmentService = FindFirstObjectByType<EmployeeAssignmentService>();
    }

    private void BindCloseButton()
    {
        if (closeButton == null)
            return;

        closeButton.onClick.RemoveListener(Close);
        closeButton.onClick.AddListener(Close);
    }

    private void UnbindCloseButton()
    {
        if (closeButton == null)
            return;

        closeButton.onClick.RemoveListener(Close);
    }

    private void BindOpenHireEmployeeButton()
    {
        if (openHireEmployeeButton == null)
            return;

        openHireEmployeeButton.onClick.RemoveListener(OpenHireEmployeePanel);
        openHireEmployeeButton.onClick.AddListener(OpenHireEmployeePanel);
    }

    private void UnbindOpenHireEmployeeButton()
    {
        if (openHireEmployeeButton == null)
            return;

        openHireEmployeeButton.onClick.RemoveListener(OpenHireEmployeePanel);
    }

    private void BindHirePanelCloseButton()
    {
        if (hirePanelCloseButton == null)
            return;

        hirePanelCloseButton.onClick.RemoveListener(CloseHirePanel);
        hirePanelCloseButton.onClick.AddListener(CloseHirePanel);
    }

    private void UnbindHirePanelCloseButton()
    {
        if (hirePanelCloseButton == null)
            return;

        hirePanelCloseButton.onClick.RemoveListener(CloseHirePanel);
    }

    private void BindRerollButton()
    {
        if (rerollButton == null)
            return;

        rerollButton.onClick.RemoveListener(OnClickRerollCandidates);
        rerollButton.onClick.AddListener(OnClickRerollCandidates);
    }

    private void UnbindRerollButton()
    {
        if (rerollButton == null)
            return;

        rerollButton.onClick.RemoveListener(OnClickRerollCandidates);
    }

    private void SubscribeEmployeeHireService()
    {
        if (subscribedEmployeeHireService == employeeHireService)
            return;

        UnsubscribeEmployeeHireService();

        if (employeeHireService == null)
            return;

        subscribedEmployeeHireService = employeeHireService;
        subscribedEmployeeHireService.CandidatesChanged += OnCandidatesChanged;
        subscribedEmployeeHireService.EmployeeHired += OnEmployeeHired;
    }

    private void UnsubscribeEmployeeHireService()
    {
        if (subscribedEmployeeHireService == null)
            return;

        subscribedEmployeeHireService.CandidatesChanged -= OnCandidatesChanged;
        subscribedEmployeeHireService.EmployeeHired -= OnEmployeeHired;
        subscribedEmployeeHireService = null;
    }

    private void SubscribeEmployeeAssignmentService()
    {
        if (subscribedEmployeeAssignmentService == employeeAssignmentService)
            return;

        UnsubscribeEmployeeAssignmentService();

        if (employeeAssignmentService == null)
            return;

        subscribedEmployeeAssignmentService = employeeAssignmentService;
        subscribedEmployeeAssignmentService.SpyProgressChanged += OnSpyProgressChanged;
    }

    private void UnsubscribeEmployeeAssignmentService()
    {
        if (subscribedEmployeeAssignmentService == null)
            return;

        subscribedEmployeeAssignmentService.SpyProgressChanged -= OnSpyProgressChanged;
        subscribedEmployeeAssignmentService = null;
    }

    private void RefreshDetailPanel(EmployeeData employee)
    {
        if (detailPanel == null)
            return;

        detailPanel.SetEmployee(employee);

        if (!isAssignmentMode)
        {
            detailPanel.SetAssignHandler(null);
            return;
        }

        if (CanAssignEmployee(employee))
        {
            detailPanel.SetAssignHandler(OnClickAssignSelectedEmployee);
            detailPanel.SetAssignButtonInteractable(true);
        }
        else
        {
            detailPanel.SetAssignHandler(null);
            detailPanel.SetAssignButtonActive(true);
            detailPanel.SetAssignButtonInteractable(false);
        }
    }

    private void RefreshStatusPanel()
    {
        if (targetFaction == null)
            EnsureTargetFaction();

        ResolveStatusPanel();

        if (statusController == null)
            return;

        statusController.SetFaction(targetFaction);
        statusController.Refresh();
    }

    private void ClearEmployeeSlotItems()
    {
        if (employeeSlotListRoot == null)
            return;

        for (int i = employeeSlotListRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = employeeSlotListRoot.GetChild(i);
            if (child != null)
                Destroy(child.gameObject);
        }
    }

    private bool CanAssignEmployee(EmployeeData employee)
    {
        return isAssignmentMode
            && employee != null
            && !employee.isReturning
            && !employee.isLost
            && !string.IsNullOrWhiteSpace(employee.employeeId)
            && assignmentTargetCity != null
            && employee.assignedCity != assignmentTargetCity;
    }

    private bool IsAlreadyAssignedToTargetCity(EmployeeData employee)
    {
        return isAssignmentMode
            && employee != null
            && !employee.isReturning
            && !employee.isLost
            && assignmentTargetCity != null
            && employee.assignedCity == assignmentTargetCity;
    }

    private void OnClickAssignSelectedEmployee()
    {
        OnClickAssignEmployee(selectedEmployee);
    }

    private void OnClickAssignEmployee(EmployeeData employee)
    {
        if (!isAssignmentMode || assignmentTargetCity == null)
        {
            Debug.LogWarning("[EmployeeManagementUI] Assignment target city is missing.");
            return;
        }

        if (employee == null)
        {
            Debug.LogWarning("[EmployeeManagementUI] Selected employee is missing.");
            return;
        }

        if (employee.isReturning || employee.isLost)
        {
            Debug.LogWarning($"[EmployeeManagementUI] Selected employee cannot be assigned. employee={employee.employeeName}");
            return;
        }

        if (employee.assignedCity == assignmentTargetCity)
        {
            Debug.Log($"[EmployeeManagementUI] Selected employee is already assigned to target city. employee={employee.employeeName}");
            return;
        }

        if (targetFaction == null)
            EnsureTargetFaction();

        if (targetFaction == null || string.IsNullOrWhiteSpace(employee.employeeId))
        {
            Debug.LogWarning("[EmployeeManagementUI] Employee assignment data is missing.");
            return;
        }

        if (!targetFaction.AssignEmployee(employee.employeeId, assignmentTargetCity, out string message))
        {
            Debug.LogWarning($"[EmployeeManagementUI] Employee assignment failed. employee={employee.employeeName}, reason={message}");
            return;
        }

        Debug.Log($"[EmployeeManagementUI] Employee assigned. employee={employee.employeeName}");
        ClosePanel();
        RefreshNow();
    }

    private void ClearAssignmentMode()
    {
        isAssignmentMode = false;
        assignmentTargetCity = null;

        if (detailPanel != null)
            detailPanel.SetAssignHandler(null);
    }

    private static bool IsSameEmployee(EmployeeData first, EmployeeData second)
    {
        if (first == null || second == null)
            return false;

        if (ReferenceEquals(first, second))
            return true;

        return !string.IsNullOrWhiteSpace(first.employeeId)
            && string.Equals(first.employeeId, second.employeeId, System.StringComparison.OrdinalIgnoreCase);
    }

    private void RefreshHireUI(bool hiringUnlocked)
    {
        SetPanelActiveSafely(hirePanelRoot, hiringUnlocked && !isHirePanelManuallyClosed, nameof(hirePanelRoot));

        if (hireLockedText != null)
        {
            hireLockedText.gameObject.SetActive(!hiringUnlocked);
            if (!hiringUnlocked)
                hireLockedText.text = "도시에 완공된 건물 3개 이상이 필요합니다.";
        }

        if (rerollButton != null)
            rerollButton.interactable = hiringUnlocked && employeeHireService != null;

        if (rerollCostText != null)
            rerollCostText.text = employeeHireService != null ? employeeHireService.RerollCost.ToString() : "0";

        if (candidateRemainText != null)
            candidateRemainText.text = employeeHireService != null && hiringUnlocked
                ? $"{employeeHireService.RemainingMonths}개월"
                : string.Empty;

        ClearCandidateItems();

        if (!hiringUnlocked || employeeHireService == null || candidateListRoot == null || candidateItemPrefab == null)
            return;

        IReadOnlyList<CEOData> candidates = employeeHireService.Candidates;
        if (candidates == null)
            return;

        for (int i = 0; i < candidates.Count; i++)
        {
            GameObject item = Instantiate(candidateItemPrefab, candidateListRoot);
            if (item != null)
                SetCandidateItemData(item, candidates[i]);
        }
    }

    private void SetCandidateItemData(GameObject item, CEOData ceo)
    {
        if (item == null)
            return;

        EmployeeDetailPanelUIController detailPanel = item.GetComponentInChildren<EmployeeDetailPanelUIController>(true);
        if (detailPanel == null)
        {
            Debug.LogWarning("[EmployeeManagementUI] Candidate item is missing EmployeeDetailPanelUIController.");
            return;
        }

        string ceoId = ceo != null ? ceo.id : string.Empty;
        if (string.IsNullOrWhiteSpace(ceoId))
        {
            detailPanel.SetCandidate(ceo);
            Debug.LogWarning("[EmployeeManagementUI] Candidate CEO id is missing.");
            return;
        }

        EmployeeAbilityType abilityType = EmployeeAbilityType.None;
        if (employeeHireService != null)
            employeeHireService.TryGetCandidateAbility(ceoId, out abilityType);

        detailPanel.SetCandidate(ceo, abilityType);
        detailPanel.SetHireHandler(() => OnClickHireCandidate(ceoId));
    }

    private void ClearCandidateItems()
    {
        if (candidateListRoot == null)
            return;

        for (int i = candidateListRoot.childCount - 1; i >= 0; i--)
        {
            Transform child = candidateListRoot.GetChild(i);
            if (child != null)
                Destroy(child.gameObject);
        }
    }

    private void OnClickHireCandidate(string _ceoId)
    {
        if (employeeHireService == null)
        {
            Debug.LogWarning("[EmployeeManagementUI] EmployeeHireService is missing.");
            return;
        }

        if (!employeeHireService.TryHireCandidate(_ceoId, out _, out string message))
            Debug.LogWarning($"[EmployeeManagementUI] Hire failed: {message}");

        RefreshNow();
    }

    private void OnClickRerollCandidates()
    {
        if (employeeHireService == null)
        {
            Debug.LogWarning("[EmployeeManagementUI] EmployeeHireService is missing.");
            return;
        }

        if (!employeeHireService.TryRerollCandidates(out string message))
            Debug.LogWarning($"[EmployeeManagementUI] Reroll failed: {message}");

        RefreshNow();
    }

    private void ResolveEmployeeHireService()
    {
        if (employeeHireService != null)
            return;

        EmployeeHireService[] services = FindObjectsByType<EmployeeHireService>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < services.Length; i++)
        {
            if (services[i] != null)
            {
                employeeHireService = services[i];
                return;
            }
        }
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

    private void SetPanelActiveSafely(GameObject target, bool isActive, string targetName)
    {
        if (target == null)
            return;

        if (!isActive && IsProtectedPanelTarget(target))
        {
            Debug.LogWarning($"[EmployeeManagementUI] {targetName} references a controller object and will not be disabled. Assign a separate UI root instead.");
            return;
        }

        target.SetActive(isActive);
    }

    private bool IsProtectedPanelTarget(GameObject target)
    {
        if (target == null)
            return false;

        if (target == gameObject)
            return true;

        return statusController != null && target == statusController.gameObject;
    }
}
