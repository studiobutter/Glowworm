using Dapper;
using Microsoft.Extensions.Logging;
using MiniExcelLibs;
using Glowworm.Core;
using Glowworm.Core.Gacha;
using Glowworm.Core.Gacha.Genshin;
using Glowworm.Core.Gacha.StarRail;
using Glowworm.Features.Database;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Glowworm.Features.Gacha;


internal abstract class GachaLogService
{


    protected readonly ILogger<GachaLogService> _logger;


    protected readonly GachaLogClient _client;


    protected GachaLogService(ILogger<GachaLogService> logger, GachaLogClient client)
    {
        _logger = logger;
        _client = client;
    }



    protected abstract GameBiz CurrentGameBiz { get; }

    protected abstract string GachaTableName { get; }

    protected abstract List<GachaLogItemEx> GetGachaLogItemsByQueryType(IEnumerable<GachaLogItemEx> items, IGachaType type);

    public IReadOnlyCollection<IGachaType> QueryGachaTypes => _client.QueryGachaTypes;



    public static string GetGachaLogText(GameBiz biz)
    {
        return biz.ToGame().Value switch
        {
            GameBiz.hk4e => Lang.GachaLogService_WishRecords,
            GameBiz.hkrpg => Lang.GachaLogService_WarpRecords,
            GameBiz.nap => Lang.GachaLogService_SignalSearchRecords,
            _ => ""
        };
    }



    public virtual List<long> GetUids()
    {
        using var dapper = DatabaseService.CreateConnection();
        return dapper.Query<long>($"SELECT DISTINCT Uid FROM {GachaTableName};").ToList();
    }



    public virtual List<GachaLogItemEx> GetGachaLogItemEx(long uid)
    {
        using var dapper = DatabaseService.CreateConnection();
        var list = dapper.Query<GachaLogItemEx>($"SELECT * FROM {GachaTableName} WHERE Uid = @uid ORDER BY Id;", new { uid }).ToList();
        foreach (IGachaType type in QueryGachaTypes)
        {
            var l = GetGachaLogItemsByQueryType(list, type);
            int index = 0;
            int pity = 0;
            foreach (var item in l)
            {
                item.Index = ++index;
                item.Pity = ++pity;
                if (item.RankType == 5)
                {
                    pity = 0;
                }
            }
        }
        return list;
    }



    public virtual string? GetGachaLogUrlFromWebCache(GameBiz gameBiz, string path)
    {
        return GachaLogClient.GetGachaUrlFromWebCache(gameBiz, path);
    }




    public virtual async Task<long> GetUidFromGachaLogUrl(string url)
    {
        long uid = await _client.GetUidByGachaUrlAsync(url);
        if (uid > 0)
        {
            using var dapper = DatabaseService.CreateConnection();
            dapper.Execute("INSERT OR REPLACE INTO GachaLogUrl (GameBiz, Uid, Url, Time) VALUES (@GameBiz, @Uid, @Url, @Time);", new GachaLogUrl(CurrentGameBiz, uid, url));
        }
        return uid;
    }



    public virtual string? GetGachaLogUrlByUid(long uid)
    {
        using var dapper = DatabaseService.CreateConnection();
        return dapper.QueryFirstOrDefault<string>("SELECT Url FROM GachaLogUrl WHERE Uid = @uid AND GameBiz = @GameBiz LIMIT 1;", new { uid, GameBiz = CurrentGameBiz });
    }



    protected abstract int InsertGachaLogItems(List<GachaLogItem> items);



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

            var internalProgress = new Progress<(IGachaType GachaType, int Page)>((x) => progress?.Report(string.Format(Lang.GachaLogService_GetGachaProgressText, x.GachaType.ToLocalization(), x.Page)));
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
                string backupFolder = AppConfig.BackupFolder ?? Path.Combine(AppConfig.UserDataFolder!, "DatabaseBackup");
                bool isNetworkPath = AppConfig.IsNetworkPath(backupFolder);

                if (isNetworkPath)
                {
                    // Pre-check: probe network reachability off the UI thread to avoid freezing
                    bool accessible = false;
                    try
                    {
                        accessible = await Task.Run(() => Directory.Exists(backupFolder));
                    }
                    catch
                    {
                    }

                    // Persist result so the Settings page can read it without re-probing
                    AppConfig.NetworkDriveAvailableCache = accessible;

                    if (!accessible)
                    {
                        AppConfig.AutoBackupGachaRecord = false;
                        Glowworm.Helpers.InAppToast.MainWindow?.Error("Network Backup Disabled", Lang.NetworkBackup_Disabled);
                        return uid;
                    }
                }

                // Show "Backing up..." toast for the duration of the network write
                var pendingToast = isNetworkPath
                    ? Glowworm.Helpers.InAppToast.MainWindow?.Pending(Lang.NetworkBackup_Pending)
                    : null;

                _ = Task.Run(async () =>
                {
                    try
                    {
                        Directory.CreateDirectory(backupFolder);

                        if (AppConfig.AutoBackupGachaRecordUIGF)
                        {
                            var uigfService = AppConfig.GetService<Glowworm.Features.Gacha.UIGF.UIGFGachaService>();
                            var archives = uigfService.GetLocalGachaArchives().Where(a => a.Uid == uid && a.Game == CurrentGameBiz.ToGame()).ToList();
                            if (archives.Count > 0)
                            {
                                string fileName = $"Glowworm_UIGF_{CurrentGameBiz.ToGame().Value}_{uid}.json";
                                string filePath = Path.Combine(backupFolder, fileName);
                                await uigfService.ExportUIGF4Async(filePath, archives.ToArray());
                                Glowworm.Helpers.InAppToast.MainWindow?.ClosePending(pendingToast);
                                if (isNetworkPath)
                                {
                                    Glowworm.Helpers.InAppToast.MainWindow?.Success(Lang.NetworkBackup_SuccessTitle, string.Format(Lang.NetworkBackup_SuccessMessage, backupFolder));
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
                                Glowworm.Helpers.InAppToast.MainWindow?.Success(Lang.NetworkBackup_SuccessTitle, string.Format(Lang.NetworkBackup_SuccessMessage, backupFolder));
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
                            Glowworm.Helpers.InAppToast.MainWindow?.Error("Network Backup Disabled", Lang.NetworkBackup_Disabled);
                        }
                    }
                });
            }
        }
        return uid;
    }






    public virtual (List<GachaTypeStats> GachaStats, List<GachaLogItemEx> ItemStats) GetGachaTypeStats(long uid)
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
                    Count_5_Up = list.Count(x => x.RankType == 5 && x.IsUp),
                    Count_5 = list.Count(x => x.RankType == 5),
                    Count_4 = list.Count(x => x.RankType == 4),
                    Count_3 = list.Count(x => x.RankType == 3),
                    StartTime = list.First().Time,
                    EndTime = list.Last().Time
                };
                stats.Ratio_5 = (double)stats.Count_5 / stats.Count;
                stats.Ratio_4 = (double)stats.Count_4 / stats.Count;
                stats.Ratio_3 = (double)stats.Count_3 / stats.Count;
                stats.List_5 = list.Where(x => x.RankType == 5).Reverse().ToList();
                stats.List_4 = list.Where(x => x.RankType == 4).Reverse().ToList();
                stats.Pity_5 = list.Last().Pity;
                if (list.Last().RankType == 5)
                {
                    stats.Pity_5 = 0;
                }
                stats.Average_5 = (double)(stats.Count - stats.Pity_5) / stats.Count_5;
                stats.Pity_4 = list.Count - 1 - list.FindLastIndex(x => x.RankType == 4);

                if (stats.Count_5_Up > 0)
                {
                    int c = stats.Count - stats.Pity_5;
                    stats.Average_5_Up = (double)c / stats.Count_5_Up;
                }

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



    public abstract Task ExportGachaLogAsync(long uid, string file, string format);




    public abstract long ImportGachaLog(string file);




    public abstract Task<string> UpdateGachaInfoAsync(GameBiz gameBiz, string lang, CancellationToken cancellationToken = default);



    public abstract Task<(string Language, int Count)> ChangeGachaItemNameAsync(GameBiz gameBiz, string lang, CancellationToken cancellationToken = default);


}




