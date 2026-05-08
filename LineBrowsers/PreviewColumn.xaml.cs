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
    private bool _leftClickPending;

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
            var msg = args.TryGetWebMessageAsString();
            if (msg == "__middleclick__") _middleClickPending = true;
            if (msg == "__leftclick__")   _leftClickPending   = true;
        };
        await WebView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(
            "document.addEventListener('mousedown',function(e){if(!window.chrome||!window.chrome.webview)return;if(e.button===1)window.chrome.webview.postMessage('__middleclick__');else if(e.button===0)window.chrome.webview.postMessage('__leftclick__');},true);");

        WebView.CoreWebView2.SourceChanged += (_, _) =>
        {
            UrlBar.Text = WebView.CoreWebView2.Source;
        };

        WebView.CoreWebView2.NavigationCompleted += (_, _) =>
        {
            BackButton.IsEnabled = WebView.CoreWebView2.CanGoBack;
        };

        // NewWindowRequested:
        //   middle-click       → default browser
        //   left-click (link)  → same WebView
        //   no click (JS open) → popup with window.opener (OAuth etc.)
        WebView.CoreWebView2.NewWindowRequested += async (_, args) =>
        {
            if (_middleClickPending)
            {
                args.Handled = true;
                _middleClickPending = false;
                _leftClickPending = false;
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(args.Uri)
                {
                    UseShellExecute = true
                });
                return;
            }

            if (_leftClickPending)
            {
                args.Handled = true;
                _leftClickPending = false;
                WebView.Source = new Uri(args.Uri);
                return;
            }

            // Programmatic window.open() — needs window.opener (e.g. OAuth popup)
            var deferral = args.GetDeferral();
            try
            {
                var owner = System.Windows.Window.GetWindow(this);
                var popup = new PopupWindow(_env, owner);
                popup.Show();
                await popup.InitializeCoreWebView2Async();
                args.NewWindow = popup.WebView.CoreWebView2;
                args.Handled = true;
            }
            finally
            {
                deferral.Complete();
            }
        };

        WebView.Source = new Uri(_initialUrl);
    }

    public void Dispose() => WebView.Dispose();

    private void Back_Click(object sender, RoutedEventArgs e) => WebView.CoreWebView2.GoBack();

    private void Navigate_Click(object sender, RoutedEventArgs e)
    {
        if (!Uri.TryCreate(UrlBar.Text, UriKind.Absolute, out var uri)) return;
        if (WebView.CoreWebView2 != null && WebView.Source == uri)
            WebView.CoreWebView2.Reload();
        else
            WebView.Source = uri;
    }

    private void UrlBar_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) Navigate_Click(sender, e);
    }

    private void OpenBrowser_Click(object sender, RoutedEventArgs e)
    {
        var url = WebView.CoreWebView2?.Source ?? _initialUrl;
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
    }

    private void Close_Click(object sender, RoutedEventArgs e) => CloseRequested?.Invoke();
}
