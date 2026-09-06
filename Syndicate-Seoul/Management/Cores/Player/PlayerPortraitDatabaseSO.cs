using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerPortraitDatabase", menuName = "NewWorld/Player Portrait Database")]
public class PlayerPortraitDatabaseSO : ScriptableObject
{
    private const string ResourcePath = "Databases/PlayerPortraitDatabase";

    [Serializable]
    public class Entry
    {
        public string id;
        public string displayName;
        public Sprite sprite;
    }

    public List<Entry> portraits = new List<Entry>();

    public static PlayerPortraitDatabaseSO Load()
    {
        return Resources.Load<PlayerPortraitDatabaseSO>(ResourcePath);
    }

    public Sprite GetSprite(string id)
    {
        Entry entry = FindEntry(id);
        return entry != null ? entry.sprite : null;
    }

    public string GetDefaultId()
    {
        if (portraits == null)
            return string.Empty;

        for (int i = 0; i < portraits.Count; i++)
        {
            Entry entry = portraits[i];
            if (entry != null && !string.IsNullOrWhiteSpace(entry.id))
                return entry.id;
        }

        return string.Empty;
    }

    private Entry FindEntry(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || portraits == null)
            return null;

        for (int i = 0; i < portraits.Count; i++)
        {
            Entry entry = portraits[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.id))
                continue;

            if (string.Equals(entry.id, id, StringComparison.OrdinalIgnoreCase))
                return entry;
        }

        return null;
    }
}
