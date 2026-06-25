using Dapper;
using Microsoft.Data.Sqlite;
using SharpSevenZip;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace Glowworm.Features.Database;

internal static class DatabaseService
{


    private static string _databasePath;


    private static string _connectionString;


    private static Lock _lock = new();


    static DatabaseService()
    {
        SQLitePCL.raw.SetProvider(new SQLitePCL.SQLite3Provider_e_sqlite3());
        SqlMapper.AddTypeHandler(new DapperSqlMapper.DateTimeOffsetHandler());
        SqlMapper.AddTypeHandler(new DapperSqlMapper.StringListHandler());
        SqlMapper.AddTypeHandler(new DapperSqlMapper.GameBizHandler());
    }




    public static SqliteConnection CreateConnection()
    {
        var con = new SqliteConnection(_connectionString);
        con.Open();
        return con;
    }



    public static void SetDatabase(string folder)
    {
        if (Directory.Exists(folder))
        {
            _databasePath = Path.GetFullPath(Path.Combine(folder, "GlowwormDatabase.db"));
            _connectionString = $"DataSource={_databasePath};";
            InitializeDatabase();
        }
    }



    private static void InitializeDatabase()
    {
        lock (_lock)
        {
            using var con = CreateConnection();
            int version = con.QueryFirstOrDefault<int>("PRAGMA USER_VERSION;");
            if (version == 0)
            {
                con.Execute("PRAGMA JOURNAL_MODE = WAL;");
            }
            foreach (var sql in DatabaseSqls.Skip(version))
            {
                con.Execute(sql);
            }
        }
    }



    public static void BackupDatabase(string file)
    {
        using var backupCon = new SqliteConnection($"DataSource={file}; Pooling=False;");
        backupCon.Open();
        using var con = CreateConnection();
        con.Execute("VACUUM;", commandType: CommandType.Text);
        con.BackupDatabase(backupCon);
    }



    public static void AutoBackupToAppDataLocal()
    {
        try
        {
#if DEBUG
            return;
#endif
#pragma warning disable CS0162 // ??????????
            string folder = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Glowworm\DatabaseBackup");
#pragma warning restore CS0162 // ??????????
            Directory.CreateDirectory(folder);
            string file = Path.Combine(folder, $"GlowwormDatabase_AutoBackup_{DateTime.Now:yyyyMMdd_HHmmss}.db");
            string archive = Path.ChangeExtension(file, ".7z");
            string archive_tmp = archive + "_tmp";
            string[] files = Directory.GetFiles(folder, "GlowwormDatabase_AutoBackup_*.7z");
            if (files.Length == 0)
            {
                BackupDatabase(file);
                new SharpSevenZipCompressor().CompressFiles(archive_tmp, file);
                File.Move(archive_tmp, archive, true);
                File.Delete(file);
            }
            else
            {
                string last = files.OrderByDescending(File.GetLastWriteTime).First();
                if (DateTime.Now - File.GetLastWriteTime(last) > TimeSpan.FromDays(7))
                {
                    BackupDatabase(file);
                    new SharpSevenZipCompressor().CompressFiles(archive_tmp, file);
                    File.Move(archive_tmp, archive, true);
                    File.Delete(file);
                    File.Delete(last);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }




    private class KVT
    {

        public KVT() { }


        public KVT(string key, string value, DateTime time)
        {
            Key = key;
            Value = value;
            Time = time;
        }

        public string Key { get; set; }

        public string Value { get; set; }

        public DateTime Time { get; set; }
    }





    public static T? GetValue<T>(string key, out DateTime time, T? defaultValue = default)
    {
        time = DateTime.MinValue;
        try
        {
            using var con = CreateConnection();
            var kvt = con.QueryFirstOrDefault<KVT>("SELECT * FROM KVT WHERE Key = @key LIMIT 1;", new { key });
            if (kvt != null)
            {
                time = kvt.Time;
                var converter = TypeDescriptor.GetConverter(typeof(T));
                if (converter == null)
                {
                    return defaultValue;
                }
                return (T?)converter.ConvertFromString(kvt.Value);
            }
            else
            {
                return defaultValue;
            }
        }
        catch
        {
            return defaultValue;
        }
    }



    public static bool TryGetValue<T>(string key, out T? result, out DateTime time, T? defaultValue = default)
    {
        result = defaultValue;
        time = DateTime.MinValue;
        try
        {
            using var con = CreateConnection();
            var kvt = con.QueryFirstOrDefault<KVT>("SELECT * FROM KVT WHERE Key = @key LIMIT 1;", new { key });
            if (kvt != null)
            {
                time = kvt.Time;
                var converter = TypeDescriptor.GetConverter(typeof(T));
                if (converter == null)
                {
                    return false;
                }
                result = (T?)converter.ConvertFromString(kvt.Value);
                return true;
            }
            else
            {
                return false;
            }
        }
        catch
        {
            return false;
        }
    }



    public static void SetValue<T>(string key, T value, DateTime? time = null)
    {
        try
        {
            using var con = CreateConnection();
            con.Execute("INSERT OR REPLACE INTO KVT (Key, Value, Time) VALUES (@Key, @Value, @Time);", new KVT(key, value?.ToString() ?? "", time ?? DateTime.Now));

        }
        catch { }
    }





    #region Database Structure


    private static readonly List<string> DatabaseSqls =
    [
        Sql_v1,
        Sql_v2,
        Sql_v3,
        Sql_v4,
        Sql_v5,
        Sql_v6,
        Sql_v7,
        Sql_v8,
        Sql_v9,
        Sql_v10,
        Sql_v11,
        Sql_v12,
        Sql_v13,
        Sql_v14,
        Sql_v15,
        Sql_v16,
        Sql_v17,
        Sql_v18,
        Sql_v19,
        Sql_v20
    ];


    private const string Sql_v1 = """
        BEGIN TRANSACTION;

        CREATE TABLE IF NOT EXISTS KVT
        (
            Key   TEXT NOT NULL PRIMARY KEY,
            Value TEXT NOT NULL,
            Time  TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS GameAccount
        (
            SHA256  TEXT    NOT NULL PRIMARY KEY,
            GameBiz INTEGER NOT NULL,
            Uid     INTEGER NOT NULL,
            Name    TEXT    NOT NULL,
            Value   BLOB    NOT NULL,
            Time    TEXT    NOT NULL
        );
        CREATE INDEX IF NOT EXISTS IX_GameAccount_GameBiz ON GameAccount (GameBiz);

        CREATE TABLE IF NOT EXISTS GachaLogUrl
        (
            GameBiz INTEGER NOT NULL,
            Uid     INTEGER NOT NULL,
            Url     TEXT    NOT NULL,
            Time    TEXT    NOT NULL,
            PRIMARY KEY (GameBiz, Uid)
        );

        CREATE TABLE IF NOT EXISTS GenshinGachaItem
        (
            Uid       INTEGER NOT NULL,
            Id        INTEGER NOT NULL,
            Name      TEXT    NOT NULL,
            Time      TEXT    NOT NULL,
            ItemId    INTEGER NOT NULL,
            ItemType  TEXT    NOT NULL,
            RankType  INTEGER NOT NULL,
            GachaType INTEGER NOT NULL,
            Count     INTEGER NOT NULL,
            Lang      TEXT,
            PRIMARY KEY (Uid, Id)
        );
        CREATE INDEX IF NOT EXISTS IX_GenshinGachaItem_Id ON GenshinGachaItem (Id);
        CREATE INDEX IF NOT EXISTS IX_GenshinGachaItem_RankType ON GenshinGachaItem (RankType);
        CREATE INDEX IF NOT EXISTS IX_GenshinGachaItem_GachaType ON GenshinGachaItem (GachaType);

        CREATE TABLE IF NOT EXISTS StarRailGachaItem
        (
            Uid       INTEGER NOT NULL,
            Id        INTEGER NOT NULL,
            Name      TEXT    NOT NULL,
            Time      TEXT    NOT NULL,
            ItemId    INTEGER NOT NULL,
            ItemType  TEXT    NOT NULL,
            RankType  INTEGER NOT NULL,
            GachaType INTEGER NOT NULL,
            GachaId   INTEGER NOT NULL,
            Count     INTEGER NOT NULL,
            Lang      TEXT,
            PRIMARY KEY (Uid, Id)
        );
        CREATE INDEX IF NOT EXISTS IX_StarRailGachaItem_Id ON StarRailGachaItem (Id);
        CREATE INDEX IF NOT EXISTS IX_StarRailGachaItem_RankType ON StarRailGachaItem (RankType);
        CREATE INDEX IF NOT EXISTS IX_StarRailGachaItem_GachaType ON StarRailGachaItem (GachaType);

        PRAGMA USER_VERSION = 1;
        COMMIT TRANSACTION;
        """;

    private const string Sql_v2 = """
        BEGIN TRANSACTION;

        CREATE TABLE IF NOT EXISTS PlayTimeItem
        (
            TimeStamp INTEGER PRIMARY KEY,
            GameBiz   INTEGER NOT NULL,
            Pid       INTEGER NOT NULL,
            State     INTEGER NOT NULL,
            CursorPos INTEGER NOT NULL,
            Message   TEXT
        );
        CREATE INDEX IF NOT EXISTS IX_PlayTimeItem_GameBiz ON PlayTimeItem(GameBiz);
        CREATE INDEX IF NOT EXISTS IX_PlayTimeItem_Pid ON PlayTimeItem(Pid);
        CREATE INDEX IF NOT EXISTS IX_PlayTimeItem_State ON PlayTimeItem(State);

        PRAGMA USER_VERSION = 2;
        COMMIT TRANSACTION;
        """;

    private const string Sql_v3 = """
        BEGIN TRANSACTION;

        CREATE TABLE IF NOT EXISTS GameAccount_dg_tmp
        (
            SHA256  TEXT    NOT NULL,
            GameBiz INTEGER NOT NULL,
            Uid     INTEGER NOT NULL,
            Name    TEXT    NOT NULL,
            Value   BLOB    NOT NULL,
            Time    TEXT    NOT NULL,
            PRIMARY KEY (SHA256, GameBiz)
        );

        INSERT INTO GameAccount_dg_tmp(SHA256, GameBiz, Uid, Name, Value, Time)
        SELECT SHA256, GameBiz, Uid, Name, Value, Time
        FROM GameAccount;

        DROP TABLE IF EXISTS GameAccount;
        ALTER TABLE GameAccount_dg_tmp RENAME TO GameAccount;
        CREATE INDEX IF NOT EXISTS IX_GameAccount_GameBiz ON GameAccount (GameBiz);

        PRAGMA USER_VERSION = 3;
        COMMIT TRANSACTION;
        """;

    private const string Sql_v4 = """
        BEGIN TRANSACTION;

        CREATE TABLE IF NOT EXISTS Setting
        (
            Key   TEXT NOT NULL PRIMARY KEY,
            Value TEXT
        );

        PRAGMA USER_VERSION = 4;
        COMMIT TRANSACTION;
        """;

    private const string Sql_v5 = """
        BEGIN TRANSACTION;

        CREATE TABLE IF NOT EXISTS GenshinGachaInfo
        (
            Id          INTEGER NOT NULL PRIMARY KEY,
            Name        TEXT,
            Icon        TEXT,
            Element     INTEGER NOT NULL,
            Level       INTEGER NOT NULL,
            CatId       INTEGER NOT NULL,
            WeaponCatId INTEGER NOT NULL
        );
        CREATE INDEX IF NOT EXISTS IX_GenshinGachaInfo_Name ON GenshinGachaInfo (Name);

        CREATE TABLE IF NOT EXISTS StarRailGachaInfo
        (
            ItemId         INTEGER NOT NULL PRIMARY KEY,
            ItemName       TEXT,
            IconUrl        TEXT,
            DamageType     INTEGER NOT NULL,
            Rarity         INTEGER NOT NULL,
            AvatarBaseType INTEGER NOT NULL,
            WikiUrl        TEXT,
            IsSystem       INTEGER NOT NULL
        );
        CREATE INDEX IF NOT EXISTS IX_StarRailGachaInfo_Name ON StarRailGachaInfo (ItemName);

        PRAGMA USER_VERSION = 5;
        COMMIT TRANSACTION;
        """;

    private const string Sql_v6 = """
        BEGIN TRANSACTION;

        CREATE INDEX IF NOT EXISTS IX_GenshinGachaItem_Name ON GenshinGachaItem (Name);
        CREATE INDEX IF NOT EXISTS IX_GenshinGachaItem_ItemId ON GenshinGachaItem (ItemId);
        CREATE INDEX IF NOT EXISTS IX_StarRailGachaItem_Name ON StarRailGachaItem (Name);
        CREATE INDEX IF NOT EXISTS IX_StarRailGachaItem_ItemId ON StarRailGachaItem (ItemId);

        PRAGMA USER_VERSION = 6;
        COMMIT TRANSACTION;
        """;

    private const string Sql_v7 = """
        BEGIN TRANSACTION;

        PRAGMA USER_VERSION = 7;
        COMMIT TRANSACTION;
        """;

    private const string Sql_v8 = """
        BEGIN TRANSACTION;

        PRAGMA USER_VERSION = 8;
        COMMIT TRANSACTION;
        """;

    private const string Sql_v9 = """
        BEGIN TRANSACTION;

        CREATE TABLE IF NOT EXISTS ZZZGachaItem
        (
            Uid       INTEGER NOT NULL,
            Id        INTEGER NOT NULL,
            Name      TEXT    NOT NULL,
            Time      TEXT    NOT NULL,
            ItemId    INTEGER NOT NULL,
            ItemType  TEXT    NOT NULL,
            RankType  INTEGER NOT NULL,
            GachaType INTEGER NOT NULL,
            Count     INTEGER NOT NULL,
            Lang      TEXT,
            PRIMARY KEY (Uid, Id)
        );
        CREATE INDEX IF NOT EXISTS IX_ZZZGachaItem_Id ON ZZZGachaItem (Id);
        CREATE INDEX IF NOT EXISTS IX_ZZZGachaItem_RankType ON ZZZGachaItem (RankType);
        CREATE INDEX IF NOT EXISTS IX_ZZZGachaItem_GachaType ON ZZZGachaItem (GachaType);

        PRAGMA USER_VERSION = 9;
        COMMIT TRANSACTION;
        """;

    private const string Sql_v10 = """
        BEGIN TRANSACTION;

        PRAGMA USER_VERSION = 10;
        COMMIT TRANSACTION;
        """;

    private const string Sql_v11 = """
        BEGIN TRANSACTION;

        PRAGMA USER_VERSION = 11;
        COMMIT TRANSACTION;
        """;

    private const string Sql_v12 = """
        BEGIN TRANSACTION;

        UPDATE PlayTimeItem SET GameBiz = 'hk4e_cn' WHERE GameBiz IN (11, 13);
        UPDATE PlayTimeItem SET GameBiz = 'hk4e_global' WHERE GameBiz = 12;
        UPDATE PlayTimeItem SET GameBiz = 'hk4e_bilibili' WHERE GameBiz = 14;
        UPDATE PlayTimeItem SET GameBiz = 'hkrpg_cn' WHERE GameBiz = 21;
        UPDATE PlayTimeItem SET GameBiz = 'hkrpg_global' WHERE GameBiz = 22;
        UPDATE PlayTimeItem SET GameBiz = 'hkrpg_bilibili' WHERE GameBiz = 24;
        UPDATE PlayTimeItem SET GameBiz = 'nap_cn' WHERE GameBiz = 41;
        UPDATE PlayTimeItem SET GameBiz = 'nap_global' WHERE GameBiz = 42;
        UPDATE PlayTimeItem SET GameBiz = 'nap_bilibili' WHERE GameBiz = 44;

        CREATE TABLE IF NOT EXISTS PlayTimeItem_dg_tmp
        (
            TimeStamp INTEGER PRIMARY KEY,
            GameBiz   TEXT    NOT NULL,
            Pid       INTEGER NOT NULL,
            State     INTEGER NOT NULL,
            CursorPos INTEGER NOT NULL,
            Message   TEXT
        );
        INSERT INTO PlayTimeItem_dg_tmp(TimeStamp, GameBiz, Pid, State, CursorPos, Message) SELECT TimeStamp, GameBiz, Pid, State, CursorPos, Message FROM PlayTimeItem;
        DROP TABLE IF EXISTS PlayTimeItem;
        ALTER TABLE PlayTimeItem_dg_tmp RENAME TO PlayTimeItem;
        CREATE INDEX IF NOT EXISTS IX_PlayTimeItem_GameBiz ON PlayTimeItem(GameBiz);
        CREATE INDEX IF NOT EXISTS IX_PlayTimeItem_Pid ON PlayTimeItem(Pid);
        CREATE INDEX IF NOT EXISTS IX_PlayTimeItem_State ON PlayTimeItem(State);

        DROP TABLE IF EXISTS GachaLogUrl;
        CREATE TABLE IF NOT EXISTS GachaLogUrl
        (
            GameBiz TEXT    NOT NULL,
            Uid     INTEGER NOT NULL,
            Url     TEXT    NOT NULL,
            Time    TEXT    NOT NULL,
            PRIMARY KEY (GameBiz, Uid)
        );

        PRAGMA USER_VERSION = 12;
        COMMIT TRANSACTION;
        """;

    private const string Sql_v13 = """
        BEGIN TRANSACTION;

        DROP TABLE IF EXISTS GameAccount;
        CREATE TABLE IF NOT EXISTS GameAccount
        (
            SHA256  TEXT    NOT NULL,
            GameBiz INTEGER NOT NULL,
            Uid     TEXT    NOT NULL,
            Name    TEXT    NOT NULL,
            Value   BLOB    NOT NULL,
            Time    TEXT    NOT NULL,
            PRIMARY KEY (SHA256, GameBiz)
        );
        CREATE INDEX IF NOT EXISTS IX_GameAccount_GameBiz ON GameAccount (GameBiz);

        CREATE TABLE IF NOT EXISTS ZZZGachaInfo
        (
            Id          INTEGER NOT NULL PRIMARY KEY,
            Name        TEXT,
            Icon        TEXT,
            Rarity      INTEGER NOT NULL,
            ElementType INTEGER NOT NULL,
            Profession  INTEGER NOT NULL
        );
        CREATE INDEX IF NOT EXISTS IX_ZZZGachaInfo_Name ON ZZZGachaInfo (Name);

        PRAGMA USER_VERSION = 13;
        COMMIT TRANSACTION;
        """;

    private const string Sql_v14 = """
        BEGIN TRANSACTION;

        PRAGMA USER_VERSION = 14;
        COMMIT TRANSACTION;
        """;

    private const string Sql_v15 = """
        BEGIN TRANSACTION;

        PRAGMA USER_VERSION = 15;
        COMMIT TRANSACTION;
        """;

    private const string Sql_v16 = """
        BEGIN TRANSACTION;

        CREATE TABLE IF NOT EXISTS GenshinBeyondGachaItem
        (
            Uid         INTEGER,
            Id          INTEGER,
            Region      TEXT,
            OpGachaType INTEGER,
            ScheduleId  INTEGER,
            ItemType    TEXT,
            ItemId      INTEGER,
            ItemName    TEXT,
            RankType    INTEGER,
            IsUp        INTEGER,
            Time        TEXT,
            PRIMARY KEY (Uid, Id)
        );
        CREATE INDEX IF NOT EXISTS IX_GenshinBeyondGachaItem_Id ON GenshinBeyondGachaItem (Id);
        CREATE INDEX IF NOT EXISTS IX_GenshinBeyondGachaItem_OpGachaType ON GenshinBeyondGachaItem (OpGachaType);

        PRAGMA USER_VERSION = 16;
        COMMIT TRANSACTION;
        """;

    private const string Sql_v17 = """
        BEGIN TRANSACTION;

        CREATE TABLE IF NOT EXISTS GenshinBeyondGachaInfo
        (
            Id   INTEGER PRIMARY KEY,
            Name TEXT,
            Rank INTEGER NOT NULL,
            Icon TEXT
        );
        CREATE INDEX IF NOT EXISTS IX_GenshinBeyondGachaInfo_Name ON GenshinBeyondGachaInfo (Name);

        PRAGMA USER_VERSION = 17;
        COMMIT TRANSACTION;
        """;

    private const string Sql_v18 = """
        BEGIN TRANSACTION;

        PRAGMA USER_VERSION = 18;
        COMMIT TRANSACTION;
        """;

    private const string Sql_v19 = """
        BEGIN TRANSACTION;

        DROP TABLE IF EXISTS GameAccount;

        PRAGMA USER_VERSION = 19;
        COMMIT TRANSACTION;
        """;

    private const string Sql_v20 = """
        BEGIN TRANSACTION;

        DROP TABLE IF EXISTS GameRecordUser;
        DROP TABLE IF EXISTS GameRecordRole;
        DROP TABLE IF EXISTS ImaginariumTheaterInfo;
        DROP TABLE IF EXISTS SpiralAbyssInfo;
        DROP TABLE IF EXISTS StygianOnslaughtInfo;
        DROP TABLE IF EXISTS TravelersDiaryAwardItem;
        DROP TABLE IF EXISTS TravelersDiaryMonthData;
        DROP TABLE IF EXISTS ApocalypticShadowInfo;
        DROP TABLE IF EXISTS ForgottenHallInfo;
        DROP TABLE IF EXISTS PureFictionInfo;
        DROP TABLE IF EXISTS SimulatedUniverseRecord;
        DROP TABLE IF EXISTS TrailblazeCalendarMonthData;
        DROP TABLE IF EXISTS TrailblazeCalendarDetailItem;
        DROP TABLE IF EXISTS DeadlyAssaultInfo;
        DROP TABLE IF EXISTS InterKnotReportSummary;
        DROP TABLE IF EXISTS InterKnotReportDetailItem;
        DROP TABLE IF EXISTS ShiyuDefenseInfo;
        DROP TABLE IF EXISTS GenshinSpiralAbyssInfo;
        DROP TABLE IF EXISTS GenshinTravelersDiaryMonthData;
        DROP TABLE IF EXISTS GenshinTravelersDiaryAwardItem;
        DROP TABLE IF EXISTS StarRailForgottenHallInfo;
        DROP TABLE IF EXISTS StarRailSimulatedUniverseRecord;
        DROP TABLE IF EXISTS StarRailTrailblazeCalendarMonthData;
        DROP TABLE IF EXISTS StarRailTrailblazeCalendarDetailItem;
        DROP TABLE IF EXISTS StarRailPureFictionInfo;
        DROP TABLE IF EXISTS StarRailApocalypticShadowInfo;
        DROP TABLE IF EXISTS ZZZInterKnotReportSummary;
        DROP TABLE IF EXISTS ZZZInterKnotReportDetailItem;
        DROP TABLE IF EXISTS ZZZShiyuDefenseInfo;
        DROP TABLE IF EXISTS ZZZDeadlyAssaultInfo;
        DROP TABLE IF EXISTS GenshinStygianOnslaughtInfo;
        DROP TABLE IF EXISTS StarRailChallengePeakData;
        DROP TABLE IF EXISTS GenshinImaginariumTheaterInfo;
        DROP TABLE IF EXISTS GenshinQueryItem;
        DROP TABLE IF EXISTS StarRailQueryItem;
        DROP TABLE IF EXISTS ZZZQueryItem;

        PRAGMA USER_VERSION = 20;
        COMMIT TRANSACTION;
        """;

    #endregion



}



