using System;
using System.Collections.Generic;
using UnityEngine;

public static class CardPackOpenService
{
    public const int DefaultDrawCount = 3;

    public static bool TryOpenAllPacks(
        FactionManager _owner,
        CardPackDatabaseSO _packDatabase,
        out int _openedCount,
        out int _failedCount,
        int _drawCount = DefaultDrawCount)
    {
        _openedCount = 0;
        _failedCount = 0;

        if (_owner == null)
        {
            Debug.LogWarning("[CardPackOpen] Owner is null.");
            return false;
        }

        if (_packDatabase == null)
        {
            Debug.LogWarning("[CardPackOpen] Pack database is null.");
            return false;
        }

        FactionCardPackInventoryScript packInventory = _owner.GetCardPackInventory();
        List<CardStack> cardPackSnapshot = packInventory != null
            ? packInventory.GetAllCardPacks()
            : null;

        if (cardPackSnapshot == null || cardPackSnapshot.Count == 0)
            return true;

        int baseDrawCount = Mathf.Max(1, _drawCount);
        int researchBonus = ResolveCardPackOpenCardCountBonus(_owner);
        int drawCount = Mathf.Max(1, baseDrawCount + researchBonus);
        if (_drawCount <= 0)
            Debug.LogWarning($"[CardPackOpen] Invalid draw count. drawCount={_drawCount}. Fallback={baseDrawCount}");

        Dictionary<string, int> gainedCardCounts = new Dictionary<string, int>();
        Dictionary<string, int> consumedPackCounts = new Dictionary<string, int>();
        Dictionary<string, int> currentPackRewards = new Dictionary<string, int>();

        for (int i = 0; i < cardPackSnapshot.Count; i++)
        {
            CardStack stack = cardPackSnapshot[i];
            if (stack == null || string.IsNullOrWhiteSpace(stack.cardId) || stack.count <= 0)
                continue;

            if (!TryGetRewardPool(
                    _packDatabase,
                    stack.cardId,
                    out CardPackData packData,
                    out Dictionary<int, List<CardData>> rewardPoolByTier,
                    out string errorMessage))
            {
                _failedCount += stack.count;
                Debug.LogWarning($"[CardPackOpen] {errorMessage} packId={stack.cardId}, count={stack.count}");
                continue;
            }

            int openedForPackId = 0;
            for (int packIndex = 0; packIndex < stack.count; packIndex++)
            {
                currentPackRewards.Clear();
                bool selectedAllRewards = true;

                for (int drawIndex = 0; drawIndex < drawCount; drawIndex++)
                {
                    if (!TryCreateRewardCardId(packData, rewardPoolByTier, out string selectedCardId))
                    {
                        selectedAllRewards = false;
                        break;
                    }

                    AddCount(currentPackRewards, selectedCardId, 1);
                }

                if (!selectedAllRewards)
                {
                    _failedCount++;
                    Debug.LogWarning($"[CardPackOpen] Failed to select reward card. packId={stack.cardId}");
                    continue;
                }

                foreach (KeyValuePair<string, int> pair in currentPackRewards)
                    AddCount(gainedCardCounts, pair.Key, pair.Value);

                openedForPackId++;
                _openedCount++;
            }

            if (openedForPackId > 0)
                consumedPackCounts.Add(stack.cardId, openedForPackId);
        }

        if (_openedCount == 0)
            return _failedCount == 0;

        if (!_owner.RemoveCardPacks(consumedPackCounts))
        {
            Debug.LogWarning("[CardPackOpen] Failed to consume card packs in batch.");
            _failedCount += _openedCount;
            _openedCount = 0;
            return false;
        }

        if (!_owner.GainCards(gainedCardCounts))
        {
            Debug.LogWarning("[CardPackOpen] Failed to grant cards in batch.");
            _failedCount += _openedCount;
            _openedCount = 0;
            return false;
        }

        return _failedCount == 0;
    }

    public static bool TryOpenPack(
        FactionManager _owner,
        CardPackDatabaseSO _packDatabase,
        string _packId,
        out CardPackOpenResult _result,
        int _drawCount = DefaultDrawCount)
    {
        _result = new CardPackOpenResult
        {
            packId = _packId,
            success = false
        };

        if (_owner == null)
        {
            _result.errorMessage = "Owner is null.";
            Debug.LogWarning("[CardPackOpen] Owner is null.");
            return false;
        }

        if (_packDatabase == null)
        {
            _result.errorMessage = "Pack database is null.";
            Debug.LogWarning("[CardPackOpen] Pack database is null.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(_packId))
        {
            _result.errorMessage = "Pack id is empty.";
            Debug.LogWarning("[CardPackOpen] Pack id is empty.");
            return false;
        }

        int baseDrawCount = Mathf.Max(1, _drawCount);
        int researchBonus = ResolveCardPackOpenCardCountBonus(_owner);
        int drawCount = Mathf.Max(1, baseDrawCount + researchBonus);
        _result.drawCount = drawCount;
        if (_drawCount <= 0)
        {
            Debug.LogWarning($"[CardPackOpen] Invalid draw count. packId={_packId}, drawCount={_drawCount}. Fallback={baseDrawCount}");
        }

        if (!TryGetRewardPool(
                _packDatabase,
                _packId,
                out CardPackData packData,
                out Dictionary<int, List<CardData>> rewardPoolByTier,
                out string errorMessage))
        {
            _result.errorMessage = errorMessage;
            Debug.LogWarning($"[CardPackOpen] {errorMessage} packId={_packId}");
            return false;
        }

        if (!_owner.HasCardPack(_packId))
        {
            _result.errorMessage = "Card pack is not owned.";
            Debug.LogWarning($"[CardPackOpen] Owner does not have pack. packId={_packId}");
            return false;
        }

        List<string> selectedCardIds = new List<string>(drawCount);
        for (int i = 0; i < drawCount; i++)
        {
            if (!TryCreateRewardCardId(packData, rewardPoolByTier, out string selectedCardId))
            {
                _result.errorMessage = "Failed to select reward card.";
                Debug.LogWarning($"[CardPackOpen] Failed to select reward card. packId={_packId}");
                return false;
            }

            selectedCardIds.Add(selectedCardId);
        }

        if (!_owner.RemoveCardPack(_packId, 1))
        {
            _result.errorMessage = "Failed to consume card pack.";
            Debug.LogWarning($"[CardPackOpen] Failed to remove pack from owner. packId={_packId}");
            return false;
        }

        for (int i = 0; i < selectedCardIds.Count; i++)
        {
            string cardId = selectedCardIds[i];
            if (!_owner.GainCard(cardId, 1))
            {
                _result.errorMessage = $"Failed to grant card: {cardId}";
                Debug.LogWarning($"[CardPackOpen] Failed to grant card. packId={_packId}, cardId={cardId}");
                return false;
            }

            _result.gainedCardIds.Add(cardId);
        }

        _result.success = true;
        return true;
    }

    private static int ResolveCardPackOpenCardCountBonus(FactionManager _owner)
    {
        if (_owner == null)
            return 0;

        FactionResearchState researchState = _owner.GetResearchState;
        if (researchState == null || researchState.modifiers == null)
            return 0;

        return Mathf.Max(0, researchState.modifiers.cardPackOpenCardCountBonus);
    }

    private static bool TryCreateRewardCardId(CardPackData _packData, Dictionary<int, List<CardData>> _rewardPoolByTier, out string _cardId)
    {
        _cardId = null;

        if (!TrySelectRewardTier(_packData, _rewardPoolByTier, out int selectedTier))
            return false;

        if (!_rewardPoolByTier.TryGetValue(selectedTier, out List<CardData> tierPool)
            || tierPool == null
            || tierPool.Count == 0)
            return false;

        CardData selectedCard = tierPool[UnityEngine.Random.Range(0, tierPool.Count)];
        if (selectedCard == null || string.IsNullOrWhiteSpace(selectedCard.id))
            return false;

        _cardId = selectedCard.id;
        return true;
    }

    private static bool TrySelectRewardTier(CardPackData _packData, Dictionary<int, List<CardData>> _rewardPoolByTier, out int _tier)
    {
        _tier = 0;

        if (_packData == null || _rewardPoolByTier == null || _rewardPoolByTier.Count == 0)
            return false;

        int totalWeight = 0;

        for (int tier = 1; tier <= 5; tier++)
        {
            int weight = Mathf.Max(0, GetPackTierRate(_packData, tier));
            if (weight <= 0 || !HasRewardCardsInTier(_rewardPoolByTier, tier))
                continue;

            totalWeight += weight;
        }

        bool useFallbackWeights = totalWeight <= 0;
        if (useFallbackWeights)
        {
            for (int tier = 1; tier <= 5; tier++)
            {
                if (HasRewardCardsInTier(_rewardPoolByTier, tier))
                    totalWeight++;
            }
        }

        if (totalWeight <= 0)
            return false;

        int roll = UnityEngine.Random.Range(0, totalWeight);
        int cumulative = 0;
        int lastCandidateTier = 0;

        for (int tier = 1; tier <= 5; tier++)
        {
            if (!HasRewardCardsInTier(_rewardPoolByTier, tier))
                continue;

            int weight = useFallbackWeights
                ? 1
                : Mathf.Max(0, GetPackTierRate(_packData, tier));
            if (weight <= 0)
                continue;

            lastCandidateTier = tier;
            cumulative += weight;
            if (roll < cumulative)
            {
                _tier = tier;
                return true;
            }
        }

        _tier = lastCandidateTier;
        return _tier > 0;
    }

    private static int GetPackTierRate(CardPackData _packData, int _tier)
    {
        if (_packData == null)
            return 0;

        switch (_tier)
        {
            case 1: return _packData.tier1Rate;
            case 2: return _packData.tier2Rate;
            case 3: return _packData.tier3Rate;
            case 4: return _packData.tier4Rate;
            case 5: return _packData.tier5Rate;
            default: return 0;
        }
    }

    private static void AddCount(Dictionary<string, int> _counts, string _id, int _count)
    {
        if (_counts.TryGetValue(_id, out int currentCount))
            _counts[_id] = currentCount + _count;
        else
            _counts.Add(_id, _count);
    }

    private static bool TryGetRewardPool(
        CardPackDatabaseSO _packDatabase,
        string _packId,
        out CardPackData _packData,
        out Dictionary<int, List<CardData>> _rewardPoolByTier,
        out string _errorMessage)
    {
        _packData = _packDatabase.GetCardPackByID(_packId);
        _rewardPoolByTier = null;
        _errorMessage = null;

        if (_packData == null)
        {
            _errorMessage = "Card pack data not found.";
            return false;
        }

        _rewardPoolByTier = GetRewardPoolByTier(_packData);
        if (_rewardPoolByTier == null || _rewardPoolByTier.Count == 0)
        {
            _errorMessage = "No cards can be drawn from this pack.";
            return false;
        }

        return true;
    }

    private static bool HasRewardCardsInTier(Dictionary<int, List<CardData>> _rewardPoolByTier, int _tier)
    {
        return _rewardPoolByTier != null
            && _rewardPoolByTier.TryGetValue(_tier, out List<CardData> cards)
            && cards != null
            && cards.Count > 0;
    }

    private static Dictionary<int, List<CardData>> GetRewardPoolByTier(CardPackData _packData)
    {
        CardDatabase database = CardDatabase.Instance;
        if (database == null || !database.IsLoaded)
        {
            Debug.LogWarning("[CardPackOpen] CardDatabase is not ready.");
            return null;
        }

        IReadOnlyList<CardData> allCards = database.GetAll();
        if (allCards == null || allCards.Count == 0)
        {
            Debug.LogWarning("[CardPackOpen] Card pool is empty.");
            return null;
        }

        HashSet<string> themeTokens = ParseThemeTokens(_packData != null ? _packData.theme : null);
        Dictionary<int, List<CardData>> result = new Dictionary<int, List<CardData>>();

        for (int i = 0; i < allCards.Count; i++)
        {
            CardData card = allCards[i];
            if (card == null || string.IsNullOrWhiteSpace(card.id))
                continue;

            if (card.tier < 1 || card.tier > 5)
                continue;

            if (!MatchesPackTheme(card, themeTokens))
                continue;

            if (!result.TryGetValue(card.tier, out List<CardData> tierCards))
            {
                tierCards = new List<CardData>();
                result.Add(card.tier, tierCards);
            }

            tierCards.Add(card);
        }

        return result;
    }

    private static HashSet<string> ParseThemeTokens(string _theme)
    {
        HashSet<string> result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(_theme))
            return result;

        string[] tokens = _theme.Split(';');
        for (int i = 0; i < tokens.Length; i++)
        {
            string token = tokens[i].Trim();
            if (string.IsNullOrWhiteSpace(token))
                continue;

            result.Add(token);
        }

        return result;
    }

    private static bool MatchesPackTheme(CardData _card, HashSet<string> _themeTokens)
    {
        if (_card == null)
            return false;

        if (_themeTokens == null || _themeTokens.Count == 0)
            return true;

        if (string.IsNullOrWhiteSpace(_card.pack))
            return false;

        string cardPack = _card.pack.Trim();
        foreach (string themeToken in _themeTokens)
        {
            if (string.Equals(cardPack, themeToken, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
