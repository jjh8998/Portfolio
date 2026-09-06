using System;
using System.Collections.Generic;
using UnityEngine;

public class RelationManager : MonoBehaviour
{
    public const int MinFavor = -100;
    public const int MaxFavor = 100;
    public const int DefaultFavor = 0;

    private readonly Dictionary<string, Dictionary<string, int>> relations = new Dictionary<string, Dictionary<string, int>>();

    [Serializable]
    public class RelationSaveData
    {
        public string fromFactionKey;
        public string toFactionKey;
        public int favor;
    }

    [Serializable]
    public class RelationManagerSaveData
    {
        public List<RelationSaveData> relations = new List<RelationSaveData>();
    }

    private void Awake()
    {
        InitializeRelations();
    }

    public int GetFavor(FactionManager _from, FactionManager _to)
    {
        if (!TryGetFactionKeys(_from, _to, out string fromKey, out string toKey))
            return DefaultFavor;

        EnsureRelation(fromKey, toKey);
        return relations[fromKey][toKey];
    }

    public void SetFavor(FactionManager _from, FactionManager _to, int _favor)
    {
        if (!TryGetFactionKeys(_from, _to, out string fromKey, out string toKey))
            return;

        EnsureRelation(fromKey, toKey);
        relations[fromKey][toKey] = Mathf.Clamp(_favor, MinFavor, MaxFavor);
    }

    public void AddFavor(FactionManager _from, FactionManager _to, int _amount)
    {
        long nextFavor = (long)GetFavor(_from, _to) + _amount;
        if (nextFavor > MaxFavor)
            nextFavor = MaxFavor;
        else if (nextFavor < MinFavor)
            nextFavor = MinFavor;

        SetFavor(_from, _to, (int)nextFavor);
    }

    public bool IsFriendly(FactionManager _from, FactionManager _to)
    {
        return GetFavor(_from, _to) >= 60;
    }

    public bool IsHostile(FactionManager _from, FactionManager _to)
    {
        return GetFavor(_from, _to) <= -60;
    }

    public string GetRelationLabel(FactionManager _from, FactionManager _to)
    {
        if (IsFriendly(_from, _to))
            return "Friendly";

        if (IsHostile(_from, _to))
            return "Hostile";

        return "Neutral";
    }

    public void InitializeRelations()
    {
        FactionManager[] factions = FindObjectsByType<FactionManager>(FindObjectsSortMode.None);

        for (int i = 0; i < factions.Length; i++)
        {
            string fromKey = GetFactionKey(factions[i]);
            if (string.IsNullOrWhiteSpace(fromKey))
                continue;

            for (int j = 0; j < factions.Length; j++)
            {
                string toKey = GetFactionKey(factions[j]);
                if (string.IsNullOrWhiteSpace(toKey) || string.Equals(fromKey, toKey, StringComparison.Ordinal))
                    continue;

                EnsureRelation(fromKey, toKey);
            }
        }
    }

    public void DecayRelationsTowardZero(int _amount)
    {
        int amount = Mathf.Abs(_amount);
        if (amount == 0)
            return;

        foreach (Dictionary<string, int> targets in relations.Values)
        {
            List<string> keys = new List<string>(targets.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                string key = keys[i];
                int favor = targets[key];

                if (favor > 0)
                    targets[key] = Mathf.Max(0, favor - amount);
                else if (favor < 0)
                    targets[key] = Mathf.Min(0, favor + amount);
            }
        }
    }

    public RelationManagerSaveData ExportSaveData()
    {
        RelationManagerSaveData data = new RelationManagerSaveData();

        foreach (KeyValuePair<string, Dictionary<string, int>> fromPair in relations)
        {
            foreach (KeyValuePair<string, int> toPair in fromPair.Value)
            {
                data.relations.Add(new RelationSaveData
                {
                    fromFactionKey = fromPair.Key,
                    toFactionKey = toPair.Key,
                    favor = Mathf.Clamp(toPair.Value, MinFavor, MaxFavor)
                });
            }
        }

        return data;
    }

    public void ImportSaveData(RelationManagerSaveData _data)
    {
        if (_data == null)
            return;

        relations.Clear();

        if (_data.relations != null)
        {
            for (int i = 0; i < _data.relations.Count; i++)
            {
                RelationSaveData entry = _data.relations[i];
                if (entry == null
                    || string.IsNullOrWhiteSpace(entry.fromFactionKey)
                    || string.IsNullOrWhiteSpace(entry.toFactionKey)
                    || string.Equals(entry.fromFactionKey, entry.toFactionKey, StringComparison.Ordinal))
                {
                    continue;
                }

                EnsureRelation(entry.fromFactionKey, entry.toFactionKey);
                relations[entry.fromFactionKey][entry.toFactionKey] = Mathf.Clamp(entry.favor, MinFavor, MaxFavor);
            }
        }

        InitializeRelations();
    }

    private bool TryGetFactionKeys(FactionManager _from, FactionManager _to, out string _fromKey, out string _toKey)
    {
        _fromKey = GetFactionKey(_from);
        _toKey = GetFactionKey(_to);

        return !string.IsNullOrWhiteSpace(_fromKey)
            && !string.IsNullOrWhiteSpace(_toKey)
            && !string.Equals(_fromKey, _toKey, StringComparison.Ordinal);
    }

    private void EnsureRelation(string _fromKey, string _toKey)
    {
        if (!relations.TryGetValue(_fromKey, out Dictionary<string, int> targets))
        {
            targets = new Dictionary<string, int>();
            relations.Add(_fromKey, targets);
        }

        if (!targets.ContainsKey(_toKey))
            targets.Add(_toKey, DefaultFavor);
    }

    private string GetFactionKey(FactionManager _faction)
    {
        if (_faction == null)
            return null;

        if (!string.IsNullOrWhiteSpace(_faction.factionName))
            return _faction.factionName;

        return _faction.gameObject.name;
    }
}
