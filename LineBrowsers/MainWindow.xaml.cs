using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using LineBrowsers.Dialogs;
using Microsoft.Web.WebView2.Core;

namespace LineBrowsers;

public partial class MainWindow : Window
{
    private readonly AppState _state;
    private readonly Dictionary<string, CoreWebView2Environment> _environments = new();
    private PreviewWindow? _previewWindow;
    private double _scrollOffset;

    public MainWindow()
    {
        InitializeComponent();
        Icon = new System.Windows.Media.Imaging.BitmapImage(
            new Uri("pack://application:,,,/app.ico"));
        _state = StateManager.Load();
        if (StateManager.IsPrivate)
            Title = "LineBrowsers — " + LocaleManager.Get("Label.PrivateMode");
        Loaded += MainWindow_Loaded;
        SizeChanged += (_, _) => UpdatePreviewBounds();
        LocationChanged += (_, _) => UpdatePreviewBounds();
        StateChanged += (_, _) => UpdatePreviewBounds();
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        foreach (var panel in _state.Panels.ToList())
        {
            var session = _state.Sessions.FirstOrDefault(s => s.SessionId == panel.SessionId);
            if (session == null) continue;
            try { await AddPanelToUI(panel, session); } catch { }
        }
    }

    private async Task<CoreWebView2Environment> GetOrCreateEnvironment(SessionConfig session)
    {
        if (_environments.TryGetValue(session.SessionId, out var existing))
            return existing;

        if (string.IsNullOrEmpty(session.ProfilePath))
            session.ProfilePath = StateManager.IsPrivate
                ? System.IO.Path.Combine(StateManager.PrivateTempRoot!, session.SessionId)
                : System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "LineBrowsers", "Profiles", session.SessionId);

        var env = await CoreWebView2Environment.CreateAsync(null, session.ProfilePath);
        _environments[session.SessionId] = env;
        return env;
    }

    private async Task AddPanelToUI(PanelConfig config, SessionConfig session)
    {
        var env = await GetOrCreateEnvironment(session);
        var column = new BrowserColumn(config, session, env);
        column.CloseRequested += () => RemovePanel(column);
        column.StateChanged += () => { UpdatePanelPositions(); StateManager.Save(_state); };
        column.PreviewRequested += (url, e) => ShowPreview(url, e);
        column.MoveLeftRequested  += () => MovePanel(column, -1);
        column.MoveRightRequested += () => MovePanel(column, +1);
        Canvas.SetTop(column, 0);
        PanelHost.Children.Add(column);
        UpdatePanelPositions();
        await column.InitializeAsync();
    }

    private void MovePanel(BrowserColumn column, int direction)
    {
        var index = PanelHost.Children.IndexOf(column);
        var newIndex = index + direction;
        if (newIndex < 0 || newIndex >= PanelHost.Children.Count) return;

        PanelHost.Children.Remove(column);
        PanelHost.Children.Insert(newIndex, column);

        _state.Panels.Remove(column.Config);
        _state.Panels.Insert(newIndex, column.Config);

        UpdatePanelPositions();
        StateManager.Save(_state);
    }

    private void RemovePanel(BrowserColumn column)
    {
        _state.Panels.Remove(column.Config);
        PanelHost.Children.Remove(column);
        column.Dispose();
        UpdatePanelPositions();
        StateManager.Save(_state);
    }

    private void UpdatePanelPositions()
    {
        double totalWidth = 0;
        foreach (BrowserColumn col in PanelHost.Children)
            totalWidth += col.Config.Width;

        double viewportWidth = PanelHost.ActualWidth;
        if (viewportWidth <= 0) return;

        double maxScroll = Math.Max(0, totalWidth - viewportWidth);
        _scrollOffset = Math.Clamp(_scrollOffset, 0, maxScroll);

        double x = -_scrollOffset;
        foreach (BrowserColumn col in PanelHost.Children)
        {
            double cw = col.Config.Width;

            Canvas.SetLeft(col, x);
            col.Height = PanelHost.ActualHeight;

            col.Visibility = Visibility.Visible;
            col.Width = cw;

            x += cw;
        }

        MainScrollBar.Maximum = maxScroll;
        MainScrollBar.ViewportSize = viewportWidth;
        MainScrollBar.Value = _scrollOffset;
        MainScrollBar.Visibility = maxScroll > 0 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void PanelHost_SizeChanged(object sender, SizeChangedEventArgs e)
        => UpdatePanelPositions();

    // ------------------------------------------------------------------ preview

    private async void ShowPreview(string url, CoreWebView2Environment env)
    {
        HidePreview();

        var win = new PreviewWindow(url, env) { Owner = this };
        win.Closed += (_, _) => _previewWindow = null;
        _previewWindow = win;
        UpdatePreviewBounds();
        win.Show();

        await win.InitializeAsync();
    }

    private void UpdatePreviewBounds()
    {
        if (_previewWindow == null) return;

        double left, top, width, height;
        if (WindowState == WindowState.Maximized)
        {
            // Get the work area of the monitor this window is currently on.
            var hwnd = new WindowInteropHelper(this).Handle;
            var hMon = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);
            var mi = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
            GetMonitorInfo(hMon, ref mi);
            left   = mi.rcWork.Left;
            top    = mi.rcWork.Top;
            width  = mi.rcWork.Right  - mi.rcWork.Left;
            height = mi.rcWork.Bottom - mi.rcWork.Top;
        }
        else
        {
            left   = Left;
            top    = Top;
            width  = ActualWidth;
            height = ActualHeight;
        }

        _previewWindow.Left   = left + width / 2;
        _previewWindow.Top    = top;
        _previewWindow.Width  = width / 2;
        _previewWindow.Height = height;
    }

    // Win32 helpers for monitor detection
    private const uint MONITOR_DEFAULTTONEAREST = 2;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    private struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);


    private void HidePreview()
    {
        _previewWindow?.Close();
        _previewWindow = null;
    }

    private void MainScrollBar_Scroll(object sender, ScrollEventArgs e)
    {
        _scrollOffset = e.NewValue;
        UpdatePanelPositions();
    }

    // ------------------------------------------------------------------ toolbar

    private async void AddPanel_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new AddPanelDialog(_state.Sessions) { Owner = this };
        if (dialog.ShowDialog() != true) return;

        SessionConfig session;
        if (dialog.IsNewSession)
        {
            session = new SessionConfig { Name = dialog.SessionName };
            _state.Sessions.Add(session);
        }
        else
        {
            session = _state.Sessions.First(s => s.SessionId == dialog.SelectedSessionId);
        }

        var config = new PanelConfig
        {
            SessionId = session.SessionId,
            Url = dialog.Url,
            Width = dialog.PanelWidth,
        };

        _state.Panels.Add(config);
        StateManager.Save(_state);
        await AddPanelToUI(config, session);
    }

    private void Sessions_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SessionManagerDialog(_state.Sessions, CanDeleteSession) { Owner = this };
        dialog.ShowDialog();
        StateManager.Save(_state);
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new SettingsDialog(_state.Theme, _state.Locale) { Owner = this };
        dialog.ShowDialog();
        _state.Theme = dialog.SelectedTheme;
        _state.Locale = dialog.SelectedLocale;
        StateManager.Save(_state);
    }

    private bool CanDeleteSession(string sessionId) =>
        !_state.Panels.Any(p => p.SessionId == sessionId);

    private void AboutButton_Click(object sender, RoutedEventArgs e)
        => new AboutDialog { Owner = this }.ShowDialog();
}
