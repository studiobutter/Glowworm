using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Glowworm.Features.ViewHost;
using Glowworm.Frameworks;
using System;
using System.Globalization;
using Windows.System;


namespace Glowworm.Features.Setting;

public sealed partial class GeneralSetting : PageBase
{

    private readonly ILogger<GeneralSetting> _logger = AppConfig.GetLogger<GeneralSetting>();


    public GeneralSetting()
    {
        this.InitializeComponent();
    }



    protected override void OnLoaded()
    {
        InitializeLanguageSelector();
        ToggleSwitch_Transparency.IsOn = AppConfig.EnableTransparency;
        ToggleSwitch_RunInSystemTray.IsOn = AppConfig.RunInSystemTray;
    }




    #region ??



    private bool _languageInitialized;


    /// <summary>
    /// ??
    /// </summary>
    private void InitializeLanguageSelector()
    {
        try
        {
            var lang = AppConfig.Language;
            ComboBox_Language.Items.Clear();
            ComboBox_Language.Items.Add(new ComboBoxItem
            {
                Content = Lang.ResourceManager.GetString("", CultureInfo.InstalledUICulture) ?? "System",
                Tag = "",
            });
            ComboBox_Language.SelectedIndex = 0;
            foreach (var (Title, LangCode) in Localization.LanguageList)
            {
                var box = new ComboBoxItem
                {
                    Content = Title,
                    Tag = LangCode,
                };
                ComboBox_Language.Items.Add(box);
                if (LangCode == lang)
                {
                    ComboBox_Language.SelectedItem = box;
                }
            }
        }
        finally
        {
            _languageInitialized = true;
        }
    }



    /// <summary>
    /// ????
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ComboBox_Language_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (ComboBox_Language.SelectedItem is ComboBoxItem item)
            {
                if (_languageInitialized)
                {
                    var lang = item.Tag as string;
                    _logger.LogInformation("Language change to {lang}", lang);
                    AppConfig.SetLanguage(lang);
                    this.Bindings.Update();
                    WeakReferenceMessenger.Default.Send(new LanguageChangedMessage());
                    AppConfig.SaveConfiguration();
                }
            }
        }
        catch (CultureNotFoundException)
        {
            AppConfig.SetLanguage(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Change Language");
        }
    }



    #endregion



    #region Appearance


    private void ToggleSwitch_Transparency_Toggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch toggleSwitch && _languageInitialized)
        {
            AppConfig.EnableTransparency = toggleSwitch.IsOn;
            AppConfig.SaveConfiguration();
        }
    }

    private void ToggleSwitch_RunInSystemTray_Toggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch toggleSwitch && _languageInitialized)
        {
            AppConfig.RunInSystemTray = toggleSwitch.IsOn;
            AppConfig.SaveConfiguration();
            App.Current.UpdateTrayIcon();
        }
    }

    #endregion



    #region System Properties



    /// <summary>
    /// ??/????
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="args"></param>
    private async void Hyperlink_VisualEffects_Click(Microsoft.UI.Xaml.Documents.Hyperlink sender, Microsoft.UI.Xaml.Documents.HyperlinkClickEventArgs args)
    {
        await Launcher.LaunchUriAsync(new Uri("ms-settings:easeofaccess-visualeffects"));
    }



    #endregion



}




