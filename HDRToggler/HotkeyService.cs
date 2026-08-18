using System.Windows;
using System.Windows.Input;

namespace HDRToggler;

public static class HotkeyService
{
    private static HotkeyWindow? _window;
    private static readonly Dictionary<int, int> _monitorIndexToId = new();

    public static event Action<int>? MonitorToggled;

    public static void Initialize(HotkeyWindow window, AppSettings settings)
    {
        _window = window;
        _window.HotkeyPressed += OnHotkeyPressed;

        foreach (var (monitorIndex, binding) in settings.Hotkeys)
        {
            if (binding is { Enabled: true, IsEmpty: false })
                Register(monitorIndex, binding);
        }
    }

    public static void Shutdown()
    {
        if (_window is not null)
            _window.HotkeyPressed -= OnHotkeyPressed;

        UnregisterAll();
    }

    public static bool Register(int monitorIndex, HotkeyBinding binding)
    {
        if (_window is null || binding.IsEmpty) return false;

        Unregister(monitorIndex);

        int id = NativeMethods.HOTKEY_ID_BASE + monitorIndex;
        uint mod = ModifiersToWin32(binding.Modifiers);
        uint vk = KeyToVirtualKey(binding.Key);

        bool ok = NativeMethods.RegisterHotKey(_window.Handle, id, mod, vk);
        if (ok)
            _monitorIndexToId[monitorIndex] = id;

        return ok;
    }

    public static void Unregister(int monitorIndex)
    {
        if (_window is null) return;
        if (_monitorIndexToId.Remove(monitorIndex, out int id))
            NativeMethods.UnregisterHotKey(_window.Handle, id);
    }

    public static void UnregisterAll()
    {
        if (_window is null) return;
        foreach (var id in _monitorIndexToId.Values)
            NativeMethods.UnregisterHotKey(_window.Handle, id);
        _monitorIndexToId.Clear();
    }

    public static bool IsKeyInUse(Key key, ModifierKeys modifiers, int excludeMonitorIndex = -1)
    {
        foreach (var (monitorIndex, binding) in GetAllBindings())
        {
            if (monitorIndex == excludeMonitorIndex) continue;
            if (binding is { Enabled: true } && binding.Key == key && binding.Modifiers == modifiers)
                return true;
        }
        return false;
    }

    public static IEnumerable<(int MonitorIndex, HotkeyBinding Binding)> GetAllBindings()
    {
        var settings = SettingsService.Load();
        return settings.Hotkeys.Select(kv => (kv.Key, kv.Value));
    }

    private static void OnHotkeyPressed(int hotkeyId)
    {
        int monitorIndex = hotkeyId - NativeMethods.HOTKEY_ID_BASE;
        MonitorToggled?.Invoke(monitorIndex);
    }

    private static uint ModifiersToWin32(ModifierKeys mod)
    {
        uint result = 0;
        if (mod.HasFlag(ModifierKeys.Control)) result |= NativeMethods.MOD_CONTROL;
        if (mod.HasFlag(ModifierKeys.Alt))     result |= NativeMethods.MOD_ALT;
        if (mod.HasFlag(ModifierKeys.Shift))   result |= NativeMethods.MOD_SHIFT;
        if (mod.HasFlag(ModifierKeys.Windows)) result |= NativeMethods.MOD_WIN;
        return result;
    }

    private static uint KeyToVirtualKey(Key key)
    {
        return key switch
        {
            Key.A => 0x41, Key.B => 0x42, Key.C => 0x43, Key.D => 0x44,
            Key.E => 0x45, Key.F => 0x46, Key.G => 0x47, Key.H => 0x48,
            Key.I => 0x49, Key.J => 0x4A, Key.K => 0x4B, Key.L => 0x4C,
            Key.M => 0x4D, Key.N => 0x4E, Key.O => 0x4F, Key.P => 0x50,
            Key.Q => 0x51, Key.R => 0x52, Key.S => 0x53, Key.T => 0x54,
            Key.U => 0x55, Key.V => 0x56, Key.W => 0x57, Key.X => 0x58,
            Key.Y => 0x59, Key.Z => 0x5A,

            Key.D0 => 0x30, Key.D1 => 0x31, Key.D2 => 0x32, Key.D3 => 0x33,
            Key.D4 => 0x34, Key.D5 => 0x35, Key.D6 => 0x36, Key.D7 => 0x37,
            Key.D8 => 0x38, Key.D9 => 0x39,

            Key.F1 => 0x70, Key.F2 => 0x71, Key.F3 => 0x72, Key.F4 => 0x73,
            Key.F5 => 0x74, Key.F6 => 0x75, Key.F7 => 0x76, Key.F8 => 0x77,
            Key.F9 => 0x78, Key.F10 => 0x79, Key.F11 => 0x7A, Key.F12 => 0x7B,

            Key.NumPad0 => 0x60, Key.NumPad1 => 0x61, Key.NumPad2 => 0x62,
            Key.NumPad3 => 0x63, Key.NumPad4 => 0x64, Key.NumPad5 => 0x65,
            Key.NumPad6 => 0x66, Key.NumPad7 => 0x67, Key.NumPad8 => 0x68,
            Key.NumPad9 => 0x69,

            Key.Space     => 0x20,
            Key.Enter     => 0x0D,
            Key.Tab       => 0x09,
            Key.Escape    => 0x1B,
            Key.Back      => 0x08,
            Key.OemPeriod => 0xBE,
            Key.OemComma  => 0xBC,

            _ => 0,
        };
    }
}
