using Microsoft.UI.Xaml.Data;
using System;

namespace Glowworm.Features.Gacha;

internal partial class RarityToZZZIconConverter : IValueConverter
{

    private const string Rarity2Background = "ms-appx:///Assets/B_Level_S.png";
    private const string Rarity3Background = "ms-appx:///Assets/A_Level_S.png";
    private const string Rarity4Background = "ms-appx:///Assets/S_Level_S.png";
    private const string TransparentBackground = "ms-appx:///Assets/Transparent.png";


    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return (int)value switch
        {
            2 => Rarity2Background,
            3 => Rarity3Background,
            4 => Rarity4Background,
            _ => TransparentBackground,
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}



