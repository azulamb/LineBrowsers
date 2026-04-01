using System.IO;
using System.Text.Json;

namespace LineBrowsers;

public static class StateManager
{
    private static readonly string StatePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "LineBrowsers", "state.json");

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static bool IsPrivate { get; private set; }
    public static string? PrivateTempRoot { get; private set; }

    public static void EnablePrivateMode()
    {
        IsPrivate = true;
        PrivateTempRoot = Path.Combine(
            Path.GetTempPath(), "LineBrowsers_priv_" + Guid.NewGuid().ToString("N"));
    }

    public static AppState Load()
    {
        if (IsPrivate) return new AppState();
        try
        {
            if (File.Exists(StatePath))
                return JsonSerializer.Deserialize<AppState>(File.ReadAllText(StatePath)) ?? new AppState();
        }
        catch { }
        return new AppState();
    }

    public static void Save(AppState state)
    {
        if (IsPrivate) return;
        Directory.CreateDirectory(Path.GetDirectoryName(StatePath)!);
        File.WriteAllText(StatePath, JsonSerializer.Serialize(state, JsonOptions));
    }

    public static void CleanupPrivateTemp()
    {
        if (PrivateTempRoot == null || !Directory.Exists(PrivateTempRoot)) return;
        try { Directory.Delete(PrivateTempRoot, recursive: true); } catch { }
    }
}
