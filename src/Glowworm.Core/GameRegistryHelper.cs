using Microsoft.Win32;
using System.IO;

namespace Glowworm.Core;

public static class GameRegistryHelper
{

    public static string? GetGameInstallPath(GameBiz biz)
    {
        string? path = GetGameInstallPathFromRegistry(biz);
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            return null;
        }
        return path;
    }


    private static string? GetGameInstallPathFromRegistry(GameBiz biz)
    {
        string? path = biz.Value switch
        {
            GameBiz.hk4e_cn => GetValue(GameRegistry.GamePath_hk4e_cn, GameRegistry.GameInstallPath) ?? GetValue(GameRegistry.GamePath_hk4e_cn_taptap, GameRegistry.GameInstallPath),
            GameBiz.hk4e_global => GetValue(GameRegistry.GamePath_hk4e_global, GameRegistry.GameInstallPath),
            GameBiz.hk4e_bilibili => GetValue(GameRegistry.GamePath_hk4e_cn_bilibili, GameRegistry.GameInstallPath),
            GameBiz.hk4e_google => GetValue(GameRegistry.GamePath_hk4e_global_google, GameRegistry.GameInstallPath),
            GameBiz.hk4e_epic => GetValue(GameRegistry.GamePath_hk4e_global_epic, GameRegistry.GameInstallPath),
            GameBiz.hkrpg_cn => GetValue(GameRegistry.GamePath_hkrpg_cn, GameRegistry.GameInstallPath) ?? GetValue(GameRegistry.GamePath_hkrpg_cn_taptap, GameRegistry.GameInstallPath),
            GameBiz.hkrpg_global => GetValue(GameRegistry.GamePath_hkrpg_global, GameRegistry.GameInstallPath),
            GameBiz.hkrpg_bilibili => GetValue(GameRegistry.GamePath_hkrpg_cn_bilibili, GameRegistry.GameInstallPath),
            GameBiz.hkrpg_epic => GetValue(GameRegistry.GamePath_hkrpg_global_epic, GameRegistry.GameInstallPath),
            GameBiz.nap_cn => GetValue(GameRegistry.GamePath_nap_cn, GameRegistry.GameInstallPath) ?? GetValue(GameRegistry.GamePath_nap_cn_taptap, GameRegistry.GameInstallPath),
            GameBiz.nap_global => GetValue(GameRegistry.GamePath_nap_global, GameRegistry.GameInstallPath),
            GameBiz.nap_bilibili => GetValue(GameRegistry.GamePath_nap_cn_bilibili, GameRegistry.GameInstallPath),
            GameBiz.nap_epic => GetValue(GameRegistry.GamePath_nap_global_epic, GameRegistry.GameInstallPath),
            GameBiz.nap_steam => GetValue(GameRegistry.GamePath_nap_global_steam, GameRegistry.GameInstallPath),
            _ => null,
        };

        return path;
    }


    private static string? GetValue(string key, string name)
    {
        try
        {
            return Registry.GetValue(key, name, null) as string;
        }
        catch
        {
            return null;
        }
    }


    public static bool IsCloudGameInstalled(GameBiz biz)
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return biz.Value switch
        {
            GameBiz.clgm_cn => Directory.Exists(Path.Join(localAppData, "miHoYo", "GenshinImpactCloudGame")),
            GameBiz.clgm_global => Directory.Exists(Path.Join(localAppData, "HoYoverse", "GenshinImpactCloudGame")),
            GameBiz.nap_cloud_cn => Directory.Exists(Path.Join(localAppData, "miHoYo", "ZenlessZoneZeroCloud")),
            GameBiz.nap_cloud_global => Directory.Exists(Path.Join(localAppData, "HoYoverse", "ZenlessZoneZeroCloud")),
            GameBiz.hkrpg_cloud_cn or GameBiz.hkrpg_cloud_global => false, // Web only
            _ => false,
        };
    }
    }
