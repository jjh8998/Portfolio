using System.Collections.Generic;
using UnityEngine;

public class EmployeeNotificationPopUpUIController : MonoBehaviour, IEscapeClosable
{
    private const string SpyOperationSuccessTitle = "스파이 작전 성공";
    private const string UnknownEmployeeName = "직원";
    private const string UnknownCityName = "알 수 없는 도시";
    private const string UnknownFactionName = "알 수 없는 세력";

    private const string SpyDetectionTitle = "\uC0B0\uC5C5 \uC2A4\uD30C\uC774 \uBC1C\uAC01";
    private const string SpyOperationSuccessPopupTitle = "스파이 작전 성공";
    private const string PlayerSpyDetectedPopupTitle = "산업 스파이 발각";
    private const string EnemySpyFoundPopupTitle = "적 산업 스파이 발견";
    private const string EnemySpyRemovedPopupTitle = "적 산업 스파이 제거";

    [SerializeField] private EmployeeAssignmentService employeeAssignmentService;
    [SerializeField] private FactionManager playerFac;
    [SerializeField] private Transform popupParent;
    [SerializeField] private GameObject spyOperationSuccessPopupPrefab;
    [SerializeField] private GameObject playerSpyDetectedPopupPrefab;
    [SerializeField] private GameObject enemySpyFoundPopupPrefab;
    [SerializeField] private GameObject enemySpyRemovedPopupPrefab;
    [SerializeField] private CityUIController cityUIController;

    private readonly Dictionary<GameObject, EmployeeNotificationToastButton> popupCache = new Dictionary<GameObject, EmployeeNotificationToastButton>();
    private CityScript targetCity;
    private EmployeeNotificationToastButton currentPopupView;
    private bool eventsBound;

    public bool IsOpen => currentPopupView != null && currentPopupView.IsOpen;

    private void Awake()
    {
        ResolveReferences();
        ClosePopup();
    }

    private void OnEnable()
    {
        ResolveReferences();
        SubscribeEvents();
    }

    private void OnDisable()
    {
        UnsubscribeEvents();
    }

    public void ClosePopup()
    {
        if (currentPopupView != null)
        {
            currentPopupView.Hide();
            currentPopupView = null;
        }
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

    private void SubscribeEvents()
    {
        if (eventsBound || employeeAssignmentService == null)
            return;

        employeeAssignmentService.SpyShareOperationSucceeded -= OnSpyShareOperationSucceeded;
        employeeAssignmentService.SpyShareOperationSucceeded += OnSpyShareOperationSucceeded;
        employeeAssignmentService.SpyDetectionResolved -= OnSpyDetectionResolved;
        employeeAssignmentService.SpyDetectionResolved += OnSpyDetectionResolved;
        eventsBound = true;
    }

    private void UnsubscribeEvents()
    {
        if (!eventsBound || employeeAssignmentService == null)
        {
            eventsBound = false;
            return;
        }

        employeeAssignmentService.SpyShareOperationSucceeded -= OnSpyShareOperationSucceeded;
        employeeAssignmentService.SpyDetectionResolved -= OnSpyDetectionResolved;
        eventsBound = false;
    }

    private void OnSpyShareOperationSucceeded(
        EmployeeData employee,
        CityScript city,
        FactionManager fromFaction,
        FactionManager toFaction,
        int amount,
        EmployeeAssignmentService.SpyShareOperationType operationType)
    {
        if (playerFac == null || employee == null || employee.ownerFaction != playerFac)
            return;

        ShowNotification(
            spyOperationSuccessPopupPrefab,
            SpyOperationSuccessPopupTitle,
            BuildDescription(employee, city, fromFaction, amount, operationType),
            city);
    }

    private void OnSpyDetectionResolved(
        EmployeeData employee,
        CityScript city,
        FactionManager targetFaction,
        EmployeeAssignmentService.SpyDetectionResultType resultType)
    {
        if (playerFac == null || employee == null)
            return;

        bool isPlayerSpyDetected = employee.ownerFaction == playerFac;
        bool isEnemySpyDetectedInPlayerCity = targetFaction == playerFac && employee.ownerFaction != playerFac;
        if (!isPlayerSpyDetected && !isEnemySpyDetectedInPlayerCity)
            return;

        GameObject popupPrefab = playerSpyDetectedPopupPrefab;
        string title = PlayerSpyDetectedPopupTitle;
        string description = BuildDetectionDescription(employee, city, targetFaction, resultType);

        if (isEnemySpyDetectedInPlayerCity)
        {
            bool isRemoved = resultType == EmployeeAssignmentService.SpyDetectionResultType.Removed;
            popupPrefab = isRemoved ? enemySpyRemovedPopupPrefab : enemySpyFoundPopupPrefab;
            title = isRemoved ? EnemySpyRemovedPopupTitle : EnemySpyFoundPopupTitle;
            description = BuildDefenderDetectionDescription(employee, city, resultType);
        }

        ShowNotification(popupPrefab, title, description, city);
    }

    private string BuildDescription(
        EmployeeData employee,
        CityScript city,
        FactionManager fromFaction,
        int amount,
        EmployeeAssignmentService.SpyShareOperationType operationType)
    {
        string employeeName = GetEmployeeName(employee);
        string cityName = GetCityName(city);
        string factionName = GetFactionName(fromFaction);

        switch (operationType)
        {
            case EmployeeAssignmentService.SpyShareOperationType.ConvertShareToCorporateAssociation:
                return $"{employeeName}이 {cityName}에서 {factionName}의 지분 {amount}%를 기업협회 지분으로 전환했습니다.";
            default:
                return $"{employeeName}이 {cityName}에서 {factionName}의 지분 {amount}%를 탈취했습니다.";
        }
    }

    private string BuildDetectionDescription(
        EmployeeData employee,
        CityScript city,
        FactionManager targetFaction,
        EmployeeAssignmentService.SpyDetectionResultType resultType)
    {
        string employeeName = GetEmployeeName(employee);
        string cityName = GetCityName(city);
        string factionName = GetFactionName(targetFaction);

        switch (resultType)
        {
            case EmployeeAssignmentService.SpyDetectionResultType.SafeExit:
                return $"{employeeName}\uC774 {cityName}\uC5D0\uC11C \uBC1C\uAC01\uB418\uC5C8\uC9C0\uB9CC \uBB34\uC0AC\uD788 \uC774\uD0C8\uD588\uC2B5\uB2C8\uB2E4. 3\uAC1C\uC6D4 \uD6C4 \uBCF5\uADC0\uD569\uB2C8\uB2E4.";
            case EmployeeAssignmentService.SpyDetectionResultType.Escape:
                return $"{employeeName}\uC774 {cityName}\uC5D0\uC11C \uBC1C\uAC01\uB418\uACE0 \uB3C4\uC8FC\uD588\uC2B5\uB2C8\uB2E4. {factionName}\uC758 \uC6B0\uD638\uB3C4\uAC00 \uAC10\uC18C\uD588\uC2B5\uB2C8\uB2E4. 3\uAC1C\uC6D4 \uD6C4 \uBCF5\uADC0\uD569\uB2C8\uB2E4.";
            case EmployeeAssignmentService.SpyDetectionResultType.Removed:
                return $"{employeeName}\uC774 {cityName}\uC5D0\uC11C \uBC1C\uAC01\uB418\uACE0 \uC81C\uAC70\uB418\uC5C8\uC2B5\uB2C8\uB2E4. {factionName}\uC758 \uC6B0\uD638\uB3C4\uAC00 \uAC10\uC18C\uD588\uC2B5\uB2C8\uB2E4.";
            default:
                return $"{employeeName}\uC774 {cityName}\uC5D0\uC11C \uBC1C\uAC01\uB418\uC5C8\uC2B5\uB2C8\uB2E4.";
        }
    }

    private string BuildDefenderDetectionTitle(EmployeeAssignmentService.SpyDetectionResultType resultType)
    {
        return resultType == EmployeeAssignmentService.SpyDetectionResultType.Removed
            ? "적 산업 스파이 제거"
            : "적 산업 스파이 발견";
    }

    private string BuildDefenderDetectionDescription(
        EmployeeData employee,
        CityScript city,
        EmployeeAssignmentService.SpyDetectionResultType resultType)
    {
        string employeeName = GetEmployeeName(employee);
        string cityName = GetCityName(city);
        string enemyFactionName = GetFactionName(employee != null ? employee.ownerFaction : null);

        switch (resultType)
        {
            case EmployeeAssignmentService.SpyDetectionResultType.SafeExit:
                return $"{enemyFactionName}의 산업 스파이 {employeeName}이 {cityName}에서 발견됐지만 무사히 이탈했습니다.";
            case EmployeeAssignmentService.SpyDetectionResultType.Escape:
                return $"{enemyFactionName}의 산업 스파이 {employeeName}이 {cityName}에서 발각되어 도주했습니다. 해당 세력과의 우호도가 감소했습니다.";
            case EmployeeAssignmentService.SpyDetectionResultType.Removed:
                return $"{enemyFactionName}의 산업 스파이 {employeeName}이 {cityName}에서 발각되어 제거되었습니다. 해당 세력과의 우호도가 감소했습니다.";
            default:
                return $"{enemyFactionName}의 산업 스파이 {employeeName}이 {cityName}에서 발견되었습니다.";
        }
    }

    private void ShowNotification(GameObject popupPrefab, string title, string description, CityScript city)
    {
        targetCity = city;
        ClosePopup();

        EmployeeNotificationToastButton popupView = GetOrCreatePopupView(popupPrefab);
        if (popupView == null)
        {
            Debug.LogWarning($"[EmployeeNotificationPopUpUI] Popup view is missing. title={title}");
            return;
        }

        currentPopupView = popupView;
        currentPopupView.SetContent(title, description);
        currentPopupView.Bind(ClosePopup, OpenTargetCityFromPopup);
        currentPopupView.Show();
    }

    private EmployeeNotificationToastButton GetOrCreatePopupView(GameObject popupPrefab)
    {
        if (popupPrefab == null)
            return null;

        if (popupCache.TryGetValue(popupPrefab, out EmployeeNotificationToastButton cachedView))
            return cachedView;

        Transform parent = popupParent != null ? popupParent : transform;
        GameObject instance = Instantiate(popupPrefab, parent);
        if (instance == null)
            return null;

        EmployeeNotificationToastButton popupView = instance.GetComponentInChildren<EmployeeNotificationToastButton>(true);
        if (popupView == null)
        {
            Debug.LogWarning("[EmployeeNotificationPopUpUI] Popup prefab is missing EmployeeNotificationToastButton.");
            Destroy(instance);
            return null;
        }

        popupView.Hide();
        popupCache.Add(popupPrefab, popupView);
        return popupView;
    }

    private void ResolveReferences()
    {
        if (employeeAssignmentService == null)
            employeeAssignmentService = FindFirstObjectByType<EmployeeAssignmentService>();
    }

    private string GetEmployeeName(EmployeeData employee)
    {
        return employee != null && !string.IsNullOrWhiteSpace(employee.employeeName)
            ? employee.employeeName
            : UnknownEmployeeName;
    }

    private string GetCityName(CityScript city)
    {
        return city != null && city.cityData != null && !string.IsNullOrWhiteSpace(city.cityData.cityName)
            ? CityDisplayNameUtility.ToKoreanDisplayName(city.cityData.cityName)
            : UnknownCityName;
    }

    private string GetFactionName(FactionManager faction)
    {
        return faction != null && !string.IsNullOrWhiteSpace(faction.factionName)
            ? faction.factionName
            : UnknownFactionName;
    }
}
