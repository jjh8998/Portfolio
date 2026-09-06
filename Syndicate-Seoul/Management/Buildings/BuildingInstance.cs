using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class BuildingInstance
{
    public BuildingData data;
    public BuildingData upgradeTargetData;
    public int remainConstructionDay;
    public int remainActivationIntervalDays;
    public float factoryProductionProgressBuffer;
    public bool isDisabled;

    /// <summary>
    /// 스캐빈저 무리가 점거 중인 잠긴 슬롯 여부. true이면 전투로 해금하기 전까지 건설/철거 불가.
    /// </summary>
    public bool scavengerLocked;

    [SerializeField] private int lastProcessedYear = -1;
    [SerializeField] private int lastProcessedMonth = -1;
    [SerializeField] private int lastProcessedDay = -1;

    public BuildingInstance(BuildingData data)
    {
        this.data = data;

        if (data != null)
        {
            remainConstructionDay = 0;
            remainActivationIntervalDays = Mathf.Max(1, data.activationIntervalDays);
            factoryProductionProgressBuffer = 0f;
        }
    }

    public bool IsEmptySlot()
    {
        return data == null || data.IsEmptySlot();
    }

    public bool IsScavengerLocked()
    {
        return scavengerLocked;
    }

    public void SetScavengerLocked(bool _locked)
    {
        scavengerLocked = _locked;
    }

    public bool IsUnderConstruction()
    {
        return !IsEmptySlot() && remainConstructionDay > 0;
    }

    public bool IsUpgrading()
    {
        return !IsEmptySlot() && upgradeTargetData != null && remainConstructionDay > 0;
    }

    public bool IsDisabled()
    {
        return isDisabled;
    }

    public bool IsOperational()
    {
        return !IsEmptySlot() && (!IsUnderConstruction() || IsUpgrading()) && !isDisabled;
    }

    public void SetDisabled(bool _isDisabled)
    {
        isDisabled = _isDisabled;
    }

    public void ToggleDisabled()
    {
        isDisabled = !isDisabled;
    }

    public void StartConstruction()
    {
        if (data == null)
        {
            remainConstructionDay = 0;
            return;
        }

        remainConstructionDay = Mathf.Max(0, data.constructionDay);
        remainActivationIntervalDays = Mathf.Max(1, data.activationIntervalDays);
        factoryProductionProgressBuffer = 0f;
        upgradeTargetData = null;
    }

    public void StartConstruction(BuildingData _targetBuilding)
    {
        if (_targetBuilding == null)
        {
            remainConstructionDay = 0;
            upgradeTargetData = null;
            return;
        }

        if (IsEmptySlot())
        {
            data = _targetBuilding;
            upgradeTargetData = null;
            remainActivationIntervalDays = Mathf.Max(1, data.activationIntervalDays);
            factoryProductionProgressBuffer = 0f;
        }
        else
        {
            upgradeTargetData = _targetBuilding;
        }

        remainConstructionDay = Mathf.Max(0, _targetBuilding.constructionDay);

        if (remainConstructionDay == 0)
            ApplyConstructionCompletion();
    }

    public bool ProgressConstructionDay()
    {
        if (!IsUnderConstruction())
        {
            return false;
        }

        remainConstructionDay--;

        if (remainConstructionDay < 0)
        {
            remainConstructionDay = 0;
        }

        if (remainConstructionDay == 0)
            ApplyConstructionCompletion();

        return remainConstructionDay == 0;
    }

    private void ApplyConstructionCompletion()
    {
        remainConstructionDay = 0;

        if (upgradeTargetData != null)
        {
            data = upgradeTargetData;
            upgradeTargetData = null;
        }

        if (data != null)
        {
            remainActivationIntervalDays = Mathf.Max(1, data.activationIntervalDays);
            factoryProductionProgressBuffer = 0f;
        }
    }

    public void GetLastProcessedDate(out int _year, out int _month, out int _day)
    {
        _year = lastProcessedYear;
        _month = lastProcessedMonth;
        _day = lastProcessedDay;
    }

    public void SetLastProcessedDate(int _year, int _month, int _day)
    {
        lastProcessedYear = _year;
        lastProcessedMonth = _month;
        lastProcessedDay = _day;
    }
}
