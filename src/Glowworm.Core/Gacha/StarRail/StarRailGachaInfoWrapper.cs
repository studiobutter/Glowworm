using System.Text.Json.Serialization;

namespace Glowworm.Core.Gacha.StarRail;

internal class StarRailGachaInfoWrapper
{

    [JsonPropertyName("list")]
    public List<StarRailGachaInfo> List { get; set; }
}




