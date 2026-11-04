using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Glowworm.Features.Announcement;

/// <summary>
/// Represents a single announcement fetched from the Glowworm announcement API.
/// </summary>
public class AnnouncementItem
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("lang")]
    public string Lang { get; set; } = string.Empty;

    [JsonPropertyName("LastUpdateTime")]
    public long LastUpdateTime { get; set; }

    [JsonPropertyName("Title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("Content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 1 = Notice, 2 = Warning, 3 = Critical
    /// </summary>
    [JsonPropertyName("Severity")]
    public int Severity { get; set; }

    public AnnouncementSeverity SeverityLevel => Severity switch
    {
        2 => AnnouncementSeverity.Warning,
        3 => AnnouncementSeverity.Critical,
        _ => AnnouncementSeverity.Notice,
    };
}

public enum AnnouncementSeverity
{
    Notice,
    Warning,
    Critical,
}

public class AnnouncementListResponse
{
    [JsonPropertyName("ann")]
    public List<AnnouncementItem> Ann { get; set; } = new();
}
