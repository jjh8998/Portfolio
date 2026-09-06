using UnityEngine;

public class CardPackData
{
    public string id;
    public string name;
    public string theme;
    public string cardPackImage;

    public int tier1Rate;
    public int tier2Rate;
    public int tier3Rate;
    public int tier4Rate;
    public int tier5Rate;

    public Sprite GetCardPackSprite()
    {
        string resourcePath = GetCardPackImageResourcePath();
        return string.IsNullOrWhiteSpace(resourcePath)
            ? null
            : Resources.Load<Sprite>(resourcePath);
    }

    private string GetCardPackImageResourcePath()
    {
        if (!string.IsNullOrWhiteSpace(cardPackImage))
            return NormalizeResourcePath(cardPackImage);

        if (!string.IsNullOrWhiteSpace(theme))
        {
            string lowerTheme = theme.ToLowerInvariant();
            if (lowerTheme.Contains("overclock"))
                return "Images/CardPack/Pack_OverClock";
            if (lowerTheme.Contains("dismantle"))
                return "Images/CardPack/Pack_Dismantle";
            if (lowerTheme.Contains("network"))
                return "Images/CardPack/Pack_Network";
            if (lowerTheme.Contains("biohazard"))
                return "Images/CardPack/Pack_Bio";
            if (lowerTheme.Contains("russian_roulette"))
                return "Images/CardPack/Pack_RussianRoulette";
        }

        return "Images/CardPack/Pack_Base";
    }

    private static string NormalizeResourcePath(string path)
    {
        string normalized = path.Trim().Replace("\\", "/");

        if (normalized.StartsWith("Assets/Resources/"))
            normalized = normalized.Substring("Assets/Resources/".Length);
        else if (normalized.StartsWith("Resources/"))
            normalized = normalized.Substring("Resources/".Length);

        int extensionIndex = normalized.LastIndexOf('.');
        if (extensionIndex > 0)
            normalized = normalized.Substring(0, extensionIndex);

        return normalized;
    }
}
