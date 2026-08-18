using System.Windows;
using System.Windows.Interop;

namespace HDRToggler;

public class HotkeyWindow : Window
{
    private HwndSource? _hwndSource;

    public event Action<int>? HotkeyPressed;

    public void CreateHandle()
    {
        var parameters = new HwndSourceParameters("HDRToggler_HotkeyReceiver")
        {
            Width  = 0,
            Height = 0,
            WindowStyle = 0,
        };

        _hwndSource = new HwndSource(parameters);
        _hwndSource.AddHook(WndProc);
    }

    public void DestroyHandle()
    {
        if (_hwndSource is not null)
        {
            _hwndSource.RemoveHook(WndProc);
            _hwndSource.Dispose();
            _hwndSource = null;
        }
    }

    public IntPtr Handle => _hwndSource?.Handle ?? IntPtr.Zero;

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == NativeMethods.WM_HOTKEY)
        {
            int id = wParam.ToInt32();
            HotkeyPressed?.Invoke(id);
            handled = true;
        }
        return IntPtr.Zero;
    }
}
