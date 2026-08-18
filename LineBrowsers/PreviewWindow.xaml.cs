using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using Microsoft.Web.WebView2.Core;

namespace LineBrowsers;

public partial class PreviewWindow : Window
{
    private readonly string _initialUrl;
    private readonly CoreWebView2Environment _env;
    private bool _middleClickPending;
    private bool _leftClickPending;

    public PreviewWindow(string url, CoreWebView2Environment env)
    {
        _initialUrl = url;
        _env = env;
        InitializeComponent();
        UrlBar.Text = url;
    }

    // ------------------------------------------------------------- window bounds

    // The owner drives our position/size in physical pixels. Going through
    // SetWindowPos instead of Left/Top/Width/Height avoids WPF converting the
    // values with a DPI that is not yet settled: before Show() the window has no
    // HWND (and therefore no monitor DPI), and a window created off the target
    // monitor is rescaled by the DPI ratio when it is moved onto it — which is
    // what made the preview mis-sized on a 175% display until the first resize.
    private (int X, int Y, int Cx, int Cy)? _deviceBounds;
    private bool _applyingBounds;

    public void SetDeviceBounds(int x, int y, int cx, int cy, DpiScale ownerDpi)
    {
        _deviceBounds = (x, y, cx, cy);

        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero)
        {
            // No HWND yet: place it in DIPs so the window is *created* on the
            // target monitor. OnSourceInitialized then applies the exact rect.
            Left   = x  / ownerDpi.DpiScaleX;
            Top    = y  / ownerDpi.DpiScaleY;
            Width  = cx / ownerDpi.DpiScaleX;
            Height = cy / ownerDpi.DpiScaleY;
            return;
        }

        ApplyDeviceBounds();
    }

    private void ApplyDeviceBounds()
    {
        if (_applyingBounds || _deviceBounds is not { } b) return;
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd == IntPtr.Zero) return;

        _applyingBounds = true;
        try { SetWindowPos(hwnd, IntPtr.Zero, b.X, b.Y, b.Cx, b.Cy, SWP_NOZORDER | SWP_NOACTIVATE); }
        finally { _applyingBounds = false; }
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        // Runs right after the HWND is created and before the first paint, so the
        // window is already the right size when it becomes visible.
        ApplyDeviceBounds();
    }

    protected override void OnDpiChanged(DpiScale oldDpi, DpiScale newDpi)
    {
        base.OnDpiChanged(oldDpi, newDpi);
        // WPF applies the OS-suggested rect (old size × DPI ratio); put ours back.
        ApplyDeviceBounds();
    }

    private const uint SWP_NOZORDER   = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(
        IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);


    public async Task InitializeAsync()
    {
        await WebView.EnsureCoreWebView2Async(_env);

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
                var popup = new PopupWindow(_env, this);
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

    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    protected override void OnClosed(EventArgs e)
    {
        WebView.Dispose();
        base.OnClosed(e);
    }
}
