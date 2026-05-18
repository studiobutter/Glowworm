namespace Glowworm.Language;

public static class Localization
{



    public static readonly IReadOnlyCollection<(string Title, string LangCode)> LanguageList = new List<(string, string)>
    {
        ("English (en-US)", "en-US"),
        ("日本語 (ja-JP)", "ja-JP"),
        ("한국어 (ko-KR)", "ko-KR"),
        ("Tiếng Việt (vi-VN)", "vi-VN"),
        ("简体中文 (zh-CN)", "zh-CN"),
        ("繁體中文 - 香港 (zh-HK)", "zh-HK"),
        ("繁體中文 - 台灣 (zh-TW)", "zh-TW"),
    }.AsReadOnly();


}



