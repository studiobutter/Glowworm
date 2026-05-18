namespace Glowworm.Features.Update;

/// <summary>
/// ????
/// </summary>
public enum UpdateState
{
    /// <summary>
    /// ??
    /// </summary>
    Stop = 0,

    /// <summary>
    /// ????????
    /// </summary>
    Pending = 1,

    /// <summary>
    /// ???
    /// </summary>
    Downloading = 2,

    /// <summary>
    /// ??
    /// </summary>
    Finish = 3,

    /// <summary>
    /// ??
    /// </summary>
    Error = 4,

    /// <summary>
    /// ???
    /// </summary>
    NotSupport = 5,
}



