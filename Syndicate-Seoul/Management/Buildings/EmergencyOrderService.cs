using UnityEngine;

public class EmergencyOrderState
{
    public bool isVisible;
    public bool canExecute;
    public int cost;
    public string failReason;
}

public class EmergencyOrderService : MonoBehaviour
{
    public static EmergencyOrderState GetState(CityScript _city, int _buildingIndex, FactionManager _playerFac)
    {
        BuildingInstance building = GetBuilding(_city, _buildingIndex);
        BuildingData buildingData = building != null ? building.data : null;

        if (buildingData == null || !buildingData.IsFactory())
        {
            return new EmergencyOrderState
            {
                isVisible = false,
                canExecute = false,
                cost = 0,
                failReason = string.Empty
            };
        }

        bool canExecute = CanExecute(_city, building, _playerFac, out string failReason);

        return new EmergencyOrderState
        {
            isVisible = true,
            canExecute = canExecute,
            cost = buildingData.emergencyOrderCost,
            failReason = failReason
        };
    }

    public static bool TryExecute(CityScript _city, int _buildingIndex, FactionManager _playerFac, out string _failReason)
    {
        BuildingInstance building = GetBuilding(_city, _buildingIndex);
        if (!CanExecute(_city, building, _playerFac, out _failReason))
            return false;

        FactionManager owner = _city.cityData.owner;
        if (owner == null)
        {
            _failReason = "Factory owner is missing.";
            return false;
        }

        if (!owner.ChangeCredit(-building.data.emergencyOrderCost))
        {
            _failReason = "Failed to spend credit.";
            return false;
        }

        if (!FactoryProductionService.TryProduceCards(building.data, owner))
        {
            owner.ChangeCredit(building.data.emergencyOrderCost);
            _failReason = "Card pack production failed.";
            return false;
        }

        _failReason = string.Empty;
        return true;
    }

    private static bool CanExecute(CityScript _city, BuildingInstance _building, FactionManager _playerFac, out string _failReason)
    {
        if (_city == null || _city.cityData == null)
        {
            _failReason = "Selected city is missing.";
            return false;
        }

        if (_building == null || _building.data == null || !_building.data.IsFactory())
        {
            _failReason = "Factory data is missing.";
            return false;
        }

        if (_building.IsUnderConstruction() && !_building.IsUpgrading())
        {
            _failReason = "Factory is under construction.";
            return false;
        }

        if (_building.IsDisabled())
        {
            _failReason = "Factory is disabled.";
            return false;
        }

        if (_city.cityData.owner == null)
        {
            _failReason = "City owner is missing.";
            return false;
        }

        if (_playerFac == null)
        {
            _failReason = "Player faction is missing.";
            return false;
        }

        if (_city.cityData.owner != _playerFac)
        {
            _failReason = "Emergency order can only be used in player-owned cities.";
            return false;
        }

        if (_playerFac.GetCredit < _building.data.emergencyOrderCost)
        {
            _failReason = "Not enough credit.";
            return false;
        }

        _failReason = string.Empty;
        return true;
    }

    private static BuildingInstance GetBuilding(CityScript _city, int _buildingIndex)
    {
        if (_city == null)
            return null;

        return _city.GetBuildingInstance(_buildingIndex);
    }
}
