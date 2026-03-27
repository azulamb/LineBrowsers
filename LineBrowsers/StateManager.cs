using System.IO;
using System.Text.Json;

namespace LineBrowsers;

public static class StateManager
{
    private static readonly string StatePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "LineBrowsers", "state.json");

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static AppState Load()
    {
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
        Directory.CreateDirectory(Path.GetDirectoryName(StatePath)!);
        File.WriteAllText(StatePath, JsonSerializer.Serialize(state, JsonOptions));
    }
}
