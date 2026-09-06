using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class EmployeeDetailPanelUIController : MonoBehaviour
{
    [SerializeField] private Image employeeIconImage;
    [SerializeField] private TMP_Text employeeNameText;
    [SerializeField] private TMP_Text assignedCityText;
    [SerializeField] private TMP_Text abilityNameText;
    [SerializeField] private TMP_Text abilityDescText;
    [Header("Buttons")]
    [FormerlySerializedAs("clickButton")]
    [SerializeField] private Button goToHireButton;
    [SerializeField] private Button hireButton;
    [SerializeField] private Button assignButton;

    private CEODatabaseSO ceoDatabase;
    private UnityAction currentClickHandler;
    private UnityAction currentHireHandler;
    private UnityAction currentAssignHandler;

    private void Awake()
    {
        SetEmployee(null);
    }

    private void OnDestroy()
    {
        ClearClickHandler();
        ClearHireHandler();
        ClearAssignHandler();
    }

    public void SetEmployee(EmployeeData _employee)
    {
        ClearClickHandler();
        ClearHireHandler();
        ClearAssignHandler();
        SetGoToHireButtonActive(false);
        SetHireButtonActive(false);
        SetAssignButtonActive(false);

        CEOData ceo = ResolveCEOData(_employee);

        if (employeeIconImage != null)
        {
            Sprite iconSprite = ceo != null ? ceo.iconSprite : null;
            if (iconSprite == null && ceo != null)
                iconSprite = ceo.profileSprite;

            employeeIconImage.sprite = iconSprite;
            employeeIconImage.enabled = iconSprite != null;
        }

        if (employeeNameText != null)
            employeeNameText.text = GetEmployeeName(_employee, ceo);

        if (assignedCityText != null)
            assignedCityText.text = GetAssignedCityName(_employee);

        SetAbilityTexts(
            GetAbilityName(_employee != null ? _employee.abilityType : EmployeeAbilityType.None),
            GetAbilityDescription(_employee != null ? _employee.abilityType : EmployeeAbilityType.None));
    }

    public void SetCandidate(CEOData _ceo)
    {
        ClearHireHandler();
        ClearAssignHandler();
        SetGoToHireButtonActive(false);
        SetHireButtonActive(_ceo != null && !string.IsNullOrWhiteSpace(_ceo.id));
        SetAssignButtonActive(false);

        if (employeeIconImage != null)
        {
            Sprite iconSprite = _ceo != null ? _ceo.iconSprite : null;
            if (iconSprite == null && _ceo != null)
                iconSprite = _ceo.profileSprite;

            employeeIconImage.sprite = iconSprite;
            employeeIconImage.enabled = iconSprite != null;
        }

        if (employeeNameText != null)
            employeeNameText.text = _ceo != null && !string.IsNullOrWhiteSpace(_ceo.name)
                ? _ceo.name
                : "Unknown CEO";

        if (assignedCityText != null)
            assignedCityText.text = "\uACE0\uC6A9 \uD6C4\uBCF4";

        SetAbilityTexts("-", "능력 정보 없음");
    }

    public void SetCandidate(CEOData _ceo, EmployeeAbilityType _abilityType)
    {
        SetCandidate(_ceo);

        if (_abilityType != EmployeeAbilityType.None)
            SetAbilityTexts(GetAbilityName(_abilityType), GetAbilityDescription(_abilityType));
    }

    public void SetEmptySlot()
    {
        ClearHireHandler();
        ClearAssignHandler();
        SetGoToHireButtonActive(true);
        SetHireButtonActive(false);
        SetAssignButtonActive(false);

        if (employeeIconImage != null)
        {
            employeeIconImage.sprite = null;
            employeeIconImage.enabled = false;
        }

        if (employeeNameText != null)
            employeeNameText.text = "\uBE48 \uC2AC\uB86F";

        if (assignedCityText != null)
            assignedCityText.text = "\uD074\uB9AD\uD558\uC5EC \uACE0\uC6A9";

        SetAbilityTexts("-", "직원이 없습니다.");
    }

    public void SetClickHandler(UnityAction _onClick)
    {
        ResolveGoToHireButton();

        if (goToHireButton == null)
            return;

        ClearClickHandler();
        currentClickHandler = _onClick;
        SetGoToHireButtonActive(currentClickHandler != null);

        if (currentClickHandler != null)
        {
            goToHireButton.onClick.RemoveListener(currentClickHandler);
            goToHireButton.onClick.AddListener(currentClickHandler);
        }
    }

    public void SetHireHandler(UnityAction _onHire)
    {
        if (hireButton == null)
        {
            Debug.LogWarning("[EmployeeDetailPanelUI] Hire button is missing.");
            return;
        }

        ClearHireHandler();
        currentHireHandler = _onHire;

        if (currentHireHandler != null)
        {
            hireButton.onClick.RemoveListener(currentHireHandler);
            hireButton.onClick.AddListener(currentHireHandler);
        }
    }

    public void SetAssignHandler(UnityAction _onAssign)
    {
        if (assignButton == null)
        {
            if (_onAssign != null)
                Debug.LogWarning("[EmployeeDetailPanelUI] Assign button is missing.");
            return;
        }

        ClearAssignHandler();
        currentAssignHandler = _onAssign;
        SetAssignButtonActive(currentAssignHandler != null);

        if (currentAssignHandler != null)
        {
            assignButton.onClick.RemoveListener(currentAssignHandler);
            assignButton.onClick.AddListener(currentAssignHandler);
        }
    }

    private void ClearClickHandler()
    {
        if (goToHireButton != null && currentClickHandler != null)
            goToHireButton.onClick.RemoveListener(currentClickHandler);

        currentClickHandler = null;
    }

    private void ClearHireHandler()
    {
        if (hireButton != null && currentHireHandler != null)
            hireButton.onClick.RemoveListener(currentHireHandler);

        currentHireHandler = null;
    }

    private void ClearAssignHandler()
    {
        if (assignButton != null && currentAssignHandler != null)
            assignButton.onClick.RemoveListener(currentAssignHandler);

        currentAssignHandler = null;
    }

    private void ResolveGoToHireButton()
    {
        if (goToHireButton != null)
            return;

        goToHireButton = GetComponent<Button>();
        if (goToHireButton == null)
            goToHireButton = GetComponentInChildren<Button>(true);
    }

    private void SetGoToHireButtonActive(bool isActive)
    {
        ResolveGoToHireButton();

        if (goToHireButton != null)
            goToHireButton.interactable = isActive;
    }

    private void SetHireButtonActive(bool isActive)
    {
        if (hireButton != null)
        {
            if (hireButton.gameObject != gameObject)
                hireButton.gameObject.SetActive(isActive);

            hireButton.interactable = isActive;
        }
    }

    public void SetAssignButtonActive(bool isActive)
    {
        if (assignButton != null)
        {
            if (assignButton.gameObject != gameObject)
                assignButton.gameObject.SetActive(isActive);

            assignButton.interactable = isActive;
        }
    }

    public void SetAssignButtonInteractable(bool isInteractable)
    {
        if (assignButton != null)
            assignButton.interactable = isInteractable;
    }

    private CEOData ResolveCEOData(EmployeeData employee)
    {
        if (employee == null || string.IsNullOrWhiteSpace(employee.ceoId))
            return null;

        if (ceoDatabase == null)
            ceoDatabase = ScriptableObject.CreateInstance<CEODatabaseSO>();

        return ceoDatabase.GetCEOByID(employee.ceoId);
    }

    private static string GetEmployeeName(EmployeeData employee, CEOData ceo)
    {
        if (employee == null)
            return "-";

        if (!string.IsNullOrWhiteSpace(employee.employeeName))
            return employee.employeeName;

        if (ceo != null && !string.IsNullOrWhiteSpace(ceo.name))
            return ceo.name;

        return string.IsNullOrWhiteSpace(employee.ceoId) ? "Unknown Employee" : employee.ceoId;
    }

    private static string GetAssignedCityName(EmployeeData employee)
    {
        if (employee == null || employee.assignedCity == null || employee.assignedCity.cityData == null)
            return "\uBBF8\uBC30\uCE58";

        string cityName = CityDisplayNameUtility.ToKoreanDisplayName(employee.assignedCity.cityData.cityName);
        return string.IsNullOrWhiteSpace(cityName) ? "\uBBF8\uBC30\uCE58" : cityName;
    }

    private void SetAbilityTexts(string abilityName, string abilityDescription)
    {
        if (abilityNameText != null)
            abilityNameText.text = abilityName;

        if (abilityDescText != null)
            abilityDescText.text = abilityDescription;
    }

    private static string GetAbilityName(EmployeeAbilityType abilityType)
    {
        switch (abilityType)
        {
            case EmployeeAbilityType.Financier:
                return "재무가";
            case EmployeeAbilityType.Researcher:
                return "연구가";
            case EmployeeAbilityType.SecurityExpert:
                return "보안 전문가";
            case EmployeeAbilityType.Operative:
                return "공작가";
            default:
                return "-";
        }
    }

    private static string GetAbilityDescription(EmployeeAbilityType abilityType)
    {
        switch (abilityType)
        {
            case EmployeeAbilityType.Financier:
                return "배치 도시의 크레딧 생산량 10% 증가";
            case EmployeeAbilityType.Researcher:
                return "배치 도시의 연구력 생산량 10% 증가";
            case EmployeeAbilityType.SecurityExpert:
                return "배치 도시의 적 스파이 발각 확률 10% 증가";
            case EmployeeAbilityType.Operative:
                return "스파이 활동 시 지분 조작 성공 확률 10% 증가";
            default:
                return "능력 없음";
        }
    }
}
