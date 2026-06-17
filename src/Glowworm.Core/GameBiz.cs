using System.Collections.ObjectModel;

namespace Glowworm.Core;

public record struct GameBiz
{

    private string _value;
    public string Value => _value ?? "";


    public string Game => Value.Contains('_') ? Value.Substring(0, Value.IndexOf('_')) : Value;


    public string Server => Value.Contains('_') ? Value.Substring(Value.IndexOf('_') + 1) : "";



    public GameBiz(string? value)
    {
        _value = value ?? "";
    }


    public const string hk4e = "hk4e";
    public const string hk4e_cn = "hk4e_cn";
    public const string hk4e_global = "hk4e_global";
    public const string hk4e_bilibili = "hk4e_bilibili";
    public const string hk4e_google = "hk4e_google";
    public const string hk4e_epic = "hk4e_epic";

    public const string clgm_cn = "clgm_cn";
    public const string clgm_global = "clgm_global";


    public const string hkrpg = "hkrpg";
    public const string hkrpg_cn = "hkrpg_cn";
    public const string hkrpg_global = "hkrpg_global";
    public const string hkrpg_bilibili = "hkrpg_bilibili";
    public const string hkrpg_epic = "hkrpg_epic";
    public const string hkrpg_cloud = "hkrpg_cloud";
    public const string hkrpg_cloud_cn = "hkrpg_cloud_cn";
    public const string hkrpg_cloud_global = "hkrpg_cloud_global";


    public const string nap = "nap";
    public const string nap_cn = "nap_cn";
    public const string nap_global = "nap_global";
    public const string nap_bilibili = "nap_bilibili";
    public const string nap_epic = "nap_epic";
    public const string nap_steam = "nap_steam";
    public const string nap_cloud = "nap_cloud";
    public const string nap_cloud_cn = "nap_cloud_cn";
    public const string nap_cloud_global = "nap_cloud_global";


    public const string None = "";



    public static ReadOnlyCollection<GameBiz> AllGameBizs { get; private set; } = new List<GameBiz>
    {
        hk4e_cn,
        hk4e_global,
        hk4e_bilibili,
        hk4e_google,
        hk4e_epic,
        clgm_cn,
        clgm_global,
        hkrpg_cn,
        hkrpg_global,
        hkrpg_bilibili,
        hkrpg_epic,
        hkrpg_cloud,
        hkrpg_cloud_cn,
        hkrpg_cloud_global,
        nap_cn,
        nap_global,
        nap_bilibili,
        nap_epic,
        nap_steam,
        nap_cloud,
        nap_cloud_cn,
        nap_cloud_global,
    }.AsReadOnly();




    public static bool TryParse(string? value, out GameBiz gameBiz)
    {
        gameBiz = new(value);
        return gameBiz.IsKnown();
    }



    public override string ToString() => Value;
    public static implicit operator GameBiz(string? value) => new(value);
    public static implicit operator string(GameBiz value) => value.Value;




    public bool IsKnown() => Value switch
    {
        hk4e_cn or hk4e_global or hk4e_bilibili or hk4e_google or hk4e_epic => true,
        clgm_cn or clgm_global => true,
        hkrpg_cn or hkrpg_global or hkrpg_bilibili or hkrpg_epic or hkrpg_cloud or hkrpg_cloud_cn or hkrpg_cloud_global => true,
        nap_cn or nap_global or nap_bilibili or nap_epic or nap_steam or nap_cloud or nap_cloud_cn or nap_cloud_global => true,
        _ => false,
    };


    public bool IsChinaServer() => Server is "cn" || Value is clgm_cn or nap_cloud_cn or hkrpg_cloud_cn;


    public bool IsGlobalServer() => Server is "global" || Value is clgm_global or nap_cloud_global or hkrpg_cloud_global;


    public bool IsBilibili() => Server is "bilibili";

    public bool IsBilibiliServer => Server is "bilibili";

    public bool IsCloudGame() => Value.Contains("cloud") || Value.Contains("clgm");


    public GameBiz ToGame() => Game switch
    {
        "clgm" => hk4e,
        "hkrpg_cloud" => hkrpg,
        _ => Game,
    };


    public string ToGameName() => ToGame().Value switch
    {
        hk4e => CoreLang.Game_GenshinImpact,
        hkrpg => CoreLang.Game_HonkaiStarRail,
        nap => CoreLang.Game_ZZZ,
        _ => "",
    };


    public string ToGameServerName() => Value switch
    {
        clgm_cn or nap_cloud_cn or hkrpg_cloud_cn => CoreLang.GameServer_ChinaCloud,
        clgm_global or nap_cloud_global or hkrpg_cloud_global => CoreLang.GameServer_GlobalCloud,
        _ => Server switch
        {
            "cn" => CoreLang.GameServer_ChinaServer,
            "global" => CoreLang.GameServer_GlobalServer,
            "bilibili" => CoreLang.GameServer_Bilibili,
            "google" => CoreLang.GameServer_GPlay,
            "epic" => CoreLang.GameServer_Epic,
            "cloud" => CoreLang.GameServer_Cloud,
            "steam" => CoreLang.GameServer_Steam,
            _ => "",
        }
    };


    public string GetGameRegistryKey() => Value switch
    {
        hk4e_cn or hk4e_bilibili => GameRegistry.GamePath_hk4e_cn,
        hk4e_global or hk4e_google or hk4e_epic => GameRegistry.GamePath_hk4e_global,
        //clgm_cn => GameRegistry.GamePath_hk4e_cloud,
        hkrpg_cn or hkrpg_bilibili => GameRegistry.GamePath_hkrpg_cn,
        hkrpg_global or hkrpg_epic => GameRegistry.GamePath_hkrpg_global,
        nap_cn or nap_bilibili => GameRegistry.GamePath_nap_cn,
        nap_global or nap_epic or nap_steam => GameRegistry.GamePath_nap_global,
        _ => "HKEY_CURRENT_USER",
    };


}
