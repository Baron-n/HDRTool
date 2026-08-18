using Microsoft.Win32;

namespace HDRToggler;

public static class StartupService
{
    private const string RegPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RegValue = "HDRToggler";

    public static bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegPath, false);
            return key?.GetValue(RegValue) is not null;
        }
        catch { return false; }
    }

    public static bool Enable()
    {
        try
        {
            var exePath = Environment.ProcessPath ?? "";
            using var key = Registry.CurrentUser.OpenSubKey(RegPath, true);
            if (key is null) return false;
            key.SetValue(RegValue, $"\"{exePath}\"");
            return true;
        }
        catch { return false; }
    }

    public static bool Disable()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RegPath, true);
            if (key is null) return false;
            key.DeleteValue(RegValue, false);
            return true;
        }
        catch { return false; }
    }
}
