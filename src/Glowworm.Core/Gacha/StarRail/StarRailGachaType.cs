using System.ComponentModel;

namespace Glowworm.Core.Gacha.StarRail;

public readonly record struct StarRailGachaType(int Value) : IGachaType
{


    /// <summary>
    /// ????
    /// </summary>
    [Description("????")]
    public const int StellarWarp = 1;


    /// <summary>
    /// ????
    /// </summary>
    [Description("????")]
    public const int DepartureWarp = 2;


    /// <summary>
    /// ??????
    /// </summary>
    [Description("??????")]
    public const int CharacterEventWarp = 11;


    /// <summary>
    /// ??????
    /// </summary>
    [Description("??????")]
    public const int LightConeEventWarp = 12;


    /// <summary>
    /// ??????
    /// </summary>
    [Description("??????")]
    public const int CharacterCollaborationWarp = 21;


    /// <summary>
    /// ??????
    /// </summary>
    [Description("??????")]
    public const int LightConeCollaborationWarp = 22;



    public string ToLocalization() => Value switch
    {
        StellarWarp => CoreLang.GachaType_StellarWarp,
        DepartureWarp => CoreLang.GachaType_DepartureWarp,
        CharacterEventWarp => CoreLang.GachaType_CharacterEventWarp,
        LightConeEventWarp => CoreLang.GachaType_LightConeEventWarp,
        CharacterCollaborationWarp => CoreLang.GachaType_CharacterCollaborationWarp,
        LightConeCollaborationWarp => CoreLang.GachaType_LightConeCollaborationWarp,
        _ => "",
    };



    public override string ToString() => Value.ToString();
    public static implicit operator StarRailGachaType(int value) => new(value);
    public static implicit operator int(StarRailGachaType gachaType) => gachaType.Value;


}



