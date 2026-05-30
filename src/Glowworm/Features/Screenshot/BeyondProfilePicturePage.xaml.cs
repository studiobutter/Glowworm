using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Glowworm.Core;
using Glowworm.Frameworks;
using Glowworm.Helpers;
using Glowworm.Language;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.System.UserProfile;

namespace Glowworm.Features.Screenshot;

public sealed partial class BeyondProfilePicturePage : PageBase
{
    private readonly ILogger<BeyondProfilePicturePage> _logger = AppConfig.GetLogger<BeyondProfilePicturePage>();

    public ObservableCollection<ScreenshotItem> ProfilePictures { get; set => SetProperty(ref field, value); }

    private string _backupFolder;

    public BeyondProfilePicturePage()
    {
        this.InitializeComponent();
        _backupFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Glowworm", "BeyondProfilePictures");
        Directory.CreateDirectory(_backupFolder);
    }

    protected override async void OnLoaded()
    {
        await LoadImagesAsync();
    }

    [RelayCommand]
    private async Task GetProfilePicturesAsync()
    {
        try
        {
            string localAppDataLow = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "LocalLow");
            string[] searchPaths = 
            [
                Path.Combine(localAppDataLow, "miHoYo", "Genshin Impact", "webres", "BeyondSelfProfile"),
                Path.Combine(localAppDataLow, "miHoYo", "原神", "webres", "BeyondSelfProfile")
            ];

            bool foundAnyFile = false;
            foreach (var path in searchPaths)
            {
                if (Directory.Exists(path))
                {
                    var files = Directory.GetFiles(path, "NormalSizeBeyondProfile_*");
                    if (files.Length > 0)
                    {
                        foundAnyFile = true;
                    }
                    foreach (var file in files)
                    {
                        if (Path.GetExtension(file) == string.Empty) // Files do not have extension
                        {
                            string fileName = Path.GetFileName(file);
                            string destFile = Path.Combine(_backupFolder, fileName + ".png");
                            if (!File.Exists(destFile))
                            {
                                File.Copy(file, destFile, true);
                            }
                        }
                    }
                }
            }

            if (!foundAnyFile)
            {
                InAppToast.MainWindow?.Error(Lang.BeyondProfilePicturePage_FolderNotFound);
                return;
            }

            await LoadImagesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get profile pictures.");
            InAppToast.MainWindow?.Error(Lang.Common_Error);
        }
    }

    private async Task LoadImagesAsync()
    {
        try
        {
            var files = Directory.GetFiles(_backupFolder, "*.png");
            var items = files.Select(f => new ScreenshotItem(f)).OrderByDescending(x => x.CreationTime).ToList();
            ProfilePictures = new ObservableCollection<ScreenshotItem>(items);
            
            if (ProfilePictures.Count == 0)
            {
                StackPanel_EmptyState.Visibility = Visibility.Visible;
                ItemsView_Images.Visibility = Visibility.Collapsed;
            }
            else
            {
                StackPanel_EmptyState.Visibility = Visibility.Collapsed;
                ItemsView_Images.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load images.");
        }
    }

    private void ItemsView_Images_ItemInvoked(ItemsView sender, ItemsViewItemInvokedEventArgs args)
    {
        if (args.InvokedItem is ScreenshotItem item)
        {
            _ = new ImageViewWindow2().ShowWindowAsync(this.XamlRoot.ContentIslandEnvironment.AppWindowId, item, ProfilePictures);
        }
    }

    private async void Button_CopyImage_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        e.Handled = true;
        if (sender is Button button && button.DataContext is ScreenshotItem item)
        {
            try
            {
                var file = await StorageFile.GetFileFromPathAsync(item.FilePath);
                ClipboardHelper.SetStorageItems(DataPackageOperation.Copy, file);
                InAppToast.MainWindow?.Success(Lang.Common_CopiedToClipboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Copy image");
            }
        }
    }

    private async void Button_SaveAs_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        e.Handled = true;
        if (sender is Button button && button.DataContext is ScreenshotItem item)
        {
            try
            {
                var picker = new FileSavePicker();
                picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                picker.FileTypeChoices.Add("PNG Image", [".png"]);
                picker.SuggestedFileName = Path.GetFileName(item.FilePath);
                WinRT.Interop.InitializeWithWindow.Initialize(picker, this.XamlRoot.GetWindowHandle());
                
                var file = await picker.PickSaveFileAsync();
                if (file != null)
                {
                    var sourceFile = await StorageFile.GetFileFromPathAsync(item.FilePath);
                    await sourceFile.CopyAndReplaceAsync(file);
                    InAppToast.MainWindow?.Success(Lang.Common_Success);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Save As image");
            }
        }
    }

    private async void Button_SetAsProfilePicture_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        e.Handled = true;
        if (sender is Button button && button.DataContext is ScreenshotItem item)
        {
            try
            {
                var dialog = new ContentDialog
                {
                    Title = Lang.BeyondProfilePicturePage_SetProfilePictureConfirmTitle,
                    Content = Lang.BeyondProfilePicturePage_SetProfilePictureConfirmContent,
                    PrimaryButtonText = Lang.Common_Confirm,
                    CloseButtonText = Lang.Common_Cancel,
                    XamlRoot = this.XamlRoot,
                    DefaultButton = ContentDialogButton.Primary
                };
                
                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    var file = await StorageFile.GetFileFromPathAsync(item.FilePath);
                    var success = await UserInformation.SetAccountPictureAsync(file);
                    if (success == SetAccountPictureResult.Success)
                    {
                        InAppToast.MainWindow?.Success(Lang.Common_Success);
                    }
                    else
                    {
                        InAppToast.MainWindow?.Error(Lang.Common_Failed);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Set Profile Picture");
                InAppToast.MainWindow?.Error(Lang.Common_Failed);
            }
        }
    }

    private async void Grid_ImageItem_DragStarting(UIElement sender, DragStartingEventArgs args)
    {
        try
        {
            if (sender is FrameworkElement grid && grid.DataContext is ScreenshotItem item)
            {
                var deferral = args.GetDeferral();
                args.AllowedOperations = DataPackageOperation.Copy;
                var file = await StorageFile.GetFileFromPathAsync(item.FilePath);
                args.Data.SetStorageItems([file], true);
                deferral.Complete();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Drag image starting");
        }
    }
}
