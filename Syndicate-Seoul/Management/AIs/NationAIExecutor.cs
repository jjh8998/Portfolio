using System.Collections.Generic;
using UnityEngine;

public enum NationAIExecuteFailReason
{
    None,
    NotEnoughMoney,
    InvalidTarget,
    InvalidState,
    MissingManager,
    Cooldown,
    Unknown
}

public class NationAIExecuteResult
{
    public bool success;
    public NationAIExecuteFailReason failReason;
    public string message;

    public NationAIExecuteResult(bool _success, NationAIExecuteFailReason _failReason, string _message = "")
    {
        success = _success;
        failReason = _failReason;
        message = _message ?? string.Empty;
    }
}

public class NationAIExecutor
{
    private const int MinWarReadyItemCount = 1;
    private const int WarCooldownMonths = 3;

    private readonly AIDebugLogger aiDebugLogger;
    private readonly Dictionary<FactionManager, int> lastWarMonthByFaction = new Dictionary<FactionManager, int>();

    public NationAIExecutor(AIDebugLogger _aiDebugLogger = null)
    {
        aiDebugLogger = _aiDebugLogger;
    }

    public int GetLastWarMonth(FactionManager _faction)
    {
        if (_faction == null)
            return -1;

        return lastWarMonthByFaction.TryGetValue(_faction, out int lastWarMonth)
            ? lastWarMonth
            : -1;
    }

    public void SetLastWarMonth(FactionManager _faction, int _lastWarMonth)
    {
        if (_faction == null)
            return;

        if (_lastWarMonth < 0)
        {
            lastWarMonthByFaction.Remove(_faction);
            return;
        }

        lastWarMonthByFaction[_faction] = _lastWarMonth;
    }

    public NationAIExecuteResult Execute(NationAIActionCandidate candidate, WarManager warManager)
    {
        if (candidate == null)
            return Fail(NationAIExecuteFailReason.InvalidTarget, "Candidate is null.");

        switch (candidate.actionType)
        {
            case NationAIActionType.BuildEconomy:
            case NationAIActionType.BuildPower:
            case NationAIActionType.BuildResearch:
            case NationAIActionType.BuildFactory:
            case NationAIActionType.BuildSupport:
                return ExecuteBuild(candidate);

            case NationAIActionType.BuyShare:
                return ExecuteSharePurchase(candidate);

            case NationAIActionType.Trade:
                return ExecuteTrade(candidate);

            case NationAIActionType.DeclareWar:
                return ExecuteWar(candidate, warManager);

            case NationAIActionType.EmergencyOrder:
                return ExecuteEmergencyOrder(candidate);

            case NationAIActionType.ScavengerUnlock:
                return ExecuteScavengerUnlock(candidate);

            case NationAIActionType.SaveMoney:
                return Success("Save money this turn.");
        }

        return Fail(NationAIExecuteFailReason.Unknown, "Unsupported action type.");
    }

    private NationAIExecuteResult ExecuteBuild(NationAIActionCandidate candidate)
    {
        if (candidate == null)
            return Fail(NationAIExecuteFailReason.InvalidTarget, "Build candidate is null.");

        FactionManager actorFaction = candidate.attackerFaction;
        if (actorFaction == null && candidate.city != null && candidate.city.cityData != null)
            actorFaction = candidate.city.cityData.owner;

        BuildingConstructionValidationResult validation =
            BuildingConstructionValidator.Validate(
                candidate.city,
                candidate.slotIndex,
                candidate.building,
                actorFaction,
                true,
                true);

        if (!validation.canBuild)
        {
            LogWarning(
                actorFaction,
                $"[AI] ExecuteBuild skipped: {validation.message}");

            return Fail(
                ConvertBuildValidationFailReason(validation.failReason),
                validation.message);
        }

        if (!candidate.city.BuildBuilding(candidate.slotIndex, candidate.building))
            return Fail(NationAIExecuteFailReason.Unknown, "BuildBuilding returned false.");

        return Success(candidate.reason);
    }

    private NationAIExecuteFailReason ConvertBuildValidationFailReason(BuildingConstructionFailReason failReason)
    {
        switch (failReason)
        {
            case BuildingConstructionFailReason.NotEnoughCredit:
                return NationAIExecuteFailReason.NotEnoughMoney;

            case BuildingConstructionFailReason.InvalidCity:
            case BuildingConstructionFailReason.InvalidOwner:
            case BuildingConstructionFailReason.InvalidBuilding:
            case BuildingConstructionFailReason.InvalidBuildingList:
            case BuildingConstructionFailReason.InvalidSlot:
                return NationAIExecuteFailReason.InvalidTarget;

            case BuildingConstructionFailReason.NotOwner:
            case BuildingConstructionFailReason.SlotLocked:
            case BuildingConstructionFailReason.SlotNotBuildable:
            case BuildingConstructionFailReason.SlotUnderConstruction:
            case BuildingConstructionFailReason.CityUnderConstruction:
            case BuildingConstructionFailReason.ResearchLocked:
            case BuildingConstructionFailReason.NotEnoughPower:
            case BuildingConstructionFailReason.DisabledForAI:
                return NationAIExecuteFailReason.InvalidState;
        }

        return NationAIExecuteFailReason.Unknown;
    }

    private NationAIExecuteResult ExecuteEmergencyOrder(NationAIActionCandidate candidate)
    {
        if (candidate == null)
            return Fail(NationAIExecuteFailReason.InvalidTarget, "Emergency order candidate is null.");

        if (candidate.city == null)
            return Fail(NationAIExecuteFailReason.InvalidTarget, "Emergency order city is null.");

        if (candidate.slotIndex < 0)
            return Fail(NationAIExecuteFailReason.InvalidState, "Emergency order slot index is invalid.");

        if (candidate.attackerFaction == null)
            return Fail(NationAIExecuteFailReason.InvalidState, "Emergency order faction is null.");

        if (!EmergencyOrderService.TryExecute(candidate.city, candidate.slotIndex, candidate.attackerFaction, out string failReason))
        {
            if (string.IsNullOrWhiteSpace(failReason))
                failReason = "Emergency order failed.";

            LogWarning(candidate.attackerFaction, $"[AI] EmergencyOrder skipped: {failReason}");
            return ClassifyEmergencyOrderFailure(failReason);
        }

        return Success(candidate.reason);
    }

    private NationAIExecuteResult ExecuteScavengerUnlock(NationAIActionCandidate candidate)
    {
        if (candidate == null)
            return Fail(NationAIExecuteFailReason.InvalidTarget, "Scavenger unlock candidate is null.");

        if (candidate.city == null || candidate.city.cityData == null)
            return Fail(NationAIExecuteFailReason.InvalidTarget, "Scavenger unlock city is missing.");

        if (candidate.attackerFaction == null)
            return Fail(NationAIExecuteFailReason.InvalidState, "Scavenger unlock faction is missing.");

        if (!ReferenceEquals(candidate.city.cityData.owner, candidate.attackerFaction))
            return Fail(NationAIExecuteFailReason.InvalidTarget, "Scavenger unlock city is not owned by attacker.");

        if (candidate.slotIndex < 0 || !candidate.city.IsBuildingSlotBuildable(candidate.slotIndex))
            return Fail(NationAIExecuteFailReason.InvalidState, "Scavenger unlock slot index is invalid.");

        if (!candidate.city.IsSlotScavengerLocked(candidate.slotIndex))
            return Fail(NationAIExecuteFailReason.InvalidState, "Scavenger unlock slot is not locked.");

        AIWarResolver resolver = Object.FindFirstObjectByType<AIWarResolver>();
        if (resolver == null)
        {
            LogWarning(candidate.attackerFaction, "[AI] ScavengerUnlock skipped: AIWarResolver is missing.");
            return Fail(NationAIExecuteFailReason.MissingManager, "AIWarResolver is missing.");
        }

        if (!resolver.TryResolveAIScavengerUnlock(candidate.city, candidate.attackerFaction, candidate.slotIndex, out bool attackerWon))
            return Fail(NationAIExecuteFailReason.Unknown, "Scavenger unlock battle could not be resolved.");

        if (!attackerWon)
            return Success("Scavenger unlock battle lost.");

        return Success(candidate.reason);
    }

    private NationAIExecuteResult ExecuteTrade(NationAIActionCandidate candidate)
    {
        if (candidate == null)
            return Fail(NationAIExecuteFailReason.InvalidTarget, "Trade candidate is null.");

        FactionManager proposerFaction = candidate.attackerFaction;
        FactionManager targetFaction = candidate.targetFaction;
        if (proposerFaction == null)
            return Fail(NationAIExecuteFailReason.InvalidState, "Trade proposer faction is null.");

        if (targetFaction == null)
            return Fail(NationAIExecuteFailReason.InvalidTarget, "Trade target faction is null.");

        if (ReferenceEquals(proposerFaction, targetFaction))
            return Fail(NationAIExecuteFailReason.InvalidTarget, "Trade target is the same faction.");

        if (!candidate.tradeRequest.HasValue)
            return Fail(NationAIExecuteFailReason.InvalidState, "Trade request is missing.");

        DiplomacyTradeRequest request = candidate.tradeRequest.Value;
        if (proposerFaction.GetCredit < request.playerOffer.credit)
            return Fail(NationAIExecuteFailReason.NotEnoughMoney, "Proposer does not have enough credit for trade.");

        if (targetFaction.GetCredit < request.targetOffer.credit)
            return Fail(NationAIExecuteFailReason.NotEnoughMoney, "Target does not have enough credit for trade.");

        if (!ValidateTradeShares(request, proposerFaction, targetFaction, out NationAIExecuteFailReason failReason, out string shareMessage))
            return Fail(failReason, shareMessage);

        DiplomacyTradeEvaluator evaluator = new DiplomacyTradeEvaluator();
        if (!evaluator.TryEvaluate(request, proposerFaction, targetFaction, out string reason))
            return Fail(NationAIExecuteFailReason.InvalidState, reason);

        DiplomacyTradeExecutor executor = new DiplomacyTradeExecutor();
        if (!executor.Execute(request, proposerFaction, targetFaction, out string message))
            return Fail(NationAIExecuteFailReason.Unknown, message);

        return Success(string.IsNullOrWhiteSpace(candidate.reason) ? message : candidate.reason);
    }

    private bool ValidateTradeShares(
        DiplomacyTradeRequest request,
        FactionManager proposerFaction,
        FactionManager targetFaction,
        out NationAIExecuteFailReason failReason,
        out string message)
    {
        CityShareManager cityShareManager = CityShareManager.instance;

        if (!ValidateTradeShareOffer(
            request.playerOffer,
            proposerFaction,
            "Proposer",
            cityShareManager,
            out failReason,
            out message))
        {
            return false;
        }

        if (!ValidateTradeShareOffer(
            request.targetOffer,
            targetFaction,
            "Target",
            cityShareManager,
            out failReason,
            out message))
        {
            return false;
        }

        failReason = NationAIExecuteFailReason.None;
        message = string.Empty;
        return true;
    }

    private bool ValidateTradeShareOffer(
        TradeSideOffer offer,
        FactionManager offeringFaction,
        string sideName,
        CityShareManager cityShareManager,
        out NationAIExecuteFailReason failReason,
        out string message)
    {
        failReason = NationAIExecuteFailReason.InvalidState;
        message = string.Empty;

        if (offer.sharePercent < 0)
        {
            message = $"{sideName} trade share percent cannot be negative.";
            return false;
        }

        if (offer.sharePercent <= 0 && offer.shareCity == null)
            return true;

        if (offer.sharePercent <= 0 && offer.shareCity != null)
        {
            message = $"{sideName} trade share city is set without a positive share percent.";
            return false;
        }

        if (offer.sharePercent > 0 && offer.shareCity == null)
        {
            failReason = NationAIExecuteFailReason.InvalidTarget;
            message = $"{sideName} trade share city is missing.";
            return false;
        }

        if (cityShareManager == null)
        {
            failReason = NationAIExecuteFailReason.MissingManager;
            message = "CityShareManager is missing.";
            return false;
        }

        if (!cityShareManager.TryPrepareCityShares(offer.shareCity, out message))
            return false;

        CityShareData shareData = offer.shareCity.cityData != null ? offer.shareCity.cityData.shareData : null;
        if (shareData == null || shareData.GetTotalShare() != 100)
        {
            message = $"{sideName} trade share data is invalid.";
            return false;
        }

        if (shareData.GetShare(offeringFaction) < offer.sharePercent)
        {
            message = $"{sideName} faction does not have enough shares.";
            return false;
        }

        return true;
    }

    private NationAIExecuteResult ExecuteWar(NationAIActionCandidate candidate, WarManager warManager)
    {
        if (warManager == null)
        {
            LogWarning(candidate != null ? candidate.attackerFaction : null, "[AI] WarManager is missing.");
            return Fail(NationAIExecuteFailReason.MissingManager, "WarManager is missing.");
        }

        if (candidate.attackerFaction == null)
            return Fail(NationAIExecuteFailReason.InvalidState, "Attacker faction is missing.");

        if (candidate.targetCity == null)
            return Fail(NationAIExecuteFailReason.InvalidTarget, "Target city is missing.");

        NationAIExecuteResult canExecuteResult = CanExecuteWar(candidate, out int currentMonthIndex, out bool hasCurrentMonthIndex);
        if (!canExecuteResult.success)
            return canExecuteResult;

        bool success = warManager.DeclareWar(candidate.targetCity, candidate.attackerFaction);
        if (success && hasCurrentMonthIndex)
            lastWarMonthByFaction[candidate.attackerFaction] = currentMonthIndex;

        if (!success)
            return Fail(NationAIExecuteFailReason.Unknown, "DeclareWar returned false.");

        return Success(candidate.reason);
    }

    private NationAIExecuteResult ExecuteSharePurchase(NationAIActionCandidate candidate)
    {
        if (candidate == null)
            return Fail(NationAIExecuteFailReason.InvalidTarget, "Share purchase candidate is null.");

        if (candidate.targetCity == null)
            return Fail(NationAIExecuteFailReason.InvalidTarget, "Share purchase target city is null.");

        if (candidate.attackerFaction == null)
            return Fail(NationAIExecuteFailReason.InvalidState, "Share purchase faction is null.");

        if (candidate.sharePurchaseAmount <= 0)
            return Fail(NationAIExecuteFailReason.InvalidState, "Share purchase amount must be greater than zero.");

        CitySharePurchaseService sharePurchaseService = Object.FindFirstObjectByType<CitySharePurchaseService>();
        if (sharePurchaseService == null)
        {
            LogWarning(candidate.attackerFaction, "[AI] CitySharePurchaseService is missing.");
            return Fail(NationAIExecuteFailReason.MissingManager, "CitySharePurchaseService is missing.");
        }

        if (candidate.attackerFaction.GetCredit < candidate.sharePurchaseCost)
            return Fail(NationAIExecuteFailReason.NotEnoughMoney, "Not enough credit to buy shares.");

        if (!sharePurchaseService.TryPurchaseShare(
                candidate.targetCity,
                candidate.attackerFaction,
                candidate.sharePurchaseAmount,
                out string message))
        {
            LogWarning(candidate.attackerFaction, $"[AI] BuyShare skipped: {message}");
            return ClassifySharePurchaseFailure(message);
        }

        return Success(candidate.reason);
    }

    private NationAIExecuteResult CanExecuteWar(NationAIActionCandidate candidate, out int currentMonthIndex, out bool hasCurrentMonthIndex)
    {
        currentMonthIndex = 0;
        hasCurrentMonthIndex = TryGetCurrentMonthIndex(out currentMonthIndex);

        CityScript targetCity = candidate.targetCity;
        FactionManager attackerFaction = candidate.attackerFaction;
        if (targetCity == null || targetCity.cityData == null)
            return Fail(NationAIExecuteFailReason.InvalidTarget, "War target city data is missing.");

        FactionManager defenderFaction = targetCity.cityData.owner;
        if (ReferenceEquals(defenderFaction, attackerFaction))
        {
            LogWarning(attackerFaction, "[AI] DeclareWar skipped: target city is already owned by attacker.");
            return Fail(NationAIExecuteFailReason.InvalidTarget, "Target city is already owned by attacker.");
        }

        if (defenderFaction == null)
        {
            LogWarning(attackerFaction, "[AI] DeclareWar skipped: defender faction is missing.");
            return Fail(NationAIExecuteFailReason.InvalidState, "Defender faction is missing.");
        }

        if (IsAIWarDeclarationToPlayerBlocked(defenderFaction))
        {
            const string message = "AI war declaration to player is blocked by developer mode.";
            LogWarning(attackerFaction, $"[AI] DeclareWar skipped: {message}");
            return Fail(NationAIExecuteFailReason.InvalidState, message);
        }

        CityShareManager cityShareManager = CityShareManager.instance != null
            ? CityShareManager.instance
            : Object.FindFirstObjectByType<CityShareManager>();

        if (cityShareManager == null)
        {
            LogWarning(attackerFaction, "[AI] DeclareWar skipped: CityShareManager not found.");
            return Fail(NationAIExecuteFailReason.MissingManager, "CityShareManager is missing.");
        }

        if (!cityShareManager.TryPrepareCityShares(targetCity, out string shareMessage))
        {
            LogWarning(attackerFaction, $"[AI] DeclareWar skipped: cannot prepare city shares. reason={shareMessage}");
            return Fail(NationAIExecuteFailReason.InvalidState, shareMessage);
        }

        if (!cityShareManager.CanClaimOwnership(targetCity, attackerFaction, out shareMessage))
        {
            LogWarning(attackerFaction, $"[AI] DeclareWar skipped: cannot claim ownership. reason={shareMessage}");
            return Fail(NationAIExecuteFailReason.InvalidState, shareMessage);
        }

        int attackerPower = ManagementResourceCalculator.CalculateMilitaryScore(attackerFaction);
        int defenderPower = ManagementResourceCalculator.CalculateMilitaryScore(defenderFaction);
        if (attackerPower <= defenderPower)
        {
            LogWarning(attackerFaction, $"[AI] DeclareWar skipped: attacker military score is not higher. attacker={attackerPower}, defender={defenderPower}");
            return Fail(NationAIExecuteFailReason.InvalidState, "Attacker military score is not higher.");
        }

        if (GetWarReadyItemCount(attackerFaction) < MinWarReadyItemCount)
        {
            LogWarning(attackerFaction, "[AI] DeclareWar skipped: not enough cards or card packs.");
            return Fail(NationAIExecuteFailReason.InvalidState, "Not enough cards or card packs.");
        }

        if (hasCurrentMonthIndex &&
            lastWarMonthByFaction.TryGetValue(attackerFaction, out int lastWarMonth) &&
            currentMonthIndex - lastWarMonth < WarCooldownMonths)
        {
            int remainingMonth = WarCooldownMonths - (currentMonthIndex - lastWarMonth);
            LogWarning(attackerFaction, $"[AI] DeclareWar skipped: war cooldown. remainingMonth={remainingMonth}");
            return Fail(NationAIExecuteFailReason.Cooldown, $"War cooldown. remainingMonth={remainingMonth}");
        }

        return Success(candidate.reason);
    }

    private bool TryGetCurrentMonthIndex(out int currentMonthIndex)
    {
        currentMonthIndex = 0;

        CalendarScript calendar = Object.FindFirstObjectByType<CalendarScript>();
        if (calendar == null || calendar.CurrentDate == null)
            return false;

        currentMonthIndex = calendar.CurrentDate.year * 12 + calendar.CurrentDate.month;
        return true;
    }

    private int GetWarReadyItemCount(FactionManager faction)
    {
        if (faction == null)
            return 0;

        int totalCount = 0;

        CardInventory cardInventory = faction.GetCardInventory();
        if (cardInventory != null)
            totalCount += cardInventory.Count;

        FactionCardPackInventoryScript cardPackInventory = faction.GetCardPackInventory();
        if (cardPackInventory != null)
            totalCount += cardPackInventory.GetTotalCardPackCount();

        return totalCount;
    }

    private bool IsAIWarDeclarationToPlayerBlocked(FactionManager defenderFaction)
    {
        if (defenderFaction == null || !defenderFaction.IsPlayerFaction)
            return false;

        DeveloperModeManager developerModeManager = Object.FindFirstObjectByType<DeveloperModeManager>();
        return developerModeManager != null && developerModeManager.BlockAIWarDeclarationToPlayer;
    }

    private NationAIExecuteResult ClassifySharePurchaseFailure(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return Fail(NationAIExecuteFailReason.Unknown, "Share purchase failed.");

        if (message.IndexOf("not enough credit", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            message.IndexOf("failed to deduct credit", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return Fail(NationAIExecuteFailReason.NotEnoughMoney, message);
        }

        if (message.IndexOf("manager is missing", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return Fail(NationAIExecuteFailReason.MissingManager, message);

        if (message.IndexOf("city is missing", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return Fail(NationAIExecuteFailReason.InvalidTarget, message);

        if (message.IndexOf("buyer is missing", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return Fail(NationAIExecuteFailReason.InvalidState, message);

        return Fail(NationAIExecuteFailReason.InvalidState, message);
    }

    private NationAIExecuteResult ClassifyEmergencyOrderFailure(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return Fail(NationAIExecuteFailReason.Unknown, "Emergency order failed.");

        if (message.IndexOf("not enough credit", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
            message.IndexOf("failed to spend credit", System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return Fail(NationAIExecuteFailReason.NotEnoughMoney, message);
        }

        if (message.IndexOf("city", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return Fail(NationAIExecuteFailReason.InvalidTarget, message);

        if (message.IndexOf("factory data", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return Fail(NationAIExecuteFailReason.InvalidTarget, message);

        if (message.IndexOf("production failed", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return Fail(NationAIExecuteFailReason.Unknown, message);

        return Fail(NationAIExecuteFailReason.InvalidState, message);
    }

    private NationAIExecuteResult Success(string message)
    {
        return new NationAIExecuteResult(true, NationAIExecuteFailReason.None, message);
    }

    private NationAIExecuteResult Fail(NationAIExecuteFailReason failReason, string message)
    {
        return new NationAIExecuteResult(false, failReason, message);
    }

    private void LogWarning(FactionManager faction, string _message)
    {
        if (aiDebugLogger != null)
        {
            aiDebugLogger.LogWarning(faction, _message);
            return;
        }

        AIDebugLogger.LogAIWarning(faction, _message);
    }
}
