using Glowworm.Core;
using Glowworm.Features.Gacha;
using Glowworm.Features.Screenshot;
using System.Collections.Generic;

namespace Glowworm.Features;

internal partial class GameFeatureConfig
{


    private GameFeatureConfig()
    {

    }


    /// <summary>
    /// ?????
    /// </summary>
    public List<string> SupportedPages { get; init; } = [];



    public static GameFeatureConfig FromGameId(GameId? gameId)
    {
        if (gameId is null)
        {
            return None;
        }

        if (gameId.GameBiz.IsCloudGame())
        {
            var cloudConfig = new GameFeatureConfig
            {
                SupportedPages = [nameof(GachaLogPage)]
            };

            if (GameRegistryHelper.IsCloudGameInstalled(gameId.GameBiz))
            {
                cloudConfig.SupportedPages.Add(nameof(ScreenshotPage));
                if (gameId.GameBiz.ToGame() == GameBiz.hk4e)
                {
                    cloudConfig.SupportedPages.Add(nameof(GenshinBeyondGachaPage));
                }
            }

            return cloudConfig;
        }

        GameFeatureConfig config = gameId.GameBiz.Value switch
        {
            GameBiz.hk4e_cn => hk4e_cn,
            GameBiz.hk4e_global or GameBiz.hk4e_google or GameBiz.hk4e_epic => hk4e_global,
            GameBiz.hk4e_bilibili => hk4e_bilibili,
            GameBiz.hkrpg_cn => hkrpg_cn,
            GameBiz.hkrpg_global or GameBiz.hkrpg_epic => hkrpg_global,
            GameBiz.hkrpg_bilibili => hkrpg_bilibili,
            GameBiz.nap_cn => nap_cn,
            GameBiz.nap_global or GameBiz.nap_epic => nap_global,
            GameBiz.nap_bilibili => nap_bilibili,
            _ => Default,
        };
        return config;
    }





    private static readonly GameFeatureConfig None = new();


    private static readonly GameFeatureConfig Default = new()
    {
        SupportedPages = [nameof(ScreenshotPage)]
    };


    private static readonly GameFeatureConfig hk4e_cn = new()
    {
        SupportedPages =
        [
            nameof(ScreenshotPage),
            nameof(GachaLogPage),
            nameof(GenshinBeyondGachaPage),
            nameof(BeyondProfilePicturePage),
        ],
    };


    private static readonly GameFeatureConfig hk4e_global = new()
    {
        SupportedPages =
        [
            nameof(ScreenshotPage),
            nameof(GachaLogPage),
            nameof(GenshinBeyondGachaPage),
            nameof(BeyondProfilePicturePage),
        ],
    };


    private static readonly GameFeatureConfig hk4e_bilibili = new()
    {
        SupportedPages =
        [
            nameof(ScreenshotPage),
            nameof(GachaLogPage),
            nameof(GenshinBeyondGachaPage),
            nameof(BeyondProfilePicturePage),
        ],
    };


    private static readonly GameFeatureConfig hkrpg_cn = new()
    {
        SupportedPages =
        [
            nameof(ScreenshotPage),
            nameof(GachaLogPage),
        ],
    };


    private static readonly GameFeatureConfig hkrpg_global = new()
    {
        SupportedPages =
        [
            nameof(ScreenshotPage),
            nameof(GachaLogPage),
        ],
    };


    private static readonly GameFeatureConfig hkrpg_bilibili = new()
    {
        SupportedPages =
        [
            nameof(ScreenshotPage),
            nameof(GachaLogPage),
        ],
    };



    private static readonly GameFeatureConfig nap_cn = new()
    {
        SupportedPages =
        [
            nameof(ScreenshotPage),
            nameof(GachaLogPage),
        ],
    };


    private static readonly GameFeatureConfig nap_global = new()
    {
        SupportedPages =
        [
            nameof(ScreenshotPage),
            nameof(GachaLogPage),
        ],
    };


    private static readonly GameFeatureConfig nap_bilibili = new()
    {
        SupportedPages =
        [
            nameof(ScreenshotPage),
            nameof(GachaLogPage),
        ],
    };



}



