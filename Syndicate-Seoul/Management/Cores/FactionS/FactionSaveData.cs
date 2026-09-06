using System;
using System.Collections.Generic;
using UnityEngine.Serialization;

[Serializable]
public class FactionSaveData
{
    public string factionName;
    public string ceoId;
    public string portraitId;
    public string startingCityName;
    [FormerlySerializedAs("gold")]
    public int credit;
    public bool isEliminated;
    public int tradePowerOffset;
    public bool initialEmployeeUnlocked;
    public List<CardStackSaveData> cards = new List<CardStackSaveData>();
    public List<CardPackStackSaveData> cardPacks = new List<CardPackStackSaveData>();
    public List<DeckSlotSaveData> deckSlots = new List<DeckSlotSaveData>();
    public ResearchStateSaveData research = new ResearchStateSaveData();
    public List<ActiveIncinerationEffectSaveData> activeIncinerationEffects = new List<ActiveIncinerationEffectSaveData>();
    public List<EmployeeSaveData> employees = new List<EmployeeSaveData>();
    public FactionAISaveData ai;
}

[Serializable]
public class CardStackSaveData
{
    public string cardId;
    public int count;
}

[Serializable]
public class CardPackStackSaveData
{
    public string packId;
    public int count;
}

[Serializable]
public class DeckSlotSaveData
{
    public int slotIndex;
    public string deckName;
    public List<string> cardIds = new List<string>();
}

[Serializable]
public class ResearchStateSaveData
{
    public string currentResearchId;
    public int currentResearchProgressRP;
    public int unassignedResearchPoint;
    public List<string> queuedResearchIds = new List<string>();
    public List<string> completedResearchIds = new List<string>();
    public List<string> unlockedBuildingIds = new List<string>();
    public ResearchModifierSet modifiers = new ResearchModifierSet();
}

[Serializable]
public class ActiveIncinerationEffectSaveData
{
    public string cardId;
    public int powerAmount;
    public int remainingMonths;
}

[Serializable]
public class EmployeeSaveData
{
    public string employeeId;
    public string employeeName;
    public string ceoId;
    public string imageId;
    public string iconImageId;
    public EmployeeAbilityType abilityType;
    public string assignedCityName;
    public int assignedMonthCounter;
    public bool isReturning;
    public int returnMonthCounter;
    public bool isLost;
}

[Serializable]
public class EmployeeHireSaveData
{
    public bool isUnlocked;
    public List<string> candidateCeoIds = new List<string>();
    public List<EmployeeHireCandidateSaveData> candidateAbilities = new List<EmployeeHireCandidateSaveData>();
    public int elapsedMonths;
}

[Serializable]
public class EmployeeHireCandidateSaveData
{
    public string ceoId;
    public EmployeeAbilityType abilityType;
}

[Serializable]
public class FactionAISaveData
{
    public int actionBaseDay;
    public string targetCityName;
    public int targetSetMonth = -1;
    public int targetKeepMonthCount;
    public int lastWarMonth = -1;
}
