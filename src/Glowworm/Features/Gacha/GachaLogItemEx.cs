
using CommunityToolkit.Mvvm.ComponentModel;
using Glowworm.Core.Gacha;
using Glowworm.Core.Gacha.Genshin;
using Glowworm.Core.Gacha.StarRail;
using Glowworm.Core.Gacha.ZZZ;

namespace Glowworm.Features.Gacha;

[INotifyPropertyChanged]
public partial class GachaLogItemEx : GachaLogItem
{

    /// <summary>
    /// ????,?? Excel ???
    /// </summary>
    public string IdText => Id.ToString();

    /// <summary>
    /// ??????????
    /// </summary>
    public int Index { get; set; }

    public int Pity { get; set; }

    public string Icon { get; set; }

    public double Progress => (double)Pity / ((GachaType is GenshinGachaType.WeaponEventWish or StarRailGachaType.LightConeEventWarp or StarRailGachaType.LightConeCollaborationWarp or ZZZGachaType.WEngineChannel or ZZZGachaType.WEngineReverberation or ZZZGachaType.BangbooChannel) ? 80 : 90) * 100;


    public bool IsPointerIn { get; set => SetProperty(ref field, value); }

    public int ItemCount { get; set; }


    public bool HasUpItem { get; set; }

    public bool IsUp { get; set; }

    public double UpTextOpacity => IsUp ? 1 : 0;

}



