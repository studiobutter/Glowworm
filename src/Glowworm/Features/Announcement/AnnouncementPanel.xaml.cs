using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Glowworm.Features.Setting;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace Glowworm.Features.Announcement;

[INotifyPropertyChanged]
public sealed partial class AnnouncementPanel : UserControl
{
    private readonly AnnouncementService _service = AppConfig.GetService<AnnouncementService>();
    private readonly ILogger<AnnouncementPanel> _logger = AppConfig.GetLogger<AnnouncementPanel>();

    public ObservableCollection<AnnouncementItem> Announcements { get; } = new();

    public bool IsLoading { get; private set => SetProperty(ref field, value); }

    public Visibility ShowEmptyState { get; private set => SetProperty(ref field, value); } = Visibility.Collapsed;

    public string LastUpdatedText { get; private set => SetProperty(ref field, value); } = string.Empty;

    public string EmptyStateText { get; private set => SetProperty(ref field, value); } = Lang.Announcement_Empty;


    public AnnouncementPanel()
    {
        this.InitializeComponent();
        this.Loaded += AnnouncementPanel_Loaded;
        WeakReferenceMessenger.Default.Register<LanguageChangedMessage>(this, (_, _) =>
        {
            this.Bindings.Update();
            _ = RefreshAsync(forceRefresh: false);
        });
    }


    private async void AnnouncementPanel_Loaded(object sender, RoutedEventArgs e)
    {
        await RefreshAsync(forceRefresh: false);
    }


    public async Task RefreshAsync(bool forceRefresh = true)
    {
        try
        {
            IsLoading = true;
            ShowEmptyState = Visibility.Collapsed;
            EmptyStateText = Lang.Announcement_Empty;
            var items = await _service.GetAnnouncementsAsync(forceRefresh);
            Announcements.Clear();
            foreach (var item in items)
            {
                Announcements.Add(item);
            }
            ShowEmptyState = Announcements.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            LastUpdatedText = string.Format(Lang.Announcement_LastUpdated, DateTimeOffset.Now.ToString("HH:mm"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load announcements for the panel");
            EmptyStateText = Lang.Announcement_LoadFailed;
            ShowEmptyState = Announcements.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        finally
        {
            IsLoading = false;
        }
    }


    private async void Button_Refresh_Click(object sender, RoutedEventArgs e)
    {
        await RefreshAsync(forceRefresh: true);
    }
}
