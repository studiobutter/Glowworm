using CommunityToolkit.Mvvm.ComponentModel;
using Glowworm.Core;
using Glowworm.Core.Gacha.Genshin;
using Glowworm.Core.Gacha.StarRail;
using Glowworm.Core.Gacha.ZZZ;
using System;
using System.Collections.Generic;


namespace Glowworm.Features.Gacha.UIGF;

public class GachaUidArchiveDisplay : ObservableObject
{

    public GameBiz Game { get; set; }

    public string GameIcon { get; set; }

    public long Uid { get; set; }

    public int Count { get; set; }

    public string LastItemGachaType { get; set; }

    public string LastItemName { get; set; }

    public DateTime LastItemTime { get; set; }


    public List<UIGFGenshinGachaItem>? hke4List { get; set; }

    public List<GenshinBeyondGachaItem>? hk4eUgcList { get; set; }

    public List<StarRailGachaItem>? hkrpgList { get; set; }

    public List<ZZZGachaItem>? napList { get; set; }


    public int Timezone
    {
        get; set
        {
            if (SetProperty(ref field, value))
            {
                LastItemTimeOffest = LastItemTime.AddHours(value);
            }
        }
    }


    public DateTime LastItemTimeOffest { get; set => SetProperty(ref field, value); }



    public string? Result { get; set => SetProperty(ref field, value); }


    public string? Error { get; set => SetProperty(ref field, value); }


}


