using System.Windows;
using System.Windows.Controls;

namespace LineBrowsers.Dialogs;

public partial class SettingsDialog : Window
{
    public AppTheme SelectedTheme { get; private set; }
    public AppLocale SelectedLocale { get; private set; }

    public SettingsDialog(AppTheme currentTheme, AppLocale currentLocale)
    {
        InitializeComponent();
        SelectedTheme = currentTheme;
        SelectedLocale = currentLocale;
        ThemeCombo.SelectedIndex = (int)currentTheme;
        LangCombo.SelectedIndex = (int)currentLocale;
    }

    private void ThemeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SelectedTheme = (AppTheme)ThemeCombo.SelectedIndex;
        ThemeManager.Apply(SelectedTheme);
    }

    private void LangCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SelectedLocale = (AppLocale)LangCombo.SelectedIndex;
        LocaleManager.Apply(SelectedLocale);
    }
}
