using System.Windows;
using System.Windows.Controls;

namespace LineBrowsers.Dialogs;

public partial class AddPanelDialog : Window
{
    private static string NewSessionItem => LocaleManager.Get("Item.NewSession");

    public string Url => UrlBox.Text;
    public double PanelWidth => double.TryParse(WidthBox.Text, out var w) ? w : 400;
    public bool IsNewSession { get; private set; }
    public string SelectedSessionId { get; private set; } = "";
    public string SessionName => NewSessionNameBox.Text;

    public AddPanelDialog(List<SessionConfig> sessions)
    {
        InitializeComponent();

        foreach (var s in sessions)
            SessionCombo.Items.Add(new ComboBoxItem { Content = s.Name, Tag = s.SessionId });

        SessionCombo.Items.Add(new ComboBoxItem { Content = NewSessionItem, Tag = null });
        SessionCombo.SelectedIndex = 0;
        NewSessionNameBox.Text = LocaleManager.Get("Default.NewSessionName");
    }

    private void SessionCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var isNew = (SessionCombo.SelectedItem as ComboBoxItem)?.Tag == null;
        var vis = isNew ? Visibility.Visible : Visibility.Collapsed;
        NewSessionLabel.Visibility = vis;
        NewSessionNameBox.Visibility = vis;
    }

    private void OK_Click(object sender, RoutedEventArgs e)
    {
        var item = SessionCombo.SelectedItem as ComboBoxItem;
        if (item?.Tag == null)
        {
            IsNewSession = true;
        }
        else
        {
            IsNewSession = false;
            SelectedSessionId = item.Tag.ToString()!;
        }
        DialogResult = true;
    }
}
