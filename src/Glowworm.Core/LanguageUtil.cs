namespace Glowworm.Core;

public static class LanguageUtil
{

    public static string FilterLanguage(string? lang)
    {
        // zh-cn,zh-tw,en-us,ja-jp,ko-kr,vi-vn
        var low = lang?.ToLower() ?? "";
        if (low.Length < 2)
        {
            low = "..";
        }
        return low switch
        {
            "zh-hk" or "zh-mo" or "zh-tw" => "zh-tw",
            "zh-cn" or "zh-sg" => "zh-cn",
            _ => low[..2] switch
            {
                "ja" => "ja-jp",
                "ko" => "ko-kr",
                "vi" => "vi-vn",
                _ => "en-us",
            }
        };
    }


    public static string FilterAudioLanguage(string? lang)
    {
        // zh-cn,zh-tw,en-us,ja-jp,ko-kr,vi-vn
        var low = lang?.ToLower() ?? "";
        if (low.Length < 2)
        {
            low = "..";
        }
        return low switch
        {
            _ => low[..2] switch
            {
                "zh" => "zh-cn",
                "ja" => "ja-jp",
                "ko" => "ko-kr",
                _ => "en-us",
            }
        };
    }


    public static List<string> GetAllLanguages()
    {
        // zh-cn,zh-tw,en-us,ja-jp,ko-kr,vi-vn
        return new List<string>
        {
            "zh-cn",
            "zh-tw",
            "en-us",
            "ja-jp",
            "ko-kr",
            "vi-vn",
        };
    }




}



