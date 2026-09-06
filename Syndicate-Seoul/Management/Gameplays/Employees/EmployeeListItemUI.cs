using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EmployeeListItemUI : MonoBehaviour
{
    [SerializeField] private Image employeeIconImage;
    [SerializeField] private TextMeshProUGUI employeeNameText;
    [SerializeField] private TextMeshProUGUI assignedCityText;
    [SerializeField] private Image spyProgressImage;
    [SerializeField] private Button selectButton;

    private CEODatabaseSO ceoDatabase;
    private EmployeeData employee;
    private Action<EmployeeData> onSelected;

    private void OnDestroy()
    {
        UnbindSelectButton();
    }

    public void SetData(EmployeeData _employee)
    {
        employee = _employee;
        string employeeName = _employee != null && !string.IsNullOrWhiteSpace(_employee.employeeName)
            ? _employee.employeeName
            : "Unknown Employee";
        string assignedCityName = GetAssignedCityName(_employee);

        if (employeeNameText != null)
            employeeNameText.text = employeeName;

        RefreshEmployeeIcon(_employee);

        if (assignedCityText != null)
            assignedCityText.text = assignedCityName;
        else
            Debug.LogWarning("[EmployeeListItemUI] Assigned city text is missing.");

        Debug.Log($"[EmployeeListItemUI] SetData. employee={employeeName}, location={assignedCityName}");

        RefreshSpyProgress(_employee);
        BindSelectButton();
    }

    public void SetData(EmployeeData _employee, Action<EmployeeData> _onSelected)
    {
        onSelected = _onSelected;
        SetData(_employee);
    }

    private void BindSelectButton()
    {
        if (selectButton == null)
            selectButton = GetComponent<Button>();

        if (selectButton == null)
            selectButton = GetComponentInChildren<Button>(true);

        if (selectButton == null)
            return;

        selectButton.onClick.RemoveListener(OnClickSelect);
        selectButton.onClick.AddListener(OnClickSelect);
    }

    private void UnbindSelectButton()
    {
        if (selectButton == null)
            return;

        selectButton.onClick.RemoveListener(OnClickSelect);
    }

    private void OnClickSelect()
    {
        onSelected?.Invoke(employee);
    }

    private string GetAssignedCityName(EmployeeData _employee)
    {
        if (_employee != null && _employee.isReturning)
            return $"\uBCF5\uADC0 \uC911 ({Mathf.Max(0, _employee.returnMonthCounter)}\uAC1C\uC6D4)";

        if (_employee == null || _employee.assignedCity == null || _employee.assignedCity.cityData == null)
            return "\uBBF8\uBC30\uCE58";

        string cityName = CityDisplayNameUtility.ToKoreanDisplayName(_employee.assignedCity.cityData.cityName);
        return string.IsNullOrWhiteSpace(cityName) ? "\uBBF8\uBC30\uCE58" : cityName;
    }

    private void RefreshSpyProgress(EmployeeData _employee)
    {
        if (spyProgressImage == null)
            return;

        bool showProgress = _employee != null && !_employee.isReturning && !_employee.isLost && _employee.IsSpy;
        spyProgressImage.gameObject.SetActive(showProgress);
        spyProgressImage.fillAmount = showProgress
            ? Mathf.Clamp01(_employee.assignedMonthCounter / 3f)
            : 0f;
    }

    private void RefreshEmployeeIcon(EmployeeData _employee)
    {
        if (employeeIconImage == null)
            return;

        CEOData ceo = ResolveCEOData(_employee);
        Sprite iconSprite = ceo != null ? ceo.iconSprite : null;
        if (iconSprite == null && ceo != null)
            iconSprite = ceo.profileSprite;

        employeeIconImage.sprite = iconSprite;
        employeeIconImage.enabled = iconSprite != null;
    }

    private CEOData ResolveCEOData(EmployeeData _employee)
    {
        if (_employee == null || string.IsNullOrWhiteSpace(_employee.ceoId))
            return null;

        if (ceoDatabase == null)
            ceoDatabase = ScriptableObject.CreateInstance<CEODatabaseSO>();

        return ceoDatabase.GetCEOByID(_employee.ceoId);
    }
}
