using System.Text.Json;
using System.Text.Json.Serialization;

namespace ExploraCostaRica.Models.Exchange;



public class DashboardExport
{
    public const string CurrentVersion = "1.0";

    [JsonPropertyName("version")]
    public string Version { get; set; } = CurrentVersion;

    [JsonPropertyName("exportedAt")]
    public DateTime? ExportedAt { get; set; }

    [JsonPropertyName("exportedBy")]
    public string? ExportedBy { get; set; }

    [JsonPropertyName("sources")]
    public List<ExportSource> Sources { get; set; } = new();

    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    public static DashboardExport? FromJson(string json) =>
        JsonSerializer.Deserialize<DashboardExport>(json, JsonOptions);
}

public class ExportSource
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string? Url { get; set; }


    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("items")]
    public List<ExportItem> Items { get; set; } = new();
}

public class ExportItem
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;


    [JsonPropertyName("mediaType")]
    public string MediaType { get; set; } = MediaTypes.Text;


    [JsonPropertyName("createdAt")]
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("category")]
    public string? Category { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("thumbnail")]
    public string? Thumbnail { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }


    [JsonPropertyName("data")]
    public JsonElement? Data { get; set; }
}

public static class MediaTypes
{
    public const string Text = "text";
    public const string Image = "image";
    public const string Gif = "gif";
    public const string Video = "video";
    public const string Audio = "audio";

    public static readonly string[] All = { Text, Image, Gif, Video, Audio };
}

public static class ExportCategories
{
    public static readonly string[] All =
    {
        "noticias", "memes", "gifs", "imagenes", "videos", "musica", "libros",
        "clima", "deportes", "tecnologia", "entretenimiento", "finanzas", "ciencia", "otros"
    };
}
