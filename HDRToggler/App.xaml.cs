using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Forms;

namespace HDRToggler;

public partial class App : System.Windows.Application
{
    private NotifyIcon _trayIcon = null!;
    private System.Drawing.Icon? _appIcon;
    private MainWindow? _mainWindow;
    private HotkeyWindow? _hotkeyWindow;
    private AppSettings? _settings;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        _appIcon = LoadEmbeddedIcon();

        _trayIcon = new NotifyIcon
        {
            Icon    = _appIcon ?? System.Drawing.SystemIcons.Application,
            Visible = true,
            Text    = "HDR Toggler",
        };

        _trayIcon.MouseClick += TrayIcon_MouseClick;

        _hotkeyWindow = new HotkeyWindow();
        _hotkeyWindow.CreateHandle();

        _settings = SettingsService.Load();
        HotkeyService.Initialize(_hotkeyWindow, _settings);
        HotkeyService.MonitorToggled += OnHotkeyToggle;

        RestoreHdrStates();
        UpdateTrayTooltip();

        HdrService.StateChanged += OnAnyStateChanged;

        OpenMainWindow();
    }

    private void OnAnyStateChanged(object? source)
    {
        if (source is App) return;
        Dispatcher.BeginInvoke(() =>
        {
            UpdateTrayTooltip();
            var monitors = HdrService.GetMonitors();
            foreach (var m in monitors)
                SaveHdrState(m.FriendlyName, m.HdrEnabled);
        });
    }

    private void OnHotkeyToggle(int monitorIndex)
    {
        var monitors = HdrService.GetMonitors();
        if (monitorIndex >= 0 && monitorIndex < monitors.Count)
        {
            var monitor = monitors[monitorIndex];
            HdrService.ToggleHdr(monitor, source: this);

            var newName = monitor.FriendlyName;
            var newEnabled = !monitor.HdrEnabled;
            _trayIcon.ShowBalloonTip(1500, "HDR Toggled",
                $"{newName}: HDR {(newEnabled ? "ON" : "OFF")}",
                ToolTipIcon.Info);

            UpdateTrayTooltip();
            SaveHdrState(newName, newEnabled);
        }
    }

    private void TrayIcon_MouseClick(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            OpenMainWindow();
        }
        else if (e.Button == MouseButtons.Right)
        {
            var menu = new TrayMenuWindow(DoExit, OpenSettings, ToggleStartup);
            menu.ShowNearCursor();
        }
    }

    private void OpenMainWindow()
    {
        if (_mainWindow is { IsVisible: true })
        {
            _mainWindow.Activate();
            return;
        }

        _mainWindow = new MainWindow(HdrService.GetMonitors());
        _mainWindow.Show();
        _mainWindow.Activate();
    }

    private void OpenSettings()
    {
        var settingsWindow = new SettingsWindow();
        settingsWindow.ShowDialog();

        _settings = SettingsService.Load();
        HotkeyService.UnregisterAll();
        foreach (var (monitorIndex, binding) in _settings.Hotkeys)
        {
            if (binding is { Enabled: true, IsEmpty: false })
                HotkeyService.Register(monitorIndex, binding);
        }
    }

    private void DoExit()
    {
        HotkeyService.Shutdown();
        _hotkeyWindow?.DestroyHandle();

        _trayIcon.Visible = false;
        _trayIcon.Dispose();
        _appIcon?.Dispose();
        Shutdown();
    }

    private void ToggleStartup()
    {
        if (_settings is null) return;

        if (_settings.StartupEnabled)
        {
            if (StartupService.Disable())
                _settings.StartupEnabled = false;
        }
        else
        {
            if (StartupService.Enable())
                _settings.StartupEnabled = true;
        }

        SettingsService.Save(_settings);
    }

    private void RestoreHdrStates()
    {
        if (_settings is null || _settings.HdrStates.Count == 0) return;

        var monitors = HdrService.GetMonitors();
        foreach (var monitor in monitors)
        {
            if (_settings.HdrStates.TryGetValue(monitor.FriendlyName, out bool desired) && desired != monitor.HdrEnabled)
                HdrService.SetHdr(monitor, desired, source: this);
        }
    }

    private void UpdateTrayTooltip()
    {
        var monitors = HdrService.GetMonitors();
        if (monitors.Count == 0)
        {
            _trayIcon.Text = "HDR Toggler";
            return;
        }

        var parts = monitors.Select(m => $"{m.FriendlyName}: {(m.HdrEnabled ? "ON" : "OFF")}");
        var tooltip = "HDR Toggler\n" + string.Join("\n", parts);

        // NotifyIcon.Text has a 63-char limit
        if (tooltip.Length > 63)
            tooltip = tooltip[..63];

        _trayIcon.Text = tooltip;
    }

    private void SaveHdrState(string friendlyName, bool enabled)
    {
        if (_settings is null) return;
        _settings.HdrStates[friendlyName] = enabled;
        SettingsService.Save(_settings);
    }

    private static System.Drawing.Icon? LoadEmbeddedIcon()
    {
        var asm = Assembly.GetExecutingAssembly();
        using var stream = asm.GetManifestResourceStream("HDRToggler.icon.HDRToggler.ico");
        if (stream is null) return null;
        return new System.Drawing.Icon(stream);
    }
}
