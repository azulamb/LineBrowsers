using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using LineBrowsers.Dialogs;
using Microsoft.Web.WebView2.Core;

namespace LineBrowsers;

public partial class BrowserColumn : UserControl
{
    public PanelConfig Config { get; }
    public event Action? CloseRequested;
    public event Action? StateChanged;
    public event Action<string, CoreWebView2Environment>? PreviewRequested;

    private readonly CoreWebView2Environment _env;
    private readonly SessionConfig _session;
    private bool _middleClickPending;
    private string? _jsScriptId;
    private string? _cssScriptId;

    public BrowserColumn(PanelConfig config, SessionConfig session, CoreWebView2Environment env)
    {
        Config = config;
        _session = session;
        _env = env;
        InitializeComponent();
        Width = config.Width;
        UrlBar.Text = config.Url;
    }

    public async Task InitializeAsync()
    {
        // Must be in the visual tree before calling EnsureCoreWebView2Async
        await WebView.EnsureCoreWebView2Async(_env);

        // Track middle-click via JS message so NewWindowRequested can distinguish it
        WebView.CoreWebView2.WebMessageReceived += (_, args) =>
        {
            if (args.TryGetWebMessageAsString() == "__middleclick__")
                _middleClickPending = true;
        };
        await WebView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(
            "document.addEventListener('mousedown',function(e){if(e.button===1)window.chrome.webview.postMessage('__middleclick__');},true);");

        // SourceChanged fires on every URL update including History API (pushState/replaceState)
        WebView.CoreWebView2.SourceChanged += (_, _) =>
        {
            UrlBar.Text = WebView.CoreWebView2.Source;
        };

        WebView.CoreWebView2.NavigationCompleted += (_, _) =>
        {
            Config.Url = WebView.CoreWebView2.Source;
            BackButton.IsEnabled = WebView.CoreWebView2.CanGoBack;
            StateChanged?.Invoke();
        };

        // NewWindowRequested: middle-click → default browser, left-click → preview
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
                PreviewRequested?.Invoke(args.Uri, _env);
            }
        };

        // Cross-domain user navigation → show in preview panel
        WebView.CoreWebView2.NavigationStarting += (_, args) =>
        {
            if (!args.IsUserInitiated) return;
            if (!Uri.TryCreate(args.Uri, UriKind.Absolute, out var newUri)) return;
            if (!Uri.TryCreate(WebView.CoreWebView2.Source, UriKind.Absolute, out var currentUri)) return;
            if (string.IsNullOrEmpty(currentUri.Host)) return;
            if (newUri.Host.Equals(currentUri.Host, StringComparison.OrdinalIgnoreCase)) return;
            args.Cancel = true;
            PreviewRequested?.Invoke(args.Uri, _env);
        };


        // Re-register saved injections
        var js = Config.OnLoadScripts.FirstOrDefault();
        if (js != null && !string.IsNullOrWhiteSpace(js.Code))
            try { _jsScriptId = await WebView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(js.Code); } catch { }

        var css = Config.OnLoadStyles.FirstOrDefault();
        if (css != null && !string.IsNullOrWhiteSpace(css.Code))
            try { _cssScriptId = await WebView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(WrapCss(css.Code)); } catch { }

        WebView.Source = new Uri(Config.Url);
    }

    public async Task InjectScriptAsync(string script)
    {
        var entry = new InjectionEntry { Code = script };
        Config.OnLoadScripts.Clear();
        Config.OnLoadScripts.Add(entry);
        try { _jsScriptId = await WebView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(script); } catch { }
        try { await WebView.CoreWebView2.ExecuteScriptAsync(script); } catch { }
        StateChanged?.Invoke();
    }

    public async Task InjectCssAsync(string css)
    {
        var entry = new InjectionEntry { Code = css };
        Config.OnLoadStyles.Clear();
        Config.OnLoadStyles.Add(entry);
        var wrapped = WrapCss(css);
        try { _cssScriptId = await WebView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(wrapped); } catch { }
        try { await WebView.CoreWebView2.ExecuteScriptAsync(wrapped); } catch { }
        StateChanged?.Invoke();
    }

    public async Task UpdateScriptAsync(InjectionEntry entry, string newCode)
    {
        if (_jsScriptId != null)
        {
            try { WebView.CoreWebView2.RemoveScriptToExecuteOnDocumentCreated(_jsScriptId); } catch { }
            _jsScriptId = null;
        }
        entry.Code = newCode;
        try { _jsScriptId = await WebView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(newCode); } catch { }
        try { await WebView.CoreWebView2.ExecuteScriptAsync(newCode); } catch { }
        StateChanged?.Invoke();
    }

    public async Task UpdateStyleAsync(InjectionEntry entry, string newCss)
    {
        if (_cssScriptId != null)
        {
            try { WebView.CoreWebView2.RemoveScriptToExecuteOnDocumentCreated(_cssScriptId); } catch { }
            _cssScriptId = null;
        }
        entry.Code = newCss;
        var wrapped = WrapCss(newCss);
        try { _cssScriptId = await WebView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(wrapped); } catch { }
        try { await WebView.CoreWebView2.ExecuteScriptAsync(wrapped); } catch { }
        StateChanged?.Invoke();
    }

    public void DeleteScript(InjectionEntry entry)
    {
        if (_jsScriptId != null)
        {
            try { WebView.CoreWebView2.RemoveScriptToExecuteOnDocumentCreated(_jsScriptId); } catch { }
            _jsScriptId = null;
        }
        Config.OnLoadScripts.Remove(entry);
        StateChanged?.Invoke();
    }

    public void DeleteStyle(InjectionEntry entry)
    {
        if (_cssScriptId != null)
        {
            try { WebView.CoreWebView2.RemoveScriptToExecuteOnDocumentCreated(_cssScriptId); } catch { }
            _cssScriptId = null;
        }
        Config.OnLoadStyles.Remove(entry);
        StateChanged?.Invoke();
    }

    public new void Dispose() => WebView.Dispose();

    // Wraps raw CSS in a JS snippet that injects a <style> tag.
    // Uses readyState check so it works both when called via
    // AddScriptToExecuteOnDocumentCreated (head not yet available)
    // and via ExecuteScriptAsync (page already loaded).
    private static string WrapCss(string css)
    {
        var escaped = JsonSerializer.Serialize(css);
        return $"(function(){{function inject(){{var s=document.createElement('style');s.textContent={escaped};document.head.appendChild(s);}}if(document.readyState==='loading'){{document.addEventListener('DOMContentLoaded',inject);}}else{{inject();}}}})();";
    }

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

    private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
    {
        var newWidth = ActualWidth + e.HorizontalChange;
        if (newWidth < 100) return;
        Width = newWidth;
        Config.Width = newWidth;
        StateChanged?.Invoke();
    }

    private void Menu_Click(object sender, RoutedEventArgs e)
    {
        var menu = new ContextMenu { PlacementTarget = (UIElement)sender, Placement = PlacementMode.Bottom };

        var panelSettings = new MenuItem { Header = LocaleManager.Get("Menu.PanelSettings") };
        panelSettings.Click += (_, _) => ShowPanelSettings();

        var injectJs = new MenuItem { Header = LocaleManager.Get("Menu.InjectJs") };
        injectJs.Click += async (_, _) => { try { await ShowInjectDialog(isCss: false); } catch { } };

        var injectCss = new MenuItem { Header = LocaleManager.Get("Menu.InjectCss") };
        injectCss.Click += async (_, _) => { try { await ShowInjectDialog(isCss: true); } catch { } };

        var close = new MenuItem { Header = LocaleManager.Get("Menu.ClosePanel") };
        close.Click += (_, _) => CloseRequested?.Invoke();

        menu.Items.Add(panelSettings);
        menu.Items.Add(new Separator());
        menu.Items.Add(injectJs);
        menu.Items.Add(injectCss);
        menu.Items.Add(new Separator());
        menu.Items.Add(close);
        menu.IsOpen = true;
    }

    private void ShowPanelSettings()
    {
        var currentUrl = WebView.Source?.ToString() ?? Config.Url;
        var dialog = new Dialogs.PanelSettingsDialog(Config.Url, Config.Width, currentUrl)
        {
            Owner = Window.GetWindow(this)
        };
        if (dialog.ShowDialog() != true) return;

        Config.Url = dialog.InitialUrl;
        var newWidth = dialog.PanelWidth;
        if (Math.Abs(Config.Width - newWidth) > 0.5)
        {
            Config.Width = newWidth;
            Width = newWidth;
        }
        StateChanged?.Invoke();
    }

    private async Task ShowInjectDialog(bool isCss)
    {
        var existing = isCss
            ? Config.OnLoadStyles.FirstOrDefault()
            : Config.OnLoadScripts.FirstOrDefault();

        var dialog = new InjectDialog(isCss, existing) { Owner = Window.GetWindow(this) };
        if (dialog.ShowDialog() != true) return;

        if (dialog.IsDeleted)
        {
            if (existing != null)
            {
                if (isCss) DeleteStyle(existing);
                else DeleteScript(existing);
            }
            return;
        }

        if (existing != null)
        {
            if (isCss) await UpdateStyleAsync(existing, dialog.Code);
            else await UpdateScriptAsync(existing, dialog.Code);
        }
        else
        {
            if (isCss) await InjectCssAsync(dialog.Code);
            else await InjectScriptAsync(dialog.Code);
        }
    }
}
