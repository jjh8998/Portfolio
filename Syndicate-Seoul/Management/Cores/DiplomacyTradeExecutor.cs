using System;
using UnityEngine;

public class DiplomacyTradeExecutor
{
    private const string TradeAcceptedMessage = "Trade accepted.";
    private const string TradeExecutionFailedMessage = "Trade execution failed.";
    private const string OwnershipManagerMissingMessage = "City ownership manager is missing.";

    private DiplomacyTradeRequest request;
    private FactionManager playerFaction;
    private FactionManager targetFaction;
    private int playerCreditDelta;
    private int targetCreditDelta;
    private int playerPowerDelta;
    private int targetPowerDelta;

    private bool playerCreditChanged;
    private bool targetCreditChanged;
    private bool playerPowerChanged;
    private bool targetPowerChanged;
    private bool playerCardRemoved;
    private bool targetCardRemoved;
    private bool playerCardGrantedToTarget;
    private bool targetCardGrantedToPlayer;
    private bool playerShareTransferredToTarget;
    private bool targetShareTransferredToPlayer;
    private bool playerCityTransferredToTarget;
    private bool targetCityTransferredToPlayer;
    private bool playerPatentLicenseGrantedToTarget;
    private bool targetPatentLicenseGrantedToPlayer;
    private PatentLicenseSaveData playerToTargetPatentLicenseSnapshot;
    private PatentLicenseSaveData targetToPlayerPatentLicenseSnapshot;

    public bool Execute(
        DiplomacyTradeRequest _request,
        FactionManager _playerFaction,
        FactionManager _targetFaction,
        out string _message)
    {
        request = _request;
        playerFaction = _playerFaction;
        targetFaction = _targetFaction;
        playerCreditDelta = request.targetOffer.credit - request.playerOffer.credit;
        targetCreditDelta = request.playerOffer.credit - request.targetOffer.credit;
        playerPowerDelta = request.targetOffer.power - request.playerOffer.power;
        targetPowerDelta = request.playerOffer.power - request.targetOffer.power;

        playerCreditChanged = false;
        targetCreditChanged = false;
        playerPowerChanged = false;
        targetPowerChanged = false;
        playerCardRemoved = false;
        targetCardRemoved = false;
        playerCardGrantedToTarget = false;
        targetCardGrantedToPlayer = false;
        playerShareTransferredToTarget = false;
        targetShareTransferredToPlayer = false;
        playerCityTransferredToTarget = false;
        targetCityTransferredToPlayer = false;
        playerPatentLicenseGrantedToTarget = false;
        targetPatentLicenseGrantedToPlayer = false;
        playerToTargetPatentLicenseSnapshot = null;
        targetToPlayerPatentLicenseSnapshot = null;

        if (!TryApplyResourceChanges(out _message))
            return false;

        if (!TryApplyCardChanges(out _message))
            return false;

        if (!TryApplyShareChanges(out _message))
            return false;

        if (!TryTransferCity(out _message))
            return false;

        if (!TryApplyPatentLicenseChanges(out _message))
            return false;

        _message = TradeAcceptedMessage;
        return true;
    }

    private bool TryApplyResourceChanges(out string message)
    {
        if (!TryApplyChange(playerCreditDelta, playerFaction.ChangeCredit, ref playerCreditChanged))
        {
            message = TradeExecutionFailedMessage;
            return false;
        }

        if (!TryApplyChange(targetCreditDelta, targetFaction.ChangeCredit, ref targetCreditChanged))
        {
            RollbackResourceChanges();
            message = TradeExecutionFailedMessage;
            return false;
        }

        if (!TryApplyChange(playerPowerDelta, playerFaction.ChangeTradePower, ref playerPowerChanged))
        {
            RollbackResourceChanges();
            message = TradeExecutionFailedMessage;
            return false;
        }

        if (!TryApplyChange(targetPowerDelta, targetFaction.ChangeTradePower, ref targetPowerChanged))
        {
            RollbackResourceChanges();
            message = TradeExecutionFailedMessage;
            return false;
        }

        message = string.Empty;
        return true;
    }

    private bool TryApplyCardChanges(out string message)
    {
        if (!TryTransferCardOut(playerFaction, request.playerOffer.cardId, request.playerOffer.cardCount, ref playerCardRemoved))
        {
            RollbackResourceChanges();
            message = TradeExecutionFailedMessage;
            return false;
        }

        if (!TryTransferCardOut(targetFaction, request.targetOffer.cardId, request.targetOffer.cardCount, ref targetCardRemoved))
        {
            RollbackCardChanges();
            RollbackResourceChanges();
            message = TradeExecutionFailedMessage;
            return false;
        }

        if (!TryTransferCardIn(targetFaction, request.playerOffer.cardId, request.playerOffer.cardCount, ref playerCardGrantedToTarget))
        {
            RollbackCardChanges();
            RollbackResourceChanges();
            message = TradeExecutionFailedMessage;
            return false;
        }

        if (!TryTransferCardIn(playerFaction, request.targetOffer.cardId, request.targetOffer.cardCount, ref targetCardGrantedToPlayer))
        {
            RollbackCardChanges();
            RollbackResourceChanges();
            message = TradeExecutionFailedMessage;
            return false;
        }

        message = string.Empty;
        return true;
    }

    private bool TryTransferCity(out string message)
    {
        if (request.playerOffer.city == null && request.targetOffer.city == null)
        {
            message = string.Empty;
            return true;
        }

        if (IsOnlyOwnedCity(playerFaction, request.playerOffer.city)
            || IsOnlyOwnedCity(targetFaction, request.targetOffer.city))
        {
            RollbackShareChanges();
            RollbackCardChanges();
            RollbackResourceChanges();
            message = DiplomacyManager.OnlyCityTradeBlockedMessage;
            return false;
        }

        CityOwnershipManager ownershipManager = CityOwnershipManager.instance;
        if (ownershipManager == null)
        {
            RollbackShareChanges();
            RollbackCardChanges();
            RollbackResourceChanges();
            message = OwnershipManagerMissingMessage;
            return false;
        }

        if (request.playerOffer.city != null)
        {
            CityOwnershipManager.TransferResult transferResult = ownershipManager.Transfer(request.playerOffer.city, targetFaction);
            if (!IsCityTransferSuccessful(transferResult))
            {
                RollbackShareChanges();
                RollbackCardChanges();
                RollbackResourceChanges();
                Debug.LogWarning($"[DiplomacyManager] Player city transfer failed: {transferResult}");
                message = TradeExecutionFailedMessage;
                return false;
            }

            playerCityTransferredToTarget = transferResult == CityOwnershipManager.TransferResult.Success;
        }

        if (request.targetOffer.city != null)
        {
            CityOwnershipManager.TransferResult transferResult = ownershipManager.Transfer(request.targetOffer.city, playerFaction);
            if (!IsCityTransferSuccessful(transferResult))
            {
                RollbackCityChanges(ownershipManager);
                RollbackShareChanges();
                RollbackCardChanges();
                RollbackResourceChanges();
                Debug.LogWarning($"[DiplomacyManager] Target city transfer failed: {transferResult}");
                message = TradeExecutionFailedMessage;
                return false;
            }

            targetCityTransferredToPlayer = transferResult == CityOwnershipManager.TransferResult.Success;
        }

        message = string.Empty;
        return true;
    }

    private bool TryApplyShareChanges(out string message)
    {
        if (request.playerOffer.sharePercent > 0)
        {
            if (!CityShareManager.instance.TransferShare(
                request.playerOffer.shareCity,
                playerFaction,
                targetFaction,
                request.playerOffer.sharePercent,
                out message))
            {
                RollbackCardChanges();
                RollbackResourceChanges();
                return false;
            }

            playerShareTransferredToTarget = true;
        }

        if (request.targetOffer.sharePercent > 0)
        {
            if (!CityShareManager.instance.TransferShare(
                request.targetOffer.shareCity,
                targetFaction,
                playerFaction,
                request.targetOffer.sharePercent,
                out message))
            {
                RollbackShareChanges();
                RollbackCardChanges();
                RollbackResourceChanges();
                return false;
            }

            targetShareTransferredToPlayer = true;
        }

        message = string.Empty;
        return true;
    }

    private bool TryApplyPatentLicenseChanges(out string message)
    {
        if (request.playerOffer.patentLicenseMonths <= 0 && request.targetOffer.patentLicenseMonths <= 0)
        {
            message = string.Empty;
            return true;
        }

        PatentResearchManager manager = PatentResearchManager.Instance;

        if (request.playerOffer.patentLicenseMonths > 0)
        {
            manager.TryGetPatentLicenseSnapshot(
                request.playerOffer.patentResearchId,
                targetFaction,
                out playerToTargetPatentLicenseSnapshot);

            if (!manager.TryGrantPatentLicense(
                request.playerOffer.patentResearchId,
                playerFaction,
                targetFaction,
                request.playerOffer.patentLicenseMonths))
            {
                RollbackChangesBeforePatentLicense();
                message = TradeExecutionFailedMessage;
                return false;
            }

            playerPatentLicenseGrantedToTarget = true;
        }

        if (request.targetOffer.patentLicenseMonths > 0)
        {
            manager.TryGetPatentLicenseSnapshot(
                request.targetOffer.patentResearchId,
                playerFaction,
                out targetToPlayerPatentLicenseSnapshot);

            if (!manager.TryGrantPatentLicense(
                request.targetOffer.patentResearchId,
                targetFaction,
                playerFaction,
                request.targetOffer.patentLicenseMonths))
            {
                RollbackPatentLicenseChanges();
                RollbackChangesBeforePatentLicense();
                message = TradeExecutionFailedMessage;
                return false;
            }

            targetPatentLicenseGrantedToPlayer = true;
        }

        message = string.Empty;
        return true;
    }

    private static bool TryApplyChange(int delta, Func<int, bool> applyChange, ref bool changed)
    {
        if (delta == 0)
            return true;

        if (applyChange == null || !applyChange(delta))
            return false;

        changed = true;
        return true;
    }

    private static bool TryTransferCardOut(FactionManager faction, string cardId, int count, ref bool changed)
    {
        if (count <= 0)
            return true;

        if (faction == null || !faction.RemoveCard(cardId, count))
            return false;

        changed = true;
        return true;
    }

    private static bool TryTransferCardIn(FactionManager faction, string cardId, int count, ref bool changed)
    {
        if (count <= 0)
            return true;

        if (faction == null || !faction.GainCard(cardId, count))
            return false;

        changed = true;
        return true;
    }

    private static bool IsCityTransferSuccessful(CityOwnershipManager.TransferResult transferResult)
    {
        return transferResult == CityOwnershipManager.TransferResult.Success
            || transferResult == CityOwnershipManager.TransferResult.SameOwner;
    }

    private bool IsOnlyOwnedCity(FactionManager faction, CityScript city)
    {
        if (faction == null || city == null || faction.ownedCities == null)
            return false;

        if (city.cityData == null || !ReferenceEquals(city.cityData.owner, faction))
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

    private void RollbackCityChanges(CityOwnershipManager ownershipManager)
    {
        if (targetCityTransferredToPlayer && request.targetOffer.city != null)
        {
            CityOwnershipManager.TransferResult transferResult = ownershipManager.Transfer(request.targetOffer.city, targetFaction);
            if (!IsCityTransferSuccessful(transferResult))
                Debug.LogError($"[DiplomacyManager] Failed to roll back AI-to-player city transfer: {transferResult}");
        }

        if (playerCityTransferredToTarget && request.playerOffer.city != null)
        {
            CityOwnershipManager.TransferResult transferResult = ownershipManager.Transfer(request.playerOffer.city, playerFaction);
            if (!IsCityTransferSuccessful(transferResult))
                Debug.LogError($"[DiplomacyManager] Failed to roll back player-to-AI city transfer: {transferResult}");
        }
    }

    private void RollbackResourceChanges()
    {
        if (targetPowerChanged && !targetFaction.ChangeTradePower(-targetPowerDelta))
            Debug.LogError("[DiplomacyManager] Failed to roll back target trade power.");

        if (playerPowerChanged && !playerFaction.ChangeTradePower(-playerPowerDelta))
            Debug.LogError("[DiplomacyManager] Failed to roll back player trade power.");

        if (targetCreditChanged && !targetFaction.ChangeCredit(-targetCreditDelta))
            Debug.LogError("[DiplomacyManager] Failed to roll back target credit.");

        if (playerCreditChanged && !playerFaction.ChangeCredit(-playerCreditDelta))
            Debug.LogError("[DiplomacyManager] Failed to roll back player credit.");
    }

    private void RollbackCardChanges()
    {
        if (targetCardGrantedToPlayer && request.targetOffer.cardCount > 0)
        {
            if (!playerFaction.RemoveCard(request.targetOffer.cardId, request.targetOffer.cardCount))
            {
                Debug.LogError("[DiplomacyManager] Failed to remove returned AI card from player during rollback.");
            }
            else if (!targetFaction.GainCard(request.targetOffer.cardId, request.targetOffer.cardCount))
            {
                Debug.LogError("[DiplomacyManager] Failed to restore AI card to target during rollback.");
            }
        }
        else if (targetCardRemoved && request.targetOffer.cardCount > 0
            && !targetFaction.GainCard(request.targetOffer.cardId, request.targetOffer.cardCount))
        {
            Debug.LogError("[DiplomacyManager] Failed to restore removed AI card during rollback.");
        }

        if (playerCardGrantedToTarget && request.playerOffer.cardCount > 0)
        {
            if (!targetFaction.RemoveCard(request.playerOffer.cardId, request.playerOffer.cardCount))
            {
                Debug.LogError("[DiplomacyManager] Failed to remove returned player card from target during rollback.");
            }
            else if (!playerFaction.GainCard(request.playerOffer.cardId, request.playerOffer.cardCount))
            {
                Debug.LogError("[DiplomacyManager] Failed to restore player card during rollback.");
            }
        }
        else if (playerCardRemoved && request.playerOffer.cardCount > 0
            && !playerFaction.GainCard(request.playerOffer.cardId, request.playerOffer.cardCount))
        {
            Debug.LogError("[DiplomacyManager] Failed to restore removed player card during rollback.");
        }
    }

    private void RollbackShareChanges()
    {
        if (targetShareTransferredToPlayer && request.targetOffer.sharePercent > 0)
        {
            if (!CityShareManager.instance.TransferShare(
                request.targetOffer.shareCity,
                playerFaction,
                targetFaction,
                request.targetOffer.sharePercent,
                out _))
            {
                Debug.LogError("[DiplomacyManager] Failed to roll back AI-to-player share transfer.");
            }
        }

        if (playerShareTransferredToTarget && request.playerOffer.sharePercent > 0)
        {
            if (!CityShareManager.instance.TransferShare(
                request.playerOffer.shareCity,
                targetFaction,
                playerFaction,
                request.playerOffer.sharePercent,
                out _))
            {
                Debug.LogError("[DiplomacyManager] Failed to roll back player-to-AI share transfer.");
            }
        }
    }

    private void RollbackPatentLicenseChanges()
    {
        PatentResearchManager manager = PatentResearchManager.Instance;

        if (targetPatentLicenseGrantedToPlayer && request.targetOffer.patentLicenseMonths > 0)
        {
            manager.RestorePatentLicenseForRollback(
                request.targetOffer.patentResearchId,
                playerFaction,
                targetToPlayerPatentLicenseSnapshot);
        }

        if (playerPatentLicenseGrantedToTarget && request.playerOffer.patentLicenseMonths > 0)
        {
            manager.RestorePatentLicenseForRollback(
                request.playerOffer.patentResearchId,
                targetFaction,
                playerToTargetPatentLicenseSnapshot);
        }
    }

    private void RollbackChangesBeforePatentLicense()
    {
        CityOwnershipManager ownershipManager = CityOwnershipManager.instance;
        if (ownershipManager != null)
            RollbackCityChanges(ownershipManager);

        RollbackShareChanges();
        RollbackCardChanges();
        RollbackResourceChanges();
    }
}
