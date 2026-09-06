using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class CardIncinerationPowerService : MonoBehaviour
{
    [SerializeField] private FactionManager factionManager;
    [SerializeField] private CalendarScript calendar;

    private readonly List<ActiveIncinerationEffect> activeEffects = new List<ActiveIncinerationEffect>();
    private bool isSubscribed;

    public event Action ActiveEffectsChanged;

    public class ActiveIncinerationEffectViewData
    {
        public string cardId;
        public int powerAmount;
        public int remainingMonths;
    }

    private class ActiveIncinerationEffect
    {
        public string cardId;
        public int powerAmount;
        public int remainingMonths;
    }

    private void Awake()
    {
        EnsureReferences();
    }

    private void OnEnable()
    {
        EnsureReferences();

        if (calendar == null || isSubscribed)
            return;

        calendar.MonthChanged += OnMonthChanged;
        isSubscribed = true;
    }

    private void OnDisable()
    {
        if (calendar != null && isSubscribed)
            calendar.MonthChanged -= OnMonthChanged;

        isSubscribed = false;
    }

    public bool TryIncinerateSelectedCard(string _cardId)
    {
        return TryIncinerateSelectedCard(_cardId, (BuildingAbility)null);
    }

    public bool TryIncinerateSelectedCard(string _cardId, BuildingAbility _ability)
    {
        EnsureReferences();

        if (string.IsNullOrWhiteSpace(_cardId))
        {
            Debug.LogWarning("[CardIncinerationPowerService] Card id is empty.");
            return false;
        }

        if (factionManager == null)
        {
            Debug.LogWarning("[CardIncinerationPowerService] FactionManager is missing.");
            return false;
        }

        CardIncinerationPowerAbility operationalAbility = _ability as CardIncinerationPowerAbility;
        if (operationalAbility == null && !TryGetOperationalCardIncinerationAbility(out operationalAbility))
        {
            Debug.LogWarning("[CardIncinerationPowerService] Operational card incineration power building is required.");
            return false;
        }

        return TryIncinerateSelectedCard(_cardId, operationalAbility);
    }

    public bool TryIncinerateSelectedCard(string _cardId, CityScript _city)
    {
        EnsureReferences();

        if (_city == null || _city.cityData == null)
        {
            Debug.LogWarning("[CardIncinerationPowerService] City or city data is missing.");
            return false;
        }

        if (_city.cityData.owner == null)
        {
            Debug.LogWarning("[CardIncinerationPowerService] City owner is missing.");
            return false;
        }

        SetFactionManager(_city.cityData.owner);

        if (!TryGetOperationalCardIncinerationAbility(_city, out CardIncinerationPowerAbility operationalAbility))
        {
            Debug.LogWarning("[CardIncinerationPowerService] Operational card incineration power building is required in the selected city.");
            return false;
        }

        return TryIncinerateSelectedCard(_cardId, operationalAbility);
    }

    public void SetFactionManager(FactionManager _factionManager)
    {
        if (_factionManager == null)
        {
            Debug.LogWarning("[CardIncinerationPowerService] Cannot set null FactionManager.");
            return;
        }

        factionManager = _factionManager;
    }

    public bool HasActiveIncinerationEffect()
    {
        for (int i = 0; i < activeEffects.Count; i++)
        {
            ActiveIncinerationEffect effect = activeEffects[i];
            if (effect == null)
                continue;

            if (effect.remainingMonths > 0 && effect.powerAmount > 0)
                return true;
        }

        return false;
    }

    public bool TryAutoIncinerateAvailableCard()
    {
        EnsureReferences();

        if (factionManager == null)
            return false;

        if (HasActiveIncinerationEffect())
            return false;

        if (!TryGetOperationalCardIncinerationAbility(out CardIncinerationPowerAbility ability))
            return false;

        if (!BuildingAbilityFactory.TryGetCardIncinerationValues(
            ability,
            out int powerGain,
            out int durationMonths,
            out int cardCost))
        {
            return false;
        }

        if (powerGain <= 0 || durationMonths <= 0 || cardCost <= 0)
            return false;

        if (!TrySelectAutoIncinerationCard(cardCost, out string selectedCardId))
            return false;

        return TryIncinerateSelectedCard(selectedCardId, ability);
    }

    private bool TryIncinerateSelectedCard(string _cardId, CardIncinerationPowerAbility _operationalAbility)
    {
        if (string.IsNullOrWhiteSpace(_cardId))
        {
            Debug.LogWarning("[CardIncinerationPowerService] Card id is empty.");
            return false;
        }

        if (factionManager == null)
        {
            Debug.LogWarning("[CardIncinerationPowerService] FactionManager is missing.");
            return false;
        }

        if (HasActiveIncinerationEffect())
        {
            Debug.LogWarning("[CardIncinerationPowerService] Card incineration is already running.");
            return false;
        }

        if (!BuildingAbilityFactory.TryGetCardIncinerationValues(
            _operationalAbility,
            out int activePowerGain,
            out int activeDurationMonths,
            out int activeCardCost))
        {
            Debug.LogWarning("[CardIncinerationPowerService] Card incineration ability is missing or invalid.");
            return false;
        }

        if (activePowerGain <= 0 || activeDurationMonths <= 0 || activeCardCost <= 0)
        {
            Debug.LogWarning("[CardIncinerationPowerService] Invalid power gain, duration, or card cost.");
            return false;
        }

        if (!factionManager.RemoveCard(_cardId, activeCardCost))
        {
            Debug.LogWarning($"[CardIncinerationPowerService] Failed to remove card. id={_cardId}, count={activeCardCost}");
            return false;
        }

        if (!factionManager.ChangeTradePower(activePowerGain))
        {
            factionManager.GainCard(_cardId, activeCardCost);
            Debug.LogWarning($"[CardIncinerationPowerService] Failed to add power. Restored card. id={_cardId}");
            return false;
        }

        activeEffects.Add(new ActiveIncinerationEffect
        {
            cardId = _cardId,
            powerAmount = activePowerGain,
            remainingMonths = activeDurationMonths
        });

        ActiveEffectsChanged?.Invoke();

        if (factionManager != null && !factionManager.IsPlayerFaction)
        {
            string factionName = !string.IsNullOrWhiteSpace(factionManager.factionName)
                ? factionManager.factionName
                : factionManager.name;
            AIDebugLogger.LogAI(
                factionManager,
                $"[AI][{factionName}] Card incineration success. " +
                $"burnedCardIds={BuildBurnedCardIdsText(_cardId, activeCardCost)}, " +
                $"powerGain={activePowerGain}, durationMonths={activeDurationMonths}, burnedCardCount={activeCardCost}");
        }

        return true;
    }

    private string BuildBurnedCardIdsText(string _cardId, int _cardCount)
    {
        if (string.IsNullOrWhiteSpace(_cardId))
            return string.Empty;

        int cardCount = Mathf.Max(1, _cardCount);
        if (cardCount == 1)
            return _cardId;

        StringBuilder builder = new StringBuilder(_cardId.Length * cardCount + cardCount - 1);
        for (int i = 0; i < cardCount; i++)
        {
            if (i > 0)
                builder.Append(',');

            builder.Append(_cardId);
        }

        return builder.ToString();
    }

    private bool TrySelectAutoIncinerationCard(int _cardCost, out string _cardId)
    {
        _cardId = string.Empty;

        CardInventory inventory = factionManager != null ? factionManager.GetCardInventory() : null;
        List<CardStack> cardStacks = inventory != null ? inventory.GetAll() : null;
        if (cardStacks == null || cardStacks.Count == 0)
            return false;

        CardDatabase database = CardDatabase.Instance;
        IReadOnlyList<CardData> allCards = database != null ? database.GetAll() : null;
        bool canUseCardDatabase = database != null && allCards != null && allCards.Count > 0;

        int bestTier = int.MaxValue;
        int bestCount = -1;
        string bestCardId = string.Empty;

        for (int i = 0; i < cardStacks.Count; i++)
        {
            CardStack stack = cardStacks[i];
            if (stack == null || string.IsNullOrWhiteSpace(stack.cardId) || stack.count < _cardCost)
                continue;

            int tier = 1;
            if (canUseCardDatabase)
            {
                CardData cardData = database.GetById(stack.cardId);
                if (cardData == null)
                    continue;

                tier = Mathf.Max(1, cardData.tier);
            }

            if (!IsBetterAutoIncinerationCandidate(stack.cardId, stack.count, tier, bestCardId, bestCount, bestTier))
                continue;

            bestCardId = stack.cardId;
            bestCount = stack.count;
            bestTier = tier;
        }

        if (string.IsNullOrWhiteSpace(bestCardId))
            return false;

        _cardId = bestCardId;
        return true;
    }

    private bool IsBetterAutoIncinerationCandidate(
        string _cardId,
        int _count,
        int _tier,
        string _bestCardId,
        int _bestCount,
        int _bestTier)
    {
        if (string.IsNullOrWhiteSpace(_bestCardId))
            return true;

        if (_tier != _bestTier)
            return _tier < _bestTier;

        if (_count != _bestCount)
            return _count > _bestCount;

        return string.Compare(_cardId, _bestCardId, StringComparison.Ordinal) < 0;
    }

    public List<ActiveIncinerationEffectSaveData> ExportActiveEffects()
    {
        List<ActiveIncinerationEffectSaveData> result = new List<ActiveIncinerationEffectSaveData>(activeEffects.Count);

        for (int i = 0; i < activeEffects.Count; i++)
        {
            ActiveIncinerationEffect effect = activeEffects[i];
            if (effect == null || effect.powerAmount <= 0 || effect.remainingMonths <= 0)
                continue;

            result.Add(new ActiveIncinerationEffectSaveData
            {
                cardId = effect.cardId,
                powerAmount = effect.powerAmount,
                remainingMonths = effect.remainingMonths
            });
        }

        return result;
    }

    public List<ActiveIncinerationEffectViewData> GetActiveEffectsViewData()
    {
        List<ActiveIncinerationEffectViewData> result = new List<ActiveIncinerationEffectViewData>(activeEffects.Count);

        for (int i = 0; i < activeEffects.Count; i++)
        {
            ActiveIncinerationEffect effect = activeEffects[i];
            if (effect == null || effect.powerAmount <= 0 || effect.remainingMonths <= 0)
                continue;

            result.Add(new ActiveIncinerationEffectViewData
            {
                cardId = effect.cardId,
                powerAmount = effect.powerAmount,
                remainingMonths = effect.remainingMonths
            });
        }

        return result;
    }

    public void ImportActiveEffects(List<ActiveIncinerationEffectSaveData> _data)
    {
        activeEffects.Clear();

        if (_data == null)
        {
            ActiveEffectsChanged?.Invoke();
            return;
        }

        for (int i = 0; i < _data.Count; i++)
        {
            ActiveIncinerationEffectSaveData effect = _data[i];
            if (effect == null)
                continue;

            if (effect.powerAmount <= 0 || effect.remainingMonths <= 0)
            {
                Debug.LogWarning("[CardIncinerationPowerService] Skipped invalid active incineration effect during import.");
                continue;
            }

            activeEffects.Add(new ActiveIncinerationEffect
            {
                cardId = effect.cardId,
                powerAmount = effect.powerAmount,
                remainingMonths = effect.remainingMonths
            });
        }

        ActiveEffectsChanged?.Invoke();
    }

    public bool IsOwnedBy(FactionManager _factionManager)
    {
        EnsureReferences();
        return factionManager == _factionManager;
    }

    private void OnMonthChanged(int _year, int _month)
    {
        EnsureReferences();
        bool hasChanged = false;

        for (int i = activeEffects.Count - 1; i >= 0; i--)
        {
            ActiveIncinerationEffect effect = activeEffects[i];
            if (effect == null)
            {
                activeEffects.RemoveAt(i);
                hasChanged = true;
                continue;
            }

            effect.remainingMonths--;
            hasChanged = true;

            if (effect.remainingMonths > 0)
                continue;

            if (factionManager == null)
                Debug.LogWarning($"[CardIncinerationPowerService] FactionManager is missing while expiring power bonus. amount={effect.powerAmount}");
            else
            {
                factionManager.ForceChangeTradePower(-effect.powerAmount);
            }

            activeEffects.RemoveAt(i);
        }

        if (hasChanged)
            ActiveEffectsChanged?.Invoke();
    }

    private void EnsureReferences()
    {
        if (factionManager == null)
            factionManager = GetComponent<FactionManager>();

        if (factionManager == null)
            factionManager = GetComponentInParent<FactionManager>();

        if (calendar == null)
            calendar = GetComponent<CalendarScript>();

        if (calendar == null)
            calendar = GetComponentInParent<CalendarScript>();

        if (calendar == null)
            calendar = FindFirstObjectByType<CalendarScript>();
    }

    private BuildingAbility GetOperationalAbility()
    {
        EnsureReferences();

        return TryGetOperationalCardIncinerationAbility(out CardIncinerationPowerAbility ability)
            ? ability
            : null;
    }

    private bool TryGetOperationalCardIncinerationAbility(out CardIncinerationPowerAbility _ability)
    {
        _ability = null;

        if (factionManager == null || factionManager.ownedCities == null)
            return false;

        for (int i = 0; i < factionManager.ownedCities.Count; i++)
        {
            CityScript city = factionManager.ownedCities[i];
            if (city == null || city.cityData == null || city.cityData.buildings == null)
                continue;

            for (int j = 0; j < city.cityData.buildings.Count; j++)
            {
                BuildingInstance building = city.cityData.buildings[j];
                if (building == null || !building.IsOperational() || building.data == null)
                    continue;

                if (building.data.ability is CardIncinerationPowerAbility ability)
                {
                    _ability = ability;
                    return true;
                }
            }
        }

        return false;
    }

    private bool TryGetOperationalCardIncinerationAbility(CityScript _city, out CardIncinerationPowerAbility _ability)
    {
        _ability = null;

        if (_city == null || _city.cityData == null || _city.cityData.buildings == null)
            return false;

        if (factionManager == null || _city.cityData.owner != factionManager)
            return false;

        for (int i = 0; i < _city.cityData.buildings.Count; i++)
        {
            BuildingInstance building = _city.cityData.buildings[i];
            if (building == null || !building.IsOperational() || building.data == null)
                continue;

            if (building.data.ability is CardIncinerationPowerAbility ability)
            {
                _ability = ability;
                return true;
            }
        }

        return false;
    }
}
