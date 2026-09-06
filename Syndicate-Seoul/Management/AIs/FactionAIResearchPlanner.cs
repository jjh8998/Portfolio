using System.Collections.Generic;
using UnityEngine;

public class FactionAIResearchPlanner : MonoBehaviour
{
    private const int TopCandidateCount = 3;
    private const float MaxCheapBonus = 5f;
    private const float EqualCostCheapBonus = 2.5f;
    private const float MinSelectionWeight = 1f;
    private const float MaxNoiseScore = 3f;

    [SerializeField] private ResearchDatabaseSO researchDatabase;

    private string lastSelectionSummary = string.Empty;

    public string LastSelectionSummary => lastSelectionSummary;

    public ResearchData PickRandomAvailableResearch(FactionManager _factionManager)
    {
        return PickScoredAvailableResearch(_factionManager, null);
    }

    public ResearchData PickScoredAvailableResearch(FactionManager _factionManager, BigFivePersonality _personality)
    {
        if (_factionManager == null)
            return null;

        if (_factionManager.GetCurrentResearch() != null)
            return null;

        ResearchDatabaseSO database = GetResearchDatabase();
        if (database == null || database.allResearches == null || database.allResearches.Count == 0)
            return null;

        List<ResearchData> availableResearches = new List<ResearchData>();

        for (int i = 0; i < database.allResearches.Count; i++)
        {
            ResearchData research = database.allResearches[i];
            if (research == null || string.IsNullOrWhiteSpace(research.id))
                continue;

            if (research.isPatentResearch
                && PatentResearchManager.Instance.IsPatentClaimedByOtherFaction(research.id, _factionManager))
            {
                continue;
            }

            if (_factionManager.CanStartResearchNow(research.id))
                availableResearches.Add(research);
        }

        if (availableResearches.Count == 0)
            return null;

        FactionAIContext context = FactionAIContextBuilder.Build(_factionManager);
        BigFivePersonality personality = _personality ?? new BigFivePersonality();

        int minCost = int.MaxValue;
        int maxCost = int.MinValue;
        for (int i = 0; i < availableResearches.Count; i++)
        {
            int cost = Mathf.Max(0, availableResearches[i].costRP);
            minCost = Mathf.Min(minCost, cost);
            maxCost = Mathf.Max(maxCost, cost);
        }

        List<ScoredResearchCandidate> scoredCandidates = new List<ScoredResearchCandidate>();
        for (int i = 0; i < availableResearches.Count; i++)
        {
            ResearchData research = availableResearches[i];
            float contextScore = EvaluateContextScore(research, context);
            float personalityScore = EvaluatePersonalityScore(research, personality);
            float patentBonus = EvaluatePatentBonus(research, personality);
            float cheapBonus = EvaluateCheapBonus(research, minCost, maxCost);
            float noise = Random.Range(0f, MaxNoiseScore);
            float finalScore = contextScore + personalityScore + patentBonus + cheapBonus + noise;

            scoredCandidates.Add(new ScoredResearchCandidate
            {
                research = research,
                contextScore = contextScore,
                personalityScore = personalityScore,
                patentBonus = patentBonus,
                cheapBonus = cheapBonus,
                noise = noise,
                finalScore = finalScore
            });
        }

        scoredCandidates.Sort((left, right) => right.finalScore.CompareTo(left.finalScore));

        ScoredResearchCandidate selectedCandidate = PickWeightedTopCandidate(scoredCandidates);
        if (selectedCandidate == null || selectedCandidate.research == null)
            return null;

        lastSelectionSummary =
            $"researchId={selectedCandidate.research.id}, category={selectedCandidate.research.category}, " +
            $"score={selectedCandidate.finalScore:F1}, context={selectedCandidate.contextScore:F1}, " +
            $"personality={selectedCandidate.personalityScore:F1}, patent={selectedCandidate.patentBonus:F1}, " +
            $"cheap={selectedCandidate.cheapBonus:F1}, noise={selectedCandidate.noise:F1}";

        return selectedCandidate.research;
    }

    private ScoredResearchCandidate PickWeightedTopCandidate(List<ScoredResearchCandidate> scoredCandidates)
    {
        if (scoredCandidates == null || scoredCandidates.Count == 0)
            return null;

        if (scoredCandidates.Count == 1)
            return scoredCandidates[0];

        int count = Mathf.Min(TopCandidateCount, scoredCandidates.Count);
        float totalWeight = 0f;
        for (int i = 0; i < count; i++)
            totalWeight += Mathf.Max(MinSelectionWeight, scoredCandidates[i].finalScore);

        float randomValue = Random.Range(0f, totalWeight);
        float accumulatedWeight = 0f;
        for (int i = 0; i < count; i++)
        {
            accumulatedWeight += Mathf.Max(MinSelectionWeight, scoredCandidates[i].finalScore);
            if (randomValue <= accumulatedWeight)
                return scoredCandidates[i];
        }

        return scoredCandidates[count - 1];
    }

    private float EvaluateContextScore(ResearchData research, FactionAIContext context)
    {
        if (research == null || context == null)
            return 0f;

        string category = research.category != null ? research.category.Trim() : string.Empty;
        if (category.Equals("Economy", System.StringComparison.OrdinalIgnoreCase))
            return EvaluateEconomyContextScore(context);

        if (category.Equals("Power", System.StringComparison.OrdinalIgnoreCase))
            return EvaluatePowerContextScore(context);

        if (category.Equals("Research", System.StringComparison.OrdinalIgnoreCase))
            return EvaluateResearchContextScore(context);

        if (category.Equals("Factory", System.StringComparison.OrdinalIgnoreCase))
            return EvaluateFactoryContextScore(context);

        if (category.Equals("Battle", System.StringComparison.OrdinalIgnoreCase))
            return EvaluateBattleContextScore(context);

        if (category.Equals("City", System.StringComparison.OrdinalIgnoreCase))
            return EvaluateCityContextScore(context);

        return 2f;
    }

    private float EvaluateEconomyContextScore(FactionAIContext context)
    {
        float moneyNeedScore = context.money < context.lowMoneyThreshold
            ? Mathf.Clamp01((float)(context.lowMoneyThreshold - context.money) / Mathf.Max(1, context.lowMoneyThreshold)) * 14f
            : 0f;
        float incomeNeedScore = context.totalIncome < context.targetIncome
            ? Mathf.Clamp01((float)(context.targetIncome - context.totalIncome) / Mathf.Max(1, context.targetIncome)) * 10f
            : 0f;

        return moneyNeedScore + incomeNeedScore;
    }

    private float EvaluatePowerContextScore(FactionAIContext context)
    {
        if (context.powerConsumption <= 0)
            return 4f;

        if (context.PowerDeficit > 0)
            return 24f + Mathf.Clamp(context.PowerDeficit, 0, 20);

        float surplusRatio = (float)context.PowerSurplus / Mathf.Max(1, context.powerConsumption);
        return Mathf.Clamp01(1f - surplusRatio) * 12f;
    }

    private float EvaluateResearchContextScore(FactionAIContext context)
    {
        if (context.currentRP >= context.targetRP)
            return 3f;

        return Mathf.Clamp01((float)(context.targetRP - context.currentRP) / Mathf.Max(1, context.targetRP)) * 22f;
    }

    private float EvaluateFactoryContextScore(FactionAIContext context)
    {
        int cityCount = context.ownedCities != null ? context.ownedCities.Count : 0;
        int targetProduction = Mathf.Max(1, cityCount);
        float factoryNeedScore = context.currentProduction < targetProduction
            ? Mathf.Clamp01((float)(targetProduction - context.currentProduction) / targetProduction) * 16f
            : 2f;

        int warReadyItemCount = GetWarReadyItemCount(context.faction);
        float cardNeedScore = warReadyItemCount <= 0
            ? 8f
            : Mathf.Clamp01((float)(6 - warReadyItemCount) / 6f) * 6f;

        return factoryNeedScore + cardNeedScore;
    }

    private float EvaluateBattleContextScore(FactionAIContext context)
    {
        if (context.nearEnemyPower <= 0)
            return 3f;

        if (context.militaryScore <= 0)
            return 24f;

        float enemyRatio = (float)context.nearEnemyPower / Mathf.Max(1, context.militaryScore);
        return Mathf.Clamp(enemyRatio * 12f, 4f, 28f);
    }

    private float EvaluateCityContextScore(FactionAIContext context)
    {
        int cityCount = context.ownedCities != null ? context.ownedCities.Count : 0;
        float cityScore = Mathf.Clamp(cityCount * 2f, 0f, 12f);
        float slotPressureScore = context.emptySlotCount <= 0
            ? 12f
            : Mathf.Clamp01((float)(cityCount - context.emptySlotCount) / Mathf.Max(1, cityCount)) * 8f;

        return cityScore + slotPressureScore;
    }

    private float EvaluatePersonalityScore(ResearchData research, BigFivePersonality personality)
    {
        if (research == null || personality == null)
            return 0f;

        string category = research.category != null ? research.category.Trim() : string.Empty;
        float score = 0f;

        if (category.Equals("Research", System.StringComparison.OrdinalIgnoreCase))
            score += personality.Openness01 * 10f;
        else if (category.Equals("Economy", System.StringComparison.OrdinalIgnoreCase))
            score += personality.Conscientiousness01 * 8f + personality.Neuroticism01 * 4f;
        else if (category.Equals("Power", System.StringComparison.OrdinalIgnoreCase))
            score += personality.Conscientiousness01 * 7f + personality.Neuroticism01 * 6f;
        else if (category.Equals("City", System.StringComparison.OrdinalIgnoreCase))
            score += personality.Conscientiousness01 * 8f;
        else if (category.Equals("Factory", System.StringComparison.OrdinalIgnoreCase))
            score += personality.Extraversion01 * 8f;
        else if (category.Equals("Battle", System.StringComparison.OrdinalIgnoreCase))
            score += personality.Extraversion01 * 7f + (1f - personality.Agreeableness01) * 5f + personality.Neuroticism01 * 5f;

        if (research.isPatentResearch)
            score += personality.Openness01 * 4f + (1f - personality.Agreeableness01) * 3f;

        return score;
    }

    private float EvaluatePatentBonus(ResearchData research, BigFivePersonality personality)
    {
        if (research == null || !research.isPatentResearch || personality == null)
            return 0f;

        return 4f + personality.Openness01 * 6f;
    }

    private float EvaluateCheapBonus(ResearchData research, int minCost, int maxCost)
    {
        if (research == null)
            return 0f;

        if (maxCost <= minCost)
            return EqualCostCheapBonus;

        float normalizedCost = Mathf.InverseLerp(minCost, maxCost, Mathf.Max(0, research.costRP));
        return MaxCheapBonus * (1f - normalizedCost);
    }

    private int GetWarReadyItemCount(FactionManager _faction)
    {
        if (_faction == null)
            return 0;

        int totalCount = 0;

        CardInventory cardInventory = _faction.GetCardInventory();
        if (cardInventory != null)
            totalCount += cardInventory.Count;

        FactionCardPackInventoryScript cardPackInventory = _faction.GetCardPackInventory();
        if (cardPackInventory != null)
            totalCount += cardPackInventory.GetTotalCardPackCount();

        return totalCount;
    }

    private ResearchDatabaseSO GetResearchDatabase()
    {
        if (researchDatabase == null)
            researchDatabase = Resources.Load<ResearchDatabaseSO>("Databases/ResearchDatabase");

        if (researchDatabase == null)
        {
            researchDatabase = ScriptableObject.CreateInstance<ResearchDatabaseSO>();
            researchDatabase.LoadCSV();
        }

        return researchDatabase;
    }

    private class ScoredResearchCandidate
    {
        public ResearchData research;
        public float contextScore;
        public float personalityScore;
        public float patentBonus;
        public float cheapBonus;
        public float noise;
        public float finalScore;
    }
}
