using System.Windows;
using Microsoft.Win32;

namespace LineBrowsers;

public static class ThemeManager
{
    public static AppTheme CurrentMode { get; private set; } = AppTheme.Auto;

    public static void Apply(AppTheme mode)
    {
        CurrentMode = mode;
        var dark = mode == AppTheme.Dark || (mode == AppTheme.Auto && IsSystemDarkMode());
        var uri = new Uri($"pack://application:,,,/Themes/{(dark ? "Dark" : "Light")}.xaml");

        var dicts = Application.Current.Resources.MergedDictionaries;
        var existing = dicts.FirstOrDefault(d =>
            d.Source?.OriginalString.EndsWith("Dark.xaml") == true ||
            d.Source?.OriginalString.EndsWith("Light.xaml") == true);
        var next = new ResourceDictionary { Source = uri };

        if (existing != null)
            dicts[dicts.IndexOf(existing)] = next;
        else
            dicts.Add(next);
    }

    public static bool IsSystemDarkMode()
    {
        using var key = Registry.CurrentUser.OpenSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        return (int)(key?.GetValue("AppsUseLightTheme") ?? 1) == 0;
    }
}
