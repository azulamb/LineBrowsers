using System.Windows;

namespace LineBrowsers.Dialogs;

public partial class InjectDialog : Window
{
    public string Code => CodeBox.Text;
    public bool IsDeleted { get; private set; }

    public InjectDialog(bool isCss, InjectionEntry? editEntry = null)
    {
        InitializeComponent();
        TypeLabel.Text = LocaleManager.Get(isCss ? "Label.CSS" : "Label.JS");

        if (editEntry != null)
        {
            Title = LocaleManager.Get(isCss ? "Title.EditCSS" : "Title.EditJS");
            CodeBox.Text = editEntry.Code;
            DeleteButton.Visibility = Visibility.Visible;
        }
        else
        {
            Title = LocaleManager.Get(isCss ? "Title.InjectCSS" : "Title.InjectJS");
        }
    }

    private void OK_Click(object sender, RoutedEventArgs e) => DialogResult = true;

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        IsDeleted = true;
        DialogResult = true;
    }
}
