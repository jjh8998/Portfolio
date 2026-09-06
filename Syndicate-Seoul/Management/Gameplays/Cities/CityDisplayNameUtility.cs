using System.Collections.Generic;

public static class CityDisplayNameUtility
{
    private static readonly Dictionary<string, string> legacyToCanonicalNames =
        new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
    {
        { "Region_01", "Region_Dobong" },
        { "Region_02", "Region_Nowon" },
        { "Region_04", "Region_Gangbuk" },
        { "Region_05", "Region_Eunpyeong" },
        { "Region_07", "Region_Seongbuk" },
        { "Region_10", "Region_Jungnang" },
        { "Region_11", "Region_Jongno" },
        { "Region_14", "Region_Dongdaemun" },
        { "Region_16", "Region_Seodaemun" },
        { "Region_18", "Region_Jung" },
        { "Region_19", "Region_Gangseo" },
        { "Region_20", "Region_Mapo" },
        { "Region_21", "Region_Seongdong" },
        { "Region_22", "Region_Gangdong" },
        { "Region_23", "Region_Gwangjin" },
        { "Region_27", "Region_Yongsan" },
        { "Region_29", "Region_Yangcheon" },
        { "Region_30", "Region_Yeongdeungpo" },
        { "Region_33", "Region_Songpa" },
        { "Region_34", "Region_Dongjak" },
        { "Region_35", "Region_Gangnam" },
        { "Region_36", "Region_Guro" },
        { "Region_37", "Region_Seocho" },
        { "Region_39", "Region_Gwanak" },
        { "Region_41", "Region_Geumcheon" },
    };

    private static readonly Dictionary<string, string> regionKoreanNames =
        new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
    {
        { "Region_Dobong", "도봉구" },
        { "Region_Nowon", "노원구" },
        { "Region_Gangbuk", "강북구" },
        { "Region_Eunpyeong", "은평구" },
        { "Region_Seongbuk", "성북구" },
        { "Region_Jungnang", "중랑구" },
        { "Region_Jongno", "종로구" },
        { "Region_Dongdaemun", "동대문구" },
        { "Region_Seodaemun", "서대문구" },
        { "Region_Jung", "중구" },
        { "Region_Gangseo", "강서구" },
        { "Region_Mapo", "마포구" },
        { "Region_Seongdong", "성동구" },
        { "Region_Gangdong", "강동구" },
        { "Region_Gwangjin", "광진구" },
        { "Region_Yongsan", "용산구" },
        { "Region_Yangcheon", "양천구" },
        { "Region_Yeongdeungpo", "영등포구" },
        { "Region_Songpa", "송파구" },
        { "Region_Dongjak", "동작구" },
        { "Region_Gangnam", "강남구" },
        { "Region_Guro", "구로구" },
        { "Region_Seocho", "서초구" },
        { "Region_Gwanak", "관악구" },
        { "Region_Geumcheon", "금천구" },
    };

    public static string ToCanonicalName(string rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName))
            return rawName;

        string trimmed = rawName.Trim();
        return legacyToCanonicalNames.TryGetValue(trimmed, out string canonicalName)
            ? canonicalName
            : trimmed;
    }

    public static string ToKoreanDisplayName(string rawName)
    {
        string canonicalName = ToCanonicalName(rawName);
        if (!string.IsNullOrWhiteSpace(canonicalName) &&
            regionKoreanNames.TryGetValue(canonicalName, out string displayName))
        {
            return displayName;
        }

        return canonicalName;
    }
}
