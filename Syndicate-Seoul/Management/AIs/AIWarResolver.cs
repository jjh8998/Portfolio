using System;
using System.Collections.Generic;
using UnityEngine;

public class AIWarResolver : MonoBehaviour
{
    public const int DefaultScavengerPowerScore = 14;

    private const float CardCommitRatio = 0.4f;
    private const string EmptyThemeKey = "<empty>";

    [SerializeField] private int scavengerPowerScore = DefaultScavengerPowerScore;

    public int ScavengerPowerScore => Mathf.Max(1, scavengerPowerScore);

    private class BattleCardEntry
    {
        public string cardId;
        public string themeKey;
        public int score;
        public int count;
    }

    private class CardCommitResult
    {
        public readonly Dictionary<string, int> cardCounts = new Dictionary<string, int>();
        public int totalScore;

        public int ConsumedCardCount
        {
            get
            {
                int count = 0;
                foreach (KeyValuePair<string, int> pair in cardCounts)
                    count += Mathf.Max(0, pair.Value);

                return count;
            }
        }
    }

    public bool ResolveAIWar(CityScript _targetCity, FactionManager _attacker)
    {
        return TryResolveAIWar(_targetCity, _attacker, out bool attackerWon) && attackerWon;
    }

    public bool TryResolveAIScavengerUnlock(CityScript _targetCity, FactionManager _attacker, int _slotIndex, out bool attackerWon)
    {
        attackerWon = false;

        if (_targetCity == null || _targetCity.cityData == null || _attacker == null)
            return false;

        if (!ReferenceEquals(_targetCity.cityData.owner, _attacker))
            return false;

        if (_slotIndex < 0 || !_targetCity.IsBuildingSlotBuildable(_slotIndex) || !_targetCity.IsSlotScavengerLocked(_slotIndex))
            return false;

        CardDatabase cardDatabase = CardDatabase.Instance;
        if (cardDatabase == null || !cardDatabase.IsLoaded)
            return false;

        CardInventory attackerInventory = _attacker.GetCardInventory();
        if (attackerInventory == null)
            return false;

        int targetScore = ScavengerPowerScore;
        CardCommitResult attackerCommit = SelectCommittedCards(attackerInventory, cardDatabase, targetScore);
        int attackerScore = attackerCommit.totalScore;
        if (attackerScore <= 0)
            return false;

        double attackerWinRate;
        if (targetScore <= 0)
        {
            attackerWinRate = 1d;
        }
        else
        {
            double attackerSquared = (double)attackerScore * attackerScore;
            double scavengerSquared = (double)targetScore * targetScore;
            attackerWinRate = attackerSquared / (attackerSquared + scavengerSquared);
        }

        _attacker.ConsumeCards(attackerCommit.cardCounts);

        attackerWon = UnityEngine.Random.value <= attackerWinRate;
        bool unlocked = false;
        if (attackerWon)
            unlocked = _targetCity.UnlockScavengerSlot(_slotIndex);

        LogScavengerUnlockResult(
            _targetCity,
            _attacker,
            _slotIndex,
            targetScore,
            attackerScore,
            attackerWinRate,
            attackerWon,
            unlocked,
            attackerCommit.ConsumedCardCount);

        if (attackerWon && !unlocked)
        {
            attackerWon = false;
            return false;
        }

        return true;
    }

    public bool TryResolveAIWar(CityScript _targetCity, FactionManager _attacker, out bool attackerWon)
    {
        attackerWon = false;

        if (_targetCity == null || _targetCity.cityData == null || _attacker == null)
            return false;

        FactionManager defender = _targetCity.cityData.owner;
        if (defender == null || ReferenceEquals(defender, _attacker))
            return false;

        CardDatabase cardDatabase = CardDatabase.Instance;
        if (cardDatabase == null || !cardDatabase.IsLoaded)
            return false;

        CardInventory attackerInventory = _attacker.GetCardInventory();
        CardInventory defenderInventory = defender.GetCardInventory();
        if (attackerInventory == null || defenderInventory == null)
            return false;

        int attackerTotalPower = ManagementResourceCalculator.CalculateMilitaryScore(_attacker);
        int defenderTotalPower = ManagementResourceCalculator.CalculateMilitaryScore(defender);
        int attackerTargetScore = attackerTotalPower > 0 ? Mathf.CeilToInt(attackerTotalPower * CardCommitRatio) : 0;
        int defenderTargetScore = defenderTotalPower > 0 ? Mathf.CeilToInt(defenderTotalPower * CardCommitRatio) : 0;

        CardCommitResult attackerCommit = SelectCommittedCards(attackerInventory, cardDatabase, attackerTargetScore);
        CardCommitResult defenderCommit = SelectCommittedCards(defenderInventory, cardDatabase, defenderTargetScore);

        int attackerCardScore = attackerCommit.totalScore;
        int defenderCardScore = defenderCommit.totalScore;
        int attackerSupportScore = CalculateSupportBuildingScore(_attacker);
        int defenderSupportScore = CalculateSupportBuildingScore(defender);
        int attackerScore = attackerCardScore + attackerSupportScore;
        int defenderScore = defenderCardScore + defenderSupportScore;

        if (attackerScore <= 0 && defenderScore <= 0)
        {
            LogResult(
                _targetCity,
                _attacker,
                defender,
                attackerTotalPower,
                attackerTargetScore,
                attackerCardScore,
                attackerSupportScore,
                attackerScore,
                defenderTotalPower,
                defenderTargetScore,
                defenderCardScore,
                defenderSupportScore,
                defenderScore,
                0d,
                defender,
                0,
                0);
            return true;
        }

        double attackerWinRate;
        if (attackerScore > 0 && defenderScore <= 0)
        {
            attackerWinRate = 1d;
        }
        else
        {
            double attackerSquared = (double)attackerScore * attackerScore;
            double defenderSquared = (double)defenderScore * defenderScore;
            attackerWinRate = attackerSquared / (attackerSquared + defenderSquared);
        }

        _attacker.ConsumeCards(attackerCommit.cardCounts);
        defender.ConsumeCards(defenderCommit.cardCounts);

        attackerWon = UnityEngine.Random.value <= attackerWinRate;
        FactionManager winner = attackerWon ? _attacker : defender;

        if (attackerWon)
            TransferCity(_targetCity, _attacker);

        LogResult(
            _targetCity,
            _attacker,
            defender,
            attackerTotalPower,
            attackerTargetScore,
            attackerCardScore,
            attackerSupportScore,
            attackerScore,
            defenderTotalPower,
            defenderTargetScore,
            defenderCardScore,
            defenderSupportScore,
            defenderScore,
            attackerWinRate,
            winner,
            attackerCommit.ConsumedCardCount,
            defenderCommit.ConsumedCardCount);

        return true;
    }

    private CardCommitResult SelectCommittedCards(CardInventory inventory, CardDatabase cardDatabase, int targetScore)
    {
        CardCommitResult result = new CardCommitResult();
        if (inventory == null || cardDatabase == null)
            return result;

        List<BattleCardEntry> availableCards = BuildAvailableCards(inventory, cardDatabase);
        if (availableCards.Count == 0)
            return result;

        if (targetScore <= 0)
            return result;

        Dictionary<string, List<BattleCardEntry>> cardsByTheme = GroupCardsByTheme(availableCards);

        CardCommitResult bestThemeSelection = null;
        int smallestOverScore = int.MaxValue;

        foreach (KeyValuePair<string, List<BattleCardEntry>> pair in cardsByTheme)
        {
            List<BattleCardEntry> themeCards = pair.Value;
            if (themeCards == null || themeCards.Count == 0)
                continue;

            CardCommitResult currentSelection = new CardCommitResult();
            for (int i = 0; i < themeCards.Count; i++)
            {
                BattleCardEntry card = themeCards[i];
                if (card == null || card.count <= 0 || card.score <= 0)
                    continue;

                int needScore = targetScore - currentSelection.totalScore;
                int takeCount = Mathf.Min(card.count, Mathf.CeilToInt((float)needScore / card.score));
                if (takeCount <= 0)
                    continue;

                AddCardToCommitResult(currentSelection, card.cardId, takeCount);
                currentSelection.totalScore += takeCount * card.score;

                if (currentSelection.totalScore < targetScore)
                    continue;

                int overScore = currentSelection.totalScore - targetScore;
                if (overScore < smallestOverScore)
                {
                    smallestOverScore = overScore;
                    bestThemeSelection = currentSelection;
                }

                break;
            }
        }

        if (bestThemeSelection != null)
            return bestThemeSelection;

        List<BattleCardEntry> fallbackThemeCards = null;
        int fallbackThemeScore = 0;
        foreach (KeyValuePair<string, List<BattleCardEntry>> pair in cardsByTheme)
        {
            List<BattleCardEntry> themeCards = pair.Value;
            if (themeCards == null || themeCards.Count == 0)
                continue;

            int themeScore = GetTotalScore(themeCards);
            if (themeScore <= fallbackThemeScore)
                continue;

            fallbackThemeScore = themeScore;
            fallbackThemeCards = themeCards;
        }

        if (fallbackThemeCards != null)
        {
            for (int i = 0; i < fallbackThemeCards.Count; i++)
            {
                BattleCardEntry card = fallbackThemeCards[i];
                if (card == null || card.count <= 0 || card.score <= 0)
                    continue;

                AddCardToCommitResult(result, card.cardId, card.count);
                result.totalScore += card.score * card.count;
            }
        }

        if (result.totalScore >= targetScore)
            return result;

        availableCards.Sort((a, b) => b.score.CompareTo(a.score));
        for (int i = 0; i < availableCards.Count; i++)
        {
            if (result.totalScore >= targetScore)
                break;

            BattleCardEntry card = availableCards[i];
            if (card == null || card.count <= 0 || card.score <= 0)
                continue;

            int alreadySelected = result.cardCounts.TryGetValue(card.cardId, out int selectedCount)
                ? selectedCount
                : 0;
            int remainingCount = card.count - alreadySelected;
            if (remainingCount <= 0)
                continue;

            int needScore = targetScore - result.totalScore;
            int takeCount = Mathf.Min(remainingCount, Mathf.CeilToInt((float)needScore / card.score));
            if (takeCount <= 0)
                continue;

            AddCardToCommitResult(result, card.cardId, takeCount);
            result.totalScore += takeCount * card.score;
        }

        return result;
    }

    private List<BattleCardEntry> BuildAvailableCards(CardInventory inventory, CardDatabase cardDatabase)
    {
        List<BattleCardEntry> result = new List<BattleCardEntry>();
        Dictionary<string, int> cardCounts = inventory.GetCardCountMapForReadOnlyUse();
        if (cardCounts == null)
            return result;

        foreach (KeyValuePair<string, int> pair in cardCounts)
        {
            if (pair.Value <= 0 || string.IsNullOrWhiteSpace(pair.Key))
                continue;

            CardData cardData = cardDatabase.GetById(pair.Key);
            if (cardData == null)
                continue;

            int cardScore = Mathf.Clamp(cardData.tier, 1, 5);
            string themeKey = string.IsNullOrWhiteSpace(cardData.pack)
                ? EmptyThemeKey
                : cardData.pack.Trim();

            result.Add(new BattleCardEntry
            {
                cardId = pair.Key,
                themeKey = themeKey,
                score = cardScore,
                count = pair.Value
            });
        }

        return result;
    }

    private int CalculateSupportBuildingScore(FactionManager _faction)
    {
        if (_faction == null || _faction.ownedCities == null)
            return 0;

        int score = 0;
        for (int i = 0; i < _faction.ownedCities.Count; i++)
        {
            CityScript city = _faction.ownedCities[i];
            if (city == null || city.cityData == null || city.cityData.buildings == null)
                continue;

            for (int j = 0; j < city.cityData.buildings.Count; j++)
            {
                BuildingInstance building = city.cityData.buildings[j];
                if (building == null || building.IsEmptySlot() || building.IsUnderConstruction() || building.IsDisabled())
                    continue;

                if (building.data == null || building.data.category != BuildingCategory.Support)
                    continue;

                int tier = Mathf.Max(1, building.data.GetTierOrDefault());
                score += 5 * tier;
            }
        }

        return score;
    }

    private Dictionary<string, List<BattleCardEntry>> GroupCardsByTheme(List<BattleCardEntry> availableCards)
    {
        Dictionary<string, List<BattleCardEntry>> result = new Dictionary<string, List<BattleCardEntry>>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < availableCards.Count; i++)
        {
            BattleCardEntry card = availableCards[i];
            if (card == null)
                continue;

            if (!result.TryGetValue(card.themeKey, out List<BattleCardEntry> themeCards))
            {
                themeCards = new List<BattleCardEntry>();
                result.Add(card.themeKey, themeCards);
            }

            themeCards.Add(card);
        }

        foreach (KeyValuePair<string, List<BattleCardEntry>> pair in result)
            pair.Value.Sort((a, b) => b.score.CompareTo(a.score));

        return result;
    }

    private int GetTotalScore(List<BattleCardEntry> cards)
    {
        if (cards == null)
            return 0;

        int totalScore = 0;
        for (int i = 0; i < cards.Count; i++)
        {
            BattleCardEntry card = cards[i];
            if (card == null || card.count <= 0)
                continue;

            totalScore += card.score * card.count;
        }

        return totalScore;
    }

    private void AddCardToCommitResult(CardCommitResult result, string cardId)
    {
        AddCardToCommitResult(result, cardId, 1);
    }

    private void AddCardToCommitResult(CardCommitResult result, string cardId, int count)
    {
        if (result == null || string.IsNullOrWhiteSpace(cardId))
            return;

        if (count <= 0)
            return;

        if (result.cardCounts.ContainsKey(cardId))
            result.cardCounts[cardId] += count;
        else
            result.cardCounts[cardId] = count;
    }

    private void TransferCity(CityScript targetCity, FactionManager attacker)
    {
        if (targetCity == null || targetCity.cityData == null || attacker == null)
            return;

        if (CityOwnershipManager.instance != null)
        {
            CityOwnershipManager.instance.Transfer(targetCity, attacker, true);
            return;
        }

        FactionManager defender = targetCity.cityData.owner;
        if (defender != null && defender != attacker)
            defender.RemoveCity(targetCity);

        targetCity.cityData.owner = attacker;

        if (!attacker.HasCity(targetCity))
            attacker.AddCity(targetCity);
    }

    private void LogResult(
        CityScript targetCity,
        FactionManager attacker,
        FactionManager defender,
        int attackerTotalPower,
        int attackerTargetScore,
        int attackerCardScore,
        int attackerSupportScore,
        int attackerFinalScore,
        int defenderTotalPower,
        int defenderTargetScore,
        int defenderCardScore,
        int defenderSupportScore,
        int defenderFinalScore,
        double attackerWinRate,
        FactionManager winner,
        int attackerConsumedCardCount,
        int defenderConsumedCardCount)
    {
        string attackerName = attacker != null ? attacker.factionName : "Unknown";
        string defenderName = defender != null ? defender.factionName : "Unknown";
        string cityName = targetCity != null && targetCity.cityData != null && !string.IsNullOrWhiteSpace(targetCity.cityData.cityName)
            ? targetCity.cityData.cityName
            : "Unknown";
        string winnerName = winner != null ? winner.factionName : "None";
        string finalOwnerName = targetCity != null && targetCity.cityData != null && targetCity.cityData.owner != null
            ? targetCity.cityData.owner.factionName
            : defenderName;
        double attackerWinPercent = attackerWinRate * 100d;

        AIDebugLogger.LogMergerAcquisition(
            $"[AI][M&A] 공격: {attackerName} -> {defenderName}, 도시={cityName}, " +
            $"attackerPower={attackerTotalPower}, attackerTarget={attackerTargetScore}, " +
            $"attackerCardScore={attackerCardScore}, attackerSupportScore={attackerSupportScore}, attackerFinalScore={attackerFinalScore}, " +
            $"defenderPower={defenderTotalPower}, defenderTarget={defenderTargetScore}, " +
            $"defenderCardScore={defenderCardScore}, defenderSupportScore={defenderSupportScore}, defenderFinalScore={defenderFinalScore}, " +
            $"attackerConsumedCards={attackerConsumedCardCount}, defenderConsumedCards={defenderConsumedCardCount}");
        AIDebugLogger.LogMergerAcquisition(
            $"[AI][M&A] 승률: {attackerName} attackerWinRate={attackerWinPercent:F2}%");
        AIDebugLogger.LogMergerAcquisition(
            $"[AI][M&A] 결과: 승자={winnerName}, 도시={cityName}, 최종소유자={finalOwnerName}");
    }

    private void LogScavengerUnlockResult(
        CityScript targetCity,
        FactionManager attacker,
        int slotIndex,
        int scavengerScore,
        int attackerScore,
        double attackerWinRate,
        bool attackerWon,
        bool unlocked,
        int consumedCardCount)
    {
        string cityName = targetCity != null && targetCity.cityData != null && !string.IsNullOrWhiteSpace(targetCity.cityData.cityName)
            ? targetCity.cityData.cityName
            : "Unknown";
        string attackerName = attacker != null && !string.IsNullOrWhiteSpace(attacker.factionName)
            ? attacker.factionName
            : "Unknown";
        double attackerWinPercent = attackerWinRate * 100d;

        AIDebugLogger.LogAI(
            attacker,
            $"[AI][ScavengerUnlock] attacker={attackerName}, city={cityName}, slot={slotIndex}, " +
            $"attackerScore={attackerScore}, scavengerScore={scavengerScore}, attackerWinRate={attackerWinPercent:F2}%, " +
            $"attackerWon={attackerWon}, unlocked={unlocked}, consumedCards={consumedCardCount}");
    }
}
