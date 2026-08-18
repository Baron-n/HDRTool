using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace HDRToggler;

public class TrayMenuViewModel : INotifyPropertyChanged
{
    private bool _startupEnabled;

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool StartupEnabled
    {
        get => _startupEnabled;
        set { _startupEnabled = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(StartupEnabled))); }
    }

    public TrayMenuViewModel(bool startupEnabled)
    {
        _startupEnabled = startupEnabled;
    }
}

public partial class TrayMenuWindow : Window
{
    private readonly Action _onExit;
    private readonly Action _onSettings;
    private readonly Action _onToggleStartup;
    private List<HdrMonitor> _monitors;
    private bool _closing;

    public TrayMenuWindow(Action onExit, Action onSettings, Action onToggleStartup)
    {
        InitializeComponent();
        _onExit = onExit;
        _onSettings = onSettings;
        _onToggleStartup = onToggleStartup;

        var settings = SettingsService.Load();
        DataContext = new TrayMenuViewModel(settings.StartupEnabled);

        _monitors = HdrService.GetMonitors();
        MonitorList.ItemsSource = _monitors;

        HdrService.StateChanged += OnExternalStateChanged;
        Closed += (_, _) => HdrService.StateChanged -= OnExternalStateChanged;
    }

    private void OnExternalStateChanged(object? source)
    {
        if (source == this) return;
        Dispatcher.Invoke(() =>
        {
            _monitors = HdrService.GetMonitors();
            MonitorList.ItemsSource = _monitors;
        });
    }

    public void ShowNearCursor()
    {
        _cursorSnapshot = System.Windows.Forms.Cursor.Position;
        Left = -10000;
        Top  = -10000;
        Show();
        Activate();
    }

    private System.Drawing.Point _cursorSnapshot;

    protected override void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);

        var source = PresentationSource.FromVisual(this);
        if (source?.CompositionTarget is null) return;

        var transform = source.CompositionTarget.TransformFromDevice;

        var cursor = _cursorSnapshot;
        var screen = System.Windows.Forms.Screen.FromPoint(cursor);
        var work   = screen.WorkingArea;

        var curLogical  = transform.Transform(new System.Windows.Point(cursor.X, cursor.Y));
        var workTopLeft = transform.Transform(new System.Windows.Point(work.Left, work.Top));
        var workBotRight = transform.Transform(new System.Windows.Point(work.Right, work.Bottom));

        double w = ActualWidth;
        double h = ActualHeight;

        double x = curLogical.X - w;
        double y = curLogical.Y - h - 8;

        Left = Math.Max(workTopLeft.X, Math.Min(x, workBotRight.X - w));
        Top  = Math.Max(workTopLeft.Y, Math.Min(y, workBotRight.Y - h));
    }

    private void MonitorItem_Click(object sender, MouseButtonEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is not HdrMonitor monitor) return;

        _monitors = _monitors
            .Select(m => m.TargetId == monitor.TargetId ? m with { HdrEnabled = !m.HdrEnabled } : m)
            .ToList();
        MonitorList.ItemsSource = _monitors;

        HdrService.ToggleHdr(monitor, source: this);
    }

    private void SettingsItem_Click(object sender, MouseButtonEventArgs e)
    {
        _closing = true;
        Close();
        _onSettings();
    }

    private void StartupItem_Click(object sender, MouseButtonEventArgs e)
    {
        _onToggleStartup();

        if (DataContext is TrayMenuViewModel vm)
            vm.StartupEnabled = StartupService.IsEnabled();
    }

    private void ExitItem_Click(object sender, MouseButtonEventArgs e)
    {
        _closing = true;
        Close();
        _onExit();
    }

    private void Window_Deactivated(object sender, EventArgs e)
    {
        if (!_closing)
            Close();
    }
}
