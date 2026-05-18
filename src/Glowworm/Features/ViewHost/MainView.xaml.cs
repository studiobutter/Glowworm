using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using NuGet.Versioning;
using Glowworm.Core;
using Glowworm.Features.Gacha;
using Glowworm.Features.Screenshot;
using Glowworm.Features.Setting;
using Glowworm.Features.Update;
using Glowworm.Helpers;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;


namespace Glowworm.Features.ViewHost;

[INotifyPropertyChanged]
public sealed partial class MainView : UserControl
{


    private readonly ILogger<MainView> _logger = AppConfig.GetLogger<MainView>();


    public GameId? CurrentGameId { get; private set => SetProperty(ref field, value); }


    private GameFeatureConfig CurrentGameFeatureConfig { get; set; }



    public MainView()
    {
        this.InitializeComponent();
        InitializeMainView();
    }



    private void InitializeMainView()
    {
        this.Loaded += MainView_Loaded;
        CurrentGameId = null;
        CurrentGameFeatureConfig = GameFeatureConfig.FromGameId(CurrentGameId);
        UpdateNavigationView();
        WeakReferenceMessenger.Default.Register<MainViewNavigateMessage>(this, OnMainViewNavigateMessageReceived);
        WeakReferenceMessenger.Default.Register<MainWindowStateChangedMessage>(this, (_, _) => _ = CheckUpdateOrShowRecentUpdateContentAsync());
        WeakReferenceMessenger.Default.Register<GameChangedMessage>(this, (_, m) => OnGameChanged(m.NewBiz));
    }


    private void OnGameChanged(GameBiz biz)
    {
        CurrentGameId = GameId.FromGameBiz(biz);
        CurrentGameFeatureConfig = GameFeatureConfig.FromGameId(CurrentGameId);
        UpdateNavigationView();
    }


    private async void MainView_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        CheckSystemProxy();
        HotkeyManager.InitializeHotkey(this.XamlRoot.GetWindowHandle());
        _ = CheckUpdateOrShowRecentUpdateContentAsync();
        // AppConfig.GetService<RpcService>().TrySetEnviromentAsync();
        
        // Auto detect game paths if not set
        foreach (var biz in GameBiz.AllGameBizs)
        {
            if (string.IsNullOrWhiteSpace(AppConfig.GetGameInstallPathRemovable(biz)))
            {
                var path = GameRegistryHelper.GetGameInstallPath(biz);
                if (path != null)
                {
                    AppConfig.SetGameInstallPath(biz, path);
                }
            }
        }

        if (!AppConfig.HasWelcomed)
        {
            AppConfig.HasWelcomed = true;
            await new ContentDialog
            {
                Title = Lang.MainView_WelcomeTitle,
                Content = Lang.MainView_WelcomeContent,
                PrimaryButtonText = Lang.Common_Confirm,
                XamlRoot = this.XamlRoot
            }.ShowAsync();
            
            var firstAvailableBiz = GameBiz.AllGameBizs.FirstOrDefault(biz => biz.Game == GameBiz.hk4e && GameRegistryHelper.GetGameInstallPath(biz) != null);
            firstAvailableBiz = firstAvailableBiz != default ? firstAvailableBiz : GameBiz.AllGameBizs.FirstOrDefault(biz => biz.Game == GameBiz.hkrpg && GameRegistryHelper.GetGameInstallPath(biz) != null);
            firstAvailableBiz = firstAvailableBiz != default ? firstAvailableBiz : GameBiz.AllGameBizs.FirstOrDefault(biz => biz.Game == GameBiz.nap && GameRegistryHelper.GetGameInstallPath(biz) != null);

            if (firstAvailableBiz != default && string.IsNullOrWhiteSpace(firstAvailableBiz.Value) == false)
            {
                AppConfig.CurrentGameBiz = firstAvailableBiz;
                WeakReferenceMessenger.Default.Send(new GameChangedMessage(firstAvailableBiz));
                NavigateTo(typeof(ScreenshotPage));
            }
        }
        else
        {
            var lastBiz = AppConfig.CurrentGameBiz;
            if (lastBiz != default && !string.IsNullOrWhiteSpace(lastBiz.Value))
            {
                WeakReferenceMessenger.Default.Send(new GameChangedMessage(lastBiz));
            }
        }
    }


    #region Navigation





    private void UpdateNavigationView()
    {
        NavigationViewItem_Screenshot.Visibility = CurrentGameFeatureConfig.SupportedPages.Contains(nameof(ScreenshotPage)).ToVisibility();
        NavigationViewItem_GachaLog.Visibility = CurrentGameFeatureConfig.SupportedPages.Contains(nameof(GachaLogPage)).ToVisibility();
        NavigationViewItem_GenshinBeyondGacha.Visibility = CurrentGameFeatureConfig.SupportedPages.Contains(nameof(GenshinBeyondGachaPage)).ToVisibility();

        if (CurrentGameId is null)
        {
            NavigateTo(typeof(BlankPage));
        }
        else if (MainView_Frame.SourcePageType?.Name is not nameof(SettingPage))
        {
            NavigateTo(MainView_Frame.SourcePageType);
        }
    }



    private void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        try
        {
            if (args.InvokedItemContainer?.IsSelected ?? false)
            {
                return;
            }
            if (args.IsSettingsInvoked)
            {
                NavigateTo(typeof(SettingPage));
            }
            else
            {
                if (args.InvokedItemContainer is NavigationViewItem item)
                {
                    var type = item.Tag switch
                    {
                        nameof(ScreenshotPage) => typeof(ScreenshotPage),
                        nameof(GachaLogPage) => typeof(GachaLogPage),
                        nameof(GenshinBeyondGachaPage) => typeof(GenshinBeyondGachaPage),
                        _ => null,
                    };
                    NavigateTo(type);
                }
            }
        }
        catch { }
    }



    private void NavigateTo(Type? page, object? param = null, NavigationTransitionInfo? infoOverride = null)
    {
        page ??= typeof(ScreenshotPage);
        if (page.Name is nameof(BlankPage) && CurrentGameId is null)
        {

        }
        else if (page.Name is not nameof(SettingPage) && !CurrentGameFeatureConfig.SupportedPages.Contains(page.Name))
        {
            page = typeof(ScreenshotPage);
        }
        MainView_Frame.Navigate(page, param ?? CurrentGameId, infoOverride);
        if (page.Name is nameof(BlankPage))
        {
            Border_OverlayMask.Opacity = 0;
        }
        else
        {
            Border_OverlayMask.Opacity = 1;
        }
    }



    private void OnMainViewNavigateMessageReceived(object _, MainViewNavigateMessage message)
    {
        NavigateTo(message.Page);
    }




    #endregion




    #region Update


    private DateTimeOffset _lastCheckUpdateTime;

    private DateTimeOffset _lastShowUpdateTime;

    private SemaphoreSlim _updateLock = new(1, 1);


    private async Task CheckUpdateOrShowRecentUpdateContentAsync()
    {
#if DEBUG || DONOT_CHECK_UPDATE
        return;
#endif
#pragma warning disable CS0162 // ??????????
        if (!await _updateLock.WaitAsync(0))
        {
            return;
        }
        await Task.Delay(1000);
#pragma warning restore CS0162 // ??????????
        try
        {
            if (_lastCheckUpdateTime == default && NuGetVersion.TryParse(AppConfig.AppVersion, out var appVersion))
            {
                _ = NuGetVersion.TryParse(AppConfig.LastAppVersion, out var lastVersion);
                if (appVersion != lastVersion)
                {
                    if (AppConfig.ShowUpdateContentAfterUpdateRestart)
                    {
                        new UpdateWindow().Activate();
                    }
                    else
                    {
                        AppConfig.LastAppVersion = AppConfig.AppVersion;
                    }
                    _lastCheckUpdateTime = DateTimeOffset.Now - TimeSpan.FromMinutes(55);
                    return;
                }
            }
            DateTimeOffset now = DateTimeOffset.Now;
            if (now - _lastCheckUpdateTime > TimeSpan.FromHours(1))
            {
                var release = await AppConfig.GetService<UpdateService>().CheckUpdateAsync(false);
                _lastCheckUpdateTime = now;
                if (release != null && now - _lastShowUpdateTime > TimeSpan.FromHours(6) && now.Date != _lastShowUpdateTime.Date)
                {
                    new UpdateWindow { NewVersion = release }.Activate();
                    _lastShowUpdateTime = now;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Check update");
        }
        finally
        {
            _updateLock.Release();
        }
    }


    #endregion




    private async void CheckSystemProxy()
    {
        try
        {
            await Task.Delay(1500);
            Uri? proxy = HttpClient.DefaultProxy.GetProxy(new Uri("https://update.studiobutter.io.vn/glowworm"));
            if (proxy is not null)
            {
                InAppToast.MainWindow?.Information("", proxy.ToString(), 5000);
            }
        }
        catch { }
    }


}



file static class BoolToVisibilityExtension
{

    public static Visibility ToVisibility(this bool value)
    {
        return value ? Visibility.Visible : Visibility.Collapsed;
    }

}




