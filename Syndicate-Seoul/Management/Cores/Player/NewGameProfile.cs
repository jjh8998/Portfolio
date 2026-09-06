public static class NewGameProfile
{
    public static bool HasPending { get; private set; }
    public static string CompanyName { get; private set; }
    public static string PortraitId { get; private set; }

    public static void Set(string companyName, string portraitId)
    {
        CompanyName = companyName != null ? companyName.Trim() : string.Empty;
        PortraitId = portraitId != null ? portraitId.Trim() : string.Empty;
        HasPending = !string.IsNullOrWhiteSpace(CompanyName);
    }

    public static bool TryConsume(out string companyName, out string portraitId)
    {
        companyName = CompanyName;
        portraitId = PortraitId;

        if (!HasPending)
        {
            companyName = string.Empty;
            portraitId = string.Empty;
            return false;
        }

        HasPending = false;
        CompanyName = string.Empty;
        PortraitId = string.Empty;
        return true;
    }
}
