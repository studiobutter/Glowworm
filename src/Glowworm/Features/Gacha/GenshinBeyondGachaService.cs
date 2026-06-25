using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Glowworm.Core;
using Glowworm.Core.Gacha.Genshin;
using Glowworm.Core.Localization;
using Glowworm.Features.Database;
using Glowworm.Features.Gacha.UIGF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;

namespace Glowworm.Features.Gacha;

internal class GenshinBeyondGachaService
{


    private readonly ILogger<GenshinBeyondGachaService> _logger;

    private readonly GenshinBeyondGachaClient _client;


    private const string GachaTableName = "GenshinBeyondGachaItem";


    public GenshinBeyondGachaService(ILogger<GenshinBeyondGachaService> logger, GenshinBeyondGachaClient client)
    {
        _logger = logger;
        _client = client;
    }



    public virtual List<long> GetUids()
    {
        using var dapper = DatabaseService.CreateConnection();
        return dapper.Query<long>($"SELECT DISTINCT Uid FROM {GachaTableName};").ToList();
    }



    public string? GetGachaLogUrlFromWebCache(GameBiz gameBiz, string path)
    {
        return GenshinBeyondGachaClient.GetGachaUrlFromWebCache(gameBiz, path);
    }



    public virtual async Task<long> GetUidFromGachaLogUrl(string url)
    {
        long uid = await _client.GetUidByGachaUrlAsync(url);
        if (uid > 0)
        {
            using var dapper = DatabaseService.CreateConnection();
            dapper.Execute("INSERT OR REPLACE INTO GachaLogUrl (GameBiz, Uid, Url, Time) VALUES (@GameBiz, @Uid, @Url, @Time);", new GachaLogUrl("hk4eugc", uid, url));
        }
        return uid;
    }



    public virtual string? GetGachaLogUrlByUid(long uid)
    {
        using var dapper = DatabaseService.CreateConnection();
        return dapper.QueryFirstOrDefault<string>("SELECT Url FROM GachaLogUrl WHERE Uid = @uid AND GameBiz = @GameBiz LIMIT 1;", new { uid, GameBiz = "hk4eugc" });
    }



    private int InsertGachaLogItems(List<GenshinBeyondGachaItem> items)
    {
        using var dapper = DatabaseService.CreateConnection();
        using var t = dapper.BeginTransaction();
        int count = dapper.Execute("""
            INSERT OR REPLACE INTO GenshinBeyondGachaItem(Uid, Id, Region, OpGachaType, ScheduleId, ItemType, ItemId, ItemName, RankType, IsUp, Time)
            VALUES (@Uid, @Id, @Region, @OpGachaType, @ScheduleId, @ItemType, @ItemId, @ItemName, @RankType, @IsUp, @Time);
            """, items, t);
        t.Commit();
        return count;
    }



    public virtual async Task<long> GetGachaLogAsync(string url, bool all, string? lang = null, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        using var dapper = DatabaseService.CreateConnection();
        // Getting UID
        progress?.Report(Lang.GachaLogService_GettingUid);
        var uid = await _client.GetUidByGachaUrlAsync(url);
        if (uid == 0)
        {
            // This account has no gacha records in the last 6 months.
            progress?.Report(Lang.GachaLogService_ThisAccountHasNoGachaRecordsInTheLast6Months);
        }
        else
        {
            long endId = 0;
            if (!all)
            {
                endId = dapper.QueryFirstOrDefault<long>($"SELECT Id FROM {GachaTableName} WHERE Uid = @Uid ORDER BY Id DESC LIMIT 1;", new { Uid = uid });
                _logger.LogInformation($"Last gacha log id of uid {uid} is {endId}");
            }

            var internalProgress = new Progress<(int GachaType, int Page)>((x) => progress?.Report(string.Format(Lang.GachaLogService_GetGachaProgressText, x.GachaType == 1000 ? CoreLang.GachaType_StandardOde : CoreLang.GachaType_EventOde, x.Page)));
            var list = (await _client.GetGachaLogAsync(url, endId, lang, internalProgress, cancellationToken)).ToList();
            if (cancellationToken.IsCancellationRequested)
            {
                throw new TaskCanceledException();
            }
            var oldCount = dapper.QueryFirstOrDefault<int>($"SELECT COUNT(*) FROM {GachaTableName} WHERE Uid = @Uid;", new { Uid = uid });
            InsertGachaLogItems(list);
            var newCount = dapper.QueryFirstOrDefault<int>($"SELECT COUNT(*) FROM {GachaTableName} WHERE Uid = @Uid;", new { Uid = uid });
            // Got {0} record(s), added {1} record(s).
            progress?.Report(string.Format(Lang.GachaLogService_GetGachaResult, list.Count, newCount - oldCount));

            if (AppConfig.AutoBackupGachaRecord)
            {
                _ = Task.Run(async () =>
                {
                    string backupFolder = AppConfig.BackupFolder ?? Path.Combine(AppConfig.UserDataFolder!, "DatabaseBackup");
                    bool isNetworkPath = AppConfig.IsNetworkPath(backupFolder);

                    // Pre-check: probe network reachability to avoid silent failures
                    if (isNetworkPath)
                    {
                        bool accessible = false;
                        try
                        {
                            accessible = Directory.Exists(backupFolder);
                        }
                        catch { }

                        // Persist result so the Settings page can read it without re-probing
                        AppConfig.NetworkDriveAvailableCache = accessible;

                        if (!accessible)
                        {
                            AppConfig.AutoBackupGachaRecord = false;
                            Glowworm.Helpers.InAppToast.MainWindow?.Error("Network Backup Disabled", Glowworm.Language.Lang.NetworkBackup_Disabled);
                            return;
                        }
                    }

                    // Show "Backing up..." toast for the duration of the network write
                    var pendingToast = isNetworkPath
                        ? Glowworm.Helpers.InAppToast.MainWindow?.Pending(Glowworm.Language.Lang.NetworkBackup_Pending)
                        : null;

                    try
                    {
                        Directory.CreateDirectory(backupFolder);

                        if (AppConfig.AutoBackupGachaRecordUIGF)
                        {
                            var uigfService = AppConfig.GetService<Glowworm.Features.Gacha.UIGF.UIGFGachaService>();
                            var archives = uigfService.GetLocalGachaArchives().Where(a => a.Uid == uid && a.Game == GameBiz.hk4e).ToList();
                            if (archives.Count > 0)
                            {
                                string fileName = $"Glowworm_UIGF_{GameBiz.hk4e}_{uid}.json";
                                string filePath = Path.Combine(backupFolder, fileName);
                                await uigfService.ExportUIGF4Async(filePath, archives.ToArray());
                                Glowworm.Helpers.InAppToast.MainWindow?.ClosePending(pendingToast);
                                if (isNetworkPath)
                                {
                                    Glowworm.Helpers.InAppToast.MainWindow?.Success(Glowworm.Language.Lang.NetworkBackup_SuccessTitle, string.Format(Glowworm.Language.Lang.NetworkBackup_SuccessMessage, backupFolder));
                                }
                            }
                            else
                            {
                                Glowworm.Helpers.InAppToast.MainWindow?.ClosePending(pendingToast);
                            }
                        }
                        else
                        {
                            string dbFile = Path.Combine(backupFolder, $"GlowwormDatabase_AutoBackup_{DateTime.Now:yyyyMMdd_HHmmss}.db");
                            string archive = Path.ChangeExtension(dbFile, ".7z");
                            DatabaseService.BackupDatabase(dbFile);
                            new SharpSevenZip.SharpSevenZipCompressor().CompressFiles(archive, dbFile);
                            System.IO.File.Delete(dbFile);
                            Glowworm.Helpers.InAppToast.MainWindow?.ClosePending(pendingToast);
                            if (isNetworkPath)
                            {
                                Glowworm.Helpers.InAppToast.MainWindow?.Success(Glowworm.Language.Lang.NetworkBackup_SuccessTitle, string.Format(Glowworm.Language.Lang.NetworkBackup_SuccessMessage, backupFolder));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Glowworm.Helpers.InAppToast.MainWindow?.ClosePending(pendingToast);
                        _logger.LogError(ex, "Auto Backup Gacha Record failed.");
                        if (isNetworkPath)
                        {
                            AppConfig.AutoBackupGachaRecord = false;
                            AppConfig.NetworkDriveAvailableCache = false;
                            Glowworm.Helpers.InAppToast.MainWindow?.Error("Network Backup Disabled", Glowworm.Language.Lang.NetworkBackup_Disabled);
                        }
                    }
                });
            }
        }
        return uid;
    }



    public GenshinBeyondGachaTypeStats? GetGachaTypeStatsType1000(long uid)
    {
        using var dapper = DatabaseService.CreateConnection();
        var list = dapper.Query<GenshinBeyondGachaItemEx>("""
            SELECT item.*, info.Icon FROM GenshinBeyondGachaItem item LEFT JOIN GenshinBeyondGachaInfo info
            ON item.ItemId = info.Id WHERE Uid = @uid AND OpGachaType = 1000 ORDER BY item.Id;
            """, new { uid }).ToList();
        if (list.Count == 0)
        {
            return null;
        }

        int index = 0;
        int pity = 0;
        foreach (var item in list)
        {
            item.Index = ++index;
            item.Pity = ++pity;
            if (item.RankType == 4)
            {
                pity = 0;
            }
        }

        var stats = new GenshinBeyondGachaTypeStats
        {
            GachaType = 1000,
            GachaTypeText = CoreLang.GachaType_StandardOde,
            Count = list.Count,
            Count_5 = list.Count(x => x.RankType == 5),
            Count_4 = list.Count(x => x.RankType == 4),
            Count_3 = list.Count(x => x.RankType == 3),
            Count_2 = list.Count(x => x.RankType == 2),
            StartTime = list.First().Time,
            EndTime = list.Last().Time,
        };
        stats.Ratio_5 = (double)stats.Count_5 / stats.Count;
        stats.Ratio_4 = (double)stats.Count_4 / stats.Count;
        stats.Ratio_3 = (double)stats.Count_3 / stats.Count;
        stats.Ratio_2 = (double)stats.Count_2 / stats.Count;
        stats.List_5 = list.Where(x => x.RankType == 5).Reverse().ToList();
        stats.List_4 = list.Where(x => x.RankType == 4).Reverse().ToList();
        stats.List_3 = list.Where(x => x.RankType == 3).Reverse().ToList();

        stats.Pity_4 = list.Last().Pity;
        if (list.Last().RankType == 4)
        {
            stats.Pity_4 = 0;
        }
        stats.Average_4 = (double)(stats.Count - stats.Pity_4) / stats.Count_4;
        stats.Pity_3 = list.Count - 1 - list.FindLastIndex(x => x.RankType == 3);
        int pity_3 = 0;
        foreach (var item in list)
        {
            pity_3++;
            if (item.RankType == 3)
            {
                item.Pity = pity_3;
                pity_3 = 0;
            }
        }

        stats.List_4.Insert(0, new GenshinBeyondGachaItemEx
        {
            OpGachaType = 1000,
            ItemName = "",
            Pity = stats.Pity_4,
            Time = list.Last().Time,
        });
        stats.List_3.Insert(0, new GenshinBeyondGachaItemEx
        {
            OpGachaType = 1000,
            ItemName = "",
            Pity = stats.Pity_3,
            Time = list.Last().Time,
        });

        return stats;
    }


    public GenshinBeyondGachaTypeStats? GetGachaTypeStatsType2000(long uid)
    {
        using var dapper = DatabaseService.CreateConnection();
        var list = dapper.Query<GenshinBeyondGachaItemEx>("""
            SELECT item.*, info.Icon FROM GenshinBeyondGachaItem item LEFT JOIN GenshinBeyondGachaInfo info
            ON item.ItemId = info.Id WHERE Uid = @uid AND OpGachaType != 1000 ORDER BY item.Id;
            """, new { uid }).ToList();
        if (list.Count == 0)
        {
            return null;
        }

        int index = 0;
        int pity = 0;
        foreach (var item in list)
        {
            item.Index = ++index;
            item.Pity = ++pity;
            if (item.RankType == 5)
            {
                pity = 0;
            }
        }

        var stats = new GenshinBeyondGachaTypeStats
        {
            GachaType = 2000,
            GachaTypeText = CoreLang.GachaType_EventOde,
            Count = list.Count,
            Count_5 = list.Count(x => x.RankType == 5),
            Count_4 = list.Count(x => x.RankType == 4),
            Count_3 = list.Count(x => x.RankType == 3),
            Count_2 = list.Count(x => x.RankType == 2),
            StartTime = list.First().Time,
            EndTime = list.Last().Time,
        };
        stats.Ratio_5 = (double)stats.Count_5 / stats.Count;
        stats.Ratio_4 = (double)stats.Count_4 / stats.Count;
        stats.Ratio_3 = (double)stats.Count_3 / stats.Count;
        stats.Ratio_2 = (double)stats.Count_2 / stats.Count;
        stats.List_5 = list.Where(x => x.RankType == 5).Reverse().ToList();
        stats.List_4 = list.Where(x => x.RankType == 4).Reverse().ToList();
        stats.List_3 = list.Where(x => x.RankType == 3).Reverse().ToList();

        stats.Pity_5 = list.Last().Pity;
        if (list.Last().RankType == 5)
        {
            stats.Pity_5 = 0;
        }
        stats.Average_5 = (double)(stats.Count - stats.Pity_5) / stats.Count_5;
        stats.Pity_4 = list.Count - 1 - list.FindLastIndex(x => x.RankType == 4);
        int pity_4 = 0;
        foreach (var item in list)
        {
            pity_4++;
            if (item.RankType == 4)
            {
                item.Pity = pity_4;
                pity_4 = 0;
            }
        }

        stats.List_5.Insert(0, new GenshinBeyondGachaItemEx
        {
            OpGachaType = 2000,
            ItemName = "",
            Pity = stats.Pity_5,
            Time = list.Last().Time,
        });
        stats.List_4.Insert(0, new GenshinBeyondGachaItemEx
        {
            OpGachaType = 2000,
            ItemName = "",
            Pity = stats.Pity_4,
            Time = list.Last().Time,
        });

        return stats;
    }


    public List<GenshinBeyondGachaItemEx>? GetGachaItemStats(long uid)
    {
        using var dapper = DatabaseService.CreateConnection();
        var list = dapper.Query<GenshinBeyondGachaItemEx>("""
            SELECT item.*, info.Icon FROM GenshinBeyondGachaItem item LEFT JOIN GenshinBeyondGachaInfo info
            ON item.ItemId = info.Id WHERE Uid = @uid ORDER BY item.Id;
            """, new { uid }).ToList();
        if (list.Count == 0)
        {
            return null;
        }
        return list.GroupBy(x => x.ItemId)
                   .Select(x => { var item = x.First(); item.Count = x.Count(); return item; })
                   .OrderByDescending(x => x.RankType)
                   .ThenByDescending(x => x.Count)
                   .ThenByDescending(x => x.Time)
                   .ToList();
    }


    public virtual int DeleteUid(long uid)
    {
        using var dapper = DatabaseService.CreateConnection();
        return dapper.Execute($"DELETE FROM {GachaTableName} WHERE Uid = @uid;", new { uid });
    }



    public virtual int DeleteGachaLogByTime(long uid, DateTime begin, DateTime end)
    {
        using var dapper = DatabaseService.CreateConnection();
        return dapper.Execute($"DELETE FROM {GachaTableName} WHERE Uid = @uid AND Time >= @begin AND Time <= @end;", new { uid, begin, end });
    }



    public async Task UpdateGachaInfoAsync(string? lang = null, CancellationToken cancellationToken = default)
    {
        var data = await _client.GetGenshinBeyondGachaInfoAsync(lang, cancellationToken);
        using var dapper = DatabaseService.CreateConnection();
        using var t = dapper.BeginTransaction();
        const string insertSql = """INSERT OR REPLACE INTO GenshinBeyondGachaInfo (Id, Name, Rank, Icon) VALUES (@Id, @Name, @Rank, @Icon);""";
        dapper.Execute(insertSql, data, t);
        t.Commit();
    }



    public virtual async Task<(string Language, int Count)> ChangeGachaItemNameAsync(string lang, CancellationToken cancellationToken = default)
    {
        await UpdateGachaInfoAsync(lang, cancellationToken);
        using var dapper = DatabaseService.CreateConnection();
        int count = dapper.Execute("""
            INSERT OR REPLACE INTO GenshinBeyondGachaItem(Uid, Id, Region, OpGachaType, ScheduleId, ItemType, ItemId, ItemName, RankType, IsUp, Time)
            SELECT item.Uid, item.Id, Region, OpGachaType, ScheduleId, ItemType, ItemId, info.Name, item.RankType, IsUp, Time
            FROM GenshinBeyondGachaItem item INNER JOIN GenshinBeyondGachaInfo info ON item.ItemId = info.Id;
            """);
        return (lang, count);
    }



    public virtual async Task ExportGachaLogAsync(long uid, string output, string format)
    {
        if (format is "uigf4.2")
        {
            await ExportAsUIGF4Async(uid, output);
        }
    }



    private async Task ExportAsUIGF4Async(long uid, string output)
    {
        using var dapper = DatabaseService.CreateConnection();
        var list = dapper.Query<UIGFGenshinGachaItem>($"""
            SELECT Uid, item.Id, OpGachaType AS GachaType, ItemId, ItemName AS Name, RankType, Time, 1 AS Count, Region AS Lang
            FROM {GachaTableName} item WHERE Uid = @uid ORDER BY item.Id;
            """, new { uid }).ToList();
        foreach (var item in list)
        {
            item.UIGFGachaType = item.GachaType switch
            {
                2000 => 301,
                _ => item.GachaType,
            };
        }
        var uigfObj = new UIGF4File();
        var archive = new UIGF4GachaArchive<UIGFGenshinGachaItem>
        {
            Uid = uid,
            List = list,
            Lang = list.LastOrDefault()?.Lang ?? "",
        };
        archive.Timezone = uid.ToString()[0] switch
        {
            '6' => -5,
            '7' => 1,
            _ => 8,
        };
        uigfObj.hk4eGachaArchives = new List<UIGF4GachaArchive<UIGFGenshinGachaItem>> { archive };

        using FileStream fs = File.Create(output);
        await System.Text.Json.JsonSerializer.SerializeAsync(fs, uigfObj, AppConfig.JsonSerializerOptions);
    }


}


public partial class GenshinBeyondGachaItemEx : GenshinBeyondGachaItem
{
    /// <summary>
    /// ??????????
    /// </summary>
    public int Index { get; set; }

    public int Pity { get; set; }

    public string Icon { get; set; }

    public int Count { get; set; }

}



public class GenshinBeyondGachaTypeStats
{

    public int GachaType { get; set; }

    public string GachaTypeText { get; set; }

    public int Count { get; set; }

    public int Pity_5 { get; set; }

    public int Pity_4 { get; set; }

    public int Pity_3 { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public int Count_5 { get; set; }

    public int Count_4 { get; set; }

    public int Count_3 { get; set; }

    public int Count_2 { get; set; }

    public double Ratio_5 { get; set; }

    public double Ratio_4 { get; set; }

    public double Ratio_3 { get; set; }

    public double Ratio_2 { get; set; }

    public double Average_5 { get; set; }

    public double Average_4 { get; set; }

    public List<GenshinBeyondGachaItemEx> List_5 { get; set; }

    public List<GenshinBeyondGachaItemEx> List_4 { get; set; }

    public List<GenshinBeyondGachaItemEx> List_3 { get; set; }

}


public partial class GenshinBeyondGachaPityProgressBackgroundBrushConverter : IValueConverter
{
    private static Color Red = Color.FromArgb(0xFF, 0xC8, 0x3C, 0x23);
    private static Color Green = Color.FromArgb(0xFF, 0x00, 0xE0, 0x79);

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is GenshinBeyondGachaItemEx item)
        {
            int pity = item.Pity;
            var brush = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 0), Opacity = 0.4 };
            int point = 64;
            double guarantee = 70;
            double offset = pity / guarantee;
            if (pity < point)
            {
                brush.GradientStops.Add(new GradientStop { Color = Green, Offset = 0 });
                brush.GradientStops.Add(new GradientStop { Color = Green, Offset = offset });
                brush.GradientStops.Add(new GradientStop { Color = Colors.Transparent, Offset = offset });
            }
            else
            {
                brush.GradientStops.Add(new GradientStop { Color = Red, Offset = 0 });
                brush.GradientStops.Add(new GradientStop { Color = Red, Offset = offset });
                brush.GradientStops.Add(new GradientStop { Color = Colors.Transparent, Offset = offset });
            }
            return brush;
        }
        return null!;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}



