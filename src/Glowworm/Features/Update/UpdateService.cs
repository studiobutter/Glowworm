using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;

namespace Glowworm.Features.Update;

internal class UpdateService
{
    private readonly ILogger<UpdateService> _logger;
    private UpdateManager? _updateManager;

    public static bool UpdateFinished { get; private set; }
    public UpdateState State { get; private set; }
    public string? ErrorMessage { get; private set; }
    public long Progress_TotalBytes { get; private set; }
    public long Progress_DownloadBytes { get; private set; }
    
    private bool _isUpdating;
    private CancellationTokenSource? _cancellationTokenSource;

    public UpdateService(ILogger<UpdateService> logger)
    {
        _logger = logger;
    }

    private void EnsureUpdateManager()
    {
        if (_updateManager != null) return;
        
        string channel = AppConfig.EnablePreviewRelease ? "preview" : "stable";
        
        if (AppConfig.UpdateSource == 1) // Cloudflare
        {
            string url = $"https://update.studiobutter.io.vn/glowworm/{channel}";
            _updateManager = new UpdateManager(new SimpleWebSource(url));
        }
        else // GitHub
        {
            _updateManager = new UpdateManager(new GithubSource("https://github.com/studiobutter/Glowworm", null, AppConfig.EnablePreviewRelease));
        }
    }

    public async Task<UpdateInfo?> CheckUpdateAsync(bool disableIgnore = false)
    {
        EnsureUpdateManager();
        if (_updateManager == null || !_updateManager.IsInstalled)
        {
            _logger.LogInformation("Glowworm is not installed via Velopack.");
            return null;
        }

        try
        {
            var updateInfo = await _updateManager.CheckForUpdatesAsync();
            if (updateInfo != null)
            {
                _logger.LogInformation("Update found: {Version}", updateInfo.TargetFullRelease.Version);
                if (!disableIgnore && AppConfig.IgnoreVersion == updateInfo.TargetFullRelease.Version.ToString())
                {
                    return null;
                }
                return updateInfo;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CheckForUpdatesAsync failed");
        }
        return null;
    }

    public async Task<UpdateInfo?> GetLatestVersionAsync(CancellationToken cancellation = default)
    {
        return await CheckUpdateAsync(true);
    }

    public async Task StartUpdateAsync(UpdateInfo release)
    {
        if (_isUpdating || UpdateFinished)
        {
            State = UpdateFinished ? UpdateState.Finish : State;
            return;
        }

        try
        {
            ClearState();
            _isUpdating = true;
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = new CancellationTokenSource();
            State = UpdateState.Pending;

            EnsureUpdateManager();
            if (_updateManager == null || !_updateManager.IsInstalled)
            {
                ErrorMessage = "Cannot update automatically (not installed via Velopack).";
                State = UpdateState.NotSupport;
                return;
            }

            await StartInternalAsync(release, _cancellationTokenSource.Token);

            if (State is UpdateState.Finish)
            {
                UpdateFinished = true;
                if (AppConfig.AutoRestartWhenUpdateFinished)
                {
                    _updateManager.ApplyUpdatesAndRestart(release);
                }
            }
            else if (State is not UpdateState.Finish and not UpdateState.Error)
            {
                _logger.LogWarning("Update stopped with unexpected state: {state}", State);
                State = UpdateState.Stop;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Start update");
            State = UpdateState.Error;
            ErrorMessage = ex.Message;
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private async Task StartInternalAsync(UpdateInfo release, CancellationToken cancellationToken = default)
    {
        try
        {
            State = UpdateState.Downloading;
            
            await _updateManager!.DownloadUpdatesAsync(release, progress =>
            {
                Progress_DownloadBytes = progress;
                Progress_TotalBytes = 100;
            });

            State = UpdateState.Finish;
            _logger.LogInformation("Update finished downloaded.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Start update internal");
            State = UpdateState.Error;
            ErrorMessage = ex.Message;
        }
    }

    public void StopUpdate()
    {
        _cancellationTokenSource?.Cancel();
    }

    private void ClearState()
    {
        State = UpdateState.Stop;
        Progress_TotalBytes = 0;
        Progress_DownloadBytes = 0;
        ErrorMessage = null;
    }
}
