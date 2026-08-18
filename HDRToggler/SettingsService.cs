using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Input;

namespace HDRToggler;

public record HotkeyBinding
{
    public Key Key { get; set; } = Key.None;
    public ModifierKeys Modifiers { get; set; } = ModifierKeys.None;
    public bool Enabled { get; set; }

    [JsonIgnore]
    public bool IsEmpty => Key == Key.None;

    public string DisplayText
    {
        get
        {
            if (IsEmpty) return "None";
            var parts = new List<string>();
            if (Modifiers.HasFlag(ModifierKeys.Control)) parts.Add("Ctrl");
            if (Modifiers.HasFlag(ModifierKeys.Alt))     parts.Add("Alt");
            if (Modifiers.HasFlag(ModifierKeys.Shift))   parts.Add("Shift");
            if (Modifiers.HasFlag(ModifierKeys.Windows)) parts.Add("Win");
            parts.Add(KeyToString(Key));
            return string.Join("+", parts);
        }
    }

    private static string KeyToString(Key key)
    {
        var name = key.ToString();
        if (name.StartsWith("D") && name.Length == 2 && char.IsDigit(name[1]))
            return name[1].ToString();
        if (name.StartsWith("NumPad"))
            return "Num " + name[6..];
        return name;
    }
}

public class AppSettings
{
    public bool StartupEnabled { get; set; }
    public Dictionary<int, HotkeyBinding> Hotkeys { get; set; } = new();
    public Dictionary<string, bool> HdrStates { get; set; } = new();
}

public static class SettingsService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private static string SettingsPath =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HDRToggler", "settings.json");

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json, JsonOpts) ?? new AppSettings();
            }
        }
        catch { }
        return new AppSettings();
    }

    public static void Save(AppSettings settings)
    {
        try
        {
            var dir = Path.GetDirectoryName(SettingsPath);
            if (dir is not null) Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(settings, JsonOpts);
            File.WriteAllText(SettingsPath, json);
        }
        catch { }
    }
}
