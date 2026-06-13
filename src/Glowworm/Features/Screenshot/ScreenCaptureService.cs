using Microsoft.Extensions.Logging;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Display;
using Microsoft.UI;
using Starward.Codec.ICC;
using Glowworm.Core;
using Glowworm.Features.Codec;
using Glowworm.Helpers;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Graphics;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Storage;

namespace Glowworm.Features.Screenshot;

internal class RunningGame
{
    public nint WindowHandle { get; set; }
    public System.Diagnostics.Process Process { get; set; }
    public GameBiz GameBiz { get; set; }
}

internal class RunningGameService
{
    public static RunningGame? GetLatestActiveGame()
    {
        nint hwnd = (nint)User32.GetForegroundWindow();
        if (hwnd == 0) return null;



        User32.GetWindowThreadProcessId(hwnd, out uint pid);
        if (pid == 0) return null;

        try
        {
            var process = System.Diagnostics.Process.GetProcessById((int)pid);
            string name = process.ProcessName;
            GameBiz biz = name switch
            {
                "GenshinImpact" => GameBiz.hk4e_global,
                "YuanShen" => GameBiz.hk4e_cn,
                "Genshin Impact Cloud" => GameBiz.clgm_global,
                "Genshin Impact Cloud Game" => GameBiz.clgm_cn,
                "StarRail" => GetStarRailBiz(hwnd),
                "ZenlessZoneZero" => GetNapBiz(hwnd),
                "Zenless Zone Zero Cloud" => GetNapCloudBiz(hwnd),
                _ => GameBiz.None,
            };

            if (biz != GameBiz.None)
            {
                return new RunningGame
                {
                    WindowHandle = hwnd,
                    Process = process,
                    GameBiz = biz,
                };
            }
        }
        catch { }

        return null;
    }


    private static GameBiz GetStarRailBiz(nint hwnd)
    {
        int count = User32.GetWindowTextLength(hwnd);
        if (count > 0)
        {
            var sb = new StringBuilder(count + 1);
            User32.GetWindowText(hwnd, sb, count + 1);
            string title = sb.ToString();
            if (title.Contains("崩坏：星穹铁道")) return GameBiz.hkrpg_cn;
        }
        return GameBiz.hkrpg_global;
    }


    private static GameBiz GetNapBiz(nint hwnd)
    {
        int count = User32.GetWindowTextLength(hwnd);
        if (count > 0)
        {
            var sb = new StringBuilder(count + 1);
            User32.GetWindowText(hwnd, sb, count + 1);
            string title = sb.ToString();
            if (title.Contains("绝区零")) return GameBiz.nap_cn;
        }
        return GameBiz.nap_global;
    }


    private static GameBiz GetNapCloudBiz(nint hwnd)
    {
        int count = User32.GetWindowTextLength(hwnd);
        if (count > 0)
        {
            var sb = new StringBuilder(count + 1);
            User32.GetWindowText(hwnd, sb, count + 1);
            string title = sb.ToString();
            if (title.Contains("绝区零")) return GameBiz.nap_cloud_cn;
        }
        return GameBiz.nap_cloud_global;
    }
}

internal class ScreenCaptureService
{


    private static ScreenCaptureService? _instance;
    private static ScreenCaptureService Instance => _instance ??= AppConfig.GetService<ScreenCaptureService>();

    private readonly ILogger<ScreenCaptureService> _logger;

    private ScreenCaptureInfoWindow? _infoWindow;

    private ConcurrentDictionary<nint, ScreenCaptureContext> _captureContexts = new();

    private static SemaphoreSlim _encodeSlim = new(1);


    public ScreenCaptureService(ILogger<ScreenCaptureService> logger)
    {
        _logger = logger;
    }



    public static void Capture()
    {
        Instance.CaptureInternal();
    }



    /// <summary>
    /// ???????????????
    /// </summary>
    /// <param name="hwnd"></param>
    /// <returns></returns>
    public static DisplayInformation GetDisplayInformationFromWindowHandle(nint hwnd)
    {
        HMONITOR monitor = User32.MonitorFromWindow(hwnd, User32.MonitorFlags.MONITOR_DEFAULTTONEAREST);
        return DisplayInformation.CreateForDisplayId(new((ulong)monitor.DangerousGetHandle()));
    }


    private async void CaptureInternal()
    {
        RunningGame? runningGame = RunningGameService.GetLatestActiveGame();
        if (runningGame is null)
        {
            return;
        }
        if (User32.IsIconic(runningGame.WindowHandle))
        {
            int count = User32.GetWindowTextLength(runningGame.WindowHandle);
            var sb = new StringBuilder(count);
            User32.GetWindowText(runningGame.WindowHandle, sb, count);
            _logger.LogWarning("Cannot capture a minimized window. HWND: {hwnd}, Title: {title}", runningGame.WindowHandle, sb.ToString());
            return;
        }
        bool captureStarted = false;
        try
        {
            using ScreenCaptureItem captureItem = await CaptureAndProceedImageAsync(runningGame);
            if (_infoWindow?.AppWindow is null)
            {
                _infoWindow = new ScreenCaptureInfoWindow();
            }
            _infoWindow.CaptureStart(runningGame.WindowHandle, captureItem.CanvasBitmap, captureItem.MaxCLL);
            captureStarted = true;
            string filePath = await SaveImageAsync(runningGame, captureItem);
            await CopyToClipboardAsync(filePath);
            _infoWindow.CaptureSuccess(runningGame.WindowHandle, captureItem.CanvasBitmap, filePath, captureItem.MaxCLL);
            if (captureItem.HDR && AppConfig.AutoConvertScreenshotToSDR)
            {
                string? sdrFilePath = await SaveAsSdrAsync(runningGame, filePath, captureItem);
                await CopyToClipboardAsync(sdrFilePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while capturing the screen.");
            if (_infoWindow?.AppWindow is null)
            {
                _infoWindow = new ScreenCaptureInfoWindow();
            }
            _infoWindow.CaptureError(runningGame.WindowHandle, captureStarted);
        }
    }



    private async Task<ScreenCaptureItem> CaptureAndProceedImageAsync(RunningGame runningGame)
    {
        if (!_captureContexts.TryGetValue(runningGame.WindowHandle, out ScreenCaptureContext? context))
        {
            context = new ScreenCaptureContext(runningGame.WindowHandle);
            context.CaptureWindowClosed += OnCaptureWindowClosed;
            _captureContexts.TryAdd(runningGame.WindowHandle, context);
        }
        using DisplayInformation displayInfo = GetDisplayInformationFromWindowHandle(runningGame.WindowHandle);
        DisplayAdvancedColorInfo colorInfo = displayInfo.GetAdvancedColorInfo();
        DirectXPixelFormat pixelFormat = colorInfo.CurrentAdvancedColorKind is DisplayAdvancedColorKind.HighDynamicRange ? DirectXPixelFormat.R16G16B16A16Float : DirectXPixelFormat.R8G8B8A8UIntNormalized;
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        using var frame = await context.CaptureAsync(pixelFormat, cts.Token).ConfigureAwait(false);
        DateTimeOffset frameTime = DateTimeOffset.Now;
        (CanvasBitmap canvasBitmap, float maxCLL) = await ProceedImageAsync(frame, runningGame, colorInfo).ConfigureAwait(false);
        ColorPrimaries colorPrimaries;
        if (maxCLL > colorInfo.SdrWhiteLevelInNits + 5)
        {
            colorPrimaries = ColorPrimaries.BT2020;
        }
        else
        {
            colorPrimaries = await GetColorPrimariesFromDisplayInformationAsync(displayInfo).ConfigureAwait(false);
        }
        return new ScreenCaptureItem
        {
            CanvasBitmap = canvasBitmap,
            ColorPrimaries = colorPrimaries,
            FrameTime = frameTime,
            HDR = maxCLL > colorInfo.SdrWhiteLevelInNits + 5,
            MaxCLL = maxCLL,
            SdrWhiteLevel = (float)colorInfo.SdrWhiteLevelInNits,
        };
    }



    public static async Task<ColorPrimaries> GetColorPrimariesFromDisplayInformationAsync(DisplayInformation displayInfo)
    {
        try
        {
            DisplayAdvancedColorInfo colorInfo = displayInfo.GetAdvancedColorInfo();
            if (colorInfo.CurrentAdvancedColorKind is DisplayAdvancedColorKind.HighDynamicRange or DisplayAdvancedColorKind.WideColorGamut)
            {
                return ColorPrimaries.BT709;
            }
            var iccStream = await displayInfo.GetColorProfileAsync();
            if (iccStream is null)
            {
                return new ColorPrimaries
                {
                    Red = new Vector2((float)colorInfo.RedPrimary.X, (float)colorInfo.RedPrimary.Y),
                    Green = new Vector2((float)colorInfo.GreenPrimary.X, (float)colorInfo.GreenPrimary.Y),
                    Blue = new Vector2((float)colorInfo.BluePrimary.X, (float)colorInfo.BluePrimary.Y),
                    White = new Vector2((float)colorInfo.WhitePoint.X, (float)colorInfo.WhitePoint.Y),
                };
            }
            else
            {
                byte[] iccData = new byte[iccStream.Size];
                await iccStream.AsStream().ReadExactlyAsync(iccData).ConfigureAwait(false);
                return ICCHelper.GetColorPrimariesFromIccData(iccData);
            }
        }
        catch
        {
            return ColorPrimaries.BT709;
        }
    }



    /// <summary>
    /// ????
    /// </summary>
    /// <param name="frame"></param>
    /// <param name="runningGame"></param>
    /// <returns></returns>
    private static async Task<(CanvasBitmap CanvasBitmap, float MaxCLL)> ProceedImageAsync(Direct3D11CaptureFrame frame, RunningGame runningGame, DisplayAdvancedColorInfo colorInfo)
    {
        return await Task.Run(() =>
        {
            using CanvasBitmap canvasBitmap = CanvasBitmap.CreateFromDirect3D11Surface(CanvasDevice.GetSharedDevice(), frame.Surface, 96);
            if (canvasBitmap.Format is DirectXPixelFormat.R8G8B8A8UIntNormalized)
            {
                bool clip = TryClipClient(runningGame.WindowHandle, frame.ContentSize, out Rect clientRect);
                if (!clip)
                {
                    clientRect = new Rect(0, 0, frame.ContentSize.Width, frame.ContentSize.Height);
                }
                CanvasRenderTarget renderTarget = new(CanvasDevice.GetSharedDevice(), (float)clientRect.Width, (float)clientRect.Height, 96, DirectXPixelFormat.R8G8B8A8UIntNormalized, CanvasAlphaMode.Premultiplied);
                try
                {
                    using CanvasDrawingSession ds = renderTarget.CreateDrawingSession();
                    ds.DrawImage(canvasBitmap, 0, 0, clientRect);
                }
                catch
                {
                    renderTarget.Dispose();
                    throw;
                }
                return (renderTarget, -1);
            }
            else
            {
                bool hdr = false;
                float maxCLL = 0;
                if (colorInfo.CurrentAdvancedColorKind is DisplayAdvancedColorKind.HighDynamicRange)
                {
                    maxCLL = GetMaxCLL(canvasBitmap);
                    hdr = maxCLL > colorInfo.SdrWhiteLevelInNits + 5;
                }
                bool clip = TryClipClient(runningGame.WindowHandle, frame.ContentSize, out Rect clientRect);
                if (!clip)
                {
                    clientRect = new Rect(0, 0, frame.ContentSize.Width, frame.ContentSize.Height);
                }
                CanvasRenderTarget renderTarget = new(CanvasDevice.GetSharedDevice(), (float)clientRect.Width, (float)clientRect.Height, 96, hdr ? DirectXPixelFormat.R16G16B16A16Float : DirectXPixelFormat.R8G8B8A8UIntNormalized, CanvasAlphaMode.Premultiplied);
                try
                {
                    using CanvasDrawingSession ds = renderTarget.CreateDrawingSession();
                    ICanvasImage output = canvasBitmap;
                    if (!hdr)
                    {
                        WhiteLevelAdjustmentEffect whiteLevelEffect = new()
                        {
                            Source = canvasBitmap,
                            InputWhiteLevel = 80,
                            OutputWhiteLevel = (float)colorInfo.SdrWhiteLevelInNits,
                            BufferPrecision = CanvasBufferPrecision.Precision16Float,
                        };
                        SrgbGammaEffect gammaEffect = new()
                        {
                            Source = whiteLevelEffect,
                            GammaMode = SrgbGammaMode.OETF,
                            BufferPrecision = CanvasBufferPrecision.Precision16Float,
                        };
                        output = gammaEffect;
                    }
                    ds.Clear(Colors.Transparent);
                    ds.DrawImage(output, 0, 0, clientRect);
                }
                catch
                {
                    renderTarget.Dispose();
                    throw;
                }
                return (renderTarget, maxCLL);
            }
        }).ConfigureAwait(false);
    }



    /// <summary>
    /// ?????
    /// </summary>
    /// <param name="canvasBitmap"></param>
    /// <param name="runningGame"></param>
    /// <param name="frameTime"></param>
    /// <returns></returns>
    private static async Task<string> SaveImageAsync(RunningGame runningGame, ScreenCaptureItem captureItem)
    {
        return await Task.Run(async () =>
        {
            string screenshotFolder;
            string subFolder = runningGame.GameBiz.ToGame().Value switch
            {
                GameBiz.hk4e => "GenshinImpact",
                GameBiz.hkrpg => "StarRail",
                GameBiz.nap => "ZZZ",
                _ => runningGame.Process.ProcessName,
            };
            string? targetFolder = AppConfig.ScreenshotFolder;
            if (Directory.Exists(targetFolder))
            {
                screenshotFolder = Path.GetFullPath(Path.Join(targetFolder, subFolder));
            }
            else
            {
                screenshotFolder = Path.GetFullPath(Path.Join(AppConfig.UserDataFolder, "Screenshots", subFolder));
            }
            Directory.CreateDirectory(screenshotFolder);

            string extension = (captureItem.HDR, AppConfig.ScreenCaptureSavedFormat) switch
            {
                (_, 1) => "avif",
                (_, 2) => "jxl",
                (false, _) => "png",
                (true, _) => "avif",
            };
            string fileName = $"{runningGame.Process.ProcessName}_{captureItem.FrameTime:yyyyMMdd_HHmmssff}.{extension}";
            string filePath = Path.Combine(screenshotFolder, fileName);
            byte[] xmpData = BuildXMPMetadata(captureItem.FrameTime);

            using MemoryStream ms = new();
            await _encodeSlim.WaitAsync().ConfigureAwait(false);
            try
            {
                if (extension is "png")
                {
                    await ImageSaver.SaveAsPngAsync(captureItem.CanvasBitmap, ms, captureItem.ColorPrimaries, xmpData).ConfigureAwait(false);
                }
                else if (extension is "avif")
                {
                    int quality = AppConfig.ScreenCaptureEncodeQuality switch
                    {
                        0 => 80,
                        1 => 90,
                        2 => 100,
                        _ => 90,
                    };
                    await ImageSaver.SaveAsAvifAsync(captureItem.CanvasBitmap, ms, captureItem.ColorPrimaries, quality, xmpData).ConfigureAwait(false);
                }
                else if (extension is "jxl")
                {
                    float distance = AppConfig.ScreenCaptureEncodeQuality switch
                    {
                        0 => 2,
                        1 => 1,
                        2 => 0,
                        _ => 1,
                    };
                    await ImageSaver.SaveAsJxlAsync(captureItem.CanvasBitmap, ms, captureItem.ColorPrimaries, distance, xmpData).ConfigureAwait(false);
                }
                else
                {
                    throw new NotSupportedException($"Unsupported image format: {extension}");
                }
            }
            finally
            {
                _encodeSlim.Release();
            }

            using var fs = File.Create(filePath);
            ms.Seek(0, SeekOrigin.Begin);
            await ms.CopyToAsync(fs).ConfigureAwait(false);
            return filePath;
        });
    }


    public static byte[] BuildXMPMetadata(DateTimeOffset time)
    {
        string value = $"""
            <x:xmpmeta xmlns:x="adobe:ns:meta/"><rdf:RDF xmlns:rdf="http://www.w3.org/1999/02/22-rdf-syntax-ns#"><rdf:Description xmlns:xmp="http://ns.adobe.com/xap/1.0/"><xmp:CreatorTool>Glowworm Launcher</xmp:CreatorTool><xmp:CreateDate>{time:yyyy-MM-ddTHH:mm:sszzz}</xmp:CreateDate></rdf:Description></rdf:RDF></x:xmpmeta>
            """;
        return Encoding.UTF8.GetBytes(value);
    }



    /// <summary>
    /// ??? SDR ??
    /// </summary>
    /// <param name="canvasImage"></param>
    /// <param name="filePath"></param>
    /// <param name="runningGame"></param>
    /// <param name="maxCLL"></param>
    /// <param name="sdrWhiteLevel"></param>
    /// <param name="frameTime"></param>
    /// <returns></returns>
    private static async Task<string?> SaveAsSdrAsync(RunningGame runningGame, string filePath, ScreenCaptureItem captureItem)
    {
        if (captureItem.HDR)
        {
            float outputMaxLuminance = captureItem.SdrWhiteLevel;
            // if (runningGame.GameBiz.Game is GameBiz.hk4e)
            // {
            //     (_, outputMaxLuminance, _) = GameSettingService.GetGenshinHDRLuminance(runningGame.GameBiz);
            // }
            using var ms = new MemoryStream();
            await ImageSaver.SaveAsUhdrAsync(captureItem.CanvasBitmap, ms, captureItem.MaxCLL, outputMaxLuminance).ConfigureAwait(false);
            ms.Position = 0;
            filePath = Path.ChangeExtension(filePath, ".jpg");
            using var fs = File.Create(filePath);
            await ms.CopyToAsync(fs).ConfigureAwait(false);
            return filePath;
        }
        return null;
    }



    public static async Task CopyToClipboardAsync(string? filePath)
    {
        if (AppConfig.AutoCopyScreenshotToClipboard)
        {
            if (File.Exists(filePath))
            {
                var file = await StorageFile.GetFileFromPathAsync(filePath);
                ClipboardHelper.SetStorageItems(DataPackageOperation.Copy, file);
            }
        }
    }


    /// <summary>
    /// ??????????
    /// </summary>
    /// <param name="hwnd"></param>
    /// <param name="contentSize"></param>
    /// <param name="clipRect"></param>
    /// <returns></returns>
    public static bool TryClipClient(nint hwnd, SizeInt32 contentSize, out Rect clipRect)
    {
        clipRect = default;
        if (!(User32.GetClientRect(hwnd, out RECT clientSize) && clientSize is { Width: > 0, Height: > 0 }))
        {
            return false;
        }
        if (clientSize.Width == contentSize.Width && clientSize.Height == contentSize.Height)
        {
            return false;
        }
        if (DwmApi.DwmGetWindowAttribute(hwnd, DwmApi.DWMWINDOWATTRIBUTE.DWMWA_EXTENDED_FRAME_BOUNDS, out RECT windowRect) != HRESULT.S_OK)
        {
            return false;
        }
        POINT clientPoint = default;
        if (!User32.ClientToScreen(hwnd, ref clientPoint))
        {
            return false;
        }
        double left = clipRect.X = clientPoint.x > windowRect.left ? (clientPoint.x - windowRect.left) : 0;
        double top = clipRect.Y = clientPoint.y > windowRect.top ? (clientPoint.y - windowRect.top) : 0;
        clipRect.Width = contentSize.Width > left ? Math.Min(contentSize.Width - left, clientSize.Width) : 1;
        clipRect.Height = contentSize.Height > top ? Math.Min(contentSize.Height - top, clientSize.Height) : 1;
        return clipRect.Right <= contentSize.Width && clipRect.Bottom <= contentSize.Height;
    }



    /// <summary>
    /// ??????
    /// </summary>
    /// <param name="canvasBitmap"></param>
    /// <returns></returns>
    public static float GetMaxCLL(CanvasBitmap canvasBitmap)
    {
        float pixelScale = MathF.Min(0.5f, 2048f / MathF.Max(canvasBitmap.SizeInPixels.Width, canvasBitmap.SizeInPixels.Height));
        using var scaleEfect = new ScaleEffect
        {
            Source = canvasBitmap,
            Scale = new Vector2(pixelScale, pixelScale),
            BufferPrecision = CanvasBufferPrecision.Precision16Float,
        };
        using var colorEffect = new ColorMatrixEffect
        {
            Source = scaleEfect,
            ColorMatrix = new Matrix5x4(
                0.2126f / 125, 0, 0, 0,
                0.7152f / 125, 0, 0, 0,
                0.0722f / 125, 0, 0, 0,
                0, 0, 0, 1,
                0, 0, 0, 0),
            BufferPrecision = CanvasBufferPrecision.Precision16Float,
        };
        using var gammaEffect = new GammaTransferEffect
        {
            Source = colorEffect,
            RedExponent = 0.5f,
            GreenDisable = true,
            BlueDisable = true,
            AlphaDisable = true,
            BufferPrecision = CanvasBufferPrecision.Precision16Float,
        };
        using var histogramEffect = new HistogramEffect
        {
            Source = gammaEffect,
            NumBins = 500,
            ChannelSelect = HistogramEffectChannelSelector.R,
            BufferPrecision = CanvasBufferPrecision.Precision16Float,
        };
        using CanvasRenderTarget renderTarget = new(CanvasDevice.GetSharedDevice(), 1, 1, 96);
        using var ds = renderTarget.CreateDrawingSession();
        ds.DrawImage(histogramEffect);
        ds.Dispose();
        float[] histogram = new float[500];
        histogramEffect.GetHistogramOutput(histogram);
        int maxBinIndex = 0;
        float cumulative = 0;
        for (int i = histogram.Length - 1; i >= 0; i--)
        {
            cumulative += histogram[i];
            if (cumulative >= 0.0001f)
            {
                maxBinIndex = i;
                break;
            }
        }
        return MathF.Pow((maxBinIndex + 0.5f) / histogram.Length, 2f) * 10000;
    }



    private void OnCaptureWindowClosed(object? sender, EventArgs e)
    {
        try
        {
            if (sender is ScreenCaptureContext context)
            {
                _captureContexts.TryRemove(context.WindowHandle, out _);
            }
            if (_captureContexts.Count == 0)
            {
                _infoWindow?.DispatcherQueue.TryEnqueue(() =>
                {
                    _infoWindow?.Close();
                    _infoWindow = null;
                });
            }
        }
        catch { }
    }


#if DEBUG
    public static async Task CaptureAppWindowAsync(nint hwnd)
    {
        try
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
            using var frame = await ScreenCaptureHelper.CaptureWindowAsync(hwnd, DirectXPixelFormat.R8G8B8A8UIntNormalized, cts.Token).ConfigureAwait(false);
            using var canvasBitmap = CanvasBitmap.CreateFromDirect3D11Surface(CanvasDevice.GetSharedDevice(), frame.Surface, 96);
            bool clip = TryClipClient(hwnd, frame.ContentSize, out Rect clientRect);
            if (!clip)
            {
                clientRect = new Rect(0, 0, frame.ContentSize.Width, frame.ContentSize.Height);
            }
            using var renderTarget = new CanvasRenderTarget(CanvasDevice.GetSharedDevice(), (float)clientRect.Width, (float)clientRect.Height, 96, DirectXPixelFormat.R8G8B8A8UIntNormalized, CanvasAlphaMode.Premultiplied);
            using (var ds = renderTarget.CreateDrawingSession())
            {
                ds.Clear(Colors.Transparent);
                ds.DrawImage(canvasBitmap, 0, 0, clientRect);
            }
            string folder = Path.Combine(AppConfig.UserDataFolder, "DebugScreenshots");
            Directory.CreateDirectory(folder);
            string filePath = Path.Combine(folder, $"Glowworm_{DateTime.Now:yyyyMMdd_HHmmss}.png");
            using var ms = new MemoryStream();
            await renderTarget.SaveAsync(ms.AsRandomAccessStream(), CanvasBitmapFileFormat.Png).AsTask().ConfigureAwait(false);
            using var fs = File.Create(filePath);
            ms.Position = 0;
            await ms.CopyToAsync(fs).ConfigureAwait(false);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = folder, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Instance._logger.LogError(ex, "Failed to capture debug screenshot");
        }
    }
#endif


}



