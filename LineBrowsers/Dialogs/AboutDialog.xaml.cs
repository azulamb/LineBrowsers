using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Navigation;

namespace LineBrowsers.Dialogs;

public partial class AboutDialog : Window
{
    private const string GitHubUrl = "https://github.com/azulamb/LineBrowsers";

    public AboutDialog()
    {
        InitializeComponent();
        var full = Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion ?? "-";
        var plusIndex = full.IndexOf('+');
        if (plusIndex >= 0)
        {
            VersionText.Text = LocaleManager.Get("Prefix.Version") + full[..plusIndex];
            CommitText.Text = LocaleManager.Get("Prefix.Commit") + full[(plusIndex + 1)..];
        }
        else
        {
            VersionText.Text = LocaleManager.Get("Prefix.Version") + full;
        }
        GitHubLink.NavigateUri = new Uri(GitHubUrl);
        GitHubLink.Inlines.Clear();
        GitHubLink.Inlines.Add(GitHubUrl);
    }

    private void GitHubLink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.ToString()) { UseShellExecute = true });
        e.Handled = true;
    }
}
