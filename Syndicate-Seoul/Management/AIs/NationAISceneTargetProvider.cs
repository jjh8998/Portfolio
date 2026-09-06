using System.Collections.Generic;
using UnityEngine;

public interface INationAITargetProvider
{
    List<FactionManager> GetEnemyFactions(FactionManager _selfFaction);
}

public class NationAISceneTargetProvider : INationAITargetProvider
{
    public List<FactionManager> GetEnemyFactions(FactionManager _selfFaction)
    {
        List<FactionManager> result = new List<FactionManager>();
        FactionManager[] allFactions = Object.FindObjectsByType<FactionManager>(FindObjectsSortMode.None);

        for (int i = 0; i < allFactions.Length; i++)
        {
            FactionManager faction = allFactions[i];
            if (faction == null)
                continue;

            if (ReferenceEquals(faction, _selfFaction))
                continue;

            result.Add(faction);
        }

        return result;
    }
}
