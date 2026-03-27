using System.Windows;
using System.Windows.Controls;

namespace LineBrowsers.Dialogs;

public partial class SessionManagerDialog : Window
{
    private readonly List<SessionConfig> _sessions;
    private readonly Func<string, bool> _canDelete;

    public SessionManagerDialog(List<SessionConfig> sessions, Func<string, bool> canDelete)
    {
        _sessions = sessions;
        _canDelete = canDelete;
        InitializeComponent();
        SessionList.ItemsSource = _sessions;
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        var sessionId = ((Button)sender).Tag.ToString()!;
        if (!_canDelete(sessionId))
        {
            MessageBox.Show("このセッションは使用中のパネルがあるため削除できません。",
                "削除できません", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        _sessions.RemoveAll(s => s.SessionId == sessionId);
        SessionList.Items.Refresh();
    }
}
