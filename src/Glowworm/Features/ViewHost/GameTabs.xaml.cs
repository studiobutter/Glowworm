using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Glowworm.Core;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.Messaging;
using Glowworm.Features.Database;
using Glowworm.Features.Setting;

namespace Glowworm.Features.ViewHost;

public sealed partial class GameTabs : UserControl
{
    public ObservableCollection<GameTabItem> GameList { get; } = new();

    public GameTabs()
    {
        this.InitializeComponent();
        InitializeGameList();
        WeakReferenceMessenger.Default.Register<LanguageChangedMessage>(this, (_, _) =>
        {
            InitializeGameList();
            this.Bindings.Update();
        });
    }

    private void InitializeGameList()
    {
        GameList.Clear();
        GameList.Add(new GameTabItem 
        { 
            Game = GameBiz.hk4e, 
            Icon = "ms-appx:///Assets/icon_ys.ico",
            Regions = new List<GameBiz> { GameBiz.hk4e_cn, GameBiz.hk4e_global, GameBiz.hk4e_bilibili, GameBiz.hk4e_google, GameBiz.hk4e_epic }
        });
        GameList.Add(new GameTabItem 
        { 
            Game = GameBiz.hkrpg, 
            Icon = "ms-appx:///Assets/icon_sr.ico",
            Regions = new List<GameBiz> { GameBiz.hkrpg_cn, GameBiz.hkrpg_global, GameBiz.hkrpg_bilibili, GameBiz.hkrpg_epic }
        });
        GameList.Add(new GameTabItem 
        { 
            Game = GameBiz.nap, 
            Icon = "ms-appx:///Assets/icon_zzz.ico",
            Regions = new List<GameBiz> { GameBiz.nap_cn, GameBiz.nap_global, GameBiz.nap_bilibili, GameBiz.nap_epic }
        });
    }

    private void Button_GameTab_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is GameTabItem item)
        {
            var lastBiz = AppConfig.GetLastRegionOfGame(item.Game);
            if (lastBiz != GameBiz.None && item.Regions.Contains(lastBiz))
            {
                SwitchToGame(lastBiz);
            }
            else
            {
                var firstAvailable = item.Regions.FirstOrDefault(r => !string.IsNullOrWhiteSpace(GameRegistryHelper.GetGameInstallPath(r)));
                if (firstAvailable != default)
                {
                    SwitchToGame(firstAvailable);
                }
                else
                {
                    SwitchToGame(item.Regions.First());
                }
            }
        }
    }

    private void Button_SwitchRegion_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is DropDownButton btn && btn.DataContext is GameTabItem item)
        {
            var flyout = btn.Flyout as MenuFlyout;
            if (flyout != null)
            {
                flyout.Items.Clear();
                foreach (var region in item.Regions)
                {
                    if (string.IsNullOrWhiteSpace(GameRegistryHelper.GetGameInstallPath(region))) continue;
                    var menuitem = new MenuFlyoutItem
                    {
                        Text = region.ToGameServerName(),
                        Tag = region
                    };
                    menuitem.Click += MenuFlyoutItem_Click;
                    flyout.Items.Add(menuitem);
                }
            }
        }
    }

    private void SwitchToGame(GameBiz biz)
    {
        AppConfig.CurrentGameBiz = biz;
        AppConfig.SetLastRegionOfGame(biz);
        WeakReferenceMessenger.Default.Send(new GameChangedMessage(biz));
    }

    private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem { Tag: GameBiz biz })
        {
            SwitchToGame(biz);
        }
    }
}

public class GameTabItem
{
    public string Name => new GameBiz(Game).ToGameName();
    public string Game { get; set; }
    public string Icon { get; set; }
    public List<GameBiz> Regions { get; set; }
}

public record GameChangedMessage(GameBiz NewBiz);
