using System.Text.Json;
using System.Text.Json.Serialization;

namespace LineBrowsers;

public enum AppTheme { Auto, Light, Dark }
public enum AppLocale { Ja, En }

public class SessionConfig
{
    public string SessionId { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "New Session";
    public string ProfilePath { get; set; } = "";
}

// Supports migration from the old List<string> format (plain string → InjectionEntry).
[JsonConverter(typeof(InjectionEntryConverter))]
public class InjectionEntry
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Code { get; set; } = "";
}

public class InjectionEntryConverter : JsonConverter<InjectionEntry>
{
    public override InjectionEntry Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
            return new InjectionEntry { Code = reader.GetString() ?? "" };

        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;
        return new InjectionEntry
        {
            Id = root.TryGetProperty("Id", out var id) ? id.GetString() ?? Guid.NewGuid().ToString() : Guid.NewGuid().ToString(),
            Code = root.TryGetProperty("Code", out var code) ? code.GetString() ?? "" : ""
        };
    }

    public override void Write(Utf8JsonWriter writer, InjectionEntry value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteString("Id", value.Id);
        writer.WriteString("Code", value.Code);
        writer.WriteEndObject();
    }
}

public class PanelConfig
{
    public string PanelId { get; set; } = Guid.NewGuid().ToString();
    public string SessionId { get; set; } = "";
    public string Url { get; set; } = "https://x.com";
    public double Width { get; set; } = 400;
    public List<InjectionEntry> OnLoadScripts { get; set; } = new();
    public List<InjectionEntry> OnLoadStyles { get; set; } = new();
    public bool IsMobile { get; set; } = false;
}

public class AppState
{
    public List<SessionConfig> Sessions { get; set; } = new();
    public List<PanelConfig> Panels { get; set; } = new();
    public AppTheme Theme { get; set; } = AppTheme.Auto;
    public AppLocale Locale { get; set; } = AppLocale.Ja;
}
