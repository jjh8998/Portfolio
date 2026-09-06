using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FactionCardInventoryScript
{
    [SerializeField] private CardInventory cardInventory = new CardInventory();

    private bool initialized;

    public event Action<string, int> CardInventoryChanged;

    public void Initialize()
    {
        if (cardInventory == null)
            cardInventory = new CardInventory();

        if (initialized)
            return;

        cardInventory.RebuildMap();
        initialized = true;
    }

    public bool GainCard(string _cardId, int _count = 1)
    {
        if (string.IsNullOrEmpty(_cardId))
        {
            Debug.LogWarning("[FactionManager] Card id is empty.");
            return false;
        }

        if (_count <= 0)
        {
            Debug.LogWarning($"[FactionManager] Invalid card count. id={_cardId}, count={_count}");
            return false;
        }

        if (cardInventory == null)
            cardInventory = new CardInventory();

        cardInventory.AddCard(_cardId, _count);
        CardInventoryChanged?.Invoke(_cardId, cardInventory.GetCount(_cardId));
        return true;
    }

    public bool GainCard(CardData _card, int _count = 1)
    {
        if (_card == null)
        {
            Debug.LogWarning("[FactionManager] Card data is null.");
            return false;
        }

        return GainCard(_card.id, _count);
    }

    public bool GainCards(Dictionary<string, int> _cardCounts)
    {
        if (_cardCounts == null || _cardCounts.Count == 0)
            return false;

        if (cardInventory == null)
            cardInventory = new CardInventory();

        if (!cardInventory.AddCards(_cardCounts))
            return false;

        foreach (KeyValuePair<string, int> pair in _cardCounts)
            CardInventoryChanged?.Invoke(pair.Key, cardInventory.GetCount(pair.Key));

        return true;
    }

    public bool RemoveCard(string _cardId, int _count, out Dictionary<string, int> _removedMap)
    {
        _removedMap = new Dictionary<string, int>();

        if (string.IsNullOrWhiteSpace(_cardId))
            return false;

        if (_count <= 0)
            return false;

        if (cardInventory == null)
            return false;

        if (cardInventory.GetCount(_cardId) < _count)
            return false;

        if (!cardInventory.RemoveCard(_cardId, _count))
            return false;

        _removedMap[_cardId] = _count;
        CardInventoryChanged?.Invoke(_cardId, cardInventory.GetCount(_cardId));
        return true;
    }

    public Dictionary<string, int> ConsumeCards(List<string> _cardIds)
    {
        Dictionary<string, int> removedMap = new Dictionary<string, int>();

        if (_cardIds == null || _cardIds.Count == 0)
            return removedMap;

        if (cardInventory == null)
            return removedMap;

        Dictionary<string, int> countMap = new Dictionary<string, int>();
        for (int i = 0; i < _cardIds.Count; i++)
        {
            string cardId = _cardIds[i];
            if (string.IsNullOrEmpty(cardId))
                continue;

            if (countMap.ContainsKey(cardId))
                countMap[cardId]++;
            else
                countMap[cardId] = 1;
        }

        foreach (KeyValuePair<string, int> pair in countMap)
        {
            int available = cardInventory.GetCount(pair.Key);
            int toRemove = Math.Min(pair.Value, available);
            if (toRemove <= 0)
                continue;

            cardInventory.RemoveCard(pair.Key, toRemove);
            removedMap[pair.Key] = toRemove;
            CardInventoryChanged?.Invoke(pair.Key, cardInventory.GetCount(pair.Key));
        }

        return removedMap;
    }

    public Dictionary<string, int> ConsumeCards(Dictionary<string, int> _cardCounts)
    {
        Dictionary<string, int> removedMap = new Dictionary<string, int>();

        if (_cardCounts == null || _cardCounts.Count == 0)
            return removedMap;

        if (cardInventory == null)
            return removedMap;

        foreach (KeyValuePair<string, int> pair in _cardCounts)
        {
            string cardId = pair.Key;
            if (string.IsNullOrWhiteSpace(cardId) || pair.Value <= 0)
                continue;

            int available = cardInventory.GetCount(cardId);
            int toRemove = Math.Min(pair.Value, available);
            if (toRemove <= 0)
                continue;

            cardInventory.RemoveCard(cardId, toRemove);
            removedMap[cardId] = toRemove;
            CardInventoryChanged?.Invoke(cardId, cardInventory.GetCount(cardId));
        }

        return removedMap;
    }

    public int GetCardCount(string _cardId)
    {
        return cardInventory != null ? cardInventory.GetCount(_cardId) : 0;
    }

    public CardInventory GetCardInventory()
    {
        if (cardInventory == null)
            cardInventory = new CardInventory();

        return cardInventory;
    }

    public List<CardStackSaveData> ExportCardInventory()
    {
        List<CardStackSaveData> result = new List<CardStackSaveData>();
        if (cardInventory == null)
            return result;

        List<CardStack> cards = cardInventory.GetAll();
        cards.Sort((a, b) => string.Compare(a?.cardId, b?.cardId, StringComparison.Ordinal));

        for (int i = 0; i < cards.Count; i++)
        {
            CardStack stack = cards[i];
            if (stack == null || string.IsNullOrWhiteSpace(stack.cardId) || stack.count <= 0)
                continue;

            result.Add(new CardStackSaveData
            {
                cardId = stack.cardId,
                count = stack.count
            });
        }

        return result;
    }

    public void ImportCardInventory(List<CardStackSaveData> _data)
    {
        cardInventory = new CardInventory();
        initialized = true;

        if (_data == null)
            return;

        CardDatabase cardDb = CardDatabase.Instance;

        for (int i = 0; i < _data.Count; i++)
        {
            CardStackSaveData stack = _data[i];
            if (stack == null)
                continue;

            if (string.IsNullOrWhiteSpace(stack.cardId))
            {
                Debug.LogWarning("[FactionManager] Skipped card import with empty cardId.");
                continue;
            }

            if (stack.count <= 0)
            {
                Debug.LogWarning($"[FactionManager] Skipped card import with invalid count. id={stack.cardId}, count={stack.count}");
                continue;
            }

            if (cardDb.GetById(stack.cardId) == null)
            {
                Debug.LogWarning($"[FactionManager] Card '{stack.cardId}' not found during import.");
                continue;
            }

            cardInventory.AddCard(stack.cardId, stack.count);
        }
    }
}
