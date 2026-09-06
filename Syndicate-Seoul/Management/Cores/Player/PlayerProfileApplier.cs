using UnityEngine;

public class PlayerProfileApplier : MonoBehaviour
{
    private void Start()
    {
        if (!NewGameProfile.TryConsume(out string companyName, out string portraitId))
            return;

        FactionManager playerFaction = FindPlayerFaction();
        if (playerFaction == null)
        {
            Debug.LogWarning("[PlayerProfileApplier] Player FactionManager not found.");
            return;
        }

        if (!string.IsNullOrWhiteSpace(companyName))
            playerFaction.factionName = companyName;

        if (!string.IsNullOrWhiteSpace(portraitId))
            playerFaction.portraitId = portraitId;
    }

    private FactionManager FindPlayerFaction()
    {
        FactionManager[] sceneFactions = FindObjectsByType<FactionManager>(FindObjectsSortMode.None);
        for (int i = 0; i < sceneFactions.Length; i++)
        {
            FactionManager faction = sceneFactions[i];
            if (faction != null && faction.IsPlayerFaction)
                return faction;
        }

        for (int i = 0; i < sceneFactions.Length; i++)
        {
            FactionManager faction = sceneFactions[i];
            if (faction != null && faction.GetComponent<NationAIController>() == null)
                return faction;
        }

        return null;
    }
}
