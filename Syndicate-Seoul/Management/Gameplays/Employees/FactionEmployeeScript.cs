using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FactionEmployeeScript
{
    [SerializeField] private List<EmployeeData> employees = new List<EmployeeData>();

    private FactionManager ownerFaction;

    public void Initialize(FactionManager _ownerFaction)
    {
        ownerFaction = _ownerFaction;

        if (employees == null)
            employees = new List<EmployeeData>();

        for (int i = employees.Count - 1; i >= 0; i--)
        {
            if (employees[i] == null)
                employees.RemoveAt(i);
        }

        EnsureUniqueEmployeeIds();

        for (int i = 0; i < employees.Count; i++)
        {
            employees[i].ownerFaction = ownerFaction;

            if (employees[i].isLost)
            {
                employees[i].assignedCity = null;
                employees[i].isReturning = false;
                employees[i].returnMonthCounter = 0;
                employees[i].assignedMonthCounter = 0;
            }
            else if (employees[i].isReturning)
            {
                employees[i].assignedCity = null;
                employees[i].returnMonthCounter = Mathf.Max(0, employees[i].returnMonthCounter);
                employees[i].assignedMonthCounter = 0;
            }

            if (string.IsNullOrWhiteSpace(employees[i].employeeName))
                employees[i].employeeName = CreateDefaultEmployeeName(i + 1);

            if (employees[i].abilityType == EmployeeAbilityType.None)
                employees[i].abilityType = GetRandomEmployeeAbilityType();
        }

    }

    public IReadOnlyList<EmployeeData> GetEmployees()
    {
        List<EmployeeData> result = new List<EmployeeData>();

        if (employees == null)
            return result;

        for (int i = 0; i < employees.Count; i++)
        {
            EmployeeData employee = employees[i];
            if (employee != null && !employee.isLost)
                result.Add(employee);
        }

        return result;
    }

    public int AddDefaultEmployees(int _count)
    {
        if (_count <= 0)
            return 0;

        if (employees == null)
            employees = new List<EmployeeData>();

        EnsureUniqueEmployeeIds();

        int addedCount = 0;
        for (int i = 0; i < _count; i++)
        {
            EmployeeData employee = CreateDefaultEmployee(employees.Count + 1);
            employee.ownerFaction = ownerFaction;
            employees.Add(employee);
            addedCount++;
        }

        return addedCount;
    }

    public bool ProgressReturningEmployees()
    {
        if (employees == null)
            return false;

        bool changed = false;
        for (int i = 0; i < employees.Count; i++)
        {
            EmployeeData employee = employees[i];
            if (employee == null || employee.isLost || !employee.isReturning)
                continue;

            employee.returnMonthCounter = Mathf.Max(0, employee.returnMonthCounter - 1);
            if (employee.returnMonthCounter <= 0)
            {
                employee.isReturning = false;
                employee.returnMonthCounter = 0;
            }

            changed = true;
        }

        return changed;
    }

    public bool BeginEmployeeReturn(EmployeeData _employee, int _returnMonths, out string _message)
    {
        _message = string.Empty;

        if (_employee == null || employees == null || !employees.Contains(_employee))
        {
            _message = "Employee not found.";
            return false;
        }

        if (_employee.isLost)
        {
            _message = "Employee is lost.";
            return false;
        }

        _employee.assignedCity = null;
        _employee.assignedMonthCounter = 0;
        _employee.isReturning = true;
        _employee.returnMonthCounter = Mathf.Max(1, _returnMonths);
        _message = $"{_employee.employeeName} is returning.";
        return true;
    }

    public bool MarkEmployeeLost(EmployeeData _employee, out string _message)
    {
        _message = string.Empty;

        if (_employee == null || employees == null || !employees.Contains(_employee))
        {
            _message = "Employee not found.";
            return false;
        }

        if (_employee.isLost)
        {
            _message = "Employee is already lost.";
            return false;
        }

        _employee.assignedCity = null;
        _employee.assignedMonthCounter = 0;
        _employee.isReturning = false;
        _employee.returnMonthCounter = 0;
        _employee.isLost = true;
        _message = $"{_employee.employeeName} was removed.";
        return true;
    }

    public bool AddEmployeeFromCEO(CEOData _ceo, out EmployeeData _employee, out string _message)
    {
        return AddEmployeeFromCEO(_ceo, EmployeeAbilityType.None, out _employee, out _message);
    }

    public bool AddEmployeeFromCEO(CEOData _ceo, EmployeeAbilityType _abilityType, out EmployeeData _employee, out string _message)
    {
        _employee = null;
        _message = string.Empty;

        if (_ceo == null || string.IsNullOrWhiteSpace(_ceo.id))
        {
            _message = "CEO data is missing.";
            return false;
        }

        if (employees == null)
            employees = new List<EmployeeData>();

        if (HasEmployeeFromCEO(_ceo.id))
        {
            _message = $"CEO already hired: {_ceo.id}";
            return false;
        }

        if (ownerFaction != null)
        {
            int slotCount = ownerFaction.GetEmployeeSlotCount();
            int currentEmployeeCount = GetActiveEmployeeCount();
            if (currentEmployeeCount >= slotCount)
            {
                _message = "Employee slots are full.";
                return false;
            }
        }

        EnsureUniqueEmployeeIds();
        EmployeeAbilityType abilityType = _abilityType == EmployeeAbilityType.None
            ? GetRandomEmployeeAbilityType()
            : _abilityType;

        HashSet<string> usedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < employees.Count; i++)
        {
            if (employees[i] != null && !string.IsNullOrWhiteSpace(employees[i].employeeId))
                usedIds.Add(employees[i].employeeId);
        }

        _employee = new EmployeeData
        {
            employeeId = CreateNextEmployeeId(usedIds),
            employeeName = string.IsNullOrWhiteSpace(_ceo.name) ? _ceo.id : _ceo.name,
            ceoId = _ceo.id,
            imageId = _ceo.imageId,
            iconImageId = _ceo.iconImageId,
            abilityType = abilityType,
            ownerFaction = ownerFaction,
            assignedCity = null,
            assignedMonthCounter = 0
        };

        employees.Add(_employee);
        _message = $"{_employee.employeeName} hired.";
        return true;
    }

    public bool HasEmployeeFromCEO(string _ceoId)
    {
        if (employees == null || string.IsNullOrWhiteSpace(_ceoId))
            return false;

        for (int i = 0; i < employees.Count; i++)
        {
            EmployeeData employee = employees[i];
            if (employee == null || string.IsNullOrWhiteSpace(employee.ceoId))
                continue;

            if (string.Equals(employee.ceoId, _ceoId, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private int GetActiveEmployeeCount()
    {
        if (employees == null)
            return 0;

        int count = 0;
        for (int i = 0; i < employees.Count; i++)
        {
            EmployeeData employee = employees[i];
            if (employee != null && !employee.isLost)
                count++;
        }

        return count;
    }

    public List<EmployeeData> GetAssignedEmployees()
    {
        List<EmployeeData> result = new List<EmployeeData>();

        if (employees == null)
            return result;

        for (int i = 0; i < employees.Count; i++)
        {
            EmployeeData employee = employees[i];
            if (employee != null && !employee.isLost && employee.IsAssigned)
                result.Add(employee);
        }

        return result;
    }

    public List<EmployeeData> GetSpyEmployees()
    {
        List<EmployeeData> result = new List<EmployeeData>();

        if (employees == null)
            return result;

        for (int i = 0; i < employees.Count; i++)
        {
            EmployeeData employee = employees[i];
            if (employee != null && !employee.isLost && employee.IsSpy)
                result.Add(employee);
        }

        return result;
    }

    public bool HasAssignedEmployeeInOwnedCity(CityScript _city)
    {
        if (_city == null || _city.cityData == null || _city.cityData.owner != ownerFaction)
            return false;

        if (employees == null)
            return false;

        for (int i = 0; i < employees.Count; i++)
        {
            EmployeeData employee = employees[i];
            if (employee == null)
                continue;

            if (employee.isLost)
                continue;

            if (employee.ownerFaction == ownerFaction && employee.assignedCity == _city)
                return true;
        }

        return false;
    }

    public bool HasAssignedEmployeeAbilityInOwnedCity(CityScript _city, EmployeeAbilityType _abilityType)
    {
        if (_city == null || _city.cityData == null || _city.cityData.owner != ownerFaction)
            return false;

        if (employees == null)
            return false;

        for (int i = 0; i < employees.Count; i++)
        {
            EmployeeData employee = employees[i];
            if (employee == null)
                continue;

            if (employee.isLost || employee.isReturning)
                continue;

            if (employee.ownerFaction == ownerFaction
                && employee.assignedCity == _city
                && employee.abilityType == _abilityType)
            {
                return true;
            }
        }

        return false;
    }

    public bool AssignEmployee(string _employeeId, CityScript _city, out string _message)
    {
        _message = string.Empty;

        if (_city == null || _city.cityData == null)
        {
            _message = "City data is missing.";
            return false;
        }

        EmployeeData employee = FindEmployee(_employeeId);
        if (employee == null)
        {
            _message = $"Employee not found: {_employeeId}";
            return false;
        }

        if (employee.isLost)
        {
            _message = $"{employee.employeeName} is lost.";
            return false;
        }

        if (employee.isReturning)
        {
            _message = $"{employee.employeeName} is returning.";
            return false;
        }

        for (int i = 0; i < employees.Count; i++)
        {
            EmployeeData otherEmployee = employees[i];
            if (otherEmployee == null || otherEmployee == employee)
                continue;

            if (otherEmployee.assignedCity == _city)
            {
                _message = $"{GetFactionName()} already has an employee assigned to {GetCityName(_city)}.";
                return false;
            }
        }

        if (employee.assignedCity != _city)
            employee.assignedMonthCounter = 0;

        employee.ownerFaction = ownerFaction;
        employee.assignedCity = _city;
        _message = $"{employee.employeeName} assigned to {GetCityName(_city)}.";
        return true;
    }

    public bool UnassignEmployee(string _employeeId, out string _message)
    {
        _message = string.Empty;

        EmployeeData employee = FindEmployee(_employeeId);
        if (employee == null)
        {
            _message = $"Employee not found: {_employeeId}";
            return false;
        }

        if (employee.isLost)
        {
            _message = $"{employee.employeeName} is lost.";
            return false;
        }

        employee.assignedCity = null;
        employee.assignedMonthCounter = 0;
        employee.isReturning = false;
        employee.returnMonthCounter = 0;
        _message = $"{employee.employeeName} unassigned.";
        return true;
    }

    public List<EmployeeSaveData> ExportSaveData()
    {
        List<EmployeeSaveData> result = new List<EmployeeSaveData>();

        if (employees == null)
            return result;

        for (int i = 0; i < employees.Count; i++)
        {
            EmployeeData employee = employees[i];
            if (employee == null || string.IsNullOrWhiteSpace(employee.employeeId))
                continue;

            result.Add(new EmployeeSaveData
            {
                employeeId = employee.employeeId,
                employeeName = employee.employeeName,
                ceoId = employee.ceoId,
                imageId = employee.imageId,
                iconImageId = employee.iconImageId,
                abilityType = employee.abilityType,
                assignedCityName = GetCitySaveName(employee.assignedCity),
                assignedMonthCounter = Mathf.Max(0, employee.assignedMonthCounter),
                isReturning = employee.isReturning,
                returnMonthCounter = Mathf.Max(0, employee.returnMonthCounter),
                isLost = employee.isLost
            });
        }

        return result;
    }

    public void ImportSaveData(List<EmployeeSaveData> _data, IDictionary<string, CityScript> _citiesByName)
    {
        employees = new List<EmployeeData>();

        if (_data != null)
        {
            for (int i = 0; i < _data.Count; i++)
            {
                EmployeeSaveData saveData = _data[i];
                if (saveData == null)
                    continue;

                CityScript assignedCity = ResolveCity(saveData.assignedCityName, _citiesByName);
                EmployeeData employee = new EmployeeData
                {
                    employeeId = saveData.employeeId,
                    employeeName = saveData.employeeName,
                    ceoId = saveData.ceoId,
                    imageId = saveData.imageId,
                    iconImageId = saveData.iconImageId,
                    abilityType = saveData.abilityType,
                    ownerFaction = ownerFaction,
                    assignedCity = saveData.isReturning || saveData.isLost ? null : assignedCity,
                    assignedMonthCounter = saveData.isReturning || saveData.isLost ? 0 : Mathf.Max(0, saveData.assignedMonthCounter),
                    isReturning = saveData.isReturning && !saveData.isLost,
                    returnMonthCounter = saveData.isReturning && !saveData.isLost ? Mathf.Max(0, saveData.returnMonthCounter) : 0,
                    isLost = saveData.isLost
                };

                employees.Add(employee);
            }
        }

        Initialize(ownerFaction);
    }

    private EmployeeData FindEmployee(string employeeId)
    {
        if (employees == null || string.IsNullOrWhiteSpace(employeeId))
            return null;

        for (int i = 0; i < employees.Count; i++)
        {
            EmployeeData employee = employees[i];
            if (employee == null)
                continue;

            if (string.Equals(employee.employeeId, employeeId, StringComparison.OrdinalIgnoreCase))
                return employee;
        }

        return null;
    }

    private void EnsureUniqueEmployeeIds()
    {
        HashSet<string> usedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < employees.Count; i++)
        {
            EmployeeData employee = employees[i];
            if (employee == null)
                continue;

            if (string.IsNullOrWhiteSpace(employee.employeeId) || !usedIds.Add(employee.employeeId))
            {
                employee.employeeId = CreateNextEmployeeId(usedIds);
                usedIds.Add(employee.employeeId);
            }
        }
    }

    private EmployeeData CreateDefaultEmployee(int employeeNumber)
    {
        HashSet<string> usedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < employees.Count; i++)
        {
            if (employees[i] != null && !string.IsNullOrWhiteSpace(employees[i].employeeId))
                usedIds.Add(employees[i].employeeId);
        }

        return new EmployeeData
        {
            employeeId = CreateNextEmployeeId(usedIds),
            employeeName = CreateDefaultEmployeeName(employeeNumber),
            abilityType = GetRandomEmployeeAbilityType(),
            ownerFaction = ownerFaction,
            assignedCity = null,
            assignedMonthCounter = 0
        };
    }

    public static EmployeeAbilityType GetRandomEmployeeAbilityType()
    {
        EmployeeAbilityType[] values = (EmployeeAbilityType[])Enum.GetValues(typeof(EmployeeAbilityType));
        List<EmployeeAbilityType> availableTypes = new List<EmployeeAbilityType>();

        for (int i = 0; i < values.Length; i++)
        {
            if (values[i] != EmployeeAbilityType.None)
                availableTypes.Add(values[i]);
        }

        if (availableTypes.Count == 0)
            return EmployeeAbilityType.None;

        int index = UnityEngine.Random.Range(0, availableTypes.Count);
        return availableTypes[index];
    }

    private string CreateNextEmployeeId(HashSet<string> usedIds)
    {
        string factionKey = GetFactionSaveKey();

        for (int i = 1; i < 1000; i++)
        {
            string employeeId = $"{factionKey}_employee_{i:000}";
            if (usedIds == null || !usedIds.Contains(employeeId))
                return employeeId;
        }

        return $"{factionKey}_employee_{Guid.NewGuid():N}";
    }

    private string CreateDefaultEmployeeName(int employeeNumber)
    {
        return $"{GetFactionName()} Employee {employeeNumber}";
    }

    private string GetFactionSaveKey()
    {
        string factionKey = ownerFaction != null ? ownerFaction.GetSaveKey() : string.Empty;
        if (string.IsNullOrWhiteSpace(factionKey))
            factionKey = GetFactionName();

        if (string.IsNullOrWhiteSpace(factionKey))
            factionKey = "faction";

        return factionKey.Trim().Replace(' ', '_');
    }

    private string GetFactionName()
    {
        if (ownerFaction == null || string.IsNullOrWhiteSpace(ownerFaction.factionName))
            return "Faction";

        return ownerFaction.factionName;
    }

    private CityScript ResolveCity(string cityName, IDictionary<string, CityScript> citiesByName)
    {
        if (string.IsNullOrWhiteSpace(cityName))
            return null;

        string canonicalCityName = CityDisplayNameUtility.ToCanonicalName(cityName);
        if (citiesByName != null && citiesByName.TryGetValue(canonicalCityName, out CityScript city))
            return city;

        CityScript[] cities = UnityEngine.Object.FindObjectsByType<CityScript>(FindObjectsSortMode.None);
        for (int i = 0; i < cities.Length; i++)
        {
            CityScript candidate = cities[i];
            if (candidate == null || candidate.cityData == null)
                continue;

            string candidateCityName = CityDisplayNameUtility.ToCanonicalName(candidate.cityData.cityName);
            if (string.Equals(candidateCityName, canonicalCityName, StringComparison.OrdinalIgnoreCase))
                return candidate;
        }

        Debug.LogWarning($"[FactionEmployee] City not found while importing employee assignment: {cityName}");
        return null;
    }

    private string GetCityName(CityScript city)
    {
        return city != null && city.cityData != null
            ? CityDisplayNameUtility.ToKoreanDisplayName(city.cityData.cityName)
            : string.Empty;
    }

    private string GetCitySaveName(CityScript city)
    {
        return city != null && city.cityData != null
            ? CityDisplayNameUtility.ToCanonicalName(city.cityData.cityName)
            : string.Empty;
    }
}
