using System;
using UnityEngine;

[Serializable]
public struct TradeSideOffer
{
    public int credit;
    public int power;
    public string cardId;
    public int cardCount;
    public CityScript city;
    public CityScript shareCity;
    public int sharePercent;
    public string patentResearchId;
    public int patentLicenseMonths;

    public TradeSideOffer(
        int _credit,
        int _power,
        string _cardId,
        int _cardCount,
        CityScript _city,
        CityScript _shareCity,
        int _sharePercent,
        string _patentResearchId = "",
        int _patentLicenseMonths = 0)
    {
        credit = _credit;
        power = _power;
        cardId = _cardId ?? string.Empty;
        cardCount = _cardCount;
        city = _city;
        shareCity = _shareCity;
        sharePercent = _sharePercent;
        patentResearchId = _patentResearchId ?? string.Empty;
        patentLicenseMonths = _patentLicenseMonths;
    }
}

[Serializable]
public struct DiplomacyTradeRequest
{
    public TradeSideOffer playerOffer;
    public TradeSideOffer targetOffer;

    public DiplomacyTradeRequest(TradeSideOffer _playerOffer, TradeSideOffer _targetOffer)
    {
        playerOffer = _playerOffer;
        targetOffer = _targetOffer;
    }

    public DiplomacyTradeRequest(int _playerToAiCredit, int _aiToPlayerCredit, int _playerToAiPower, int _aiToPlayerPower)
    {
        playerOffer = new TradeSideOffer(_playerToAiCredit, _playerToAiPower, string.Empty, 0, null, null, 0);
        targetOffer = new TradeSideOffer(_aiToPlayerCredit, _aiToPlayerPower, string.Empty, 0, null, null, 0);
    }

    public DiplomacyTradeRequest(
        int _playerToAiCredit,
        int _aiToPlayerCredit,
        int _playerToAiPower,
        int _aiToPlayerPower,
        string _playerToAiCardId,
        int _playerToAiCardCount,
        string _aiToPlayerCardId,
        int _aiToPlayerCardCount)
        : this(
            _playerToAiCredit,
            _aiToPlayerCredit,
            _playerToAiPower,
            _aiToPlayerPower,
            _playerToAiCardId,
            _playerToAiCardCount,
            _aiToPlayerCardId,
            _aiToPlayerCardCount,
            null,
            null,
            0,
            null,
            0)
    {
    }

    public DiplomacyTradeRequest(
        int _playerToAiCredit,
        int _aiToPlayerCredit,
        int _playerToAiPower,
        int _aiToPlayerPower,
        string _playerToAiCardId,
        int _playerToAiCardCount,
        string _aiToPlayerCardId,
        int _aiToPlayerCardCount,
        CityScript _playerToAiCity)
        : this(
            _playerToAiCredit,
            _aiToPlayerCredit,
            _playerToAiPower,
            _aiToPlayerPower,
            _playerToAiCardId,
            _playerToAiCardCount,
            _aiToPlayerCardId,
            _aiToPlayerCardCount,
            _playerToAiCity,
            null,
            0,
            null,
            0)
    {
    }

    public DiplomacyTradeRequest(
        int _playerToAiCredit,
        int _aiToPlayerCredit,
        int _playerToAiPower,
        int _aiToPlayerPower,
        string _playerToAiCardId,
        int _playerToAiCardCount,
        string _aiToPlayerCardId,
        int _aiToPlayerCardCount,
        CityScript _playerToAiCity,
        CityScript _playerToAiShareCity,
        int _playerToAiSharePercent,
        CityScript _aiToPlayerShareCity,
        int _aiToPlayerSharePercent)
    {
        playerOffer = new TradeSideOffer(
            _playerToAiCredit,
            _playerToAiPower,
            _playerToAiCardId,
            _playerToAiCardCount,
            _playerToAiCity,
            _playerToAiShareCity,
            _playerToAiSharePercent);
        targetOffer = new TradeSideOffer(
            _aiToPlayerCredit,
            _aiToPlayerPower,
            _aiToPlayerCardId,
            _aiToPlayerCardCount,
            null,
            _aiToPlayerShareCity,
            _aiToPlayerSharePercent);
    }

    public DiplomacyTradeRequest(DiplomacyCreditTradeRequest _request)
    {
        playerOffer = new TradeSideOffer(_request.playerToAiCredit, 0, string.Empty, 0, null, null, 0);
        targetOffer = new TradeSideOffer(_request.aiToPlayerCredit, 0, string.Empty, 0, null, null, 0);
    }
}

[Serializable]
public struct DiplomacyCreditTradeRequest
{
    public int playerToAiCredit;
    public int aiToPlayerCredit;

    public DiplomacyCreditTradeRequest(int _playerToAiCredit, int _aiToPlayerCredit)
    {
        playerToAiCredit = _playerToAiCredit;
        aiToPlayerCredit = _aiToPlayerCredit;
    }

    public DiplomacyTradeRequest ToTradeRequest()
    {
        return new DiplomacyTradeRequest(playerToAiCredit, aiToPlayerCredit, 0, 0);
    }
}

public class DiplomacyManager : MonoBehaviour
{
    public const string OnlyCityTradeBlockedMessage = "You cannot trade your only city.";

    private const string PlayerFactionMissingMessage = "Player faction is missing.";
    private const string TargetFactionMissingMessage = "Trade target is missing.";
    private const string SameFactionTradeMessage = "You cannot trade with the same faction.";
    private const string NegativeValueMessage = "Trade values cannot be negative.";
    private const string EmptyTradeMessage = "Enter at least one trade item.";
    private const string InvalidCityMessage = "The offered city is invalid.";
    private const string CityNotOwnedMessage = "The player does not own the offered city.";
    private const string TargetCityNotOwnedMessage = "The target faction does not own the offered city.";
    private const string ShareManagerMissingMessage = "City share manager is missing.";
    private const string PlayerShareCityMissingMessage = "Select a player city when offering shares.";
    private const string TargetShareCityMissingMessage = "Select a target city when requesting shares.";
    private const string InvalidShareCityMessage = "The offered share city is invalid.";
    private const string PlayerShareInsufficientMessage = "The player does not have enough shares in the offered city.";
    private const string TargetShareInsufficientMessage = "The target faction does not have enough shares in the requested city.";
    private const string PlayerCreditInsufficientMessage = "The player does not have enough credit.";
    private const string TargetCreditInsufficientMessage = "The target faction does not have enough credit.";
    private const string PlayerPowerInsufficientMessage = "The player does not have enough power.";
    private const string TargetPowerInsufficientMessage = "The target faction does not have enough power.";
    private const string PlayerCardIdMissingMessage = "Enter a player card ID when offering cards.";
    private const string TargetCardIdMissingMessage = "Enter a target card ID when requesting cards.";
    private const string PlayerCardInsufficientMessage = "The player does not have enough copies of the offered card.";
    private const string TargetCardInsufficientMessage = "The target faction does not have enough copies of the requested card.";
    private const string InvalidPatentLicenseMessage = "The patent license offer is invalid.";
    private const string PlayerNetPowerNegativeMessage = "The player would end the trade with negative net power.";
    private const string TargetNetPowerNegativeMessage = "The target faction would end the trade with negative net power.";
    private const string TradeAvailableMessage = "Trade is valid.";

    [Header("Factions")]
    [SerializeField] private FactionManager playerFaction;
    [SerializeField] private FactionManager selectedTargetFaction;

    private readonly DiplomacyTradeEvaluator tradeEvaluator = new DiplomacyTradeEvaluator();
    private readonly DiplomacyTradeExecutor tradeExecutor = new DiplomacyTradeExecutor();

    public event Action<FactionManager> SelectedTargetFactionChanged;

    public FactionManager PlayerFaction => playerFaction;
    public FactionManager SelectedTargetFaction => selectedTargetFaction;

    private void Awake()
    {
        TryResolvePlayerFaction();
    }

    public void SetPlayerFaction(FactionManager _playerFaction)
    {
        playerFaction = _playerFaction;
    }

    public void SelectTargetFaction(FactionManager _targetFaction)
    {
        if (ReferenceEquals(selectedTargetFaction, _targetFaction))
            return;

        selectedTargetFaction = _targetFaction;
        SelectedTargetFactionChanged?.Invoke(selectedTargetFaction);
    }

    public bool ValidateTradeInput(int _playerToAiCredit, int _aiToPlayerCredit, out string _message)
    {
        return CanTrade(new DiplomacyTradeRequest(_playerToAiCredit, _aiToPlayerCredit, 0, 0), out _message);
    }

    public bool CanTrade(int _playerToAiCredit, int _aiToPlayerCredit, out string _message)
    {
        return CanTrade(new DiplomacyTradeRequest(_playerToAiCredit, _aiToPlayerCredit, 0, 0), out _message);
    }

    public bool CanTrade(DiplomacyCreditTradeRequest _request, out string _message)
    {
        return CanTrade(_request.ToTradeRequest(), out _message);
    }

    public bool CanTrade(DiplomacyTradeRequest _request, out string _message)
    {
        if (!TryResolveTradeParties(out FactionManager player, out FactionManager target, out _message))
            return false;

        return ValidateTradeRequest(_request, player, target, out _message);
    }

    public bool ShouldAiAcceptCreditTrade(int _playerToAiCredit, int _aiToPlayerCredit)
    {
        return ShouldAiAcceptTrade(new DiplomacyTradeRequest(_playerToAiCredit, _aiToPlayerCredit, 0, 0));
    }

    public bool TryEvaluateAiCreditTrade(int _playerToAiCredit, int _aiToPlayerCredit, out string _reason)
    {
        return TryEvaluateAiTrade(new DiplomacyTradeRequest(_playerToAiCredit, _aiToPlayerCredit, 0, 0), out _reason);
    }

    public bool ShouldAiAcceptTrade(DiplomacyTradeRequest _request)
    {
        return TryEvaluateAiTrade(_request, out _);
    }

    public bool TryEvaluateAiTrade(DiplomacyTradeRequest _request, out string _reason)
    {
        return TryEvaluateAiTrade(_request, out _reason, out _, out _);
    }

    public bool TryEvaluateAiTrade(
        DiplomacyTradeRequest _request,
        out string _reason,
        out int _aiReceiveValue,
        out int _aiGiveValue)
    {
        if (!TryResolveTradeParties(out FactionManager player, out FactionManager target, out _reason))
        {
            _aiReceiveValue = 0;
            _aiGiveValue = 0;
            return false;
        }

        return tradeEvaluator.TryEvaluate(_request, player, target, out _reason, out _aiReceiveValue, out _aiGiveValue);
    }

    public bool TryCalculateAiTradeValues(
        DiplomacyTradeRequest _request,
        out string _message,
        out int _aiReceiveValue,
        out int _aiGiveValue)
    {
        if (!TryResolveTradeParties(out FactionManager player, out FactionManager target, out _message))
        {
            _aiReceiveValue = 0;
            _aiGiveValue = 0;
            return false;
        }

        tradeEvaluator.CalculateTradeValues(_request, player, target, out _aiReceiveValue, out _aiGiveValue);
        _message = string.Empty;
        return true;
    }

    public bool TryExecuteCreditTrade(int _playerToAiCredit, int _aiToPlayerCredit, out string _message)
    {
        return TryExecuteTrade(new DiplomacyTradeRequest(_playerToAiCredit, _aiToPlayerCredit, 0, 0), out _message);
    }

    public bool TryExecuteCreditTrade(DiplomacyCreditTradeRequest _request, out string _message)
    {
        return TryExecuteTrade(_request.ToTradeRequest(), out _message);
    }

    public bool TryExecuteTrade(DiplomacyTradeRequest _request, out string _message)
    {
        if (!CanTrade(_request, out _message))
            return false;

        if (!TryEvaluateAiTrade(_request, out _message))
            return false;

        return tradeExecutor.Execute(_request, playerFaction, selectedTargetFaction, out _message);
    }

    private bool TryResolveTradeParties(
        out FactionManager player,
        out FactionManager target,
        out string message)
    {
        player = null;
        target = null;

        if (!TryResolvePlayerFaction())
        {
            message = PlayerFactionMissingMessage;
            return false;
        }

        player = playerFaction;
        target = selectedTargetFaction;

        if (target == null)
        {
            message = TargetFactionMissingMessage;
            return false;
        }

        if (ReferenceEquals(target, player))
        {
            message = SameFactionTradeMessage;
            return false;
        }

        message = string.Empty;
        return true;
    }

    private bool ValidateTradeRequest(
        DiplomacyTradeRequest request,
        FactionManager player,
        FactionManager target,
        out string message)
    {
        if (HasNegativeValues(request))
        {
            message = NegativeValueMessage;
            return false;
        }

        if (!HasTradeContent(request))
        {
            message = EmptyTradeMessage;
            return false;
        }

        if (!ValidateCityTrade(request, player, target, out message))
            return false;

        if (!ValidateShareTrade(request, player, target, out message))
            return false;

        if (player.GetCredit < request.playerOffer.credit)
        {
            message = PlayerCreditInsufficientMessage;
            return false;
        }

        if (target.GetCredit < request.targetOffer.credit)
        {
            message = TargetCreditInsufficientMessage;
            return false;
        }

        if (player.GetNetPower < request.playerOffer.power)
        {
            message = PlayerPowerInsufficientMessage;
            return false;
        }

        if (target.GetNetPower < request.targetOffer.power)
        {
            message = TargetPowerInsufficientMessage;
            return false;
        }

        if (!ValidateCardTrade(request, player, target, out message))
            return false;

        if (!ValidatePatentLicenseTrade(request, player, target, out message))
            return false;

        if (player.GetNetPower + request.targetOffer.power - request.playerOffer.power < 0)
        {
            message = PlayerNetPowerNegativeMessage;
            return false;
        }

        if (target.GetNetPower + request.playerOffer.power - request.targetOffer.power < 0)
        {
            message = TargetNetPowerNegativeMessage;
            return false;
        }

        message = TradeAvailableMessage;
        return true;
    }

    private static bool HasNegativeValues(DiplomacyTradeRequest request)
    {
        return request.playerOffer.credit < 0
            || request.targetOffer.credit < 0
            || request.playerOffer.power < 0
            || request.targetOffer.power < 0
            || request.playerOffer.cardCount < 0
            || request.targetOffer.cardCount < 0
            || request.playerOffer.sharePercent < 0
            || request.targetOffer.sharePercent < 0
            || request.playerOffer.patentLicenseMonths < 0
            || request.targetOffer.patentLicenseMonths < 0;
    }

    private static bool HasTradeContent(DiplomacyTradeRequest request)
    {
        return request.playerOffer.credit > 0
            || request.targetOffer.credit > 0
            || request.playerOffer.power > 0
            || request.targetOffer.power > 0
            || request.playerOffer.cardCount > 0
            || request.targetOffer.cardCount > 0
            || request.playerOffer.city != null
            || request.targetOffer.city != null
            || request.playerOffer.sharePercent > 0
            || request.targetOffer.sharePercent > 0
            || request.playerOffer.patentLicenseMonths > 0
            || request.targetOffer.patentLicenseMonths > 0;
    }

    private bool ValidateCityTrade(
        DiplomacyTradeRequest request,
        FactionManager player,
        FactionManager target,
        out string message)
    {
        if (request.playerOffer.city == null && request.targetOffer.city == null)
        {
            message = string.Empty;
            return true;
        }

        if (request.playerOffer.city != null)
        {
            CityScript city = request.playerOffer.city;
            if (city.cityData == null)
            {
                message = InvalidCityMessage;
                return false;
            }

            if (!player.HasCity(city) || !ReferenceEquals(city.cityData.owner, player))
            {
                message = CityNotOwnedMessage;
                return false;
            }

            if (IsOnlyOwnedCity(player, city))
            {
                message = OnlyCityTradeBlockedMessage;
                return false;
            }
        }

        if (request.targetOffer.city != null)
        {
            CityScript city = request.targetOffer.city;
            if (city.cityData == null)
            {
                message = InvalidCityMessage;
                return false;
            }

            if (!target.HasCity(city) || !ReferenceEquals(city.cityData.owner, target))
            {
                message = TargetCityNotOwnedMessage;
                return false;
            }

            if (IsOnlyOwnedCity(target, city))
            {
                message = OnlyCityTradeBlockedMessage;
                return false;
            }
        }

        message = string.Empty;
        return true;
    }

    private bool IsOnlyOwnedCity(FactionManager faction, CityScript city)
    {
        if (faction == null || city == null || faction.ownedCities == null)
            return false;

        int ownedCityCount = 0;
        for (int i = 0; i < faction.ownedCities.Count; i++)
        {
            CityScript ownedCity = faction.ownedCities[i];
            if (ownedCity == null || ownedCity.cityData == null)
                continue;

            if (!ReferenceEquals(ownedCity.cityData.owner, faction))
                continue;

            ownedCityCount++;
            if (ownedCityCount > 1)
                return false;
        }

        return ownedCityCount <= 1;
    }

    private bool ValidateShareTrade(
        DiplomacyTradeRequest request,
        FactionManager player,
        FactionManager target,
        out string message)
    {
        if (request.playerOffer.sharePercent <= 0 && request.targetOffer.sharePercent <= 0)
        {
            message = string.Empty;
            return true;
        }

        if (CityShareManager.instance == null)
        {
            message = ShareManagerMissingMessage;
            return false;
        }

        if (request.playerOffer.sharePercent > 0)
        {
            if (request.playerOffer.shareCity == null)
            {
                message = PlayerShareCityMissingMessage;
                return false;
            }

            if (!ValidateShareAvailability(
                request.playerOffer.shareCity,
                player,
                request.playerOffer.sharePercent,
                PlayerShareInsufficientMessage,
                out message))
            {
                return false;
            }
        }

        if (request.targetOffer.sharePercent > 0)
        {
            if (request.targetOffer.shareCity == null)
            {
                message = TargetShareCityMissingMessage;
                return false;
            }

            if (!ValidateShareAvailability(
                request.targetOffer.shareCity,
                target,
                request.targetOffer.sharePercent,
                TargetShareInsufficientMessage,
                out message))
            {
                return false;
            }
        }

        message = string.Empty;
        return true;
    }

    private bool ValidateShareAvailability(
        CityScript city,
        FactionManager faction,
        int sharePercent,
        string insufficientMessage,
        out string message)
    {
        if (city == null || city.cityData == null)
        {
            message = InvalidShareCityMessage;
            return false;
        }

        if (!CityShareManager.instance.TryPrepareCityShares(city, out message))
            return false;

        CityShareData shareData = city.cityData.shareData;
        if (shareData == null || shareData.GetTotalShare() != 100)
        {
            message = InvalidShareCityMessage;
            return false;
        }

        if (shareData.GetShare(faction) < sharePercent)
        {
            message = insufficientMessage;
            return false;
        }

        message = string.Empty;
        return true;
    }

    private bool ValidateCardTrade(
        DiplomacyTradeRequest request,
        FactionManager player,
        FactionManager target,
        out string message)
    {
        if (request.playerOffer.cardCount > 0 && string.IsNullOrWhiteSpace(request.playerOffer.cardId))
        {
            message = PlayerCardIdMissingMessage;
            return false;
        }

        if (request.targetOffer.cardCount > 0 && string.IsNullOrWhiteSpace(request.targetOffer.cardId))
        {
            message = TargetCardIdMissingMessage;
            return false;
        }

        if (request.playerOffer.cardCount > 0
            && player.GetCardCount(request.playerOffer.cardId) < request.playerOffer.cardCount)
        {
            message = PlayerCardInsufficientMessage;
            return false;
        }

        if (request.targetOffer.cardCount > 0
            && target.GetCardCount(request.targetOffer.cardId) < request.targetOffer.cardCount)
        {
            message = TargetCardInsufficientMessage;
            return false;
        }

        message = string.Empty;
        return true;
    }

    private bool ValidatePatentLicenseTrade(
        DiplomacyTradeRequest request,
        FactionManager player,
        FactionManager target,
        out string message)
    {
        if (!ValidatePatentLicenseOffer(request.playerOffer, player, target, out message))
            return false;

        if (!ValidatePatentLicenseOffer(request.targetOffer, target, player, out message))
            return false;

        message = string.Empty;
        return true;
    }

    private bool ValidatePatentLicenseOffer(
        TradeSideOffer offer,
        FactionManager seller,
        FactionManager buyer,
        out string message)
    {
        if (offer.patentLicenseMonths == 0)
        {
            if (!string.IsNullOrWhiteSpace(offer.patentResearchId))
            {
                message = InvalidPatentLicenseMessage;
                return false;
            }

            message = string.Empty;
            return true;
        }

        if (string.IsNullOrWhiteSpace(offer.patentResearchId))
        {
            message = InvalidPatentLicenseMessage;
            return false;
        }

        PatentResearchManager manager = PatentResearchManager.Instance;
        if (!manager.CanGrantPatentLicense(
            offer.patentResearchId,
            seller,
            buyer,
            offer.patentLicenseMonths,
            out message))
        {
            if (string.IsNullOrWhiteSpace(message))
                message = InvalidPatentLicenseMessage;

            return false;
        }

        message = string.Empty;
        return true;
    }

    private bool TryResolvePlayerFaction()
    {
        if (playerFaction != null)
            return true;

        FactionManager[] factions = FindObjectsByType<FactionManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        if (factions == null || factions.Length == 0)
        {
            Debug.LogWarning($"{nameof(DiplomacyManager)}: {nameof(playerFaction)} is not assigned and no fallback faction was found.", this);
            return false;
        }

        for (int i = 0; i < factions.Length; i++)
        {
            FactionManager faction = factions[i];
            if (faction == null)
                continue;

            if (faction.IsPlayerFaction)
            {
                playerFaction = faction;
                return true;
            }
        }

        if (factions.Length == 1)
        {
            playerFaction = factions[0];
            return true;
        }

        Debug.LogWarning($"{nameof(DiplomacyManager)}: {nameof(playerFaction)} is not assigned and could not be resolved automatically.", this);
        return false;
    }
}
