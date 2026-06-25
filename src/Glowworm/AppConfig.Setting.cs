using Dapper;
using Glowworm.Core;
using Glowworm.Features.Database;
using Glowworm.Features.ViewHost;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Glowworm;

public static partial class AppConfig
{


    #region Static Setting

    /// <summary>
    /// 0: GitHub, 1: Cloudflare
    /// </summary>
    public static int UpdateSource
    {
        get => GetValue(0);
        set => SetValue(value);
    }

    public static bool EnablePreviewRelease
    {
        get => GetValue<bool>();
        set => SetValue(value);
    }

    public static string? IgnoreVersion
    {
        get => GetValue<string>();
        set => SetValue(value);
    }


    public static bool IgnoreRunningGame
    {
        get => GetValue<bool>();
        set => SetValue(value);
    }

    public static bool ShowNoviceGacha
    {
        get => GetValue<bool>();
        set => SetValue(value);
    }

    public static bool ShowChronicledWish
    {
        get => GetValue(true);
        set => SetValue(value);
    }

    public static bool HideGenshinCloudChina
    {
        get => GetValue(!GameRegistryHelper.IsCloudGameInstalled(GameBiz.clgm_cn));
        set => SetValue(value);
    }

    public static bool HideStarRailCloudChina
    {
        get => GetValue(true); // Default to hiding Web Cloud
        set => SetValue(value);
    }

    public static string? GachaLanguage
    {
        get => GetValue<string>();
        set => SetValue(value);
    }

    public static string? AccentColor
    {
        get => GetValue<string>();
        set => SetValue(value);
    }


    public static bool UseSystemThemeColor
    {
        get => GetValue<bool>();
        set => SetValue(value);
    }

    public static bool EnableTransparency
    {
        get => GetValue(true);
        set => SetValue(value);
    }

    public static bool EnableNavigationViewLeftCompact
    {
        get => GetValue<bool>();
        set => SetValue(value);
    }


    public static string? LastAppVersion
    {
        get => GetValue<string>();
        set => SetValue(value);
    }

    /// <summary>
    /// 当前选择的游戏区服
    /// </summary>
    public static GameBiz CurrentGameBiz
    {
        get => GetValue<string>();
        set => SetValue(value);
    }

    public static bool HasWelcomed
    {
        get => GetValue<bool>();
        set => SetValue(value);
    }

    public static string? SelectedGameBizs
    {
        get => GetValue<string>();
        set => SetValue(value);
    }

    public static string? CachedGameInfo
    {
        get => GetValue<string>();
        set => SetValue(value);
    }

    public static string? BackupFolder
    {
        get => GetValue<string>();
        set => SetValue(value);
    }

    public static bool AutoBackupGachaRecord
    {
        get => GetValue<bool>();
        set => SetValue(value);
    }

    public static bool AutoBackupGachaRecordUIGF
    {
        get => GetValue<bool>();
        set => SetValue(value);
    }

    public static bool IsNetworkPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        if (path!.StartsWith(@"\\")) return true;
        try
        {
            string? root = System.IO.Path.GetPathRoot(path);
            if (string.IsNullOrEmpty(root)) return false;
            var drive = new System.IO.DriveInfo(root);
            return drive.DriveType == System.IO.DriveType.Network;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// In-memory cache of the last known network drive reachability for the backup folder.
    /// Shared across all services so the settings page can display status instantly
    /// without an additional blocking probe on every navigation.
    /// null = unknown (not yet probed), true = reachable, false = unreachable.
    /// </summary>
    public static bool? NetworkDriveAvailableCache { get; set; }

    public static bool IsCloudSyncPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;

        string normalized = path.Replace('/', '\\');
        var segments = normalized.Split(new[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0)
        {
            return false;
        }

        return segments.Any(segment =>
            segment.Equals("OneDrive", StringComparison.OrdinalIgnoreCase)
            || segment.Equals("iCloudDrive", StringComparison.OrdinalIgnoreCase)
            || segment.Equals("BaiduSyncdisk", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 固定待选择的游戏区服图标
    /// </summary>
    public static bool IsGameBizSelectorPinned
    {
        get => GetValue<bool>();
        set => SetValue(value);
    }



    /// <summary>
    /// 更新完成后自动重启
    /// </summary>
    public static bool AutoRestartWhenUpdateFinished
    {
        get => GetValue(true);
        set => SetValue(value);
    }

    /// <summary>
    /// 更新后重启显示更新内容
    /// </summary>
    public static bool ShowUpdateContentAfterUpdateRestart
    {
        get => GetValue(true);
        set => SetValue(value);
    }

    public static bool RunInSystemTray
    {
        get => GetValue(true);
        set => SetValue(value);
    }



    /// <summary>
    /// 截图文件夹
    /// </summary>
    public static string? ScreenshotFolder
    {
        get => GetValue<string>();
        set => SetValue(value);
    }

    /// <summary>
    /// BeyondProfilePictures backup folder
    /// </summary>
    public static string? BeyondProfilePicturesBackupFolder
    {
        get => GetValue<string>();
        set => SetValue(value);
    }

    /// <summary>
    /// 截图快捷键
    /// </summary>
    public static string? ScreenshotCaptureHotkey
    {
        // Alt + D
        get => GetValue("1+68");
        set => SetValue(value);
    }

    public static bool AutoConvertScreenshotToSDR
    {
        get => GetValue(true);
        set => SetValue(value);
    }

    public static bool AutoCopyScreenshotToClipboard
    {
        get => GetValue(true);
        set => SetValue(value);
    }

    /// <summary>
    /// 0: PNG, 1: AVIF, 2: JPEG XL
    /// </summary>
    public static int ScreenCaptureSavedFormat
    {
        get => GetValue(0);
        set => SetValue(value);
    }

    /// <summary>
    /// 0: Middle, 1: High, 2: Lossless
    /// </summary>
    public static int ScreenCaptureEncodeQuality
    {
        get => GetValue(1);
        set => SetValue(value);
    }



    #endregion



    #region Dynamic Setting


    public static long GetLastUidInGachaLogPage(GameBiz biz)
    {
        return GetValue<long>(default, $"last_gacha_uid_{biz}");
    }

    public static void SetLastUidInGachaLogPage(GameBiz biz, long value)
    {
        SetValue(value, $"last_gacha_uid_{biz}");
    }



    public static string? GetDisplayGachaBanners(GameBiz biz)
    {
        return GetValue<string>(default, $"display_gacha_banners_{biz}");
    }

    public static void SetDisplayGachaBanners(GameBiz biz, string value)
    {
        SetValue(value, $"display_gacha_banners_{biz}");
    }


    /// <summary>
    /// 外部截图文件夹
    /// </summary>
    /// <param name="biz"></param>
    /// <returns></returns>
    public static string? GetExternalScreenshotFolder(GameBiz biz)
    {
        return GetValue<string>(default, $"external_screenshot_folder_{biz}");
    }

    public static void SetExternalScreenshotFolder(GameBiz biz, string? value)
    {
        SetValue(value, $"external_screenshot_folder_{biz}");
    }


    public static string? GetGameInstallPathRemovable(GameBiz biz)
    {
        return GetValue<string>(default, $"game_install_path_{biz}");
    }

    public static void SetGameInstallPath(GameBiz biz, string? value)
    {
        SetValue(value, $"game_install_path_{biz}");
    }



    #endregion



    #region Setting Method


    private static Dictionary<string, string?> _settingCache;


    private static void InitializeSettingProvider()
    {
        try
        {
            if (_settingCache is null)
            {
                using var dapper = DatabaseService.CreateConnection();
                _settingCache = dapper.Query<(string Key, string? Value)>("SELECT Key, Value FROM Setting;").ToDictionary(x => x.Key, x => x.Value);
            }
        }
        catch { }
    }


    public static T? GetValue<T>(T? defaultValue = default, [CallerMemberName] string? key = null)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return defaultValue;
        }
        if (string.IsNullOrWhiteSpace(UserDataFolder))
        {
            return defaultValue;
        }
        InitializeSettingProvider();
        if (_settingCache is null)
        {
            return defaultValue;
        }
        try
        {
            if (_settingCache.TryGetValue(key, out string? value))
            {
                return ConvertFromString(value, defaultValue);
            }
            using var dapper = DatabaseService.CreateConnection();
            value = dapper.QueryFirstOrDefault<string>("SELECT Value FROM Setting WHERE Key=@key LIMIT 1;", new { key });
            _settingCache[key] = value;
            return ConvertFromString(value, defaultValue);
        }
        catch
        {
            return defaultValue;
        }
    }


    private static T? ConvertFromString<T>(string? value, T? defaultValue = default)
    {
        if (value is null)
        {
            return defaultValue;
        }
        var converter = TypeDescriptor.GetConverter(typeof(T));
        if (converter == null)
        {
            return defaultValue;
        }
        return (T?)converter.ConvertFromString(value);
    }


    public static void SetValue<T>(T? value, [CallerMemberName] string? key = null)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }
        if (string.IsNullOrWhiteSpace(UserDataFolder))
        {
            return;
        }
        InitializeSettingProvider();
        if (_settingCache is null)
        {
            return;
        }
        try
        {
            string? val = value?.ToString();
            if (_settingCache.TryGetValue(key, out string? cacheValue) && cacheValue == val)
            {
                return;
            }
            _settingCache[key] = val;
            using var dapper = DatabaseService.CreateConnection();
            dapper.Execute("INSERT OR REPLACE INTO Setting (Key, Value) VALUES (@key, @val);", new { key, val });
        }
        catch { }
    }


    public static void DeleteAllSettings()
    {
        try
        {
            using var dapper = DatabaseService.CreateConnection();
            dapper.Execute("DELETE FROM Setting WHERE TRUE;");
        }
        catch { }
    }


    public static void ClearCache()
    {
        _settingCache.Clear();
    }


    public static GameBiz GetLastRegionOfGame(string game)
    {
        var val = GetValue<string>(null, $"LastRegion_{game}");
        if (string.IsNullOrWhiteSpace(val)) return GameBiz.None;
        return val;
    }

    public static void SetLastRegionOfGame(GameBiz biz)
    {
        SetValue(biz.Value, $"LastRegion_{biz.ToGame().Value}");
    }

    #endregion

}
