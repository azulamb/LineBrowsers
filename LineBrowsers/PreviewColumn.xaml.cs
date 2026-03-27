using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Web.WebView2.Core;

namespace LineBrowsers;

public partial class PreviewColumn : UserControl
{
    public event Action? CloseRequested;

    private readonly string _initialUrl;
    private readonly CoreWebView2Environment _env;
    private bool _middleClickPending;

    public PreviewColumn(string url, CoreWebView2Environment env)
    {
        _initialUrl = url;
        _env = env;
        InitializeComponent();
        UrlBar.Text = url;
    }

    public async Task InitializeAsync()
    {
        await WebView.EnsureCoreWebView2Async(_env);

        // Track middle-click via JS message so NewWindowRequested can distinguish it
        WebView.CoreWebView2.WebMessageReceived += (_, args) =>
        {
            if (args.TryGetWebMessageAsString() == "__middleclick__")
                _middleClickPending = true;
        };
        await WebView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(
            "document.addEventListener('mousedown',function(e){if(e.button===1)window.chrome.webview.postMessage('__middleclick__');},true);");

        WebView.CoreWebView2.SourceChanged += (_, _) =>
        {
            UrlBar.Text = WebView.CoreWebView2.Source;
        };

        WebView.CoreWebView2.NavigationCompleted += (_, _) =>
        {
            BackButton.IsEnabled = WebView.CoreWebView2.CanGoBack;
        };

        // NewWindowRequested: middle-click → default browser, left-click → open in this preview
        WebView.CoreWebView2.NewWindowRequested += (_, args) =>
        {
            args.Handled = true;
            if (_middleClickPending)
            {
                _middleClickPending = false;
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(args.Uri)
                {
                    UseShellExecute = true
                });
            }
            else
            {
                WebView.Source = new Uri(args.Uri);
            }
        };

        WebView.Source = new Uri(_initialUrl);
    }

    public new void Dispose() => WebView.Dispose();

    private void Back_Click(object sender, RoutedEventArgs e) => WebView.CoreWebView2.GoBack();

    private void Navigate_Click(object sender, RoutedEventArgs e)
    {
        if (Uri.TryCreate(UrlBar.Text, UriKind.Absolute, out var uri))
            WebView.Source = uri;
    }

    private void UrlBar_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) Navigate_Click(sender, e);
    }

    private void Close_Click(object sender, RoutedEventArgs e) => CloseRequested?.Invoke();
}
