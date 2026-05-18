using System;
using System.Diagnostics;
using System.Timers;

namespace Glowworm.Features.Update;

internal static class LogUploadService
{

    private static LogUploadClient _client;

    private static Timer _timer;

    private static DateTimeOffset _startTime;

    private static bool _started;


    public static void Start()
    {
        try
        {
            _timer = new Timer(TimeSpan.FromMinutes(5));
            _timer.Elapsed += UploadLog;
            _timer.Start();
            _startTime = DateTimeOffset.Now;
            UploadLog(null, null!);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }



    public static void Stop()
    {
        try
        {
            _timer?.Stop();
            _timer?.Dispose();
            if (_started)
            {
                _ = _client?.UploadLogAsync(new LogUploadRequestBase("Runtime", "Stop"));
            }
        }
        catch { }
    }



    private static async void UploadLog(object? sender, ElapsedEventArgs e)
    {
        try
        {
            if (_client is null)
            {
                _client = AppConfig.GetService<LogUploadClient>();
#if DEBUG
                _client.AppName = "Glowworm.Debug";
#else
                _client.AppName = "Glowworm";
#endif
                _client.AppVersion = AppConfig.AppVersion;
                _client.DeviceId = AppConfig.DeviceId.ToString();
                _client.SessionId = AppConfig.SessionId.ToString();
            }
            if (!_started)
            {
                await _client.UploadLogAsync(new LogUploadRequestBase("Runtime", "Start") { Time = _startTime });
                _started = true;
            }
            await _client.UploadLogAsync(new LogUploadRequestBase("Runtime", "Running"));
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }


}



