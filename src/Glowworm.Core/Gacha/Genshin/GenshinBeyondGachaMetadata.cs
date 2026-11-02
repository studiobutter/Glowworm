namespace Glowworm.Core.Gacha.Genshin;

public static class GenshinBeyondGachaMetadata
{
    private const string GlobalUrlTemplate = "https://raw.githubusercontent.com/SnapHutaoRemasteringProject/Snap.Metadata/refs/heads/main/Genshin/{0}/BeyondItem.json";
    private const string ChinaUrlTemplate = "https://raw.gitcode.com/JWJUN233233/Snap.Metadata/raw/main/Genshin/{0}/BeyondItem.json";

    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> LanguageCultures =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["CHT"] = ["cht", "zh-tw", "zh-hk", "zh-mo", "zh-hant"],
            ["CHS"] = ["chs", "zh-cn", "zh-sg", "zh-my", "zh-hans"],
            ["DE"] = ["de"],
            ["EN"] = ["en"],
            ["ES"] = ["es"],
            ["FR"] = ["fr"],
            ["ID"] = ["id"],
            ["IT"] = ["it"],
            ["JP"] = ["jp", "ja"],
            ["KR"] = ["kr", "ko"],
            ["PT"] = ["pt"],
            ["RU"] = ["ru"],
            ["TH"] = ["th"],
            ["TR"] = ["tr"],
            ["VI"] = ["vi"]
        };

    public static string GetLanguageCode(string? language)
    {
        if (string.IsNullOrWhiteSpace(language))
        {
            return "EN";
        }

        var normalizedLanguage = language.Trim().Replace('_', '-');
        if (TryGetLanguageCode(normalizedLanguage, out var languageCode))
        {
            return languageCode;
        }

        var languageParts = normalizedLanguage.Split('-', StringSplitOptions.RemoveEmptyEntries);
        for (var partCount = languageParts.Length - 1; partCount > 0; partCount--)
        {
            var languagePrefix = string.Join("-", languageParts.Take(partCount));
            if (TryGetLanguageCode(languagePrefix, out languageCode))
            {
                return languageCode;
            }
        }

        if (languageParts.Length > 0 && languageParts[0].Equals("zh", StringComparison.OrdinalIgnoreCase))
        {
            return languageParts.Length > 1 &&
                   (languageParts[1].Equals("TW", StringComparison.OrdinalIgnoreCase) ||
                    languageParts[1].Equals("HK", StringComparison.OrdinalIgnoreCase) ||
                    languageParts[1].Equals("MO", StringComparison.OrdinalIgnoreCase) ||
                    languageParts[1].Equals("Hant", StringComparison.OrdinalIgnoreCase))
                ? "CHT"
                : "CHS";
        }

        if (languageParts.Length > 0 && TryGetLanguageCode(languageParts[0], out languageCode))
        {
            return languageCode;
        }

        return "EN";
    }

    public static string GetMetadataUrl(GameBiz gameBiz, string? language)
    {
        var urlTemplate = gameBiz.IsChinaServer() || gameBiz.IsBilibiliServer
            ? ChinaUrlTemplate
            : GlobalUrlTemplate;

        return string.Format(
            System.Globalization.CultureInfo.InvariantCulture,
            urlTemplate,
            GetLanguageCode(language));
    }

    public static string GetGachaApiLanguage(string? language)
    {
        return GetLanguageCode(language) switch
        {
            "CHT" => "zh-tw",
            "CHS" => "zh-cn",
            "DE" => "de-de",
            "EN" => "en-us",
            "ES" => "es-es",
            "FR" => "fr-fr",
            "ID" => "id-id",
            "IT" => "it-it",
            "JP" => "ja-jp",
            "KR" => "ko-kr",
            "PT" => "pt-pt",
            "RU" => "ru-ru",
            "TH" => "th-th",
            "TR" => "tr-tr",
            "VI" => "vi-vn",
            _ => "en-us"
        };
    }

    private static bool TryGetLanguageCode(string language, out string languageCode)
    {
        foreach (var (code, cultures) in LanguageCultures)
        {
            if (code.Equals(language, StringComparison.OrdinalIgnoreCase) ||
                cultures.Contains(language, StringComparer.OrdinalIgnoreCase))
            {
                languageCode = code;
                return true;
            }
        }

        languageCode = string.Empty;
        return false;
    }
}
