using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Glowworm.Core;
using Glowworm.Features.Update;
using Glowworm.Features.ViewHost;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;


namespace Glowworm;

public partial class App : Application
{

    private readonly DispatcherQueue _uiDispatcherQueue;

    private readonly Timer _gcTimer = new(TimeSpan.FromSeconds(60));

    public static new App Current => (App)Application.Current;


    public App()
    {
        this.InitializeComponent();
        RequestedTheme = ApplicationTheme.Dark;
        _uiDispatcherQueue = DispatcherQueue.GetForCurrentThread();
        UnhandledException += App_UnhandledException;
        _gcTimer.Elapsed += (_, _) => GC.Collect();
    }


    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        string logFile = AppConfig.LogFile;
        if (string.IsNullOrWhiteSpace(logFile))
        {
            string logFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Glowworm", "log");
            Directory.CreateDirectory(logFolder);
            logFile = Path.Combine(logFolder, $"Glowworm_{DateTime.Now:yyMMdd}.log");
        }
        var sb = new StringBuilder();
        sb.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] App Crash:");
        sb.AppendLine(e.Exception.ToString());
        if (e.Exception.Data.Count > 0)
        {
            foreach (DictionaryEntry item in e.Exception.Data)
            {
                sb.AppendLine($"{item.Key}: {item.Value}");
            }
        }
        using var fs = File.Open(logFile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
        using var sw = new StreamWriter(fs);
        sw.Write(sb);
    }


    protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs _)
    {
        await AppConfig.CheckEnviromentAsync();

        instance = AppInstance.GetCurrent();
        instance.Activated += AppInstance_Activated;

        var main = AppInstance.FindOrRegisterForKey("main");
        if (!main.IsCurrent)
        {
            await main.RedirectActivationToAsync(instance.GetActivatedEventArgs());
            Environment.Exit(0);
        }
        
        m_MainWindow = new MainWindow();
        m_MainWindow.Activate();
    }


    private AppInstance instance;

    private MainWindow m_MainWindow;



    public void EnsureMainWindow()
    {
        m_MainWindow ??= new MainWindow();
        m_MainWindow.Activate();
        m_MainWindow.Show();
    }



    private void AppInstance_Activated(object? sender, AppActivationArguments e)
    {
        _uiDispatcherQueue.TryEnqueue(EnsureMainWindow);
    }



    public static AppInstance? FindInstanceForKey(string key)
    {
        foreach (var item in AppInstance.GetInstances())
        {
            if (item.Key == key)
            {
                return item;
            }
        }
        return null;
    }



    public new void Exit()
    {
        _gcTimer?.Stop();
        _gcTimer?.Dispose();
        LogUploadService.Stop();
        m_MainWindow?.Close();
        Application.Current.Exit();
    }



}




