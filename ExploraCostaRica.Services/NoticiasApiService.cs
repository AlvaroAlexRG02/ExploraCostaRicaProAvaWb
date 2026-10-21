using ExploraCostaRica.Models.DTOs;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace ExploraCostaRica.Services
{
    public class NoticiasApiService : INoticiasApiService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public NoticiasApiService(
            HttpClient httpClient,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
        }

        public async Task<List<NoticiaDto>> ObtenerNoticiasAsync(
            string busqueda = "Costa Rica")
        {
            var apiKey = _configuration["GNews:ApiKey"];

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException(
                    "Falta configurar la API Key de GNews.");
            }

            var consulta = Uri.EscapeDataString(busqueda);

            var url = "https://gnews.io/api/v4/search"
                + $"?q={consulta}"
                + "&lang=es"
                + "&max=10"
                + $"&apikey={Uri.EscapeDataString(apiKey)}";

            var respuesta = await _httpClient.GetAsync(url);

            respuesta.EnsureSuccessStatusCode();

            var datos = await respuesta.Content
                .ReadFromJsonAsync<GNewsRespuesta>();

            if (datos?.Articles == null)
            {
                return new List<NoticiaDto>();
            }

            return datos.Articles.Select(n => new NoticiaDto
            {
                Id = n.Url ?? "",
                Titulo = n.Title ?? "",
                Descripcion = n.Description ?? "",
                Contenido = n.Content ?? "",
                Url = n.Url ?? "",
                ImagenUrl = n.Image ?? "",
                Fuente = n.Source?.Name ?? "GNews",
                FechaPublicacion = n.PublishedAt

            }).ToList();
        }
        // Convierte las noticias al DTO común del proyecto.
        public async Task<List<MediaItemDto>> BuscarContenidoAsync(
            string busqueda = "Costa Rica")
        {
            var noticias = await ObtenerNoticiasAsync(busqueda);

            return noticias.Select(n => new MediaItemDto
            {
                SchemaVersion = "1.0",
                ExternalId = CrearExternalId(n.Url),
                Title = n.Titulo,
                Description = n.Descripcion,
                Author = n.Fuente,
                Url = n.Url,
                ImageUrl = string.IsNullOrWhiteSpace(n.ImagenUrl)
                    ? null
                    : n.ImagenUrl,
                Category = "Turismo",
                Destination = busqueda.Trim(),
                ContentType = "Noticia",
                Source = "GNews",
                PublishedAt = n.FechaPublicacion
            }).ToList();
        }

        // ExternalId en SQL admite 250 caracteres; las URLs pueden ser más largas.
        private static string CrearExternalId(string url)
        {
            return url.Length <= 250
                ? url
                : Convert.ToHexString(
                    SHA256.HashData(Encoding.UTF8.GetBytes(url)));
        }
        private class GNewsRespuesta
        {
            [JsonPropertyName("articles")]
            public List<GNewsArticulo> Articles { get; set; } = new();
        }

        private class GNewsArticulo
        {
            [JsonPropertyName("title")]
            public string? Title { get; set; }

            [JsonPropertyName("description")]
            public string? Description { get; set; }

            [JsonPropertyName("content")]
            public string? Content { get; set; }

            [JsonPropertyName("url")]
            public string? Url { get; set; }

            [JsonPropertyName("image")]
            public string? Image { get; set; }

            [JsonPropertyName("publishedAt")]
            public DateTime? PublishedAt { get; set; }

            [JsonPropertyName("source")]
            public GNewsFuente? Source { get; set; }
        }

        private class GNewsFuente
        {
            [JsonPropertyName("name")]
            public string? Name { get; set; }
        }
    }
}
