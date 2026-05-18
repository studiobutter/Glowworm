using System.ComponentModel;

namespace Glowworm.Core.Gacha.Genshin;

public readonly record struct GenshinGachaType(int Value) : IGachaType
{


    /// <summary>
    /// ????
    /// </summary>
    [Description("????")]
    public const int NoviceWish = 100;

    /// <summary>
    /// ????
    /// </summary>
    [Description("????")]
    public const int PermanentWish = 200;

    /// <summary>
    /// ??????
    /// </summary>
    [Description("??????")]
    public const int CharacterEventWish = 301;

    /// <summary>
    /// ??????
    /// </summary>
    [Description("??????")]
    public const int WeaponEventWish = 302;

    /// <summary>
    /// ??????-2
    /// </summary>
    [Description("??????-2")]
    public const int CharacterEventWish_2 = 400;

    /// <summary>
    /// ????
    /// </summary>
    [Description("????")]
    public const int ChronicledWish = 500;



    public string ToLocalization() => Value switch
    {
        NoviceWish => CoreLang.GachaType_NoviceWish,
        PermanentWish => CoreLang.GachaType_PermanentWish,
        CharacterEventWish => CoreLang.GachaType_CharacterEventWish,
        CharacterEventWish_2 => CoreLang.GachaType_CharacterEventWish_2,
        WeaponEventWish => CoreLang.GachaType_WeaponEventWish,
        ChronicledWish => CoreLang.GachaType_ChronicledWish,
        _ => "",
    };



    public override string ToString() => Value.ToString();
    public static implicit operator GenshinGachaType(int value) => new(value);
    public static implicit operator int(GenshinGachaType value) => value.Value;


}



