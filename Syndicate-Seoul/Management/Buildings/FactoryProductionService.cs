using System;
using UnityEngine;

public static class FactoryProductionService
{
    public static event Action<FactionManager, string, int> CardPackProduced;

    private static CardPackDatabaseSO cardPackDatabase;

    public static bool TryProduceCards(BuildingData _factoryData, FactionManager _owner)
    {
        if (_factoryData == null || !_factoryData.IsFactory())
        {
            return false;
        }

        if (_owner == null)
        {
            Debug.LogWarning("[Factory] Owner is null. Cannot grant card packs.");
            return false;
        }

        return TryProduceCardPacks(_factoryData, _owner);
    }

    private static bool TryProduceCardPacks(BuildingData _factoryData, FactionManager _owner)
    {
        int packCount = Mathf.Max(1, _factoryData != null ? _factoryData.cardYieldAmount : 1);
        int selectedTier = SelectCardPackTier(_factoryData);
        string packId = GetRandomCardPackIdByTier(selectedTier);

        if (string.IsNullOrWhiteSpace(packId))
        {
            Debug.LogWarning($"[Factory] Invalid card pack id for tier {selectedTier}.");
            return false;
        }

        CardPackDatabaseSO database = GetCardPackDatabase();
        if (database != null && database.GetCardPackByID(packId) == null)
        {
            Debug.LogWarning($"[Factory] Card pack not found. packId={packId}");
            return false;
        }

        if (!_owner.GainCardPack(packId, packCount))
        {
            Debug.LogWarning($"[Factory] Failed to grant card pack. id={packId}, count={packCount}");
            return false;
        }

        CardPackProduced?.Invoke(_owner, packId, packCount);
        return true;
    }

    private static int SelectCardPackTier(BuildingData _factoryData)
    {
        float tier1Rate = Mathf.Max(0, _factoryData != null ? _factoryData.tier1Rate : 0);
        float tier2Rate = Mathf.Max(0, _factoryData != null ? _factoryData.tier2Rate : 0);
        float tier3Rate = Mathf.Max(0, _factoryData != null ? _factoryData.tier3Rate : 0);
        float tier4Rate = Mathf.Max(0, _factoryData != null ? _factoryData.tier4Rate : 0);
        float tier5Rate = Mathf.Max(0, _factoryData != null ? _factoryData.tier5Rate : 0);

        float totalRate = tier1Rate + tier2Rate + tier3Rate + tier4Rate + tier5Rate;
        if (totalRate <= 0f)
            return 1;

        float roll = UnityEngine.Random.Range(0f, totalRate);
        float cumulative = 0f;

        cumulative += tier1Rate;
        if (roll < cumulative)
            return 1;

        cumulative += tier2Rate;
        if (roll < cumulative)
            return 2;

        cumulative += tier3Rate;
        if (roll < cumulative)
            return 3;

        cumulative += tier4Rate;
        if (roll < cumulative)
            return 4;

        return 5;
    }

    private static string GetRandomCardPackIdByTier(int _tier)
    {
        CardPackDatabaseSO database = GetCardPackDatabase();
        if (database == null || database.allCardPacks == null || database.allCardPacks.Count == 0)
            return GetDefaultCardPackIdByTier(_tier);

        string tierSuffix = $"_t{Mathf.Clamp(_tier, 1, 5)}";
        string selectedPackId = null;
        int candidateCount = 0;

        for (int i = 0; i < database.allCardPacks.Count; i++)
        {
            CardPackData cardPack = database.allCardPacks[i];
            if (cardPack == null || string.IsNullOrWhiteSpace(cardPack.id))
                continue;

            if (!cardPack.id.EndsWith(tierSuffix, StringComparison.OrdinalIgnoreCase))
                continue;

            candidateCount++;
            if (UnityEngine.Random.Range(0, candidateCount) == 0)
                selectedPackId = cardPack.id;
        }

        return !string.IsNullOrWhiteSpace(selectedPackId)
            ? selectedPackId
            : GetDefaultCardPackIdByTier(_tier);
    }

    private static string GetDefaultCardPackIdByTier(int _tier)
    {
        switch (_tier)
        {
            case 1:
                return "CP_overclock_t1";
            case 2:
                return "CP_overclock_t2";
            case 3:
                return "CP_overclock_t3";
            case 4:
                return "CP_overclock_t4";
            case 5:
                return "CP_overclock_t5";
        }

        return "CP_overclock_t1";
    }

    private static CardPackDatabaseSO GetCardPackDatabase()
    {
        if (cardPackDatabase == null)
        {
            cardPackDatabase = Resources.Load<CardPackDatabaseSO>("Databases/CardPackDatabase");
            if (cardPackDatabase == null)
            {
                cardPackDatabase = ScriptableObject.CreateInstance<CardPackDatabaseSO>();
                cardPackDatabase.LoadCSV();
                Debug.LogWarning("[Factory] Card pack DB asset not found in Resources. Fallback instance created.");
            }
        }

        return cardPackDatabase;
    }
}
