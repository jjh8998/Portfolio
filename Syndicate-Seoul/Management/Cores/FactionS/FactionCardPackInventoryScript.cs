using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FactionCardPackInventoryScript
{
    [SerializeField] private CardInventory cardPackInventory = new CardInventory();

    private bool initialized;

    public event Action<string, int> CardPackInventoryChanged;

    public void Initialize()
    {
        if (cardPackInventory == null)
            cardPackInventory = new CardInventory();

        if (initialized)
            return;

        cardPackInventory.RebuildMap();
        initialized = true;
    }

    public bool GainCardPack(string _packId, int _count = 1)
    {
        if (string.IsNullOrWhiteSpace(_packId))
        {
            Debug.LogWarning("[FactionManager] Card pack id is empty.");
            return false;
        }

        if (_count <= 0)
        {
            Debug.LogWarning($"[FactionManager] Invalid card pack count. id={_packId}, count={_count}");
            return false;
        }

        if (cardPackInventory == null)
            cardPackInventory = new CardInventory();

        cardPackInventory.AddCard(_packId, _count);
        CardPackInventoryChanged?.Invoke(_packId, cardPackInventory.GetCount(_packId));
        return true;
    }

    public bool RemoveCardPack(string _packId, int _count = 1)
    {
        if (string.IsNullOrWhiteSpace(_packId))
            return false;

        if (_count <= 0)
            return false;

        if (cardPackInventory == null)
            return false;

        if (cardPackInventory.GetCount(_packId) < _count)
            return false;

        if (!cardPackInventory.RemoveCard(_packId, _count))
            return false;

        CardPackInventoryChanged?.Invoke(_packId, cardPackInventory.GetCount(_packId));
        return true;
    }

    public bool RemoveCardPacks(Dictionary<string, int> _packCounts)
    {
        if (_packCounts == null || _packCounts.Count == 0)
            return false;

        if (cardPackInventory == null)
            return false;

        if (!cardPackInventory.RemoveCards(_packCounts))
            return false;

        foreach (KeyValuePair<string, int> pair in _packCounts)
            CardPackInventoryChanged?.Invoke(pair.Key, cardPackInventory.GetCount(pair.Key));

        return true;
    }

    public int GetCardPackCount(string _packId)
    {
        return cardPackInventory != null ? cardPackInventory.GetCount(_packId) : 0;
    }

    public bool HasCardPack(string _packId, int _count = 1)
    {
        return GetCardPackCount(_packId) >= _count;
    }

    public int GetTotalCardPackCount()
    {
        return cardPackInventory != null ? cardPackInventory.Count : 0;
    }

    public List<CardStack> GetAllCardPacks()
    {
        List<CardStack> result = new List<CardStack>();
        if (cardPackInventory == null)
            return result;

        result = cardPackInventory.GetAll();
        result.Sort((a, b) => string.Compare(a?.cardId, b?.cardId, StringComparison.Ordinal));
        return result;
    }

    public List<CardPackStackSaveData> ExportCardPackInventory()
    {
        List<CardPackStackSaveData> result = new List<CardPackStackSaveData>();
        if (cardPackInventory == null)
            return result;

        List<CardStack> packs = cardPackInventory.GetAll();
        packs.Sort((a, b) => string.Compare(a?.cardId, b?.cardId, StringComparison.Ordinal));

        for (int i = 0; i < packs.Count; i++)
        {
            CardStack stack = packs[i];
            if (stack == null || string.IsNullOrWhiteSpace(stack.cardId) || stack.count <= 0)
                continue;

            result.Add(new CardPackStackSaveData
            {
                packId = stack.cardId,
                count = stack.count
            });
        }

        return result;
    }

    public void ImportCardPackInventory(List<CardPackStackSaveData> _data)
    {
        cardPackInventory = new CardInventory();
        initialized = true;

        if (_data == null)
            return;

        for (int i = 0; i < _data.Count; i++)
        {
            CardPackStackSaveData stack = _data[i];
            if (stack == null)
                continue;

            if (string.IsNullOrWhiteSpace(stack.packId))
            {
                Debug.LogWarning("[FactionManager] Skipped card pack import with empty packId.");
                continue;
            }

            if (stack.count <= 0)
            {
                Debug.LogWarning($"[FactionManager] Skipped card pack import with invalid count. id={stack.packId}, count={stack.count}");
                continue;
            }

            cardPackInventory.AddCard(stack.packId, stack.count);
        }
    }
}
