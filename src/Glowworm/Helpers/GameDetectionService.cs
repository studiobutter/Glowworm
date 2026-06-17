using Glowworm.Core;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;

namespace Glowworm.Helpers;

public static class GameDetectionService
{


    public static string? GetGameInstallPath(GameBiz biz)
    {
        var keys = GetRegistryKeys(biz);
        foreach (var key in keys)
        {
            try
            {
                var path = Registry.GetValue(key, GameRegistry.GameInstallPath, null) as string;
                if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
                {
                    return path;
                }
            }
            catch { }
        }
        return null;
    }



    private static List<string> GetRegistryKeys(GameBiz biz)
    {
        var list = new List<string>();
        if (biz == GameBiz.hk4e_cn)
        {
            list.Add(GameRegistry.GamePath_hk4e_cn);
            list.Add(GameRegistry.GamePath_hk4e_cn_taptap);
        }
        else if (biz == GameBiz.hk4e_global)
        {
            list.Add(GameRegistry.GamePath_hk4e_global);
        }
        else if (biz == GameBiz.hk4e_google)
        {
            list.Add(GameRegistry.GamePath_hk4e_global_google);
        }
        else if (biz == GameBiz.hk4e_epic)
        {
            list.Add(GameRegistry.GamePath_hk4e_global_epic);
        }
        else if (biz == GameBiz.hk4e_bilibili)
        {
            list.Add(GameRegistry.GamePath_hk4e_cn_bilibili);
        }
        else if (biz == GameBiz.hkrpg_cn)
        {
            list.Add(GameRegistry.GamePath_hkrpg_cn);
            list.Add(GameRegistry.GamePath_hkrpg_cn_taptap);
        }
        else if (biz == GameBiz.hkrpg_global)
        {
            list.Add(GameRegistry.GamePath_hkrpg_global);
        }
        else if (biz == GameBiz.hkrpg_epic)
        {
            list.Add(GameRegistry.GamePath_hkrpg_global_epic);
        }
        else if (biz == GameBiz.hkrpg_bilibili)
        {
            list.Add(GameRegistry.GamePath_hkrpg_cn_bilibili);
        }
        else if (biz == GameBiz.nap_cn)
        {
            list.Add(GameRegistry.GamePath_nap_cn);
            list.Add(GameRegistry.GamePath_nap_cn_taptap);
        }
        else if (biz == GameBiz.nap_global)
        {
            list.Add(GameRegistry.GamePath_nap_global);
        }
        else if (biz == GameBiz.nap_epic)
        {
            list.Add(GameRegistry.GamePath_nap_global_epic);
        }
        else if (biz == GameBiz.nap_steam)
        {
            list.Add(GameRegistry.GamePath_nap_global_steam);
        }
        else if (biz == GameBiz.nap_bilibili)
        {
            list.Add(GameRegistry.GamePath_nap_cn_bilibili);
        }
        return list;
    }



}
