using UnityEngine;

public static partial class FactoryBuildingProgressService
{
    public static void ProgressDay(BuildingInstance _building, GameDate _date, FactionManager _owner)
    {
        if (_building == null || _building.data == null || !_building.data.IsFactory())
        {
            return;
        }

        if (!_building.IsOperational())
        {
            return;
        }

        if (_date == null)
        {
            Debug.LogWarning("[Factory] Date is null. Production skipped.");
            return;
        }

        if (_owner == null)
        {
            Debug.LogWarning("[Factory] Owner is null. Production skipped.");
            return;
        }

        if (HasProcessedDate(_building, _date))
        {
            return;
        }

        SetProcessedDate(_building, _date);

        float powerSatisfactionRate = ManagementResourceCalculator.CalculatePowerSatisfactionRate(_owner);
        if (powerSatisfactionRate <= 0f)
        {
            return;
        }

        if (_building.remainActivationIntervalDays > 0)
        {
            _building.factoryProductionProgressBuffer += powerSatisfactionRate;

            while (_building.factoryProductionProgressBuffer >= 1f && _building.remainActivationIntervalDays > 0)
            {
                _building.remainActivationIntervalDays--;
                _building.factoryProductionProgressBuffer -= 1f;
            }
        }

        if (_building.remainActivationIntervalDays > 0)
        {
            return;
        }

        if (FactoryProductionService.TryProduceCards(_building.data, _owner))
        {
            _building.remainActivationIntervalDays = Mathf.Max(1, _building.data.activationIntervalDays);
            _building.factoryProductionProgressBuffer = 0f;
        }
    }

    private static bool HasProcessedDate(BuildingInstance _building, GameDate _date)
    {
        _building.GetLastProcessedDate(out int year, out int month, out int day);
        return year == _date.year
            && month == _date.month
            && day == _date.day;
    }

    private static void SetProcessedDate(BuildingInstance _building, GameDate _date)
    {
        _building.SetLastProcessedDate(_date.year, _date.month, _date.day);
    }
}

