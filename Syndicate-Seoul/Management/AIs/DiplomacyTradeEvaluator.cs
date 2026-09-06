using UnityEngine;

public partial class DiplomacyTradeEvaluator
{
    private const int PowerValueWeight = 2;
    private const int CardTierValueWeight = 2;
    private const int OwnCityShareProtectionValue = 1000;
    private const int OwnCityShareProtectionCreditThreshold = 100;
    private const float OneYearPatentLicenseMultiplier = 1.0f;
    private const float ThreeYearPatentLicenseMultiplier = 2.5f;
    private const float FiveYearPatentLicenseMultiplier = 4.0f;
    private const float DefaultPatentLicenseStrategyMultiplier = 1.0f;

    private const string TradeRejectedByAiMessage = "The AI does not consider this trade favorable.";
    private const string TradeAcceptedMessage = "Trade accepted.";

    public bool TryEvaluate(DiplomacyTradeRequest _request, out string _reason)
    {
        return TryEvaluate(_request, null, null, out _reason);
    }

    public bool TryEvaluate(
        DiplomacyTradeRequest _request,
        FactionManager _playerFaction,
        FactionManager _targetFaction,
        out string _reason)
    {
        return TryEvaluate(_request, _playerFaction, _targetFaction, out _reason, out _, out _);
    }

    public bool TryEvaluate(
        DiplomacyTradeRequest _request,
        FactionManager _playerFaction,
        FactionManager _targetFaction,
        out string _reason,
        out int _aiReceiveValue,
        out int _aiGiveValue)
    {
        CalculateTradeValues(_request, _playerFaction, _targetFaction, out _aiReceiveValue, out _aiGiveValue);

        if (_aiReceiveValue < _aiGiveValue)
        {
            _reason = TradeRejectedByAiMessage;
            return false;
        }

        _reason = TradeAcceptedMessage;
        return true;
    }

    public void CalculateTradeValues(
        DiplomacyTradeRequest _request,
        FactionManager _playerFaction,
        FactionManager _targetFaction,
        out int _aiReceiveValue,
        out int _aiGiveValue)
    {
        _aiReceiveValue = CalculateTradeValue(
            _request.playerOffer.credit,
            _request.playerOffer.power,
            _request.playerOffer.cardId,
            _request.playerOffer.cardCount,
            _request.playerOffer.city)
            + CalculateShareTradeValue(_request.playerOffer.shareCity, _request.playerOffer.sharePercent, _playerFaction)
            + CalculatePatentLicenseValue(_request.playerOffer.patentResearchId, _request.playerOffer.patentLicenseMonths);
        _aiGiveValue = CalculateTradeValue(
            _request.targetOffer.credit,
            _request.targetOffer.power,
            _request.targetOffer.cardId,
            _request.targetOffer.cardCount,
            _request.targetOffer.city)
            + CalculateShareTradeValue(_request.targetOffer.shareCity, _request.targetOffer.sharePercent, _targetFaction)
            + CalculatePatentLicenseValue(_request.targetOffer.patentResearchId, _request.targetOffer.patentLicenseMonths);
    }

    private int CalculateTradeValue(int _creditAmount, int _powerAmount)
    {
        return CalculateTradeValue(_creditAmount, _powerAmount, string.Empty, 0);
    }

    private int CalculateTradeValue(int _creditAmount, int _powerAmount, string _cardId, int _cardCount)
    {
        return CalculateTradeValue(_creditAmount, _powerAmount, _cardId, _cardCount, null);
    }

    private int CalculateTradeValue(int _creditAmount, int _powerAmount, string _cardId, int _cardCount, CityScript _city)
    {
        int cardValue = 0;

        if (_cardCount > 0)
            cardValue = CalculateCardTradeValue(_cardId) * _cardCount;

        return _creditAmount + (_powerAmount * PowerValueWeight) + cardValue + CalculateCityTradeValue(_city);
    }

    private int CalculateCardTradeValue(string _cardId)
    {
        if (string.IsNullOrWhiteSpace(_cardId))
            return 0;

        CardDatabase cardDatabase = CardDatabase.Instance;
        CardData card = cardDatabase != null ? cardDatabase.GetById(_cardId) : null;
        if (card == null)
            return 0;

        return Mathf.Max(0, card.tier) * CardTierValueWeight;
    }

    private int CalculateCityTradeValue(CityScript _city)
    {
        return CityValueCalculator.CalculateCityValue(_city);
    }

    private int CalculateShareTradeValue(CityScript _city, int _sharePercent, FactionManager _offeringFaction)
    {
        if (_sharePercent <= 0)
            return 0;

        int shareValue = CityValueCalculator.CalculateShareValue(_city, _sharePercent);
        if (IsProtectedOwnCityShare(_city, _offeringFaction))
            shareValue += OwnCityShareProtectionValue;

        return shareValue;
    }

    private int CalculatePatentLicenseValue(string _researchId, int _licenseMonths)
    {
        if (string.IsNullOrWhiteSpace(_researchId) || _licenseMonths <= 0)
            return 0;

        float durationMultiplier = GetPatentLicenseDurationMultiplier(_licenseMonths);
        if (durationMultiplier <= 0)
            return 0;

        ResearchDatabaseSO database = Resources.Load<ResearchDatabaseSO>("Databases/ResearchDatabase");
        if (database == null)
        {
            database = ScriptableObject.CreateInstance<ResearchDatabaseSO>();
            database.LoadCSV();
        }

        ResearchData research = database != null ? database.GetResearchById(_researchId) : null;
        if (research == null)
            return 0;

        int baseValue = Mathf.Max(0, research.costRP);
        float strategyMultiplier = DefaultPatentLicenseStrategyMultiplier;
        float licenseValue = baseValue * durationMultiplier * strategyMultiplier;
        return Mathf.CeilToInt(licenseValue);
    }

    private float GetPatentLicenseDurationMultiplier(int licenseMonths)
    {
        if (licenseMonths == PatentResearchManager.OneYearLicenseMonths)
            return OneYearPatentLicenseMultiplier;

        if (licenseMonths == PatentResearchManager.ThreeYearLicenseMonths)
            return ThreeYearPatentLicenseMultiplier;

        if (licenseMonths == PatentResearchManager.FiveYearLicenseMonths)
            return FiveYearPatentLicenseMultiplier;

        return 0.0f;
    }

    private bool IsProtectedOwnCityShare(CityScript _city, FactionManager _offeringFaction)
    {
        if (_city == null || _city.cityData == null || _city.cityData.owner == null)
            return false;

        if (_offeringFaction == null)
            return false;

        return ReferenceEquals(_city.cityData.owner, _offeringFaction)
            && !IsCreditCriticallyLow(_offeringFaction);
    }

    private bool IsCreditCriticallyLow(FactionManager _faction)
    {
        return _faction != null && _faction.GetCredit < OwnCityShareProtectionCreditThreshold;
    }

}
