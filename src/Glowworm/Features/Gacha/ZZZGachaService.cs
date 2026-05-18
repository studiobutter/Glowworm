using Dapper;
using Microsoft.Extensions.Logging;
using MiniExcelLibs;
using Glowworm.Core;
using Glowworm.Core.Gacha;
using Glowworm.Core.Gacha.Genshin;
using Glowworm.Core.Gacha.StarRail;
using Glowworm.Core.Gacha.ZZZ;
using Glowworm.Features.Database;
using Glowworm.Features.Gacha.UIGF;
using Glowworm.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Glowworm.Features.Gacha;

internal class ZZZGachaService : GachaLogService
{


    protected override GameBiz CurrentGameBiz { get; } = GameBiz.nap;

    protected override string GachaTableName { get; } = "ZZZGachaItem";


    public ZZZGachaService(ILogger<ZZZGachaService> logger, ZZZGachaClient client) : base(logger, client)
    {
    }


    protected override List<GachaLogItemEx> GetGachaLogItemsByQueryType(IEnumerable<GachaLogItemEx> items, IGachaType type)
    {
        return type switch
        {
            _ => items.Where(x => x.GachaType == type.Value).ToList(),
        };
    }



    public override List<GachaLogItemEx> GetGachaLogItemEx(long uid)
    {
        using var dapper = DatabaseService.CreateConnection();
        var list = dapper.Query<GachaLogItemEx>("""
            SELECT item.*, info.Icon FROM ZZZGachaItem item LEFT JOIN ZZZGachaInfo info ON item.ItemId=Info.Id WHERE Uid=@uid ORDER BY item.Id;
            """, new { uid }).ToList();
        foreach (var type in QueryGachaTypes)
        {
            var l = GetGachaLogItemsByQueryType(list, type);
            int index = 0;
            int pity = 0;
            bool hasNoUp = GachaNoUp.Dictionary.TryGetValue($"{CurrentGameBiz}{type.Value}", out var noUp);
            foreach (var item in l)
            {
                item.Index = ++index;
                item.Pity = ++pity;
                if (item.RankType == 4)
                {
                    pity = 0;
                    item.HasUpItem = hasNoUp;
                    if (hasNoUp)
                    {
                        bool isUp = true;
                        if (noUp!.Items.TryGetValue(item.ItemId, out GachaNoUpItem? noUpItem))
                        {
                            foreach ((DateTime start, DateTime end) in noUpItem.NoUpTimes)
                            {
                                if (item.Time >= start && item.Time <= end)
                                {
                                    isUp = false;
                                    break;
                                }
                            }
                        }
                        item.IsUp = isUp;
                    }
                }
            }
        }
        return list;
    }


    public override (List<GachaTypeStats> GachaStats, List<GachaLogItemEx> ItemStats) GetGachaTypeStats(long uid)
    {
        var statsList = new List<GachaTypeStats>();
        var groupStats = new List<GachaLogItemEx>();
        using var dapper = DatabaseService.CreateConnection();
        var allItems = GetGachaLogItemEx(uid);
        if (allItems.Count > 0)
        {
            foreach (IGachaType type in QueryGachaTypes)
            {
                var list = GetGachaLogItemsByQueryType(allItems, type);
                if (list.Count == 0)
                {
                    continue;
                }
                var stats = new GachaTypeStats
                {
                    GachaType = type.Value,
                    GachaTypeText = type.ToLocalization(),
                    Count = list.Count,
                    Count_5_Up = list.Count(x => x.RankType == 4 && x.IsUp),
                    Count_5 = list.Count(x => x.RankType == 4),
                    Count_4 = list.Count(x => x.RankType == 3),
                    Count_3 = list.Count(x => x.RankType == 2),
                    StartTime = list.First().Time,
                    EndTime = list.Last().Time
                };
                stats.Ratio_5 = (double)stats.Count_5 / stats.Count;
                stats.Ratio_4 = (double)stats.Count_4 / stats.Count;
                stats.Ratio_3 = (double)stats.Count_3 / stats.Count;
                stats.List_5 = list.Where(x => x.RankType == 4).Reverse().ToList();
                stats.List_4 = list.Where(x => x.RankType == 3).Reverse().ToList();
                stats.Pity_5 = list.Last().Pity;
                if (list.Last().RankType == 4)
                {
                    stats.Pity_5 = 0;
                }
                stats.Average_5 = (double)(stats.Count - stats.Pity_5) / stats.Count_5;
                stats.Pity_4 = list.Count - 1 - list.FindLastIndex(x => x.RankType == 3);

                if (stats.Count_5_Up > 0)
                {
                    int c = stats.Count - stats.Pity_5;
                    stats.Average_5_Up = (double)c / stats.Count_5_Up;
                }

                int pity_4 = 0;
                foreach (var item in list)
                {
                    pity_4++;
                    if (item.RankType == 3)
                    {
                        item.Pity = pity_4;
                        pity_4 = 0;
                    }
                }

                statsList.Add(stats);
                if (CurrentGameBiz == GameBiz.hk4e && type.Value == GenshinGachaType.NoviceWish && stats.Count == 20)
                {
                    continue;
                }
                else if (CurrentGameBiz == GameBiz.hkrpg && type.Value == StarRailGachaType.DepartureWarp && stats.Count == 50)
                {
                    continue;
                }
                else
                {
                    stats.List_5.Insert(0, new GachaLogItemEx
                    {
                        GachaType = type.Value,
                        Name = Lang.GachaStatsCard_Pity,
                        Pity = stats.Pity_5,
                        Time = list.Last().Time,
                        HasUpItem = GachaNoUp.Dictionary.TryGetValue($"{CurrentGameBiz}{type.Value}", out _),
                    });
                    stats.List_4.Insert(0, new GachaLogItemEx
                    {
                        GachaType = type.Value,
                        Name = Lang.GachaStatsCard_Pity,
                        Pity = stats.Pity_4,
                        Time = list.Last().Time,
                        HasUpItem = GachaNoUp.Dictionary.TryGetValue($"{CurrentGameBiz}{type.Value}", out _),
                    });
                }
            }
            groupStats = allItems.GroupBy(x => x.ItemId)
                                 .Select(x => { var item = x.First(); item.ItemCount = x.Count(); return item; })
                                 .OrderByDescending(x => x.RankType)
                                 .ThenByDescending(x => x.ItemCount)
                                 .ThenByDescending(x => x.Time)
                                 .ToList();
        }
        return (statsList, groupStats);
    }




    protected override int InsertGachaLogItems(List<GachaLogItem> items)
    {
        using var dapper = DatabaseService.CreateConnection();
        using var t = dapper.BeginTransaction();
        var affect = dapper.Execute("""
            INSERT OR REPLACE INTO ZZZGachaItem (Uid, Id, Name, Time, ItemId, ItemType, RankType, GachaType, Count, Lang)
            VALUES (@Uid, @Id, @Name, @Time, @ItemId, @ItemType, @RankType, @GachaType, @Count, @Lang);
            """, items, t);
        t.Commit();
        return affect;
    }



    public override async Task ExportGachaLogAsync(long uid, string file, string format)
    {
        await ExportAsJsonAsync(uid, file);
    }



    private async Task ExportAsJsonAsync(long uid, string output)
    {
        using var dapper = DatabaseService.CreateConnection();
        var list = dapper.Query<ZZZGachaItem>($"SELECT * FROM {GachaTableName} WHERE Uid = @uid ORDER BY Id;", new { uid }).ToList();
        var uigfObj = new UIGF4File();
        var archive = new UIGF4GachaArchive<ZZZGachaItem>
        {
            Uid = uid,
            List = list,
            Lang = list.LastOrDefault()?.Lang ?? "",
        };
        uigfObj.napGachaArchives = new List<UIGF4GachaArchive<ZZZGachaItem>> { archive };
        
        using FileStream fs = File.Create(output);
        await JsonSerializer.SerializeAsync(fs, uigfObj, AppConfig.JsonSerializerOptions);
    }



    public override long ImportGachaLog(string file)
    {
        var str = File.ReadAllText(file);
        var obj = JsonSerializer.Deserialize<UIGF3File<ZZZGachaItem>>(str);
        if (obj != null)
        {
            string lang = obj.Info.Lang ?? "";
            long uid = obj.Info.Uid;
            foreach (var item in obj.List)
            {
                if (item.Lang is null)
                {
                    item.Lang = lang;
                }
                if (item.Uid == 0)
                {
                    item.Uid = uid;
                }
            }
            var count = InsertGachaLogItems(obj.List.ToList<GachaLogItem>());
            // ???????? {count} ?
            InAppToast.MainWindow?.Success($"Uid {obj.Info.Uid}", string.Format(Lang.ZZZGachaService_ImportSignalSearchRecordsSuccessfully, count), 5000);
            return obj.Info.Uid;
        }
        return 0;
    }



    public override async Task<string> UpdateGachaInfoAsync(GameBiz gameBiz, string lang, CancellationToken cancellationToken = default)
    {
        var data = await _client.GetZZZGachaInfoAsync(gameBiz, lang, cancellationToken);
        using var dapper = DatabaseService.CreateConnection();
        using var t = dapper.BeginTransaction();
        const string insertSql = """
            INSERT OR REPLACE INTO ZZZGachaInfo (Id, Name, Icon, Rarity, ElementType, Profession)
            VALUES (@Id, @Name, @Icon, @Rarity, @ElementType, @Profession);
            """;
        dapper.Execute(insertSql, data.List, t);
        t.Commit();
        return data.Language;
    }


    public override async Task<(string Language, int Count)> ChangeGachaItemNameAsync(GameBiz gameBiz, string lang, CancellationToken cancellationToken = default)
    {
        lang = await UpdateGachaInfoAsync(gameBiz, lang, cancellationToken);
        using var dapper = DatabaseService.CreateConnection();
        int count = dapper.Execute("""
             INSERT OR REPLACE INTO ZZZGachaItem (Uid, Id, Name, Time, ItemId, ItemType, RankType, GachaType, Count, Lang)
             SELECT item.Uid, item.Id, info.Name, Time, ItemId, ItemType, RankType, GachaType, Count, @Lang
             FROM ZZZGachaItem item INNER JOIN ZZZGachaInfo info ON item.ItemId = info.Id;
             """, new { Lang = lang });
        return (lang, count);
    }




    private class UIGFObj
    {
        public UIGFObj() { }

        public UIGFObj(long uid, List<ZZZGachaItem> list)
        {
            this.info = new UIGFInfo(uid, list);
            this.list = list;
        }

        public UIGFInfo info { get; set; }

        public List<ZZZGachaItem> list { get; set; }
    }


    private class UIGFInfo
    {
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public long uid { get; set; }

        public string lang { get; set; }

        public int region_time_zone { get; set; } = 0;

        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public long export_timestamp { get; set; }

        public string export_app { get; set; } = "Glowworm";

        public string export_app_version { get; set; } = AppConfig.AppVersion ?? "";

        public string uigf_version { get; set; } = "v1.0";

        public UIGFInfo() { }

        public UIGFInfo(long uid, List<ZZZGachaItem> list)
        {
            this.uid = uid;
            lang = list.FirstOrDefault()?.Lang ?? "";
            export_timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
        }
    }


}




