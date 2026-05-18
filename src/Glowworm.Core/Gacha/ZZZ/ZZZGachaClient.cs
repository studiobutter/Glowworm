using System.Net.Http.Json;
using System.Text.RegularExpressions;

namespace Glowworm.Core.Gacha.ZZZ;

public class ZZZGachaClient : GachaLogClient
{



    public override IReadOnlyCollection<IGachaType> QueryGachaTypes { get; init; } = new ZZZGachaType[] { 1, 2, 3, 5, 102, 103 }.Cast<IGachaType>().ToList().AsReadOnly();



    public ZZZGachaClient(HttpClient? httpClient = null) : base(httpClient)
    {

    }



    protected override string GetGachaUrlPrefix(string gachaUrl, string? lang = null)
    {
        var match = Regex.Match(gachaUrl, @"(https://webstatic\.mihoyo\.com/nap[!-z]+)");
        if (match.Success)
        {
            gachaUrl = match.Groups[1].Value;
            var index = gachaUrl.IndexOf('?');
            if (index >= 0)
            {
                var auth = gachaUrl.Substring(index).Split('#')[0];
                gachaUrl = API_PREFIX_ZZZ_CN + auth;
            }
        }
        else
        {
            match = Regex.Match(gachaUrl, @"(https://gs\.hoyoverse\.com/nap[!-z]+)");
            if (match.Success)
            {
                gachaUrl = match.Groups[1].Value;
                var index = gachaUrl.IndexOf('?');
                if (index >= 0)
                {
                    var auth = gachaUrl.Substring(index).Split('#')[0];
                    gachaUrl = API_PREFIX_ZZZ_OS + auth;
                }
            }
            else
            {
                match = Regex.Match(gachaUrl, @"(https://public-operation-common[!-z]+)");
                if (match.Success)
                {
                    gachaUrl = match.Groups[1].Value.Split('#')[0];
                }
                else
                {
                    throw new ArgumentException(CoreLang.Gacha_CannotParseTheWishRecordURL);
                }
            }
        }

        gachaUrl = Regex.Replace(gachaUrl, @"([?&])gacha_type=\d+", "$1").Replace("?&", "?").TrimEnd('?', '&');
        gachaUrl = Regex.Replace(gachaUrl, @"([?&])real_gacha_type=\d+", "$1").Replace("?&", "?").TrimEnd('?', '&');
        gachaUrl = Regex.Replace(gachaUrl, @"([?&])page=\d+", "$1").Replace("?&", "?").TrimEnd('?', '&');
        gachaUrl = Regex.Replace(gachaUrl, @"([?&])size=\d+", "$1").Replace("?&", "?").TrimEnd('?', '&');
        gachaUrl = Regex.Replace(gachaUrl, @"([?&])end_id=\d*", "$1").Replace("?&", "?").TrimEnd('?', '&');
        if (!string.IsNullOrWhiteSpace(lang))
        {
            gachaUrl = Regex.Replace(gachaUrl, @"&lang=[^&]+", $"&lang={LanguageUtil.FilterLanguage(lang)}");
        }
        return gachaUrl;
    }


    public override async Task<long> GetUidByGachaUrlAsync(string gachaUrl)
    {
        var prefix = GetGachaUrlPrefix(gachaUrl);
        foreach (var gachaType in QueryGachaTypes)
        {
            var param = new GachaLogQuery(gachaType, 1, 5, 0);
            var list = await GetGachaLogByQueryAsync<ZZZGachaItem>(prefix, param);
            if (list.Count != 0)
            {
                return list.First().Uid;
            }
        }
        return 0;
    }




    public override async Task<IEnumerable<GachaLogItem>> GetGachaLogAsync(string gachaUrl, long endId = 0, string? lang = null, IProgress<(IGachaType GachaType, int Page)>? progress = null, CancellationToken cancellationToken = default)
    {
        return await GetGachaLogAsync<ZZZGachaItem>(gachaUrl, endId, lang, progress, cancellationToken);
    }




    public override async Task<IEnumerable<GachaLogItem>> GetGachaLogAsync(string gachaUrl, IGachaType gachaType, long endId = 0, string? lang = null, IProgress<(IGachaType GachaType, int Page)>? progress = null, CancellationToken cancellationToken = default)
    {
        return await GetGachaLogAsync<ZZZGachaItem>(gachaUrl, gachaType, endId, lang, progress, cancellationToken);
    }




    public override async Task<IEnumerable<GachaLogItem>> GetGachaLogAsync(string gachaUrl, GachaLogQuery query, CancellationToken cancellationToken = default)
    {
        string prefix = GetGachaUrlPrefix(gachaUrl);
        return await GetGachaLogByQueryAsync<ZZZGachaItem>(prefix, query, cancellationToken);
    }


    protected override async Task<List<T>> GetGachaLogByQueryAsync<T>(string gachaUrlPrefix, GachaLogQuery param, CancellationToken cancellationToken = default)
    {
        await Task.Delay(Random.Shared.Next(200, 300), cancellationToken);
        var url = $"{gachaUrlPrefix}&{param}";
        var wrapper = await _httpClient.GetFromJsonAsync(url, typeof(miHoYoApiWrapper<GachaLogResult<T>>), GachaLogJsonContext.Default, cancellationToken) as miHoYoApiWrapper<GachaLogResult<T>>;
        if (wrapper is null)
        {
            return new List<T>();
        }
        else if (wrapper.Retcode != 0)
        {
            throw new miHoYoApiException(wrapper.Retcode, wrapper.Message);
        }
        else
        {
            return wrapper.Data.List;
        }
    }


    protected override async Task<List<T>> GetGachaLogByTypeAsync<T>(string prefix, IGachaType gachaType, long endId = 0, IProgress<(IGachaType GachaType, int Page)>? progress = null, CancellationToken cancellationToken = default)
    {
        var param = new GachaLogQuery(gachaType, 1, 5, 0);
        var result = new List<T>();
        while (true)
        {
            progress?.Report((gachaType, param.Page));
            var list = await GetGachaLogByQueryAsync<T>(prefix, param, cancellationToken);
            result.AddRange(list);
            if (list.Count == 5 && list.Last().Id > endId)
            {
                param.Page++;
                param.EndId = list.Last().Id;
            }
            else
            {
                break;
            }
        }
        return result;
    }




}


