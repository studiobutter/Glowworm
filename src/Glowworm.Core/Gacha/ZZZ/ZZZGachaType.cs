using System.ComponentModel;

namespace Glowworm.Core.Gacha.ZZZ;

public readonly record struct ZZZGachaType(int Value) : IGachaType
{


    /// <summary>
    /// ????
    /// </summary>
    [Description("????")]
    public const int StandardChannel = 1;

    /// <summary>
    /// ????
    /// </summary>
    [Description("????")]
    public const int ExclusiveChannel = 2;

    /// <summary>
    /// ????
    /// </summary>
    [Description("????")]
    public const int WEngineChannel = 3;

    /// <summary>
    /// ????
    /// </summary>
    [Description("????")]
    public const int BangbooChannel = 5;


    /// <summary>
    /// ????
    /// </summary>
    [Description("????")]
    public const int ExclusiveRescreening = 102;


    /// <summary>
    /// ????
    /// </summary>
    [Description("????")]
    public const int WEngineReverberation = 103;


    public string ToLocalization() => Value switch
    {
        StandardChannel => CoreLang.GachaType_StandardChannel,
        ExclusiveChannel => CoreLang.GachaType_ExclusiveChannel,
        WEngineChannel => CoreLang.GachaType_WEngineChannel,
        BangbooChannel => CoreLang.GachaType_BangbooChannel,
        ExclusiveRescreening => CoreLang.GachaType_ExclusiveRescreening,
        WEngineReverberation => CoreLang.GachaType_WEngineReverberation,
        _ => "",
    };



    public override string ToString() => Value.ToString();
    public static implicit operator ZZZGachaType(int value) => new(value);
    public static implicit operator int(ZZZGachaType gachaType) => gachaType.Value;


}


