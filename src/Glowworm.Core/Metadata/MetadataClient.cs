using Glowworm.Core.Metadata.Github;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Glowworm.Core.Metadata;

public class MetadataClient
{


    private const string API_PREFIX_CLOUDFLARE = "https://glowworm-static.scighost.com/metadata";

    private const string API_PREFIX_GITHUB = "https://raw.githubusercontent.com/Scighost/Glowworm/metadata";

    private const string API_PREFIX_JSDELIVR = "https://cdn.jsdelivr.net/gh/Scighost/Glowworm@metadata";


    private string API_PREFIX = API_PREFIX_CLOUDFLARE;

#if DEV
    private const string API_VERSION = "dev";
#else
    private const string API_VERSION = "v1";
#endif


    private readonly HttpClient _httpClient;


    public MetadataClient(int apiIndex = 0, HttpClient? httpClient = null)
    {
        SetApiPrefix(apiIndex);
        if (httpClient is null)
        {
            _httpClient = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All }) { DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher };
        }
        else
        {
            _httpClient = httpClient;
        }
    }



    public void SetApiPrefix(int index)
    {
        API_PREFIX = index switch
        {
            1 => API_PREFIX_GITHUB,
            2 => API_PREFIX_JSDELIVR,
            _ => API_PREFIX_CLOUDFLARE,
        };
    }



    private async Task<T> CommonGetAsync<T>(string url, CancellationToken cancellationToken = default) where T : class
    {
        var res = await _httpClient.GetFromJsonAsync(url, typeof(T), MetadataJsonContext.Default, cancellationToken) as T;
        if (res == null)
        {
            throw new NullReferenceException("");
        }
        else
        {
            return res;
        }
    }




    private string GetUrl(string suffix)
    {
        return $"{API_PREFIX}/{API_VERSION}/{suffix}";
    }









    #region Github



    public async Task<GithubRelease?> GetGithubLatestReleaseAsync(CancellationToken cancellationToken = default)
    {
        const string url = "https://api.github.com/repos/studiobutter/Glowworm/releases?page=1&per_page=1";
        var list = await CommonGetAsync<List<GithubRelease>>(url, cancellationToken);
        return list?.FirstOrDefault();
    }



    public async Task<List<GithubRelease>> GetGithubReleaseAsync(int page, int perPage, CancellationToken cancellationToken = default)
    {
        string url = $"https://api.github.com/repos/studiobutter/Glowworm/releases?page={page}&per_page={perPage}";
        var list = await CommonGetAsync<List<GithubRelease>>(url, cancellationToken);
        return list ?? new List<GithubRelease>();
    }



    public async Task<GithubRelease?> GetGithubReleaseAsync(string tag, CancellationToken cancellationToken = default)
    {
        string url = $"https://api.github.com/repos/studiobutter/Glowworm/releases/tags/{tag}";
        return await CommonGetAsync<GithubRelease>(url, cancellationToken);
    }


    public async Task<string> RenderGithubMarkdownAsync(string markdown, CancellationToken cancellationToken = default)
    {
        const string url = "https://api.github.com/markdown";
        var request = new GithubMarkdownRequest
        {
            Text = markdown,
            Mode = "gfm",
            Context = "studiobutter/Glowworm",
        };
        var content = new StringContent(JsonSerializer.Serialize(request, typeof(GithubMarkdownRequest), MetadataJsonContext.Default), new MediaTypeHeaderValue("application/json"));
        var response = await _httpClient.PostAsync(url, content, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }



    #endregion



}



