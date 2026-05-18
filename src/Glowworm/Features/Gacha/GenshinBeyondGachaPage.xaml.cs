using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Glowworm.Core;
using Glowworm.Core.Gacha.Genshin;
using Glowworm.Features.Gacha.UIGF;
using Glowworm.Frameworks;
using Glowworm.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;


namespace Glowworm.Features.Gacha;

public sealed partial class GenshinBeyondGachaPage : PageBase
{


    private readonly ILogger<GenshinBeyondGachaPage> _logger = AppConfig.GetLogger<GenshinBeyondGachaPage>();

    private readonly GenshinBeyondGachaService _gachaLogService = AppConfig.GetService<GenshinBeyondGachaService>();



    public GenshinBeyondGachaPage()
    {
        InitializeComponent();
    }


    public ObservableCollection<long> UidList { get; set => SetProperty(ref field, value); }


    [ObservableProperty]
    public partial long? SelectUid { get; set; }
    partial void OnSelectUidChanged(long? value)
    {
        AppConfig.SetLastUidInGachaLogPage("hk4eugc", value ?? 0);
        UpdateGachaTypeStats(value);
    }




    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (CurrentGameBiz.IsGlobalServer())
        {
            MenuFlyoutItem_CloudGameWeb.Visibility = Visibility.Collapsed;
        }
    }


    protected override async void OnLoaded()
    {
        await Task.Delay(16);
        Initialize();
        await UpdateGachaInfoAsync();
    }



    protected override void OnUnloaded()
    {
        GachaStatsType1000 = null;
        GachaStatsType2000 = null;
        GachaItemStats = null;
    }



    private void Initialize()
    {
        try
        {
            SelectUid = null;
            UidList = new(_gachaLogService.GetUids());
            var lastUid = AppConfig.GetLastUidInGachaLogPage("hk4eugc");
            if (UidList.Contains(lastUid))
            {
                SelectUid = lastUid;
            }
            else
            {
                SelectUid = UidList.FirstOrDefault();
            }
            if (UidList.Count == 0)
            {
                StackPanel_Emoji.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Initialize");
        }
    }


    public GenshinBeyondGachaTypeStats? GachaStatsType1000 { get; set => SetProperty(ref field, value); }

    public GenshinBeyondGachaTypeStats? GachaStatsType2000 { get; set => SetProperty(ref field, value); }

    public List<GenshinBeyondGachaItemEx>? GachaItemStats { get; set => SetProperty(ref field, value); }


    private int errorCount = 0;


    private void UpdateGachaTypeStats(long? uid)
    {
        try
        {
            GachaStatsType1000 = null;
            GachaStatsType2000 = null;

            if (uid.HasValue && uid.Value != 0)
            {
                GachaStatsType1000 = _gachaLogService.GetGachaTypeStatsType1000(uid.Value);
                GachaStatsType2000 = _gachaLogService.GetGachaTypeStatsType2000(uid.Value);
                GachaItemStats = _gachaLogService.GetGachaItemStats(uid.Value);
            }

            if (GachaStatsType1000 is null && GachaStatsType2000 is null)
            {
                StackPanel_Emoji.Visibility = Visibility.Visible;
            }
            else
            {
                StackPanel_Emoji.Visibility = Visibility.Collapsed;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateGachaTypeStats");
        }
    }




    private async Task UpdateGachaInfoAsync()
    {
        try
        {
            await _gachaLogService.UpdateGachaInfoAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update wiki data hk4eugc");
        }
    }



    [RelayCommand]
    private async Task UpdateGachaLogAsync(string? param = null)
    {
        try
        {
            await UpdateGachaInfoAsync();
            string? url = null;
            if (param is "cache")
            {
                if (SelectUid is null or 0)
                {
                    return;
                }
                url = _gachaLogService.GetGachaLogUrlByUid(SelectUid.Value);
                if (string.IsNullOrWhiteSpace(url))
                {
                    // ???? uid {uid} ???? URL
                    InAppToast.MainWindow?.Warning(null, string.Format(Lang.GachaLogPage_CannotFindSavedURLOfUid, SelectUid));
                    return;
                }
            }
            else
            {
                var path = GameRegistryHelper.GetGameInstallPath(AppConfig.CurrentGameBiz);
                if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
                {
                    // ?????
                    InAppToast.MainWindow?.Warning(null, Lang.GachaLogPage_GameNotInstalled);
                    return;
                }
                url = _gachaLogService.GetGachaLogUrlFromWebCache(AppConfig.CurrentGameBiz, path);
                if (string.IsNullOrWhiteSpace(url))
                {
                    // ???? URL,?????????????
                    errorCount++;
                    if (errorCount > 2 && IsGachaCacheFileExists())
                    {
                        errorCount = 0;
                        InAppToast.MainWindow?.ShowWithButton(InfoBarSeverity.Warning,
                                                                     Lang.GachaLogPage_AuthkeyTimeoutAlwaysOccurs,
                                                                     null,
                                                                     Lang.GachaLogPage_ClearURLCacheFiles,
                                                                     () => _ = DeleteGachaCacheFileAsync());
                    }
                    else
                    {
                        InAppToast.MainWindow?.Warning(null, Lang.GachaLogPage_CannotFindURL);
                    }
                    return;
                }
            }
            await UpdateGachaLogInternalAsync(url, param is "all");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update gacha log");
            InAppToast.MainWindow?.Error(ex);
        }
    }



    private async Task UpdateGachaLogInternalAsync(string url, bool all = false)
    {
        try
        {
            var uid = await _gachaLogService.GetUidFromGachaLogUrl(url);
            var cancelSource = new CancellationTokenSource();
            var button = new Button
            {
                // ??
                Content = Lang.Common_Cancel,
                HorizontalAlignment = HorizontalAlignment.Right,
            };
            var infoBar = new InfoBar
            {
                Severity = InfoBarSeverity.Informational,
                Background = Application.Current.Resources["CustomAcrylicBrush"] as Brush,
                ActionButton = button,
            };
            button.Click += (_, _) =>
            {
                cancelSource.Cancel();
                // ?????
                infoBar.Message = Lang.GachaLogPage_OperationCanceled;
                infoBar.ActionButton = null;
            };
            InAppToast.MainWindow?.Show(infoBar);
            var progress = new Progress<string>((str) => infoBar.Message = str);
            var newUid = await _gachaLogService.GetGachaLogAsync(url, all, GachaLanguage, progress, cancelSource.Token);
            infoBar.Title = $"Uid {newUid}";
            infoBar.Severity = InfoBarSeverity.Success;
            infoBar.ActionButton = null;
            if (SelectUid == uid)
            {
                UpdateGachaTypeStats(uid);
            }
            else
            {
                if (!UidList.Contains(uid))
                {
                    UidList.Add(uid);
                }
                SelectUid = uid;
            }
        }
        catch (TaskCanceledException)
        {
            _logger.LogInformation("Get gacha log canceled");
        }
        catch (miHoYoApiException ex)
        {
            _logger.LogWarning("Request mihoyo api error: {error}", ex.Message);
            // ?? -101 ? -1
            if (ex.ReturnCode is -101 or -1)
            {
                // authkey timeout
                // ?????????????????
                errorCount++;
                if (errorCount > 1 && IsGachaCacheFileExists())
                {
                    errorCount = 0;
                    InAppToast.MainWindow?.ShowWithButton(InfoBarSeverity.Warning,
                                                                 Lang.GachaLogPage_AuthkeyTimeoutAlwaysOccurs,
                                                                 null,
                                                                 Lang.GachaLogPage_ClearURLCacheFiles,
                                                                 () => _ = DeleteGachaCacheFileAsync());
                }
                else
                {
                    InAppToast.MainWindow?.Warning("Authkey Timeout", Lang.GachaLogPage_CannotFindURL);
                }
            }
            else
            {
                InAppToast.MainWindow?.Warning(null, ex.Message);
            }
        }
    }



    [RelayCommand]
    private async Task InputUrlAsync()
    {
        try
        {
            var textbox = new TextBox();
            var dialog = new ContentDialog
            {
                // ?? URL
                Title = Lang.GachaLogPage_InputURL,
                Content = textbox,
                // ??
                PrimaryButtonText = Lang.Common_Confirm,                // ??
                SecondaryButtonText = Lang.Common_Cancel,
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot,
            };
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var url = textbox.Text;
                if (!string.IsNullOrWhiteSpace(url))
                {
                    await UpdateGachaLogInternalAsync(url);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Input url");
            InAppToast.MainWindow?.Error(ex);
        }
    }



    public string? GachaLanguage
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                AppConfig.GachaLanguage = value;
            }
        }
    } = AppConfig.GachaLanguage;



    [RelayCommand]
    private async Task CopyUrlAsync()
    {
        try
        {
            if (SelectUid is null or 0)
            {
                return;
            }
            var url = _gachaLogService.GetGachaLogUrlByUid(SelectUid.Value);
            if (!string.IsNullOrWhiteSpace(url))
            {
                ClipboardHelper.SetText(url);
                FontIcon_CopyUrl.Glyph = "\uE8FB"; // accept
                await Task.Delay(1000);
                FontIcon_CopyUrl.Glyph = "\uE8C8";  // copy
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Copy url");
        }
    }



    [RelayCommand]
    private async Task ChangeGachaItemNameAsync()
    {
        try
        {
            string lang = string.IsNullOrWhiteSpace(GachaLanguage) ? System.Globalization.CultureInfo.CurrentUICulture.Name : GachaLanguage;
            (lang, int count) = await _gachaLogService.ChangeGachaItemNameAsync(lang);
            InAppToast.MainWindow?.Success(null, string.Format(Lang.GachaLogPage_0GachaItemsHaveBeenChangedToLanguage1, count, lang), 5000);
            UpdateGachaTypeStats(SelectUid);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Change gacha item name");
        }
    }



    [RelayCommand]
    private async Task DeleteUidAsync()
    {
        try
        {
            var uid = SelectUid;
            if (uid is null or 0)
            {
                return;
            }
            var dialog = new ContentDialog
            {
                Title = Lang.Common_Delete,
                Content = string.Format(Lang.GachaLogPage_DeleteGachaRecordsWarning, uid),
                PrimaryButtonText = Lang.Common_Confirm,
                SecondaryButtonText = Lang.Common_Cancel,
                DefaultButton = ContentDialogButton.Secondary,
                XamlRoot = this.XamlRoot,
            };
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var count = _gachaLogService.DeleteUid(uid.Value);
                InAppToast.MainWindow?.Success(null, string.Format(Lang.GachaLogPage_DeletedGachaRecordsOfUid, count, uid));
                Initialize();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete uid");
            InAppToast.MainWindow?.Error(ex);
        }
    }



    [RelayCommand]
    private async Task DeleteUidByTimeAsync()
    {
        try
        {
            var dialog = new DeleteGachaLogDialog
            {
                CurrentGameBiz = GameBiz.hk4e_global, // Beyond gacha is only for Genshin
                DefaultUid = this.SelectUid,
                XamlRoot = this.XamlRoot,
            };
            var result = await dialog.ShowAsync();
            if (dialog.Deleted)
            {
                UpdateGachaTypeStats(dialog.SelectUid);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete uid");
        }
    }



    [RelayCommand]
    private async Task ExportGachaLogAsync(string format)
    {
        try
        {
            if (SelectUid is null or 0)
            {
                return;
            }
            long uid = SelectUid.Value;
            var ext = "json";
            var suggestName = $"Glowworm_Export_Beyond_{uid}_{DateTime.Now:yyyyMMddHHmmss}.{ext}";
            var file = await FileDialogHelper.OpenSaveFileDialogAsync(this.XamlRoot, suggestName, (ext, $".{ext}"));
            if (file is not null)
            {
                await _gachaLogService.ExportGachaLogAsync(uid, file, format);
                var storageFile = await StorageFile.GetFileFromPathAsync(file);
                var options = new FolderLauncherOptions();
                options.ItemsToSelect.Add(storageFile);
                await Launcher.LaunchFolderAsync(await storageFile.GetParentAsync(), options);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Export gacha log");
            InAppToast.MainWindow?.Error(ex);
        }
    }



    [RelayCommand]
    private void OpenUIGF4Window()
    {
        new UIGF4GachaWindow().Activate();
    }



    [RelayCommand]
    private async Task DeleteGachaCacheFileAsync()
    {
        try
        {
            var installPath = GameRegistryHelper.GetGameInstallPath(AppConfig.CurrentGameBiz);
            if (Directory.Exists(installPath))
            {
                var path = GenshinBeyondGachaClient.GetGachaCacheFilePath(CurrentGameBiz, installPath);
                if (File.Exists(path))
                {
                    var file = await StorageFile.GetFileFromPathAsync(path);
                    if (file != null)
                    {
                        var option = new FolderLauncherOptions();
                        option.ItemsToSelect.Add(file);
                        await Launcher.LaunchFolderAsync(await file.GetParentAsync(), option);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete gacha cache file");
        }
    }



    private bool IsGachaCacheFileExists()
    {
        try
        {
            var installPath = GameRegistryHelper.GetGameInstallPath(AppConfig.CurrentGameBiz);
            if (Directory.Exists(installPath))
            {
                var path = GenshinBeyondGachaClient.GetGachaCacheFilePath(CurrentGameBiz, installPath);
                return File.Exists(path);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Check gacha cache file exists");
        }
        return false;
    }



    [RelayCommand]
    private async Task UpdateGachaLogFromCloudGameAsync()
    {
        try
        {
            string? url = null;
            string company = CurrentGameBiz.IsGlobalServer() ? "HoYoverse" : "miHoYo";
            string logPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), company, "GenshinImpactCloudGame", "config", "logs", "MiHoYoSDK.log");
            string pattern = CurrentGameBiz.IsGlobalServer() ? "\"url\":\"https://gs.hoyoverse.com/genshin/event/e20250716gacha/" : "\"url\":\"https://webstatic.mihoyo.com/hk4e/event/e20250716gacha/";
            url = GetLastMatchingUrl(logPath, pattern);

            if (string.IsNullOrWhiteSpace(url))
            {
                InAppToast.MainWindow?.Warning(null, Lang.GachaLogPage_CannotFindURL);
                return;
            }

            await UpdateGachaLogInternalAsync(url, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update gacha log from cloud game");
            InAppToast.MainWindow?.Error(ex);
        }
    }

    private string? GetLastMatchingUrl(string logPath, string pattern)
    {
        if (!File.Exists(logPath))
        {
            return null;
        }

        string? matchingUrl = null;
        foreach (var line in File.ReadLines(logPath))
        {
            if (line.Contains(pattern))
            {
                matchingUrl = line;
            }
        }

        if (matchingUrl != null)
        {
            int startIndex = matchingUrl.IndexOf("\"url\":\"");
            if (startIndex >= 0)
            {
                startIndex += 7;
                int endIndex = matchingUrl.IndexOf("\"", startIndex);
                if (endIndex > startIndex)
                {
                    return matchingUrl.Substring(startIndex, endIndex - startIndex);
                }
            }
        }
        return null;
    }


    [RelayCommand]
    private void OpenCloudGameWindow()
    {
        try
        {
            new CloudGameGachaWindow { GameBiz = CurrentGameBiz }.Activate();
        }
        catch { }
    }


    [RelayCommand]
    private void OpenItemStatsPane()
    {
        SplitView_Content.IsPaneOpen = true;
    }


}
