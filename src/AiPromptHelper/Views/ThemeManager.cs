using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using AiPromptHelper.Core;
namespace AiPromptHelper.Views;
public static class ThemeManager
{
    public static void Apply(Settings settings)
    {
        bool systemDark = false;
        try { systemDark = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize", "AppsUseLightTheme", 1) is int v && v == 0; } catch { }
        bool dark = settings.Theme == "dark" || (settings.Theme == "system" && systemDark);
        var r = Application.Current.Resources;
        void Set(string key, string color) => r[key] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        Set("PageBrush", dark ? "#1E2725" : "#F7F8F6"); Set("CardBrush", dark ? "#28332F" : "#FFFFFF");
        Set("SoftBrush", dark ? "#344A43" : "#DCE9E5"); Set("TextBrush", dark ? "#E4ECE8" : "#293735");
        Set("MutedBrush", dark ? "#B2C4BB" : "#586C67"); Set("LineBrush", dark ? "#485950" : "#DCE3DF");
        Set("ErrorBrush", dark ? "#FFA5A5" : "#B33B3B"); Set("AccentBrush", settings.AccentColor);
        Set("HoverBrush", dark ? "#3B4942" : "#E7EEE9"); Set("DisabledBrush",dark?"#ADB8B2":"#65716B"); Set("DisabledBackgroundBrush",dark?"#303833":"#EDF0ED"); Set("ScrollTrackBrush",dark?"#252E29":"#EEF1EE"); Set("ScrollThumbBrush",dark?"#82958A":"#7C8F84"); Set("ScrollHoverBrush",dark?"#B7C7BE":"#536B5F");
        var c = (Color)ColorConverter.ConvertFromString(settings.AccentColor);
        double Channel(byte b) { var x = b / 255d; return x <= .04045 ? x / 12.92 : Math.Pow((x + .055) / 1.055, 2.4); }
        var luminance = .2126 * Channel(c.R) + .7152 * Channel(c.G) + .0722 * Channel(c.B);
        Set("AccentTextBrush", Appearance.ContrastText(c.R,c.G,c.B));
        r[SystemColors.WindowBrushKey] = r["CardBrush"]; r[SystemColors.WindowTextBrushKey] = r["TextBrush"];
        r[SystemColors.ControlBrushKey] = r["CardBrush"]; r[SystemColors.ControlTextBrushKey] = r["TextBrush"];
        r[SystemColors.GrayTextBrushKey] = r["DisabledBrush"]; r[SystemColors.InactiveSelectionHighlightBrushKey] = r["SoftBrush"]; r[SystemColors.InactiveSelectionHighlightTextBrushKey] = r["TextBrush"];
        r[SystemColors.HighlightBrushKey] = r["AccentBrush"]; r[SystemColors.HighlightTextBrushKey] = r["AccentTextBrush"];
        foreach (Window window in Application.Current.Windows) window.FontSize = Appearance.PointsToDip(settings.FontSizePt);
    }
    public static void Setup(Window window, Settings settings, Window? owner = null)
    {
        window.SetResourceReference(FrameworkElement.StyleProperty, typeof(Window));
        window.UseLayoutRounding=true; window.SnapsToDevicePixels=true;
        window.FontSize = Appearance.PointsToDip(settings.FontSizePt);
        window.Icon = new System.Windows.Media.Imaging.BitmapImage(new Uri("pack://application:,,,/Assets/TsunaguChat.ico"));
        window.WindowStartupLocation = owner == null ? WindowStartupLocation.CenterScreen : WindowStartupLocation.CenterOwner;
        if (owner != null) { window.Owner = owner; window.ShowInTaskbar = false; }
    }
}



