using System.Collections.Generic;
using UnityEngine;

public partial class FactionManager
{
    public FactionSaveData ExportSaveData()
    {
        EnsureSubsystemsInitialized();
        InitializeSlots();

        return new FactionSaveData
        {
            factionName = factionName,
            ceoId = ceoId,
            portraitId = portraitId,
            startingCityName = startingCity != null && startingCity.cityData != null ? startingCity.cityData.cityName : string.Empty,
            credit = credit,
            isEliminated = isEliminated,
            tradePowerOffset = tradePowerOffset,
            cards = factionCardInventory.ExportCardInventory(),
            cardPacks = factionCardPackInventory.ExportCardPackInventory(),
            deckSlots = ExportDeckSlots(),
            research = factionResearch.ExportResearchState(),
            activeIncinerationEffects = ExportActiveIncinerationEffects(),
            employees = factionEmployee.ExportSaveData(),
            initialEmployeeUnlocked = initialEmployeeUnlocked,
            ai = ExportAISaveData()
        };
    }

    public void ImportSaveData(FactionSaveData _data, IDictionary<string, CityScript> _citiesByName = null)
    {
        EnsureSubsystemsInitialized();

        if (_data == null)
        {
            Debug.LogWarning("[FactionManager] ImportSaveData received null data.");
            return;
        }

        if (!string.IsNullOrWhiteSpace(_data.factionName))
            factionName = _data.factionName;

        if (!string.IsNullOrWhiteSpace(_data.ceoId))
            ceoId = _data.ceoId;

        if (!string.IsNullOrWhiteSpace(_data.portraitId))
            portraitId = _data.portraitId;

        RestoreStartingCity(_data.startingCityName, _citiesByName);

        credit = Mathf.Max(0, _data.credit);
        isEliminated = !IsPlayerFaction && _data.isEliminated;
        tradePowerOffset = _data.tradePowerOffset;
        factionCardInventory.ImportCardInventory(_data.cards);
        factionCardPackInventory.ImportCardPackInventory(_data.cardPacks);
        ImportDeckSlots(_data.deckSlots);
        factionResearch.ImportResearchState(_data.research);
        ImportActiveIncinerationEffects(_data.activeIncinerationEffects);
        initialEmployeeUnlocked = _data.initialEmployeeUnlocked;
        bool hasImportedEmployees = _data.employees != null && _data.employees.Count > 0;
        factionEmployee.ImportSaveData(_data.employees, _citiesByName);
        ImportAISaveData(_data.ai, _citiesByName);
        DisableAIControllerIfEliminated();
        if (!initialEmployeeUnlocked && HasAnyEmployee())
            initialEmployeeUnlocked = true;

        TryUnlockInitialEmployeeByBuildingCount();

        CreditChanged?.Invoke(credit);
        NotifyPowerChanged();
        if (hasImportedEmployees)
            EmployeeAssignmentChanged?.Invoke();
    }

    private FactionAISaveData ExportAISaveData()
    {
        NationAIController aiController = GetComponent<NationAIController>();
        return aiController != null ? aiController.ExportAISaveData() : null;
    }

    private void ImportAISaveData(FactionAISaveData _data, IDictionary<string, CityScript> _citiesByName)
    {
        NationAIController aiController = GetComponent<NationAIController>();
        if (aiController == null)
            return;

        aiController.ImportAISaveData(_data, _citiesByName);
    }

    private void RestoreStartingCity(string _startingCityName, IDictionary<string, CityScript> _citiesByName)
    {
        if (string.IsNullOrWhiteSpace(_startingCityName))
        {
            Debug.LogWarning($"[FactionManager] Starting city name is empty while importing faction '{GetSaveKey()}'.");
            return;
        }

        string canonicalStartingCityName = CityDisplayNameUtility.ToCanonicalName(_startingCityName);
        CityScript city = null;
        if (_citiesByName != null)
            _citiesByName.TryGetValue(canonicalStartingCityName, out city);

        if (city == null)
        {
            CityScript[] cities = FindObjectsByType<CityScript>(FindObjectsSortMode.None);
            for (int i = 0; i < cities.Length; i++)
            {
                CityScript candidate = cities[i];
                if (candidate == null || candidate.cityData == null)
                    continue;

                string candidateCityName = CityDisplayNameUtility.ToCanonicalName(candidate.cityData.cityName);
                if (!string.Equals(candidateCityName, canonicalStartingCityName, System.StringComparison.OrdinalIgnoreCase))
                    continue;

                city = candidate;
                break;
            }
        }

        if (city == null)
        {
            Debug.LogWarning($"[FactionManager] Starting city not found while importing faction '{GetSaveKey()}': {_startingCityName}");
            return;
        }

        startingCity = city;
    }

    private List<ActiveIncinerationEffectSaveData> ExportActiveIncinerationEffects()
    {
        CardIncinerationPowerService service = FindCardIncinerationPowerService();
        if (service == null)
            return new List<ActiveIncinerationEffectSaveData>();

        return service.ExportActiveEffects();
    }

    private void ImportActiveIncinerationEffects(List<ActiveIncinerationEffectSaveData> _data)
    {
        CardIncinerationPowerService service = FindCardIncinerationPowerService();
        if (service == null)
        {
            if (_data != null && _data.Count > 0)
                Debug.LogWarning("[FactionManager] CardIncinerationPowerService not found while importing active incineration effects.");

            return;
        }

        service.ImportActiveEffects(_data);
    }

    private CardIncinerationPowerService FindCardIncinerationPowerService()
    {
        CardIncinerationPowerService service = GetComponent<CardIncinerationPowerService>();
        if (service != null)
            return service;

        CardIncinerationPowerService[] services = FindObjectsByType<CardIncinerationPowerService>(FindObjectsSortMode.None);
        for (int i = 0; i < services.Length; i++)
        {
            CardIncinerationPowerService candidate = services[i];
            if (candidate != null && candidate.IsOwnedBy(this))
                return candidate;
        }

        return null;
    }

    private List<DeckSlotSaveData> ExportDeckSlots()
    {
        InitializeSlots();

        List<DeckSlotSaveData> result = new List<DeckSlotSaveData>(savedDeckSlots.Length);

        for (int i = 0; i < savedDeckSlots.Length; i++)
        {
            DeckSlot slot = savedDeckSlots[i];
            List<string> cardIds = slot != null && slot.cardIds != null
                ? new List<string>(slot.cardIds)
                : new List<string>();

            result.Add(new DeckSlotSaveData
            {
                slotIndex = i,
                deckName = slot != null && !string.IsNullOrWhiteSpace(slot.deckName) ? slot.deckName : $"Deck {i + 1}",
                cardIds = cardIds
            });
        }

        return result;
    }

    private void ImportDeckSlots(List<DeckSlotSaveData> _data)
    {
        InitializeSlots();

        DeckSlot[] importedSlots = new DeckSlot[5];
        for (int i = 0; i < importedSlots.Length; i++)
        {
            importedSlots[i] = new DeckSlot
            {
                deckName = $"Deck {i + 1}",
                cardIds = new List<string>()
            };
        }

        if (_data != null)
        {
            CardDatabase cardDb = CardDatabase.Instance;

            for (int i = 0; i < _data.Count; i++)
            {
                DeckSlotSaveData slotData = _data[i];
                if (slotData == null)
                    continue;

                if (slotData.slotIndex < 0 || slotData.slotIndex >= importedSlots.Length)
                {
                    Debug.LogWarning($"[FactionManager] Skipped deck slot import with invalid slot index: {slotData.slotIndex}");
                    continue;
                }

                DeckSlot slot = importedSlots[slotData.slotIndex];
                slot.deckName = string.IsNullOrWhiteSpace(slotData.deckName) ? $"Deck {slotData.slotIndex + 1}" : slotData.deckName;
                slot.cardIds.Clear();

                if (slotData.cardIds == null)
                    continue;

                for (int j = 0; j < slotData.cardIds.Count; j++)
                {
                    string cardId = slotData.cardIds[j];
                    if (string.IsNullOrWhiteSpace(cardId))
                    {
                        Debug.LogWarning($"[FactionManager] Skipped empty card id in deck slot {slotData.slotIndex}.");
                        continue;
                    }

                    if (cardDb.GetById(cardId) == null)
                    {
                        Debug.LogWarning($"[FactionManager] Card '{cardId}' not found for deck slot {slotData.slotIndex}.");
                        continue;
                    }

                    slot.cardIds.Add(cardId);
                }
            }
        }

        savedDeckSlots = importedSlots;
        lastSelectedSlot = Mathf.Clamp(lastSelectedSlot, 0, savedDeckSlots.Length - 1);
    }
}
