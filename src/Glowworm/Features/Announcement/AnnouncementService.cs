using Dapper;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Glowworm.Features.Announcement;

/// <summary>
/// Fetches and caches announcements from the Glowworm announcement API.
/// </summary>
public class AnnouncementService
{
    private const string BaseUrl = "https://glowworm.studiobutter.io.vn/Announcement/List";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AnnouncementService> _logger;
    private readonly object _stateLock = new();

    private List<AnnouncementItem>? _cachedItems;
    private string? _cachedLang;
    private DateTimeOffset _lastFetchTime;
    private readonly SemaphoreSlim _fetchLock = new(1, 1);

    // Cache for 15 minutes
    private static readonly TimeSpan CacheExpiry = TimeSpan.FromMinutes(15);


    public AnnouncementService(IHttpClientFactory httpClientFactory, ILogger<AnnouncementService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }


    /// <summary>
    /// Gets announcements for the current UI language.
    /// Returns cached results if still fresh; otherwise fetches from the API.
    /// </summary>
    public async Task<List<AnnouncementItem>> GetAnnouncementsAsync(bool forceRefresh = false)
    {
        var lang = ResolveApiLang();
        if (!forceRefresh && _cachedItems is not null && _cachedLang == lang && DateTimeOffset.UtcNow - _lastFetchTime < CacheExpiry)
        {
            return _cachedItems;
        }

        await _fetchLock.WaitAsync();

        try
        {
            if (!forceRefresh && _cachedItems is not null && _cachedLang == lang && DateTimeOffset.UtcNow - _lastFetchTime < CacheExpiry)
            {
                return _cachedItems;
            }

            var url = $"{BaseUrl}?lang={lang}";

            var client = _httpClientFactory.CreateClient();
            var json = await client.GetStringAsync(url);
            var response = JsonSerializer.Deserialize<AnnouncementListResponse>(json, AppConfig.JsonSerializerOptions);
            _cachedItems = response?.Ann ?? new List<AnnouncementItem>();
            _cachedLang = lang;
            _lastFetchTime = DateTimeOffset.UtcNow;
            return _cachedItems;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch announcements");
            if (_cachedItems is not null && _cachedLang == lang)
            {
                return _cachedItems;
            }

            throw;
        }
        finally
        {
            _fetchLock.Release();
        }
    }


    public HashSet<int> GetReadAnnouncementIds()
    {
        using var connection = AppConfig.CreateDatabaseConnection();
        return connection.Query<int>("SELECT AnnouncementId FROM AnnouncementState WHERE IsRead = 1;").ToHashSet();
    }


    public HashSet<int> GetDismissedAnnouncementIds()
    {
        using var connection = AppConfig.CreateDatabaseConnection();
        return connection.Query<int>("SELECT AnnouncementId FROM AnnouncementState WHERE IsDismissed = 1;").ToHashSet();
    }


    public bool HasUnreadAnnouncements(IEnumerable<AnnouncementItem> announcements)
    {
        var readIds = GetReadAnnouncementIds();
        return announcements.Any(item => !readIds.Contains(item.Id));
    }


    public void MarkAnnouncementsAsRead(IEnumerable<int> announcementIds)
    {
        var ids = announcementIds.Distinct().ToArray();
        if (ids.Length == 0)
        {
            return;
        }

        lock (_stateLock)
        {
            using var connection = AppConfig.CreateDatabaseConnection();
            using var transaction = connection.BeginTransaction();
            connection.Execute(
                """
                INSERT INTO AnnouncementState (AnnouncementId, IsRead)
                VALUES (@Id, 1)
                ON CONFLICT (AnnouncementId) DO UPDATE SET IsRead = 1;
                """,
                ids.Select(id => new { Id = id }),
                transaction);
            transaction.Commit();
        }
    }


    public void DismissAnnouncement(int announcementId)
    {
        lock (_stateLock)
        {
            using var connection = AppConfig.CreateDatabaseConnection();
            connection.Execute(
                """
                INSERT INTO AnnouncementState (AnnouncementId, IsRead, IsDismissed)
                VALUES (@announcementId, 1, 1)
                ON CONFLICT (AnnouncementId) DO UPDATE SET IsRead = 1, IsDismissed = 1;
                """,
                new { announcementId });
        }
    }


    /// <summary>
    /// Maps the current <see cref="CultureInfo"/> to the lang tag expected by the API.
    /// </summary>
    private static string ResolveApiLang()
    {
        var culture = CultureInfo.CurrentUICulture;
        return culture.Name.ToLowerInvariant() switch
        {
            "zh-cn" => "zh-cn",
            "zh-tw" or "zh-hant" => "zh-tw",
            "ja" or "ja-jp" => "ja-jp",
            "ko" or "ko-kr" => "ko-kr",
            "vi" or "vi-vn" => "vi-vn",
            _ => "en-us",
        };
    }
}
