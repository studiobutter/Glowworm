namespace Glowworm.Core;

public class GameInfo
{
    public GameId GameId { get; set; }

    public GameBiz GameBiz => GameId.GameBiz;

    public GameDisplay Display { get; set; }

    public bool IsBilibiliServer() => GameBiz.IsBilibiliServer;
}


public class GameDisplay
{
    public string Name { get; set; }

    public GameBackground Background { get; set; }

    public GameImage Icon { get; set; }

    public GameImage Thumbnail { get; set; }

    public GameImage Logo { get; set; }
}

public class GameBackground
{
    public string Url { get; set; }
}

public class GameImage
{
    public string Url { get; set; }
}
