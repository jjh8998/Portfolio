using System;
using System.Collections.Generic;

[Serializable]
public class ResearchProgressEntry
{
    public string researchId;
    public int currentRP;
}

[Serializable]
public class FactionResearchState
{
    public List<string> completedResearchIds = new List<string>();
    public List<string> queuedResearchIds = new List<string>();
    public string currentResearchId;
    public int currentResearchProgressRP;
    public int rpStock;
    public int unassignedResearchPoint;
    public List<ResearchProgressEntry> researchProgressEntries = new List<ResearchProgressEntry>();
    public List<string> unlockedBuildingIds = new List<string>();
    // Research effects are not applied yet. This only stores future-ready state.
    public ResearchModifierSet modifiers = new ResearchModifierSet();

    public void EnsureInitialized()
    {
        if (completedResearchIds == null)
            completedResearchIds = new List<string>();

        if (queuedResearchIds == null)
            queuedResearchIds = new List<string>();

        if (researchProgressEntries == null)
            researchProgressEntries = new List<ResearchProgressEntry>();

        if (unlockedBuildingIds == null)
            unlockedBuildingIds = new List<string>();

        if (modifiers == null)
            modifiers = new ResearchModifierSet();
    }
}
