using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace LineBrowsers;

public partial class PopupWindow : Window
{
    private readonly CoreWebView2Environment _env;

    public PopupWindow(CoreWebView2Environment env, Window owner)
    {
        _env = env;
        Owner = owner;
        InitializeComponent();
    }

    public async Task InitializeCoreWebView2Async()
    {
        await WebView.EnsureCoreWebView2Async(_env);

        WebView.CoreWebView2.WindowCloseRequested += (_, _) => Close();

        // Popups that open further windows (e.g. sub-auth flows) navigate in-place
        WebView.CoreWebView2.NewWindowRequested += (_, args) =>
        {
            args.Handled = true;
            WebView.Source = new Uri(args.Uri);
        };
    }

    protected override void OnClosed(EventArgs e)
    {
        WebView.Dispose();
        base.OnClosed(e);
    }
}
