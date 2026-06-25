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
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Vanara.PInvoke;


namespace Glowworm;

public partial class App : Application
{

    private readonly DispatcherQueue _uiDispatcherQueue;

    private readonly Timer _gcTimer = new(TimeSpan.FromSeconds(60));

    private bool _trayIconAdded;
    private bool _isExiting;
    private IntPtr _hTrayIcon;

    private const uint TrayIconId = 1;
    private const uint TrayIconCallbackMessage = (uint)User32.WindowMessage.WM_APP + 1u;

    public bool IsExiting => _isExiting;

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
        if (AppConfig.RunInSystemTray)
        {
            InitializeTrayIcon(m_MainWindow.WindowHandle);
        }

        if (AppConfig.AutoBackupGachaRecord)
        {
            string backupFolder = AppConfig.BackupFolder ?? Path.Combine(AppConfig.UserDataFolder!, "DatabaseBackup");
            if (AppConfig.IsNetworkPath(backupFolder))
            {
                bool isAccessible = false;
                try
                {
                    if (Directory.Exists(backupFolder))
                    {
                        isAccessible = true;
                    }
                }
                catch { }

                // Persist result for the Settings page to read without re-probing
                AppConfig.NetworkDriveAvailableCache = isAccessible;

                if (!isAccessible)
                {
                    AppConfig.AutoBackupGachaRecord = false;
                    _uiDispatcherQueue.TryEnqueue(async () =>
                    {
                        await Task.Delay(1000);
                        Glowworm.Helpers.InAppToast.MainWindow?.Error("Network Backup Disabled", Glowworm.Language.Lang.NetworkBackup_Disabled, 5000);
                    });
                }
            }
        }
    }

    private void InitializeTrayIcon(IntPtr hwnd)
    {
        _uiDispatcherQueue.TryEnqueue(() =>
        {
            if (_hTrayIcon == IntPtr.Zero)
            {
                _hTrayIcon = LoadImage(IntPtr.Zero, Path.Combine(AppContext.BaseDirectory, "logo.ico"), IMAGE_ICON, 0, 0, LR_LOADFROMFILE | LR_DEFAULTSIZE);
            }
            var nid = new NOTIFYICONDATA
            {
                cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATA>(),
                hWnd = hwnd,
                uID = TrayIconId,
                uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP,
                uCallbackMessage = TrayIconCallbackMessage,
                hIcon = _hTrayIcon,
                szTip = "Glowworm",
            };

            _trayIconAdded = Shell_NotifyIcon(NIM_ADD, ref nid);
            nid.uTimeoutOrVersion = 4;
            Shell_NotifyIcon(NIM_SETVERSION, ref nid);
        });
    }


    private AppInstance instance;

    private MainWindow m_MainWindow;



    public void EnsureMainWindow()
    {
        m_MainWindow ??= new MainWindow();
        m_MainWindow.Activate();
        m_MainWindow.Show();
    }


    public void UpdateTrayIcon()
    {
        if (AppConfig.RunInSystemTray)
        {
            if (!_trayIconAdded && m_MainWindow != null)
            {
                InitializeTrayIcon(m_MainWindow.WindowHandle);
            }
        }
        else
        {
            if (_trayIconAdded)
            {
                RemoveTrayIcon();
            }
        }
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
        if (_isExiting)
        {
            return;
        }

        _isExiting = true;
        _gcTimer?.Stop();
        _gcTimer?.Dispose();
        RemoveTrayIcon();
        if (_hTrayIcon != IntPtr.Zero)
        {
            DestroyIcon(_hTrayIcon);
            _hTrayIcon = IntPtr.Zero;
        }
        m_MainWindow?.Close();
        base.Exit();
    }

    private void RemoveTrayIcon()
    {
        if (!_trayIconAdded || m_MainWindow is null)
        {
            return;
        }

        var nid = new NOTIFYICONDATA
        {
            cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATA>(),
            hWnd = m_MainWindow.WindowHandle,
            uID = TrayIconId,
        };
        Shell_NotifyIcon(NIM_DELETE, ref nid);
        _trayIconAdded = false;
    }

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "LoadIconW")]
    private static extern IntPtr LoadIcon(IntPtr hInstance, IntPtr lpIconName);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "LoadImageW")]
    private static extern IntPtr LoadImage(IntPtr hInst, string name, uint type, int cx, int cy, uint fuLoad);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool DestroyIcon(IntPtr hIcon);

    [DllImport("shell32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "Shell_NotifyIconW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA lpData);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NOTIFYICONDATA
    {
        public uint cbSize;
        public IntPtr hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public IntPtr hIcon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;
        public uint dwState;
        public uint dwStateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfo;
        public uint uTimeoutOrVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string szInfoTitle;
        public uint dwInfoFlags;
        public Guid guidItem;
        public IntPtr hBalloonIcon;
    }

    private const uint NIF_MESSAGE = 0x00000001;
    private const uint NIF_ICON = 0x00000002;
    private const uint NIF_TIP = 0x00000004;
    private const uint NIM_ADD = 0x00000000;
    private const uint NIM_DELETE = 0x00000002;
    private const uint NIM_SETVERSION = 0x00000004;
    private const int IDI_APPLICATION = 0x7F00;
    private const uint IMAGE_ICON = 1;
    private const uint LR_LOADFROMFILE = 0x00000010;
    private const uint LR_DEFAULTSIZE = 0x00000040;



}




