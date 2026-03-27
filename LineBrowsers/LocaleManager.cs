using System.Windows;

namespace LineBrowsers;

public static class LocaleManager
{
    public static AppLocale CurrentLocale { get; private set; } = AppLocale.Ja;

    public static void Apply(AppLocale locale)
    {
        CurrentLocale = locale;
        var uri = new Uri($"pack://application:,,,/Locales/{locale}.xaml");

        var dicts = Application.Current.Resources.MergedDictionaries;
        var existing = dicts.FirstOrDefault(d =>
            d.Source?.OriginalString.EndsWith("/Ja.xaml") == true ||
            d.Source?.OriginalString.EndsWith("/En.xaml") == true);
        var next = new ResourceDictionary { Source = uri };

        if (existing != null)
            dicts[dicts.IndexOf(existing)] = next;
        else
            dicts.Add(next);
    }

    public static string Get(string key) =>
        Application.Current.Resources[key] as string ?? key;
}
