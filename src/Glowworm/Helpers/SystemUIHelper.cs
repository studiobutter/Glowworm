using System;
using Vanara.PInvoke;

namespace Glowworm.Helpers;


/// <summary>
/// ?????,?????????????
/// </summary>
[Obsolete("?????", true)]
public static class SystemUIHelper
{


    /// <summary>
    /// ???? - ???? - ????
    /// </summary>
    public static bool TransparencyEffectEnabled
    {
        get
        {
            User32.SystemParametersInfo(User32.SPI.SPI_GETDISABLEOVERLAPPEDCONTENT, out bool enabled);
            return enabled;
        }
        set => User32.SystemParametersInfo(User32.SPI.SPI_SETDISABLEOVERLAPPEDCONTENT, value);
    }



    /// <summary>
    /// ???? - ???? - ????
    /// </summary>
    public static bool AnimationEffectEnabled
    {
        get
        {
            User32.SystemParametersInfo(User32.SPI.SPI_GETCLIENTAREAANIMATION, out bool enabled);
            return enabled;
        }
        set => User32.SystemParametersInfo(User32.SPI.SPI_SETCLIENTAREAANIMATION, value);
    }



}



