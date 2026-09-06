public class FactionAIReasonFormatter
{
    public string FormatActionReason(NationAIActionScore _actionScore)
    {
        if (_actionScore == null)
            return string.Empty;

        if (!_actionScore.isAvailable)
            return _actionScore.unavailableReason;

        return $"{_actionScore.categoryName} | " +
               $"context:{_actionScore.scoreParts.contextScore:0.##} " +
               $"personal:{_actionScore.scoreParts.personalScore:0.##} " +
               $"noise:{_actionScore.scoreParts.noiseScore:0.##}";
    }

    public string FormatCandidateReason(NationAIActionCandidate _candidate)
    {
        if (_candidate == null)
            return string.Empty;

        if (_candidate.actionType == NationAIActionType.DeclareWar)
            return FormatWarReason(_candidate.targetCity);

        if (_candidate.actionType == NationAIActionType.BuyShare)
            return FormatSharePurchaseReason(_candidate.targetCity, _candidate.sharePurchaseAmount);

        if (_candidate.actionType == NationAIActionType.Trade)
            return FormatTradeReason(_candidate);

        if (_candidate.actionType == NationAIActionType.EmergencyOrder)
            return FormatEmergencyOrderReason(_candidate.city, _candidate.building);

        if (_candidate.actionType == NationAIActionType.ScavengerUnlock)
            return FormatScavengerUnlockReason(_candidate.city, _candidate.slotIndex);

        return FormatBuildReason(
            _candidate.actionType,
            _candidate.city,
            _candidate.building,
            _candidate.isNewConstruction);
    }

    private string FormatBuildReason(
        NationAIActionType _actionType,
        CityScript _city,
        BuildingData _building,
        bool _isNewConstruction)
    {
        string cityName = _city != null && _city.cityData != null
            ? _city.cityData.cityName
            : "Unknown";

        string buildingName = _building != null ? _building.name : "Unknown";
        string actionVerb = _isNewConstruction ? "build" : "upgrade to";

        if (_actionType == NationAIActionType.BuildEconomy)
            return $"{cityName}: {actionVerb} economy building {buildingName}";

        if (_actionType == NationAIActionType.BuildPower)
            return $"{cityName}: {actionVerb} power building {buildingName}";

        if (_actionType == NationAIActionType.BuildResearch)
            return $"{cityName}: {actionVerb} research building {buildingName}";

        if (_actionType == NationAIActionType.BuildFactory)
            return $"{cityName}: {actionVerb} factory {buildingName}";

        if (_actionType == NationAIActionType.BuildSupport)
            return $"{cityName}: {actionVerb} support building {buildingName}";

        return "Select action";
    }

    private string FormatWarReason(CityScript _targetCity)
    {
        string cityName = _targetCity != null && _targetCity.cityData != null
            ? _targetCity.cityData.cityName
            : "Unknown";

        return $"Target city: {cityName}";
    }

    private string FormatSharePurchaseReason(CityScript _targetCity, int _sharePurchaseAmount)
    {
        string cityName = _targetCity != null && _targetCity.cityData != null
            ? _targetCity.cityData.cityName
            : "Unknown";

        return $"Target city: {cityName}, buy corporate association share {_sharePurchaseAmount}%";
    }

    private string FormatTradeReason(NationAIActionCandidate _candidate)
    {
        string targetName = _candidate.targetFaction != null
            ? _candidate.targetFaction.factionName
            : "Unknown";

        if (!_candidate.tradeRequest.HasValue)
            return $"Trade with {targetName}";

        DiplomacyTradeRequest request = _candidate.tradeRequest.Value;
        if (request.targetOffer.shareCity != null && request.targetOffer.sharePercent > 0)
        {
            string cityName = request.targetOffer.shareCity.cityData != null
                ? request.targetOffer.shareCity.cityData.cityName
                : "Unknown";
            return $"Acquire share via trade with {targetName}: give {request.playerOffer.credit} credit, receive {cityName} share {request.targetOffer.sharePercent}%";
        }

        return $"Trade with {targetName}: give {request.playerOffer.credit} credit, receive {request.targetOffer.credit} credit";
    }

    private string FormatEmergencyOrderReason(CityScript _city, BuildingData _building)
    {
        string cityName = _city != null && _city.cityData != null
            ? _city.cityData.cityName
            : "Unknown";

        string buildingName = _building != null ? _building.name : "Unknown";
        return $"{cityName}: emergency order from factory {buildingName}";
    }

    private string FormatScavengerUnlockReason(CityScript _city, int _slotIndex)
    {
        string cityName = _city != null && _city.cityData != null
            ? _city.cityData.cityName
            : "Unknown";

        return $"{cityName}: unlock scavenger slot {_slotIndex}";
    }
}
