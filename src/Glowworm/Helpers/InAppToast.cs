using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Xaml.Interactivity;
using System;
using System.Threading.Tasks;

namespace Glowworm.Helpers;

public class InAppToast : Behavior<StackPanel>
{

    private readonly DispatcherQueueTimer _dismissTimer;


    public string Tag
    {
        get { return (string)GetValue(TagProperty); }
        set { SetValue(TagProperty, value); }
    }
    public static readonly DependencyProperty TagProperty =
        DependencyProperty.Register("Tag", typeof(string), typeof(InAppToast), new PropertyMetadata(default));


    public static InAppToast? MainWindow { get; private set; }




    public InAppToast()
    {
        _dismissTimer = DispatcherQueue.CreateTimer();
        _dismissTimer.Interval = TimeSpan.FromSeconds(30);
        _dismissTimer.IsRepeating = true;
        _dismissTimer.Tick += _dismissTimer_Tick;
    }


    protected override void OnAttached()
    {
        base.OnAttached();
        if (Tag is nameof(MainWindow))
        {
            MainWindow = this;
        }
    }


    protected override void OnDetaching()
    {
        base.OnDetaching();
        if (Tag is nameof(MainWindow))
        {
            MainWindow = null;
        }
    }



    private void _dismissTimer_Tick(DispatcherQueueTimer sender, object args)
    {
        try
        {
            int i = 0;
            var count = AssociatedObject.Children.Count;
            while (i < count)
            {
                var item = AssociatedObject.Children[i] as InfoBar;
                if (item != null && !item.IsOpen)
                {
                    AssociatedObject.Children.RemoveAt(i);
                    count--;
                }
                else
                {
                    i++;
                }
            }
        }
        catch { }
    }



    public void Show(InfoBar infoBar, int duration = 0, int index = -1)
    {
        DispatcherQueue.TryEnqueue(async () =>
        {
            try
            {
                infoBar.IsOpen = true;
                if (index > 0)
                {
                    AssociatedObject.Children.Insert(index, infoBar);
                }
                else
                {
                    AssociatedObject.Children.Add(infoBar);
                }
                if (duration > 0)
                {
                    await Task.Delay(duration);
                    infoBar.IsOpen = false;
                }
            }
            catch { }
        });
    }


    private void AddInfoBar(InfoBarSeverity severity, string? title, string? message, int duration = 0)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            var infoBar = new InfoBar
            {
                Title = title,
                Message = message,
                Severity = severity,
                IsOpen = true,
            };
            if (severity == InfoBarSeverity.Informational)
            {
                infoBar.Background = Application.Current.Resources["CustomAcrylicBrush"] as Brush;
            }
            Show(infoBar, duration);
        });
    }




    public void Information(string? title, string? message = null, int duration = 3000)
    {
        AddInfoBar(InfoBarSeverity.Informational, title, message, duration);
    }



    public void Success(string? title, string? message = null, int duration = 3000)
    {
        AddInfoBar(InfoBarSeverity.Success, title, message, duration);
    }




    public void Warning(string? title, string? message = null, int duration = 5000)
    {
        AddInfoBar(InfoBarSeverity.Warning, title, message, duration);
    }



    /// <summary>
    /// Shows a persistent informational toast with no auto-dismiss.
    /// The caller is responsible for closing it by setting <see cref="InfoBar.IsOpen"/> = false
    /// when the pending operation completes.
    /// </summary>
    /// <returns>The <see cref="InfoBar"/> that was shown, or null if the toast system is unavailable.</returns>
    public InfoBar? Pending(string? title, string? message = null)
    {
        InfoBar? infoBar = null;
        // Must run synchronously on UI thread so the caller gets the reference immediately
        DispatcherQueue.TryEnqueue(() =>
        {
            infoBar = new InfoBar
            {
                Title = title,
                Message = message,
                Severity = InfoBarSeverity.Informational,
                IsOpen = true,
                IsClosable = false,
                Background = Application.Current.Resources["CustomAcrylicBrush"] as Brush,
            };
            AssociatedObject.Children.Add(infoBar);
        });
        return infoBar;
    }



    /// <summary>
    /// Closes a pending toast returned by <see cref="Pending"/>.
    /// Safe to call from any thread.
    /// </summary>
    public void ClosePending(InfoBar? infoBar)
    {
        if (infoBar is null) return;
        DispatcherQueue.TryEnqueue(() =>
        {
            try { infoBar.IsOpen = false; } catch { }
        });
    }





    public void Error(string? title, string? message = null, int duration = 5000)
    {
        AddInfoBar(InfoBarSeverity.Error, title, message, duration);
    }



    public void Error(Exception ex, string? message = null, int duration = 5000)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            AddInfoBar(InfoBarSeverity.Error, ex.GetType().Name, ex.Message, duration);
        }
        else
        {
            AddInfoBar(InfoBarSeverity.Error, $"{ex.GetType().Name} - {message}", ex.Message, duration);
        }
    }


    public void ShowWithButton(InfoBarSeverity severity, string? title, string? message, string buttonContent, Action buttonAction, Action? closedAction = null, int duration = 0)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            var infoBar = Create(severity, title, message, buttonContent, buttonAction, closedAction);
            Show(infoBar, duration);
        });
    }


    private InfoBar Create(InfoBarSeverity severity, string? title, string? message = null, string? buttonContent = null, Action? buttonAction = null, Action? closedAction = null)
    {
        Button? button = null;
        if (!string.IsNullOrWhiteSpace(buttonContent) && buttonAction != null)
        {
            button = new Button
            {
                Content = buttonContent,
                HorizontalAlignment = HorizontalAlignment.Right,
            };
            button.Click += (_, _) =>
            {
                try
                {
                    buttonAction();
                }
                catch { }
            };
        }
        var infoBar = new InfoBar
        {
            Severity = severity,
            Title = title,
            Message = message,
            ActionButton = button,
            IsOpen = true,
        };
        if (closedAction is not null)
        {
            infoBar.CloseButtonClick += (_, _) =>
            {
                try
                {
                    closedAction();
                }
                catch { }
            };
        }
        return infoBar;
    }



}



