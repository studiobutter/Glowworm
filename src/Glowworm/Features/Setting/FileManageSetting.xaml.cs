using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppLifecycle;
using SharpSevenZip;
using Glowworm.Features.Database;
using Glowworm.Frameworks;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Glowworm.Helpers;
using Windows.Storage;
using Windows.System;


namespace Glowworm.Features.Setting;

public sealed partial class FileManageSetting : PageBase
{

    private readonly ILogger<FileManageSetting> _logger = AppConfig.GetLogger<FileManageSetting>();


    public FileManageSetting()
    {
        this.InitializeComponent();
    }

    public string GenshinImpactText => Core.Localization.CoreLang.Game_GenshinImpact;
    public string HonkaiStarRailText => Core.Localization.CoreLang.Game_HonkaiStarRail;
    public string ChinaCloudText => Core.Localization.CoreLang.GameServer_ChinaCloud;

    public bool HideGenshinCloudChina
    {
        get => AppConfig.HideGenshinCloudChina;
        set
        {
            if (AppConfig.HideGenshinCloudChina != value)
            {
                AppConfig.HideGenshinCloudChina = value;
                OnPropertyChanged(nameof(HideGenshinCloudChina));
            }
        }
    }

    public bool HideStarRailCloudChina
    {
        get => AppConfig.HideStarRailCloudChina;
        set
        {
            if (AppConfig.HideStarRailCloudChina != value)
            {
                AppConfig.HideStarRailCloudChina = value;
                OnPropertyChanged(nameof(HideStarRailCloudChina));
            }
        }
    }

    protected override void OnLoaded()
    {
        GetLastBackupTime();
        _ = UpdateCacheSizeAsync();
    }




    #region ?????



    /// <summary>
    /// ???????
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    private async Task ChangeUserDataFolderAsync()
    {
        try
        {
            var dialog = new ContentDialog
            {
                Title = Lang.Common_Attention,
                // Inform user and confirm (choose to change folder)
                Content = $"""
                {Lang.SettingPage_TheCurrentLocationOfTheDataFolderIs}

                {AppConfig.UserDataFolder}

                {Lang.SettingPage_WouldLikeToReselectDataFolder}
                """,
                PrimaryButtonText = Lang.Common_ChangeFolder,
                SecondaryButtonText = Lang.Common_Cancel,
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot,
            };
            var result = await dialog.ShowAsync();
            if (result is ContentDialogResult.Primary)
            {
                try
                {
                    // Let user pick a new folder. If canceled, do nothing.
                    var path = await FileDialogHelper.PickFolderAsync(this.XamlRoot.GetWindowHandle());
                    if (string.IsNullOrWhiteSpace(path))
                    {
                        return;
                    }

                    // Validate folder can be created/written to before applying
                    try
                    {
                        Directory.CreateDirectory(path);
                        string testFile = Path.Combine(path, $"write_test_{Guid.NewGuid():N}.tmp");
                        File.WriteAllBytes(testFile, new byte[] { 1 });
                        File.Delete(testFile);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Selected folder is not writable: {path}", path);
                        // inform user via toast and abort
                        InAppToast.MainWindow?.Error((string?)null, string.Format(Lang.FileSettingPage_SelectedFolderNotWritable, path), 5000);
                        return;
                    }

                    AppConfig.UserDataFolder = path;
                    AppConfig.SaveConfiguration();
                    // Restart application so new data folder is applied
                    AppInstance.GetCurrent().UnregisterKey();
                    Process.Start(AppConfig.GlowwormExecutePath);
                    App.Current.Exit();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Change data folder - picking folder");
                    // do not restart if picker failed
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Change data folder");
        }
    }



    [RelayCommand]
    private void RescanGameDirectories()
    {
        try
        {
            int count = 0;
            foreach (var biz in Core.GameBiz.AllGameBizs)
            {
                var path = Core.GameRegistryHelper.GetGameInstallPath(biz);
                if (path != null)
                {
                    AppConfig.SetGameInstallPath(biz, path);
                    count++;
                }
            }
            InAppToast.MainWindow?.Success(null, $"Rescanned {count} game directories.", 5000);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Rescan game directories");
        }
    }



    /// <summary>
    /// ???????
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    private async Task OpenUserDataFolderAsync()
    {
        try
        {
            if (Directory.Exists(AppConfig.UserDataFolder))
            {
                await Launcher.LaunchUriAsync(new Uri(AppConfig.UserDataFolder));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Open user data folder");
        }
    }


    /// <summary>
    /// ??????
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    private async Task DeleteAllSettingAsync()
    {
        try
        {
            var dialog = new ContentDialog
            {
                Title = Lang.SettingPage_DeleteAllSettings,
                // ?????,????????
                Content = Lang.SettingPage_AfterDeletingTheSoftwareWillBeRestartedAutomatically,
                PrimaryButtonText = Lang.Common_Delete,
                SecondaryButtonText = Lang.Common_Cancel,
                DefaultButton = ContentDialogButton.Secondary,
                XamlRoot = this.XamlRoot,
            };
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                AppConfig.DeleteAllSettings();
                AppInstance.GetCurrent().UnregisterKey();
                Process.Start(AppConfig.GlowwormExecutePath);
                App.Current.Exit();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete all setting");
        }

    }



    private void TextBlock_IsTextTrimmedChanged(TextBlock sender, IsTextTrimmedChangedEventArgs args)
    {
        if (sender.FontSize > 12)
        {
            sender.FontSize -= 1;
        }
    }



    #endregion




    #region ?????



    public string LastDatabaseBackupTime { get; set => SetProperty(ref field, value); }


    private void GetLastBackupTime()
    {
        try
        {
            if (DatabaseService.TryGetValue("LastBackupDatabase", out string? file, out DateTime time))
            {
                file = Path.Join(AppConfig.UserDataFolder, "DatabaseBackup", file);
                if (File.Exists(file))
                {
                    LastDatabaseBackupTime = $"{""}  {time:yyyy-MM-dd HH:mm:ss}";
                }
                else
                {
                    _logger.LogWarning("Last backup database file not found: {file}", file);
                }
            }
        }
        catch { }
    }



    [RelayCommand]
    private async Task BackupDatabaseAsync()
    {
        try
        {
            if (Directory.Exists(AppConfig.UserDataFolder))
            {
                var folder = Path.Combine(AppConfig.UserDataFolder, "DatabaseBackup");
                Directory.CreateDirectory(folder);
                DateTime time = DateTime.Now;
                await Task.Run(() =>
                {
                    string file = Path.Combine(folder, $"GlowwormDatabase_{time:yyyyMMdd_HHmmss}.db");
                    string archive = Path.ChangeExtension(file, ".7z");
                    DatabaseService.BackupDatabase(file);
                    new SharpSevenZipCompressor().CompressFiles(archive, file);
                    DatabaseService.SetValue("LastBackupDatabase", Path.GetFileName(archive), time);
                    File.Delete(file);
                });
                LastDatabaseBackupTime = $"{""}  {time:yyyy-MM-dd HH:mm:ss}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backup database");
            LastDatabaseBackupTime = ex.Message;
        }
    }



    [RelayCommand]
    private async Task OpenLastBackupDatabaseAsync()
    {
        try
        {
            if (DatabaseService.TryGetValue("LastBackupDatabase", out string? file, out DateTime time))
            {
                file = Path.Join(AppConfig.UserDataFolder, "DatabaseBackup", file);
                if (File.Exists(file))
                {
                    var item = await StorageFile.GetFileFromPathAsync(file);
                    var folder = await StorageFolder.GetFolderFromPathAsync(Path.GetDirectoryName(file));
                    var options = new FolderLauncherOptions
                    {
                        ItemsToSelect = { item }
                    };
                    await Launcher.LaunchFolderAsync(folder, options);
                }
                else
                {
                    _logger.LogWarning("Last backup database file not found: {file}", file);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Open last backup database");
        }
    }







    #endregion




    #region Log



    /// <summary>
    /// ???????
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    private async Task OpenLogFolderAsync()
    {
        try
        {
            if (File.Exists(AppConfig.LogFile))
            {
                var item = await StorageFile.GetFileFromPathAsync(AppConfig.LogFile);
                var options = new FolderLauncherOptions();
                options.ItemsToSelect.Add(item);
                await Launcher.LaunchFolderPathAsync(Path.GetDirectoryName(AppConfig.LogFile), options);
            }
            else
            {
                await Launcher.LaunchFolderPathAsync(Path.Combine(AppConfig.CacheFolder, "log"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Open log folder");
        }
    }



    #endregion




    #region Cache



    public string LogCacheSize { get; set => SetProperty(ref field, value); } = "0.00 KB";

    public string ImageCacheSize { get; set => SetProperty(ref field, value); } = "0.00 KB";

    public string WebCacheSize { get; set => SetProperty(ref field, value); } = "0.00 KB";

    public string GameCacheSize { get; set => SetProperty(ref field, value); } = "0.00 KB";


    /// <summary>
    /// ??????
    /// </summary>
    /// <returns></returns>
    private async Task UpdateCacheSizeAsync()
    {
        try
        {
            var local = AppConfig.CacheFolder;
            LogCacheSize = await GetFolderSizeStringAsync(Path.Combine(local, "log"));
            ImageCacheSize = await GetFolderSizeStringAsync(Path.Combine(local, "cache"));
            WebCacheSize = await GetFolderSizeStringAsync(Path.Combine(local, "webview"));
            GameCacheSize = await GetFolderSizeStringAsync(Path.Combine(local, "game"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update cache size");
        }
    }



    private static async Task<string> GetFolderSizeStringAsync(string folder) => await Task.Run(() =>
    {
        if (Directory.Exists(folder))
        {
            double size = Directory.GetFiles(folder, "*", SearchOption.AllDirectories).Sum(file => new FileInfo(file).Length);
            if (size < (1 << 20))
            {
                return $"{size / (1 << 10):F2} KB";
            }
            else
            {
                return $"{size / (1 << 20):F2} MB";
            }
        }
        else
        {
            return "0.00 KB";
        }
    });



    /// <summary>
    /// ????
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    private async Task ClearCacheAsync()
    {
        try
        {
            var local = AppConfig.CacheFolder;
            await DeleteFolderAsync(Path.Combine(local, "log"));
            await DeleteFolderAsync(Path.Combine(local, "crash"));
            await DeleteFolderAsync(Path.Combine(local, "cache"));
            await DeleteFolderAsync(Path.Combine(local, "webview"));
            await DeleteFolderAsync(Path.Combine(local, "update"));
            await DeleteFolderAsync(Path.Combine(local, "game"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Clear cache");
        }
        await UpdateCacheSizeAsync();
    }



    private async Task DeleteFolderAsync(string folder) => await Task.Run(() =>
    {
        if (Directory.Exists(folder))
        {
            try
            {
                Directory.Delete(folder, true);
                Directory.CreateDirectory(folder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Delete folder '{folder}'", folder);
            }
        }
    });


    #endregion



}




