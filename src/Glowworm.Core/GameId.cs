namespace Glowworm.Core;

public record GameId(GameBiz GameBiz)
{
    public static GameId FromGameBiz(GameBiz gameBiz) => new GameId(gameBiz);
}
