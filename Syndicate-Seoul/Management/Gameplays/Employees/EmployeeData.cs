using System;

[Serializable]
public enum EmployeeAbilityType
{
    None,
    Financier,
    Researcher,
    SecurityExpert,
    Operative
}

[Serializable]
public class EmployeeData
{
    public string employeeId;
    public string employeeName;
    public string ceoId;
    public string imageId;
    public string iconImageId;
    public EmployeeAbilityType abilityType;
    public FactionManager ownerFaction;
    public CityScript assignedCity;
    public int assignedMonthCounter;
    public bool isReturning;
    public int returnMonthCounter;
    public bool isLost;

    public bool IsAssigned => assignedCity != null && !isReturning && !isLost;

    public bool IsSpy
    {
        get
        {
            return !isReturning
                && !isLost
                && assignedCity != null
                && assignedCity.cityData != null
                && ownerFaction != null
                && assignedCity.cityData.owner != ownerFaction;
        }
    }
}
