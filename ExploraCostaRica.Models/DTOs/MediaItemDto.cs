
using System.Text.Json.Serialization;

namespace ExploraCostaRica.Models.DTOs
{
    public class MediaItemDto
    {
        // Versión del formato de intercambio.
        [JsonPropertyName("schemaVersion")]
        public string SchemaVersion { get; set; } = "1.0";

        // Identificador externo del contenido.
        [JsonPropertyName("externalId")]
        public string? ExternalId { get; set; }

        // Información principal.
        [JsonPropertyName("title")]
        public string Title { get; set; } = "";

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("author")]
        public string? Author { get; set; }

        [JsonPropertyName("url")]
        public string? Url { get; set; }

        [JsonPropertyName("imageUrl")]
        public string? ImageUrl { get; set; }

        // Clasificación del contenido.
        [JsonPropertyName("category")]
        public string? Category { get; set; }

        [JsonPropertyName("destination")]
        public string? Destination { get; set; }

        [JsonPropertyName("contentType")]
        public string? ContentType { get; set; }

        // Fuente original.
        [JsonPropertyName("source")]
        public string? Source { get; set; }

        // Fecha de publicación.
        [JsonPropertyName("publishedAt")]
        public DateTime? PublishedAt { get; set; }
    }
}
