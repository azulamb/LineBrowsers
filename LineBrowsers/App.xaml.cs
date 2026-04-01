using System.Threading;
using System.Windows;
using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;

namespace LineBrowsers;

public partial class App : Application
{
    private Mutex? _singleInstanceMutex;

    protected override void OnStartup(StartupEventArgs e)
    {
        try
        {
            CoreWebView2Environment.GetAvailableBrowserVersionString();
        }
        catch
        {
            MessageBox.Show(
                "WebView2 ランタイムが見つかりません。\nMicrosoft Edge がインストールされているか確認してください。",
                "起動エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
            return;
        }

        // Suppress E_INVALIDARG (0x80070057) thrown internally by WebView2's native layer.
        // These bubble up through the Dispatcher when fired from COM callbacks outside
        // managed stack frames and cannot be caught with try/catch in user code.
        DispatcherUnhandledException += (_, args) =>
        {
            if (args.Exception is ArgumentException && args.Exception.HResult == unchecked((int)0x80070057))
                args.Handled = true;
        };

#if DEBUG
        StateManager.EnablePrivateMode();
#else
        if (e.Args.Contains("--private"))
            StateManager.EnablePrivateMode();
#endif

        if (!StateManager.IsPrivate)
        {
            _singleInstanceMutex = new Mutex(initiallyOwned: true, "LineBrowsers_SingleInstance", out var createdNew);
            if (!createdNew)
            {
                _singleInstanceMutex.Dispose();
                _singleInstanceMutex = null;
                Shutdown(0);
                return;
            }
        }

        // Apply saved theme and locale before the window is created
        var state = StateManager.Load();
        ThemeManager.Apply(state.Theme);
        LocaleManager.Apply(state.Locale);

        // Watch for system theme changes (for Auto mode)
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        StateManager.CleanupPrivateTemp();
        _singleInstanceMutex?.ReleaseMutex();
        _singleInstanceMutex?.Dispose();
        base.OnExit(e);
    }

    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        if (e.Category == UserPreferenceCategory.General && ThemeManager.CurrentMode == AppTheme.Auto)
            Dispatcher.Invoke(() => ThemeManager.Apply(AppTheme.Auto));
    }
}
