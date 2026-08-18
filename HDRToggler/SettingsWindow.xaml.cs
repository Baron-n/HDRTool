using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace HDRToggler;

public class MonitorHotkeyViewModel : INotifyPropertyChanged
{
    private readonly int _monitorIndex;
    private bool _isRecording;
    private bool _hotkeyEnabled;
    private string _hotkeyDisplay = "None";
    private Key _key = Key.None;
    private ModifierKeys _modifiers = ModifierKeys.None;

    public event PropertyChangedEventHandler? PropertyChanged;

    public int MonitorIndex => _monitorIndex;
    public string FriendlyName { get; }

    public bool IsRecording
    {
        get => _isRecording;
        set { _isRecording = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsRecording))); }
    }

    public bool HotkeyEnabled
    {
        get => _hotkeyEnabled;
        set { _hotkeyEnabled = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HotkeyEnabled))); }
    }

    public string HotkeyDisplay
    {
        get => _hotkeyDisplay;
        set { _hotkeyDisplay = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(HotkeyDisplay))); }
    }

    public Key Key
    {
        get => _key;
        set => _key = value;
    }

    public ModifierKeys Modifiers
    {
        get => _modifiers;
        set => _modifiers = value;
    }

    public bool HdrEnabled { get; set; }

    public MonitorHotkeyViewModel(int monitorIndex, string friendlyName, HotkeyBinding? binding, bool hdrEnabled)
    {
        _monitorIndex = monitorIndex;
        FriendlyName = friendlyName;
        HdrEnabled = hdrEnabled;

        if (binding is not null)
        {
            _key = binding.Key;
            _modifiers = binding.Modifiers;
            _hotkeyEnabled = binding.Enabled;
            _hotkeyDisplay = binding.DisplayText;
        }
    }
}

public partial class SettingsWindow : Window
{
    private List<MonitorHotkeyViewModel> _viewModels = new();
    private MonitorHotkeyViewModel? _recordingViewModel;
    private bool _closing;

    public SettingsWindow()
    {
        InitializeComponent();
        LoadMonitors();
    }

    private void LoadMonitors()
    {
        var monitors = HdrService.GetMonitors();
        var settings = SettingsService.Load();

        _viewModels = monitors.Select((m, i) =>
        {
            settings.Hotkeys.TryGetValue(i, out var binding);
            return new MonitorHotkeyViewModel(i, m.FriendlyName, binding, m.HdrEnabled);
        }).ToList();

        MonitorList.ItemsSource = _viewModels;
    }

    private void Root_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left && e.Source == Root && e.LeftButton == MouseButtonState.Pressed)
            DragMove();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private void RecordButton_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not System.Windows.FrameworkElement fe) return;
        if (fe.DataContext is not MonitorHotkeyViewModel vm) return;

        if (_recordingViewModel is not null)
        {
            _recordingViewModel.IsRecording = false;
            _recordingViewModel.HotkeyDisplay = _recordingViewModel.Key == Key.None
                ? "None"
                : FormatHotkey(_recordingViewModel.Key, _recordingViewModel.Modifiers);
        }

        _recordingViewModel = vm;
        vm.IsRecording = true;
        vm.HotkeyDisplay = "Press keys...";
        StatusText.Text = "";

        PreviewKeyDown += OnPreviewKeyDown;
    }

    private void OnPreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (_recordingViewModel is null) return;

        var key = e.Key == Key.System ? e.SystemKey : e.Key;

        if (key is Key.LeftCtrl or Key.RightCtrl or Key.LeftAlt or Key.RightAlt
            or Key.LeftShift or Key.RightShift or Key.LWin or Key.RWin)
            return;

        if (key == Key.Escape)
        {
            CancelRecording();
            e.Handled = true;
            return;
        }

        var mod = Keyboard.Modifiers;

        if (mod == ModifierKeys.None)
        {
            StatusText.Text = "Please include at least one modifier key (Ctrl, Alt, Shift, or Win).";
            e.Handled = true;
            return;
        }

        if (HotkeyService.IsKeyInUse(key, mod, _recordingViewModel.MonitorIndex))
        {
            StatusText.Text = "This combination is already assigned to another monitor.";
            e.Handled = true;
            return;
        }

        _recordingViewModel.Key = key;
        _recordingViewModel.Modifiers = mod;
        _recordingViewModel.HotkeyDisplay = FormatHotkey(key, mod);
        _recordingViewModel.IsRecording = false;
        _recordingViewModel.HotkeyEnabled = true;
        _recordingViewModel = null;

        PreviewKeyDown -= OnPreviewKeyDown;
        StatusText.Text = "";
        e.Handled = true;
    }

    private void CancelRecording()
    {
        if (_recordingViewModel is null) return;
        _recordingViewModel.IsRecording = false;
        _recordingViewModel.HotkeyDisplay = _recordingViewModel.Key == Key.None
            ? "None"
            : FormatHotkey(_recordingViewModel.Key, _recordingViewModel.Modifiers);
        _recordingViewModel = null;
        PreviewKeyDown -= OnPreviewKeyDown;
        StatusText.Text = "";
    }

    private void ToggleEnabled_Click(object sender, MouseButtonEventArgs e)
    {
        if (sender is not System.Windows.FrameworkElement fe) return;
        if (fe.DataContext is not MonitorHotkeyViewModel vm) return;
        if (vm.Key == Key.None) return;

        vm.HotkeyEnabled = !vm.HotkeyEnabled;
    }

    private void ClearAll_Click(object sender, RoutedEventArgs e)
    {
        foreach (var vm in _viewModels)
        {
            vm.Key = Key.None;
            vm.Modifiers = ModifierKeys.None;
            vm.HotkeyEnabled = false;
            vm.HotkeyDisplay = "None";
        }
        StatusText.Text = "All hotkeys cleared. Click Save to apply.";
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (_recordingViewModel is not null)
            CancelRecording();

        var settings = SettingsService.Load();
        settings.Hotkeys.Clear();
        foreach (var vm in _viewModels)
        {
            if (vm.Key != Key.None)
            {
                settings.Hotkeys[vm.MonitorIndex] = new HotkeyBinding
                {
                    Key = vm.Key,
                    Modifiers = vm.Modifiers,
                    Enabled = vm.HotkeyEnabled,
                };
            }
        }

        SettingsService.Save(settings);

        HotkeyService.UnregisterAll();
        foreach (var (index, binding) in settings.Hotkeys)
        {
            if (binding is { Enabled: true, IsEmpty: false })
                HotkeyService.Register(index, binding);
        }

        StatusText.Text = "Settings saved.";
    }

    protected override void OnDeactivated(EventArgs e)
    {
        base.OnDeactivated(e);
        if (!_closing && _recordingViewModel is not null)
            CancelRecording();
    }

    protected override void OnClosed(EventArgs e)
    {
        _closing = true;
        PreviewKeyDown -= OnPreviewKeyDown;
        base.OnClosed(e);
    }

    private static string FormatHotkey(Key key, ModifierKeys mod)
    {
        var parts = new List<string>();
        if (mod.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
        if (mod.HasFlag(ModifierKeys.Alt))     parts.Add("Alt");
        if (mod.HasFlag(ModifierKeys.Shift))   parts.Add("Shift");
        if (mod.HasFlag(ModifierKeys.Windows)) parts.Add("Win");

        var keyName = key.ToString();
        if (keyName.StartsWith("D") && keyName.Length == 2 && char.IsDigit(keyName[1]))
            keyName = keyName[1].ToString();
        else if (keyName.StartsWith("NumPad"))
            keyName = "Num " + keyName[6..];

        parts.Add(keyName);
        return string.Join("+", parts);
    }
}
