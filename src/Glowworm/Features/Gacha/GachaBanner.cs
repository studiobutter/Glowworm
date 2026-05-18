using Glowworm.Core.Gacha;

namespace Glowworm.Features.Gacha;

public class GachaBanner
{


    public IGachaType GachaType { get; private set; }


    public int Value => GachaType.Value;


    public string ToLocalization() => GachaType.ToLocalization();



    public GachaBanner(IGachaType gachaType)
    {
        GachaType = gachaType;
    }


}



