using System.Collections.Generic;
using UnityEngine;

public class FactionAIEmployeePlanner : MonoBehaviour
{
    public bool TryRun(
        FactionManager _faction,
        BigFivePersonality _personality,
        FactionAITargetCityMemory _targetCityMemory,
        int _currentMonthIndex)
    {
        if (_faction == null || _faction.IsPlayerFaction || _faction.IsEliminated)
            return false;

        bool changed = false;
        if (_faction.TryUnlockInitialEmployeeByBuildingCount())
            changed = true;

        if (TryHireEmployee(_faction))
            changed = true;

        if (TryAssignEmployee(_faction, _personality, _targetCityMemory, _currentMonthIndex))
            changed = true;

        return changed;
    }

    private bool TryHireEmployee(FactionManager faction)
    {
        if (faction == null || !faction.IsEmployeeHiringUnlocked)
            return false;

        CEOStartupAssigner startupAssigner = FindFirstObjectByType<CEOStartupAssigner>();
        IReadOnlyList<CEOData> leftoverCEOs = startupAssigner != null ? startupAssigner.GetLeftoverCEOs() : null;
        if (leftoverCEOs == null || leftoverCEOs.Count == 0)
            return false;

        List<CEOData> candidates = new List<CEOData>();
        for (int i = 0; i < leftoverCEOs.Count; i++)
        {
            CEOData ceo = leftoverCEOs[i];
            if (ceo == null || string.IsNullOrWhiteSpace(ceo.id))
                continue;

            if (IsCEOAlreadyHired(ceo.id))
                continue;

            candidates.Add(ceo);
        }

        if (candidates.Count == 0)
            return false;

        CEOData selectedCEO = candidates[Random.Range(0, candidates.Count)];
        if (!faction.TryHireEmployeeFromCEO(selectedCEO, out EmployeeData employee, out _))
            return false;

        AIDebugLogger.LogAI(
            faction,
            $"[AI][Employee] Hired employee. employee={GetEmployeeName(employee)}, ceoId={selectedCEO.id}");
        return true;
    }

    private bool TryAssignEmployee(
        FactionManager faction,
        BigFivePersonality personality,
        FactionAITargetCityMemory targetCityMemory,
        int currentMonthIndex)
    {
        EmployeeData employee = FindUnassignedEmployee(faction);
        if (employee == null)
            return false;

        CityScript targetCity = ShouldAssignSpy(faction, personality, targetCityMemory, currentMonthIndex)
            ? targetCityMemory.GetTargetCity()
            : null;

        if (targetCity == null)
            targetCity = FindBestOwnedCityForEmployee(faction);

        if (targetCity == null)
            return false;

        if (employee.assignedCity == targetCity)
            return false;

        if (!faction.AssignEmployee(employee.employeeId, targetCity, out string message))
            return false;

        AIDebugLogger.LogAI(
            faction,
            $"[AI][Employee] Assigned employee. employee={GetEmployeeName(employee)}, city={GetCityName(targetCity)}, message={message}");
        return true;
    }

    private bool IsCEOAlreadyHired(string ceoId)
    {
        if (string.IsNullOrWhiteSpace(ceoId))
            return true;

        FactionManager[] factions = FindObjectsByType<FactionManager>(FindObjectsSortMode.None);
        for (int i = 0; i < factions.Length; i++)
        {
            FactionManager faction = factions[i];
            if (faction != null && faction.HasEmployeeFromCEO(ceoId))
                return true;
        }

        return false;
    }

    private EmployeeData FindUnassignedEmployee(FactionManager faction)
    {
        IReadOnlyList<EmployeeData> employees = faction != null ? faction.GetEmployees() : null;
        if (employees == null)
            return null;

        for (int i = 0; i < employees.Count; i++)
        {
            EmployeeData employee = employees[i];
            if (employee == null || employee.isLost || employee.isReturning || employee.IsAssigned)
                continue;

            return employee;
        }

        return null;
    }

    private bool ShouldAssignSpy(
        FactionManager faction,
        BigFivePersonality personality,
        FactionAITargetCityMemory targetCityMemory,
        int currentMonthIndex)
    {
        if (faction == null || personality == null || targetCityMemory == null)
            return false;

        if (personality.extraversion < 60 && personality.agreeableness > 40)
            return false;

        if (!targetCityMemory.HasValidTargetCity(faction, currentMonthIndex))
            return false;

        CityScript targetCity = targetCityMemory.GetTargetCity();
        if (targetCity == null || targetCity.cityData == null)
            return false;

        if (targetCity.cityData.owner == null || ReferenceEquals(targetCity.cityData.owner, faction))
            return false;

        return !HasAssignedEmployeeInCity(faction, targetCity);
    }

    private CityScript FindBestOwnedCityForEmployee(FactionManager faction)
    {
        if (faction == null || faction.ownedCities == null)
            return null;

        CityScript bestCity = null;
        int bestScore = int.MinValue;

        for (int i = 0; i < faction.ownedCities.Count; i++)
        {
            CityScript city = faction.ownedCities[i];
            if (city == null || city.cityData == null || city.cityData.buildings == null)
                continue;

            if (!ReferenceEquals(city.cityData.owner, faction))
                continue;

            if (HasAssignedEmployeeInCity(faction, city))
                continue;

            int score = CalculateCityEmployeeValue(city);
            if (score <= bestScore)
                continue;

            bestScore = score;
            bestCity = city;
        }

        return bestCity;
    }

    private int CalculateCityEmployeeValue(CityScript city)
    {
        if (city == null || city.cityData == null || city.cityData.buildings == null)
            return 0;

        int score = 0;
        for (int i = 0; i < city.cityData.buildings.Count; i++)
        {
            BuildingInstance building = city.cityData.buildings[i];
            if (building == null || building.IsEmptySlot() || building.IsUnderConstruction() || building.IsUpgrading())
                continue;

            if (building.data == null)
                continue;

            score += Mathf.Max(0, building.data.bonusIncome);
            score += Mathf.Max(0, building.data.rpOutput);
            score += Mathf.Max(0, building.data.powerOutput);
        }

        return score;
    }

    private bool HasAssignedEmployeeInCity(FactionManager faction, CityScript city)
    {
        IReadOnlyList<EmployeeData> employees = faction != null ? faction.GetEmployees() : null;
        if (employees == null || city == null)
            return false;

        for (int i = 0; i < employees.Count; i++)
        {
            EmployeeData employee = employees[i];
            if (employee == null || employee.isLost || employee.isReturning)
                continue;

            if (employee.assignedCity == city)
                return true;
        }

        return false;
    }

    private string GetEmployeeName(EmployeeData employee)
    {
        if (employee != null && !string.IsNullOrWhiteSpace(employee.employeeName))
            return employee.employeeName;

        if (employee != null && !string.IsNullOrWhiteSpace(employee.employeeId))
            return employee.employeeId;

        return "Unknown";
    }

    private string GetCityName(CityScript city)
    {
        if (city == null || city.cityData == null)
            return "Unknown";

        if (!string.IsNullOrWhiteSpace(city.cityData.cityName))
            return city.cityData.cityName;

        return city.name;
    }
}
