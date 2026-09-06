using UnityEngine;

public class CitySharePurchaseService : MonoBehaviour
{
    [SerializeField] private int pricePerSharePercent = 30;

    public int GetPurchasePrice(int _amount)
    {
        if (_amount <= 0)
            return 0;

        return Mathf.Max(0, pricePerSharePercent) * _amount;
    }

    public bool CanPurchaseShare(CityScript _city, FactionManager _buyer, int _amount, out string _message)
    {
        if (_city == null)
        {
            _message = "City is missing.";
            return false;
        }

        if (_buyer == null)
        {
            _message = "Buyer is missing.";
            return false;
        }

        if (_amount <= 0)
        {
            _message = "Purchase amount must be greater than 0.";
            return false;
        }

        CityShareManager cityShareManager = CityShareManager.instance != null
            ? CityShareManager.instance
            : Object.FindFirstObjectByType<CityShareManager>();

        if (cityShareManager == null)
        {
            _message = "City share manager is missing.";
            return false;
        }

        if (!cityShareManager.TryPrepareCityShares(_city, out _message))
            return false;

        if (_city.cityData == null || _city.cityData.shareData == null)
        {
            _message = "City share data is missing.";
            return false;
        }

        if (_city.cityData.shareData.GetTotalShare() != 100)
        {
            _message = "City share total is invalid.";
            return false;
        }

        if (_city.cityData.shareData.UnassignedSharePercent < _amount)
        {
            _message = "Not enough corporate association shares.";
            return false;
        }

        int totalPrice = GetPurchasePrice(_amount);
        if (_buyer.GetCredit < totalPrice)
        {
            _message = "Not enough credit.";
            return false;
        }

        _message = string.Empty;
        return true;
    }

    public bool TryPurchaseShare(CityScript _city, FactionManager _buyer, int _amount, out string _message)
    {
        if (!CanPurchaseShare(_city, _buyer, _amount, out _message))
            return false;

        int totalPrice = GetPurchasePrice(_amount);
        if (!_buyer.ChangeCredit(-totalPrice))
        {
            _message = "Failed to deduct credit.";
            return false;
        }

        CityShareManager cityShareManager = CityShareManager.instance != null
            ? CityShareManager.instance
            : Object.FindFirstObjectByType<CityShareManager>();
        if (cityShareManager == null)
        {
            _buyer.ChangeCredit(totalPrice);
            _message = "City share manager is missing.";
            return false;
        }

        if (!cityShareManager.TransferShare(_city, null, _buyer, _amount, out _message))
        {
            if (!_buyer.ChangeCredit(totalPrice))
                Debug.LogError("[CitySharePurchaseService] Failed to roll back credit after share purchase failure.");

            return false;
        }

        _message = $"기업협회 지분 {_amount}%를 구매했습니다.";
        return true;
    }
}
