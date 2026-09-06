using System.Collections.Generic;
using UnityEngine;

public class FactionAITargetCityMemory : MonoBehaviour
{
    [SerializeField] private int defaultKeepMonthCount = 6;
    [SerializeField] private float ownerTooStrongPowerRatio = 2f;
    [SerializeField] private float warDefeatClearPowerRatio = 1.5f;
    [SerializeField] private int minimumCreditToKeepTargetCity = 30;

    private CityScript targetCity;
    private int targetSetMonth = -1;
    private int keepMonthCount;

    public int DefaultKeepMonthCount => Mathf.Max(1, defaultKeepMonthCount);

    public bool HasValidTargetCity(FactionManager _faction, int _currentMonth)
    {
        if (ShouldClearTargetCity(_faction, _currentMonth))
            ClearTargetCity(GetClearReason(_faction, _currentMonth));

        return targetCity != null;
    }

    public CityScript GetTargetCity()
    {
        return targetCity;
    }

    public void ExportSaveData(FactionAISaveData _data)
    {
        if (_data == null)
            return;

        _data.targetCityName = targetCity != null && targetCity.cityData != null
            ? targetCity.cityData.cityName
            : string.Empty;
        _data.targetSetMonth = targetSetMonth;
        _data.targetKeepMonthCount = keepMonthCount;
    }

    public void ImportSaveData(FactionAISaveData _data, IDictionary<string, CityScript> _citiesByName)
    {
        if (_data == null)
        {
            ClearTargetCity();
            return;
        }

        targetCity = null;
        targetSetMonth = _data.targetSetMonth;
        keepMonthCount = Mathf.Max(0, _data.targetKeepMonthCount);

        if (string.IsNullOrWhiteSpace(_data.targetCityName))
            return;

        string cityName = CityDisplayNameUtility.ToCanonicalName(_data.targetCityName);
        if (_citiesByName != null && _citiesByName.TryGetValue(cityName, out CityScript city) && city != null)
        {
            targetCity = city;
            return;
        }

        LogTargetCityWarning($"[AI][TargetCity] Target city not found while importing: {_data.targetCityName}");
    }

    public void SetTargetCity(CityScript _city, int _currentMonth, int _keepMonthCount)
    {
        if (_city == null)
            return;

        bool targetChanged = !ReferenceEquals(targetCity, _city);
        targetCity = _city;
        targetSetMonth = _currentMonth;
        keepMonthCount = Mathf.Max(1, _keepMonthCount > 0 ? _keepMonthCount : DefaultKeepMonthCount);

        if (targetChanged)
            LogTargetCity($"[AI][TargetCity] Set target city: {GetCityName(targetCity)}");
    }

    public void ClearTargetCity()
    {
        ClearTargetCity("clear requested");
    }

    private void ClearTargetCity(string reason)
    {
        if (targetCity != null)
            LogTargetCity($"[AI][TargetCity] Clear target city. reason={reason}");

        targetCity = null;
        targetSetMonth = -1;
        keepMonthCount = 0;
    }

    public bool ShouldClearTargetCity(FactionManager _faction, int _currentMonth)
    {
        if (targetCity == null)
            return false;

        if (targetCity.cityData == null)
            return true;

        if (_faction != null && ReferenceEquals(targetCity.cityData.owner, _faction))
            return true;

        if (_currentMonth >= 0 && targetSetMonth >= 0)
        {
            int monthsElapsed = _currentMonth - targetSetMonth;
            if (monthsElapsed >= Mathf.Max(1, keepMonthCount))
                return true;
        }

        if (targetCity.cityData.owner == null)
            return true;

        if (IsTargetOwnerTooStrong(_faction))
            return true;

        if (IsEconomyTooWeak(_faction))
            return true;

        return false;
    }

    public void NotifyWarResult(
        CityScript _city,
        FactionManager _attacker,
        FactionManager _defender,
        bool _attackerWon)
    {
        if (targetCity == null || _city == null || !ReferenceEquals(targetCity, _city))
            return;

        FactionManager attachedFaction = GetComponent<FactionManager>();
        if (attachedFaction != null && !ReferenceEquals(attachedFaction, _attacker))
            return;

        if (_attackerWon)
        {
            ClearTargetCity("war won");
            return;
        }

        int attackerPower = ManagementResourceCalculator.CalculateMilitaryScore(_attacker);
        int defenderPower = ManagementResourceCalculator.CalculateMilitaryScore(_defender);
        float clearRatio = Mathf.Max(1f, warDefeatClearPowerRatio);

        if (defenderPower >= Mathf.Max(1, attackerPower) * clearRatio)
        {
            ClearTargetCity("war defeat and defender too strong");
            return;
        }

        LogTargetCity("[AI][TargetCity] Keep target city. reason=war defeat but close power");
    }

    private string GetClearReason(FactionManager _faction, int _currentMonth)
    {
        if (targetCity == null)
            return "target is null";

        if (targetCity.cityData == null)
            return "cityData is null";

        if (_faction != null && ReferenceEquals(targetCity.cityData.owner, _faction))
            return "target city is now owned by faction";

        if (_currentMonth >= 0 && targetSetMonth >= 0)
        {
            int monthsElapsed = _currentMonth - targetSetMonth;
            if (monthsElapsed >= Mathf.Max(1, keepMonthCount))
                return "expired";
        }

        if (targetCity.cityData.owner == null)
            return "target owner is null";

        if (IsTargetOwnerTooStrong(_faction))
            return "target owner too strong";

        if (IsEconomyTooWeak(_faction))
            return "economy too weak";

        return "unknown";
    }

    private bool IsTargetOwnerTooStrong(FactionManager _faction)
    {
        if (_faction == null || targetCity == null || targetCity.cityData == null)
            return false;

        FactionManager owner = targetCity.cityData.owner;
        if (owner == null || ReferenceEquals(owner, _faction))
            return false;

        int factionPower = ManagementResourceCalculator.CalculateMilitaryScore(_faction);
        int ownerPower = ManagementResourceCalculator.CalculateMilitaryScore(owner);
        float clearRatio = Mathf.Max(1f, ownerTooStrongPowerRatio);

        return ownerPower >= Mathf.Max(1, factionPower) * clearRatio;
    }

    private bool IsEconomyTooWeak(FactionManager _faction)
    {
        return _faction != null && _faction.GetCredit < Mathf.Max(0, minimumCreditToKeepTargetCity);
    }

    private void LogTargetCity(string _message)
    {
        AIDebugLogger.LogAI(GetComponent<FactionManager>(), _message);
    }

    private void LogTargetCityWarning(string _message)
    {
        AIDebugLogger.LogAIWarning(GetComponent<FactionManager>(), _message);
    }

    private string GetCityName(CityScript city)
    {
        if (city != null && city.cityData != null && !string.IsNullOrWhiteSpace(city.cityData.cityName))
            return city.cityData.cityName;

        return city != null ? city.name : "Unknown";
    }
}
