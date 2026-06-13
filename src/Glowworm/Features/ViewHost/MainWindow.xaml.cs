using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppLifecycle;
using Glowworm.Features.Database;
using Glowworm.Features.Screenshot;
using Glowworm.Features.Setting;
using Glowworm.Frameworks;
using Glowworm.Helpers;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Windows.Graphics;


namespace Glowworm.Features.ViewHost;

[INotifyPropertyChanged]
public sealed partial class MainWindow : WindowEx
{


    public static new MainWindow Current { get; private set; }

    private const uint TrayCallbackMessage = (uint)User32.WindowMessage.WM_APP + 1u;
    private const int TrayMenuOpenId = 1001;
    private const int TrayMenuExitId = 1002;

    private readonly SystemBackdropHelper _backdropHelper;


    public MainWindow()
    {
        Current = this;
        MainWindowId = AppWindow.Id;
        _backdropHelper = new SystemBackdropHelper(this, SystemBackdropProperty.MicaAltDefault);
        this.InitializeComponent();
        InitializeMainWindow();
    }



    private void InitializeMainWindow()
    {
        Title = "Glowworm";
        AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        AppWindow.TitleBar.IconShowOptions = IconShowOptions.ShowIconAndSystemMenu;
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        AppWindow.Closing += AppWindow_Closing;
        Content.KeyDown += Content_KeyDown;
        CenterInScreen(1200, 676);
        AdaptTitleBarButtonColorToActuallTheme();
        SetDragRectangles(new RectInt32(0, 0, 100000, (int)(48 * UIScale)));
        SetIcon();
        WTSRegisterSessionNotification(WindowHandle, 0);
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsMaximizable = false;
            presenter.IsResizable = false;
        }
        if (AppConfig.EnableTransparency)
        {
            _backdropHelper.TrySetMica(true);
        }
    }



    public override void CenterInScreen(int? width = null, int? height = null)
    {
        width = width <= 0 ? null : width;
        height = height <= 0 ? null : height;
        POINT point;
        GetCursorPos(out point);
        DisplayArea display = DisplayArea.GetFromPoint(new PointInt32(point.X, point.Y), DisplayAreaFallback.Nearest);
        double scale = UIScale;
        int w = (int)((width * scale) ?? AppWindow.Size.Width);
        int h = (int)((height * scale) ?? AppWindow.Size.Height);
        int x = display.WorkArea.X + (display.WorkArea.Width - w) / 2;
        int y = display.WorkArea.Y + (display.WorkArea.Height - h) / 2;
        AppWindow.MoveAndResize(new RectInt32(x, y, w, h));
    }



    public override void Show()
    {
        double uiScale = UIScale;
        if (Math.Abs(AppWindow.Size.Width - 1200 * uiScale) > 10 || Math.Abs(AppWindow.Size.Height - 676 * uiScale) > 10)
        {
            CenterInScreen(1200, 676);
        }
        base.Show();
    }



    private void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (App.Current is App app && app.IsExiting)
        {
            return;
        }

        if (AppConfig.RunInSystemTray)
        {
            args.Cancel = true;
            Hide();
        }
        else
        {
            App.Current.Exit();
        }
    }



    private void Content_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        // Removed Escape key closing the app
#if DEBUG
        if (e.Key == Windows.System.VirtualKey.F12)
        {
            _ = ScreenCaptureService.CaptureAppWindowAsync(WindowHandle);
        }
#endif
    }



    private DateTimeOffset _lastActivatedTime = DateTimeOffset.Now;


    protected override nint WindowSubclassProc(HWND hWnd, uint uMsg, nint wParam, nint lParam, nuint uIdSubclass, nint dwRefData)
    {
        if (uMsg == (uint)User32.WindowMessage.WM_ACTIVATE || uMsg == (uint)User32.WindowMessage.WM_POINTERACTIVATE)
        {
            // ????
            if (wParam is 0x1 or 0x2)
            {
                // WA_ACTIVE or WA_CLICKACTIVE
                var now = DateTimeOffset.Now;
                WeakReferenceMessenger.Default.Send(new MainWindowStateChangedMessage
                {
                    Activate = true,
                    CurrentTime = now,
                    LastActivatedTime = _lastActivatedTime,
                });
                _lastActivatedTime = now;
            }
        }
        else if (uMsg == (uint)User32.WindowMessage.WM_SYSCOMMAND)
        {
            if (wParam == 0xF030)
            {
                // SC_MAXIMIZE
                // ?????????????,WinAppSDK ????? Bug
                return IntPtr.Zero;
            }
        }
        else if (uMsg == (uint)User32.WindowMessage.WM_WTSSESSION_CHANGE)
        {
            if (wParam == 0x7)
            {
                // WTS_SESSION_LOCK
                // ??,??????
                WeakReferenceMessenger.Default.Send(new MainWindowStateChangedMessage { SessionLock = true, CurrentTime = DateTimeOffset.Now });
            }
            else if (wParam == 0x8)
            {
                // WTS_SESSION_UNLOCK 
            }
        }
        else if (uMsg == (uint)User32.WindowMessage.WM_DEVICECHANGE)
        {
            // ??????/??
            if (wParam == 0x8000)
            {
                // DBT_DEVICEARRIVAL
                User32.DEV_BROADCAST_HDR dev = Marshal.PtrToStructure<User32.DEV_BROADCAST_HDR>(lParam);
                if (dev.dbch_devicetype is User32.DBT_DEVTYPE.DBT_DEVTYP_VOLUME)
                {
                    // WeakReferenceMessenger.Default.Send(new RemovableStorageDeviceChangedMessage());
                }
            }
            else if (wParam == 0x8004)
            {
                // DBT_DEVICEREMOVECOMPLETE
                User32.DEV_BROADCAST_HDR dev = Marshal.PtrToStructure<User32.DEV_BROADCAST_HDR>(lParam);
                if (dev.dbch_devicetype is User32.DBT_DEVTYPE.DBT_DEVTYP_VOLUME)
                {
                    // WeakReferenceMessenger.Default.Send(new RemovableStorageDeviceChangedMessage());
                }
            }
        }
        else if (uMsg == TrayCallbackMessage)
        {
            // Shell may send different mouse messages for tray interactions depending on OS.
            int msg = (int)lParam & 0xFFFF;
            if (msg == (int)User32.WindowMessage.WM_LBUTTONDBLCLK || msg == (int)User32.WindowMessage.WM_LBUTTONUP)
            {
                App.Current.EnsureMainWindow();
                return IntPtr.Zero;
            }
            if (msg == (int)User32.WindowMessage.WM_RBUTTONUP || msg == (int)User32.WindowMessage.WM_CONTEXTMENU)
            {
                try
                {
                    ShowTrayContextMenu();
                }
                catch { }
                return IntPtr.Zero;
            }
        }
        else if (uMsg == (uint)User32.WindowMessage.WM_COMMAND)
        {
            int commandId = (int)wParam & 0xFFFF;
            if (commandId == TrayMenuOpenId)
            {
                App.Current.EnsureMainWindow();
                return IntPtr.Zero;
            }
            if (commandId == TrayMenuExitId)
            {
                App.Current.Exit();
                return IntPtr.Zero;
            }
        }
        else if (uMsg == (uint)User32.WindowMessage.WM_HOTKEY)
        {
            if (wParam == 44444)
            {
                this.Show();
            }
            else if (wParam == 44445)
            {
                // ??
                ScreenCaptureService.Capture();
            }
        }
        return base.WindowSubclassProc(hWnd, uMsg, wParam, lParam, uIdSubclass, dwRefData);
    }

    private void ShowTrayContextMenu()
    {
        IntPtr menu = CreatePopupMenu();
        if (menu == IntPtr.Zero)
        {
            return;
        }

        try
        {
            AppendMenu(menu, MF_STRING, new UIntPtr(TrayMenuOpenId), "Open");
            AppendMenu(menu, MF_STRING, new UIntPtr(TrayMenuExitId), "Exit");
            POINT point;
            GetCursorPos(out point);
            SetForegroundWindow(WindowHandle);
            int commandId = TrackPopupMenuEx(menu, TPM_LEFTALIGN | TPM_RIGHTBUTTON | TPM_RETURNCMD | TPM_NONOTIFY, point.X, point.Y, WindowHandle, IntPtr.Zero);
            if (commandId == TrayMenuOpenId)
            {
                App.Current.EnsureMainWindow();
            }
            else if (commandId == TrayMenuExitId)
            {
                App.Current.Exit();
            }
            PostMessage(WindowHandle, WM_NULL, IntPtr.Zero, IntPtr.Zero);
        }
        finally
        {
            DestroyMenu(menu);
        }
    }



    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr CreatePopupMenu();

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "AppendMenuW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AppendMenu(IntPtr hMenu, uint uFlags, UIntPtr uIDNewItem, string lpNewItem);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyMenu(IntPtr hMenu);

    private const uint MF_STRING = 0x00000000;
    private const uint TPM_LEFTALIGN = 0x00000000;
    private const uint TPM_RIGHTBUTTON = 0x0002;
    private const uint TPM_RETURNCMD = 0x0100;
    private const uint TPM_NONOTIFY = 0x0080;
    private const uint WM_NULL = 0x0000;

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern int TrackPopupMenuEx(IntPtr hMenu, uint uFlags, int x, int y, IntPtr hWnd, IntPtr lptpm);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [LibraryImport("wtsapi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool WTSRegisterSessionNotification(IntPtr hWnd, int dwFlags);


    [LibraryImport("wtsapi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool WTSUnRegisterSessionNotification(IntPtr hWnd);

}
