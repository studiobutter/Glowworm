using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using NuGet.Versioning;
using Glowworm.Features.Update;
using Glowworm.Frameworks;
using System;
using System.Threading.Tasks;


namespace Glowworm.Features.Setting;

public sealed partial class AboutSetting : PageBase
{


    private readonly ILogger<AboutSetting> _logger = AppConfig.GetLogger<AboutSetting>();


    public AboutSetting()
    {
        this.InitializeComponent();
    }




    /// <summary>
    /// ???
    /// </summary>
    public bool EnablePreviewRelease
    {
        get; set
        {
            if (SetProperty(ref field, value))
            {
                AppConfig.EnablePreviewRelease = value;
                AppConfig.GetService<UpdateService>().ResetUpdateManager();
            }
        }
    } = AppConfig.EnablePreviewRelease;

    public int UpdateSource
    {
        get; set
        {
            if (SetProperty(ref field, value))
            {
                AppConfig.UpdateSource = value;
                AppConfig.GetService<UpdateService>().ResetUpdateManager();
            }
        }
    } = AppConfig.UpdateSource;


    /// <summary>
    /// ????
    /// </summary>
    public string? LatestVersion { get; set => SetProperty(ref field, value); }


    /// <summary>
    /// ??????
    /// </summary>
    public string? UpdateErrorText { get; set => SetProperty(ref field, value); }


    /// <summary>
    /// ????
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    private async Task CheckUpdateAsync()
    {
        try
        {
            LatestVersion = null;
            UpdateErrorText = null;
            var release = await AppConfig.GetService<UpdateService>().GetLatestVersionAsync();
            if (release != null)
            {
                new UpdateWindow { NewVersion = release }.Activate();
            }
            else
            {
                LatestVersion = AppConfig.AppVersion;
            }
        }
        catch (Exception ex)
        {
            UpdateErrorText = ex.Message;
            _logger.LogError(ex, "Check update");
        }
    }




}



