using Glowworm.Core.Gacha;
using Glowworm.Core.Gacha.StarRail;
using Glowworm.Core.Gacha.ZZZ;
using Glowworm.Core.Gacha.Genshin;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Glowworm.Features.Gacha.UIGF;

public class UIGF4File
{

    [JsonPropertyName("info")]
    public UIGF4FileInfo Info { get; set; }


    [JsonPropertyName("hk4e")]
    public List<UIGF4GachaArchive<UIGFGenshinGachaItem>>? hk4eGachaArchives { get; set; }


    [JsonPropertyName("hkrpg")]
    public List<UIGF4GachaArchive<StarRailGachaItem>>? hkrpgGachaArchives { get; set; }


    [JsonPropertyName("nap")]
    public List<UIGF4GachaArchive<ZZZGachaItem>>? napGachaArchives { get; set; }

    [JsonPropertyName("hk4e_ugc")]
    public List<UIGF4GachaArchive<GenshinBeyondGachaItem>>? hk4eUgcGachaArchives { get; set; }


    public UIGF4File()
    {
        Info = new();
        hk4eGachaArchives = new();
        hkrpgGachaArchives = new();
        napGachaArchives = new();
        hk4eUgcGachaArchives = new();
    }


}



public class UIGF4FileInfo
{

    /// <summary>
    /// ???,?
    /// </summary>
    [JsonPropertyName("export_timestamp")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public long ExportTimestamp { get; set; }


    [JsonPropertyName("export_app")]
    public string ExportApp { get; set; }


    [JsonPropertyName("export_app_version")]
    public string ExportAppVersion { get; set; }


    [JsonPropertyName("version")]
    public string Version { get; set; } = "v4.2a";


    public UIGF4FileInfo()
    {
        ExportTimestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
        ExportApp = "Glowworm";
        ExportAppVersion = AppConfig.AppVersion;
    }


}



public class UIGF4GachaArchive<T>
{

    [JsonPropertyName("uid")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long Uid { get; set; }


    [JsonPropertyName("timezone")]
    public int Timezone { get; set; }


    [JsonPropertyName("lang")]
    public string Lang { get; set; }


    [JsonPropertyName("list")]
    public List<T> List { get; set; }


}



