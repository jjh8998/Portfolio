using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class CityShareData
{
    [Serializable]
    public class FactionShareEntry
    {
        public FactionManager faction;
        [Range(0, 100)] public int sharePercent;
    }

    [Serializable]
    public class ShareSaveEntry
    {
        public string factionId;
        public string factionName;
        public int sharePercent;
    }

    [Serializable]
    public class ShareSaveData
    {
        public int unassignedSharePercent = 100;
        public List<ShareSaveEntry> shares = new List<ShareSaveEntry>();
    }

    [SerializeField] private List<FactionShareEntry> shares = new List<FactionShareEntry>();
    [SerializeField, Range(0, 100)] private int unassignedSharePercent = 100;

    public IReadOnlyList<FactionShareEntry> Shares
    {
        get
        {
            Sanitize();
            return shares;
        }
    }

    public int UnassignedSharePercent
    {
        get
        {
            Sanitize();
            return unassignedSharePercent;
        }
    }

    public int GetShare(FactionManager _faction)
    {
        if (_faction == null)
            return 0;

        Sanitize();

        for (int i = 0; i < shares.Count; i++)
        {
            if (ReferenceEquals(shares[i].faction, _faction))
                return shares[i].sharePercent;
        }

        return 0;
    }

    public int GetTotalAssignedShare()
    {
        Sanitize();

        int totalAssignedShare = 0;
        for (int i = 0; i < shares.Count; i++)
            totalAssignedShare += shares[i].sharePercent;

        return totalAssignedShare;
    }

    public int GetTotalShare()
    {
        return GetTotalAssignedShare() + UnassignedSharePercent;
    }

    public void ClearAllShares()
    {
        shares.Clear();
        unassignedSharePercent = 100;
    }

    public bool SetShare(FactionManager _faction, int _sharePercent)
    {
        if (_faction == null)
            return false;

        Sanitize();

        int nextSharePercent = Mathf.Clamp(_sharePercent, 0, 100);
        int currentSharePercent = GetShare(_faction);
        int delta = nextSharePercent - currentSharePercent;

        if (delta > unassignedSharePercent)
            return false;

        UpsertFactionShare(_faction, nextSharePercent);
        unassignedSharePercent -= delta;
        Sanitize();
        return true;
    }

    public bool ChangeShare(FactionManager _faction, int _delta)
    {
        if (_faction == null)
            return false;

        int currentSharePercent = GetShare(_faction);
        int nextSharePercent = currentSharePercent + _delta;

        if (nextSharePercent < 0 || nextSharePercent > 100)
            return false;

        return SetShare(_faction, nextSharePercent);
    }

    public FactionManager GetHighestShareHolder()
    {
        Sanitize();

        int highestSharePercent = 0;
        FactionManager highestShareHolder = null;
        bool hasTie = false;

        for (int i = 0; i < shares.Count; i++)
        {
            FactionShareEntry entry = shares[i];
            if (entry == null || entry.faction == null || entry.sharePercent <= 0)
                continue;

            if (entry.sharePercent > highestSharePercent)
            {
                highestSharePercent = entry.sharePercent;
                highestShareHolder = entry.faction;
                hasTie = false;
                continue;
            }

            if (entry.sharePercent == highestSharePercent)
                hasTie = true;
        }

        if (hasTie || highestSharePercent <= 0)
            return null;

        return highestShareHolder;
    }

    public int GetHighestSharePercent()
    {
        Sanitize();

        int highestSharePercent = 0;
        for (int i = 0; i < shares.Count; i++)
        {
            if (shares[i] == null)
                continue;

            highestSharePercent = Mathf.Max(highestSharePercent, shares[i].sharePercent);
        }

        return highestSharePercent;
    }

    public bool HasTieForHighestShare()
    {
        Sanitize();

        int highestSharePercent = GetHighestSharePercent();
        if (highestSharePercent <= 0)
            return false;

        int highestShareHolderCount = 0;
        for (int i = 0; i < shares.Count; i++)
        {
            FactionShareEntry entry = shares[i];
            if (entry == null || entry.faction == null)
                continue;

            if (entry.sharePercent != highestSharePercent)
                continue;

            highestShareHolderCount++;
            if (highestShareHolderCount > 1)
                return true;
        }

        return false;
    }

    public ShareSaveData ExportSaveData()
    {
        Sanitize();

        ShareSaveData result = new ShareSaveData
        {
            unassignedSharePercent = unassignedSharePercent
        };

        for (int i = 0; i < shares.Count; i++)
        {
            FactionShareEntry entry = shares[i];
            if (entry == null || entry.faction == null)
                continue;

            string factionId = entry.faction.GetSaveKey();
            if (string.IsNullOrWhiteSpace(factionId))
            {
                Debug.LogWarning("[CityShareData] Share entry skipped with empty faction save key.");
                continue;
            }

            result.shares.Add(new ShareSaveEntry
            {
                factionId = factionId,
                factionName = entry.faction.factionName,
                sharePercent = entry.sharePercent
            });
        }

        return result;
    }

    public void ImportSaveData(ShareSaveData _saveData, IDictionary<string, FactionManager> _factionsById)
    {
        ClearAllShares();

        if (_saveData == null)
            return;

        unassignedSharePercent = Mathf.Clamp(_saveData.unassignedSharePercent, 0, 100);

        if (_saveData.shares == null)
        {
            Sanitize();
            return;
        }

        for (int i = 0; i < _saveData.shares.Count; i++)
        {
            ShareSaveEntry saveEntry = _saveData.shares[i];
            if (saveEntry == null)
                continue;

            FactionManager faction = null;
            if (!string.IsNullOrWhiteSpace(saveEntry.factionId) && _factionsById != null)
                _factionsById.TryGetValue(saveEntry.factionId, out faction);

            if (faction == null && !string.IsNullOrWhiteSpace(saveEntry.factionName))
                faction = FindFactionByName(saveEntry.factionName, _factionsById);

            if (faction == null)
            {
                string fallbackKey = !string.IsNullOrWhiteSpace(saveEntry.factionId) ? saveEntry.factionId : saveEntry.factionName;
                Debug.LogWarning($"[CityShareData] Faction not found while importing share data: {fallbackKey}");
                continue;
            }

            shares.Add(new FactionShareEntry
            {
                faction = faction,
                sharePercent = Mathf.Clamp(saveEntry.sharePercent, 0, 100)
            });
        }

        Sanitize();
    }

    private FactionManager FindFactionByName(string factionName, IDictionary<string, FactionManager> factionsById)
    {
        if (string.IsNullOrWhiteSpace(factionName) || factionsById == null)
            return null;

        foreach (KeyValuePair<string, FactionManager> pair in factionsById)
        {
            FactionManager faction = pair.Value;
            if (faction == null || string.IsNullOrWhiteSpace(faction.factionName))
                continue;

            if (string.Equals(faction.factionName, factionName, StringComparison.OrdinalIgnoreCase))
                return faction;
        }

        return null;
    }

    private void UpsertFactionShare(FactionManager faction, int sharePercent)
    {
        for (int i = 0; i < shares.Count; i++)
        {
            if (!ReferenceEquals(shares[i].faction, faction))
                continue;

            shares[i].sharePercent = sharePercent;
            return;
        }

        shares.Add(new FactionShareEntry
        {
            faction = faction,
            sharePercent = sharePercent
        });
    }

    private void Sanitize()
    {
        Dictionary<FactionManager, int> mergedShares = new Dictionary<FactionManager, int>();

        for (int i = 0; i < shares.Count; i++)
        {
            FactionShareEntry entry = shares[i];
            if (entry == null || entry.faction == null)
                continue;

            int sharePercent = Mathf.Clamp(entry.sharePercent, 0, 100);
            if (sharePercent <= 0)
                continue;

            if (mergedShares.ContainsKey(entry.faction))
                mergedShares[entry.faction] += sharePercent;
            else
                mergedShares[entry.faction] = sharePercent;
        }

        shares.Clear();

        int remainingShare = 100;
        foreach (KeyValuePair<FactionManager, int> pair in mergedShares)
        {
            if (remainingShare <= 0)
                break;

            int clampedSharePercent = Mathf.Clamp(pair.Value, 0, remainingShare);
            if (clampedSharePercent <= 0)
                continue;

            shares.Add(new FactionShareEntry
            {
                faction = pair.Key,
                sharePercent = clampedSharePercent
            });

            remainingShare -= clampedSharePercent;
        }

        unassignedSharePercent = remainingShare;
    }
}
