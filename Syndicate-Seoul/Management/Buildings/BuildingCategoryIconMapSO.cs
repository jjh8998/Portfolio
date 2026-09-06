using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BuildingCategoryIconMap", menuName = "City/Building Category Icon Map")]
public class BuildingCategoryIconMapSO : ScriptableObject
{
    [Serializable]
    public class IconMapping
    {
        public BuildingCategory category;
        public Sprite sprite;
        public Material material;
        public Color buttonNormalColor = new Color32(0x03, 0x0F, 0x1C, 0xE0);
        public Color buttonSelectedColor = new Color32(0x05, 0x1C, 0x2D, 0xF0);
        public Color buttonDisabledColor = new Color32(0x03, 0x0F, 0x1C, 0x73);
        public Color buttonTextColor = new Color32(0xD5, 0xFB, 0xFF, 0xFF);
    }

    [SerializeField] private List<IconMapping> mappings = new List<IconMapping>();

    public bool TryGetMapping(BuildingCategory category, out IconMapping mapping)
    {
        if (mappings != null)
        {
            for (int i = 0; i < mappings.Count; i++)
            {
                IconMapping candidate = mappings[i];
                if (candidate == null || candidate.category != category)
                    continue;

                mapping = candidate;
                return true;
            }
        }

        mapping = null;
        return false;
    }
}
