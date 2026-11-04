using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Glowworm.Features.Announcement;

public sealed partial class AnnouncementCard : UserControl
{
    public static readonly DependencyProperty ItemProperty =
        DependencyProperty.Register(nameof(Item), typeof(AnnouncementItem), typeof(AnnouncementCard),
            new PropertyMetadata(null, OnItemChanged));

    public AnnouncementItem Item
    {
        get => (AnnouncementItem)GetValue(ItemProperty);
        set => SetValue(ItemProperty, value);
    }

    public string SeverityIcon { get; private set; } = "\uF167";

    public SolidColorBrush SeverityColor { get; private set; } = new SolidColorBrush(Colors.Gray);

    public Visibility WarningBackgroundVisibility { get; private set; } = Visibility.Collapsed;

    public Visibility CriticalBackgroundVisibility { get; private set; } = Visibility.Collapsed;


    public AnnouncementCard()
    {
        this.InitializeComponent();
    }


    private static void OnItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AnnouncementCard card && e.NewValue is AnnouncementItem item)
        {
            card.SeverityIcon = item.Severity switch
            {
                2 => "\uF736",
                3 => "\uEB90",
                _ => "\uF167",
            };
            card.SeverityColor = new SolidColorBrush(item.Severity switch
            {
                2 => Windows.UI.Color.FromArgb(255, 255, 185, 0),
                3 => Windows.UI.Color.FromArgb(255, 196, 43, 28),
                _ => Windows.UI.Color.FromArgb(255, 0, 120, 212),
            });
            card.WarningBackgroundVisibility = item.Severity == 2 ? Visibility.Visible : Visibility.Collapsed;
            card.CriticalBackgroundVisibility = item.Severity == 3 ? Visibility.Visible : Visibility.Collapsed;
            card.Bindings.Update();
        }
    }
}
