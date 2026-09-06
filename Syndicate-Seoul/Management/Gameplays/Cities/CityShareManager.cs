using System;
using UnityEngine;

public class CityShareManager : MonoBehaviour
{
    public static CityShareManager instance;
    public event Action<CityScript, FactionManager, FactionManager> AnyCityShareChanged;

    private void Awake()
    {
        if (instance == null)
            instance = this;
        else if (instance != this)
            Destroy(gameObject);
    }

    private void OnEnable()
    {
        BindOwnershipEvents();
    }

    private void Start()
    {
        BindOwnershipEvents();
        InitializeAllCityShares(false);
    }

    private void OnDisable()
    {
        if (CityOwnershipManager.instance != null)
            CityOwnershipManager.instance.AnyCityOwnerChanged -= OnCityOwnerChanged;
    }

    private void BindOwnershipEvents()
    {
        if (CityOwnershipManager.instance == null)
            return;

        CityOwnershipManager.instance.AnyCityOwnerChanged -= OnCityOwnerChanged;
        CityOwnershipManager.instance.AnyCityOwnerChanged += OnCityOwnerChanged;
    }

    private void OnCityOwnerChanged(CityScript _city, FactionManager _oldOwner, FactionManager _newOwner)
    {
        TryInitializeCityShares(_city, false, out _);
    }

    public void InitializeAllCityShares(bool _force)
    {
        CityScript[] cities = FindObjectsByType<CityScript>(FindObjectsSortMode.None);
        for (int i = 0; i < cities.Length; i++)
            TryInitializeCityShares(cities[i], _force, out _);
    }

    public bool TryPrepareCityShares(CityScript _city, out string _message)
    {
        if (!TryInitializeCityShares(_city, false, out _message))
            return false;

        if (!HasInitializedShares(_city))
        {
            _message = "City share data is not initialized.";
            return false;
        }

        return true;
    }

    public bool TryInitializeCityShares(CityScript _city, bool _force, out string _message)
    {
        if (_city == null || _city.cityData == null)
        {
            _message = "City data is missing.";
            return false;
        }

        CityShareData shareData = EnsureShareData(_city.cityData);
        bool hasAssignedShares = shareData.GetTotalAssignedShare() > 0;

        if (!_force && hasAssignedShares)
        {
            _message = string.Empty;
            return true;
        }

        shareData.ClearAllShares();

        if (_city.cityData.owner == null)
        {
            _message = "City owner is missing. Share data was initialized as fully unassigned.";
            return true;
        }

        if (!shareData.SetShare(_city.cityData.owner, 25))
        {
            _message = "Failed to give the city owner the default 25% share.";
            return false;
        }

        _message = string.Empty;
        return true;
    }

    public bool HasCityShareData(CityScript _city)
    {
        return _city != null && _city.cityData != null && _city.cityData.shareData != null;
    }

    public bool HasInitializedShares(CityScript _city)
    {
        if (!HasCityShareData(_city))
            return false;

        CityShareData shareData = _city.cityData.shareData;
        return shareData.GetTotalShare() == 100;
    }

    public bool HasShare(CityScript _city, FactionManager _faction)
    {
        if (!TryGetShareData(_city, out CityShareData shareData, out _))
            return false;

        return shareData.GetShare(_faction) > 0;
    }

    public bool IsSoleTopShareHolder(CityScript _city, FactionManager _faction)
    {
        if (_faction == null)
            return false;

        if (!TryGetShareData(_city, out CityShareData shareData, out _))
            return false;

        return ReferenceEquals(shareData.GetHighestShareHolder(), _faction);
    }

    public bool CanClaimOwnership(CityScript _city, FactionManager _faction, out string _message)
    {
        if (_faction == null)
        {
            _message = "Faction is missing.";
            return false;
        }

        if (!TryGetShareData(_city, out CityShareData shareData, out _message))
            return false;

        if (shareData.HasTieForHighestShare())
        {
            _message = "There is a tie for the highest city share.";
            return false;
        }

        FactionManager highestShareHolder = shareData.GetHighestShareHolder();
        if (highestShareHolder == null)
        {
            _message = "There is no sole highest city share holder.";
            return false;
        }

        if (!ReferenceEquals(highestShareHolder, _faction))
        {
            _message = $"{_faction.factionName} does not hold the highest city share.";
            return false;
        }

        _message = string.Empty;
        return true;
    }

    public bool TransferShare(CityScript _city, FactionManager _fromFaction, FactionManager _toFaction, int _amount, out string _message)
    {
        if (_amount <= 0)
        {
            _message = "Share transfer amount must be greater than 0.";
            return false;
        }

        if (ReferenceEquals(_fromFaction, _toFaction))
        {
            _message = "Source and destination share holders must be different.";
            return false;
        }

        if (!TryGetShareData(_city, out CityShareData shareData, out _message))
            return false;

        if (_fromFaction == null && _toFaction == null)
        {
            _message = "Both source and destination share holders are missing.";
            return false;
        }

        if (_fromFaction == null)
        {
            if (shareData.UnassignedSharePercent < _amount)
            {
                _message = "Not enough unassigned shares.";
                return false;
            }

            if (!shareData.ChangeShare(_toFaction, _amount))
            {
                _message = "Failed to assign shares to the target faction.";
                return false;
            }
        }
        else if (_toFaction == null)
        {
            if (shareData.GetShare(_fromFaction) < _amount)
            {
                _message = $"{_fromFaction.factionName} does not have enough shares.";
                return false;
            }

            if (!shareData.ChangeShare(_fromFaction, -_amount))
            {
                _message = "Failed to return shares to the unassigned pool.";
                return false;
            }
        }
        else
        {
            if (shareData.GetShare(_fromFaction) < _amount)
            {
                _message = $"{_fromFaction.factionName} does not have enough shares.";
                return false;
            }

            if (!shareData.ChangeShare(_fromFaction, -_amount))
            {
                _message = "Failed to deduct shares from the source faction.";
                return false;
            }

            if (!shareData.ChangeShare(_toFaction, _amount))
            {
                shareData.ChangeShare(_fromFaction, _amount);
                _message = "Failed to add shares to the destination faction.";
                return false;
            }
        }

        if (shareData.GetTotalShare() != 100)
        {
            _message = "City share total became invalid after transfer.";
            return false;
        }

        AnyCityShareChanged?.Invoke(_city, _fromFaction, _toFaction);
        _message = string.Empty;
        return true;
    }

    public void TransferAllShares(FactionManager _fromFaction, FactionManager _toFaction)
    {
        if (_fromFaction == null || _toFaction == null || ReferenceEquals(_fromFaction, _toFaction))
            return;

        CityScript[] cities = FindObjectsByType<CityScript>(FindObjectsSortMode.None);
        for (int i = 0; i < cities.Length; i++)
        {
            CityScript city = cities[i];
            if (!TryGetShareData(city, out CityShareData shareData, out string message))
            {
                Debug.LogWarning($"[CityShareManager] Failed to read city share data while transferring all shares. reason={message}");
                continue;
            }

            int sharePercent = shareData.GetShare(_fromFaction);
            if (sharePercent <= 0)
                continue;

            if (!TransferShare(city, _fromFaction, _toFaction, sharePercent, out message))
                Debug.LogWarning($"[CityShareManager] Failed to transfer all shares. city={GetCityName(city)}, from={_fromFaction.factionName}, to={_toFaction.factionName}, amount={sharePercent}, reason={message}");
        }
    }

    private static CityShareData EnsureShareData(CityData cityData)
    {
        if (cityData.shareData == null)
            cityData.shareData = new CityShareData();

        return cityData.shareData;
    }

    private bool TryGetShareData(CityScript _city, out CityShareData shareData, out string _message)
    {
        shareData = null;

        if (_city == null || _city.cityData == null)
        {
            _message = "City data is missing.";
            return false;
        }

        shareData = EnsureShareData(_city.cityData);

        if (shareData.GetTotalShare() != 100)
        {
            _message = "City share total is invalid.";
            return false;
        }

        _message = string.Empty;
        return true;
    }

    private static string GetCityName(CityScript city)
    {
        return city != null && city.cityData != null && !string.IsNullOrWhiteSpace(city.cityData.cityName)
            ? city.cityData.cityName
            : "Unknown";
    }
}
