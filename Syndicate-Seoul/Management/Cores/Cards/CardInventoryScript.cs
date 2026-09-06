using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CardStack
{
    public string cardId;
    public int count;

    public CardStack(string _cardId, int _count)
    {
        cardId = _cardId;
        count = _count;
    }
}

[Serializable]
public class CardInventory : ISerializationCallbackReceiver
{
    [SerializeField] private List<CardStack> cards = new List<CardStack>();

    private Dictionary<string, int> cardMap = new Dictionary<string, int>();

    /// <summary>
    /// 총 카드 수 (핵심)
    /// </summary>
    public int Count
    {
        get
        {
            int total = 0;
            foreach (var pair in cardMap)
                total += pair.Value;

            return total;
        }
    }

    /// <summary>
    /// 카드 종류 수 (필요할 때만 사용)
    /// </summary>
    public int UniqueCardTypeCount => cardMap.Count;

    public void OnBeforeSerialize()
    {
        SyncCardsFromMap();
    }

    public void OnAfterDeserialize()
    {
        RebuildMap();
    }

    public void RebuildMap()
    {
        if (cardMap == null)
            cardMap = new Dictionary<string, int>();
        else
            cardMap.Clear();

        if (cards == null) return;

        for (int i = 0; i < cards.Count; i++)
        {
            CardStack stack = cards[i];
            if (stack == null) continue;
            if (string.IsNullOrEmpty(stack.cardId)) continue;
            if (stack.count <= 0) continue;

            if (cardMap.ContainsKey(stack.cardId))
            {
                Debug.LogWarning($"[CardInventory] Duplicate cardId detected: {stack.cardId}");
                cardMap[stack.cardId] += stack.count;
            }
            else
            {
                cardMap.Add(stack.cardId, stack.count);
            }
        }
    }

    private void SyncCardsFromMap()
    {
        if (cardMap == null)
            cardMap = new Dictionary<string, int>();

        if (cards == null)
            cards = new List<CardStack>();
        else
            cards.Clear();

        // 정렬 (디버깅 안정성)
        var keys = new List<string>(cardMap.Keys);
        keys.Sort(StringComparer.Ordinal);

        foreach (var key in keys)
        {
            int value = cardMap[key];
            if (value <= 0) continue;

            cards.Add(new CardStack(key, value));
        }
    }

    public int GetCount(string _cardId)
    {
        if (string.IsNullOrEmpty(_cardId)) return 0;

        return cardMap.TryGetValue(_cardId, out int count) ? count : 0;
    }

    public bool HasCard(string _cardId, int _count = 1)
    {
        return GetCount(_cardId) >= _count;
    }

    public void AddCard(string _cardId, int _count = 1)
    {
        if (string.IsNullOrEmpty(_cardId)) return;
        if (_count <= 0) return;

        if (cardMap == null)
            cardMap = new Dictionary<string, int>();

        if (cardMap.ContainsKey(_cardId))
            cardMap[_cardId] += _count;
        else
            cardMap.Add(_cardId, _count);

        SyncCardsFromMap();
    }

    public void AddCard(CardData _card, int _count = 1)
    {
        if (_card == null) return;
        AddCard(_card.id, _count);
    }

    public bool AddCards(Dictionary<string, int> _cardCounts)
    {
        if (_cardCounts == null || _cardCounts.Count == 0)
            return false;

        foreach (KeyValuePair<string, int> pair in _cardCounts)
        {
            if (string.IsNullOrEmpty(pair.Key) || pair.Value <= 0)
                return false;
        }

        if (cardMap == null)
            cardMap = new Dictionary<string, int>();

        foreach (KeyValuePair<string, int> pair in _cardCounts)
        {
            if (cardMap.ContainsKey(pair.Key))
                cardMap[pair.Key] += pair.Value;
            else
                cardMap.Add(pair.Key, pair.Value);
        }

        SyncCardsFromMap();
        return true;
    }

    public bool RemoveCard(string _cardId, int _count = 1)
    {
        if (string.IsNullOrEmpty(_cardId)) return false;
        if (_count <= 0) return false;
        if (cardMap == null) return false;

        if (!cardMap.TryGetValue(_cardId, out int count)) return false;
        if (count < _count) return false;

        count -= _count;

        if (count <= 0)
            cardMap.Remove(_cardId);
        else
            cardMap[_cardId] = count;

        SyncCardsFromMap();
        return true;
    }

    public bool RemoveCard(CardData _card, int _count = 1)
    {
        if (_card == null) return false;
        return RemoveCard(_card.id, _count);
    }

    public bool RemoveCards(Dictionary<string, int> _cardCounts)
    {
        if (_cardCounts == null || _cardCounts.Count == 0)
            return false;

        if (cardMap == null)
            return false;

        foreach (KeyValuePair<string, int> pair in _cardCounts)
        {
            if (string.IsNullOrEmpty(pair.Key) || pair.Value <= 0)
                return false;

            if (!cardMap.TryGetValue(pair.Key, out int count) || count < pair.Value)
                return false;
        }

        foreach (KeyValuePair<string, int> pair in _cardCounts)
        {
            int count = cardMap[pair.Key] - pair.Value;
            if (count <= 0)
                cardMap.Remove(pair.Key);
            else
                cardMap[pair.Key] = count;
        }

        SyncCardsFromMap();
        return true;
    }

    public List<CardStack> GetAll()
    {
        List<CardStack> result = new List<CardStack>(cardMap.Count);

        foreach (var pair in cardMap)
            result.Add(new CardStack(pair.Key, pair.Value));

        return result;
    }

    /// <summary>
    /// Performance-oriented read-only access. Do not modify the returned map.
    /// </summary>
    public Dictionary<string, int> GetCardCountMapForReadOnlyUse()
    {
        return cardMap;
    }

    public List<CardData> CreateBattleDeck(Dictionary<string, CardData> _cardDatabase)
    {
        List<CardData> result = new List<CardData>();

        if (_cardDatabase == null) return result;

        foreach (var pair in cardMap)
        {
            if (!_cardDatabase.TryGetValue(pair.Key, out CardData card)) continue;

            for (int i = 0; i < pair.Value; i++)
                result.Add(card.Clone());
        }

        return result;
    }
}
