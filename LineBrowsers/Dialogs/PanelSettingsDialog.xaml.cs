using System.Windows;

namespace LineBrowsers.Dialogs;

public partial class PanelSettingsDialog : Window
{
    private readonly string _currentUrl;

    public string InitialUrl => UrlBox.Text;
    public double PanelWidth => double.TryParse(WidthBox.Text, out var w) && w >= 100 ? w : 400;
    public bool EnablePreview => EnablePreviewCheck.IsChecked == true;
    public bool IsMobile => MobileModeCheck.IsChecked == true;

    public PanelSettingsDialog(string initialUrl, double width, string currentUrl, bool enablePreview, bool isMobile)
    {
        InitializeComponent();
        _currentUrl = currentUrl;
        UrlBox.Text = initialUrl;
        WidthBox.Text = ((int)width).ToString();
        EnablePreviewCheck.IsChecked = enablePreview;
        MobileModeCheck.IsChecked = isMobile;
    }

    private void GetCurrentUrl_Click(object sender, RoutedEventArgs e)
        => UrlBox.Text = _currentUrl;

    private void OK_Click(object sender, RoutedEventArgs e) => DialogResult = true;
}
