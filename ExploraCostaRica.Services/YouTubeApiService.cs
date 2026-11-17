using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using ExploraCostaRica.Models.DTOs;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace ExploraCostaRica.Services
{
    // ============================================================
    // API 4 - YOUTUBE DATA API v3
    // ------------------------------------------------------------
    // Busca videos de destinos, naturaleza y cultura de Costa Rica.
    //
    // Documentación: https://developers.google.com/youtube/v3/docs/search/list
    // Endpoint usado: GET /youtube/v3/search
    //
    // IMPORTANTE - CUOTA:
    // YouTube da 10 000 unidades diarias gratis y CADA búsqueda
    // gasta 100 unidades (≈ 100 búsquedas al día). Por eso los
    // resultados se guardan en caché 6 horas: la misma búsqueda
    // no vuelve a gastar cuota.
    //
    // La API Key NO está en el código: se lee de User Secrets
    // con el nombre "YouTube:ApiKey".
    // ============================================================
    public class YouTubeApiService : IYouTubeApiService
    {
        private static readonly TimeSpan DuracionCache = TimeSpan.FromHours(6);

        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;

        public YouTubeApiService(
            HttpClient httpClient,
            IMemoryCache cache,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _cache = cache;
            _configuration = configuration;

            _httpClient.BaseAddress = new Uri("https://www.googleapis.com/youtube/v3/");

            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        // ========================================================
        // 1. CONSULTAR YOUTUBE
        // ========================================================
        public async Task<List<VideoYouTubeDto>> BuscarVideosAsync(
            string busqueda, int cantidad = 12)
        {
            var lista = new List<VideoYouTubeDto>();

            if (string.IsNullOrWhiteSpace(busqueda))
            {
                return lista;
            }

            // La clave se pide aquí (y no en el constructor) para que,
            // si falta, solo falle la búsqueda de videos y no toda la app.
            string? apiKey = _configuration["YouTube:ApiKey"];

            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new InvalidOperationException(
                    "Falta configurar la API Key de YouTube (YouTube:ApiKey).");
            }

            // Agregamos "Costa Rica" si la persona no lo escribió,
            // para que los videos sean del país.
            string consulta = busqueda.Trim();

            if (!consulta.Contains("costa rica", StringComparison.OrdinalIgnoreCase))
            {
                consulta += " Costa Rica";
            }

            // part=snippet       -> título, descripción, canal, miniatura
            // type=video         -> solo videos (no canales ni playlists)
            // regionCode=CR      -> prioriza resultados para Costa Rica
            // relevanceLanguage  -> prioriza videos en español
            // safeSearch=strict  -> evita contenido inapropiado
            string url = "search"
                + "?part=snippet"
                + "&type=video"
                + $"&q={Uri.EscapeDataString(consulta)}"
                + $"&maxResults={Math.Clamp(cantidad, 1, 25)}"
                + "&regionCode=CR"
                + "&relevanceLanguage=es"
                + "&safeSearch=strict"
                + $"&key={Uri.EscapeDataString(apiKey)}";

            using var respuesta = await _httpClient.GetAsync(url);

            respuesta.EnsureSuccessStatusCode();

            await using var stream = await respuesta.Content.ReadAsStreamAsync();
            using var documento = await JsonDocument.ParseAsync(stream);

            if (!documento.RootElement.TryGetProperty("items", out var items)
                || items.ValueKind != JsonValueKind.Array)
            {
                return lista;
            }

            foreach (var item in items.EnumerateArray())
            {
                // El id del video viene en: id.videoId
                if (!item.TryGetProperty("id", out var idObjeto))
                {
                    continue;
                }

                string videoId = Texto(idObjeto, "videoId");

                if (string.IsNullOrWhiteSpace(videoId)
                    || !item.TryGetProperty("snippet", out var snippet))
                {
                    continue;
                }

                DateTime? fecha = null;

                if (DateTime.TryParse(Texto(snippet, "publishedAt"),
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AdjustToUniversal,
                        out var fechaLeida))
                {
                    fecha = fechaLeida;
                }

                // YouTube devuelve el texto con códigos HTML (&quot; &amp;...);
                // HtmlDecode los convierte a caracteres normales.
                lista.Add(new VideoYouTubeDto
                {
                    VideoId = videoId,
                    Titulo = WebUtility.HtmlDecode(Texto(snippet, "title")),
                    Descripcion = WebUtility.HtmlDecode(Texto(snippet, "description")),
                    Canal = WebUtility.HtmlDecode(Texto(snippet, "channelTitle")),
                    UrlMiniatura = ObtenerMiniatura(snippet),
                    FechaPublicacion = fecha
                });
            }

            return lista;
        }

        // ========================================================
        // 2. CONVERTIR AL FORMATO COMÚN (MediaItemDto) + CACHÉ
        // ========================================================
        public async Task<List<MediaItemDto>> BuscarContenidoAsync(
            string busqueda, int cantidad = 12)
        {
            if (string.IsNullOrWhiteSpace(busqueda))
            {
                return new List<MediaItemDto>();
            }

            string clave = $"youtube:{busqueda.Trim().ToLowerInvariant()}:{cantidad}";

            // Si ya buscamos esto en las últimas 6 horas, no gastamos cuota.
            if (_cache.TryGetValue(clave, out List<MediaItemDto>? enCache)
                && enCache != null)
            {
                return enCache;
            }

            var videos = await BuscarVideosAsync(busqueda, cantidad);

            var contenidos = videos.Select(v => new MediaItemDto
            {
                SchemaVersion = "1.0",
                ExternalId = v.VideoId,
                Title = v.Titulo,
                Description = string.IsNullOrWhiteSpace(v.Descripcion)
                    ? $"Video de {v.Canal} en YouTube."
                    : v.Descripcion,
                Author = v.Canal,
                Url = v.UrlVideo,
                ImageUrl = v.UrlMiniatura,
                Category = "Turismo",
                Destination = "Costa Rica",
                ContentType = "Video",
                Source = "YouTube",
                PublishedAt = v.FechaPublicacion
            }).ToList();

            _cache.Set(clave, contenidos, DuracionCache);

            return contenidos;
        }

        // ========================================================
        // MÉTODOS DE APOYO
        // ========================================================

        // YouTube trae miniaturas en varios tamaños; usamos la más
        // grande disponible: high (480x360) -> medium -> default.
        private static string ObtenerMiniatura(JsonElement snippet)
        {
            if (!snippet.TryGetProperty("thumbnails", out var miniaturas)
                || miniaturas.ValueKind != JsonValueKind.Object)
            {
                return string.Empty;
            }

            foreach (var tamano in new[] { "high", "medium", "default" })
            {
                if (miniaturas.TryGetProperty(tamano, out var miniatura))
                {
                    string url = Texto(miniatura, "url");

                    if (!string.IsNullOrWhiteSpace(url))
                    {
                        return url;
                    }
                }
            }

            return string.Empty;
        }

        // Lee un texto de un JSON sin romper si falta o viene null.
        private static string Texto(JsonElement elemento, string propiedad)
        {
            return elemento.ValueKind == JsonValueKind.Object
                && elemento.TryGetProperty(propiedad, out var valor)
                && valor.ValueKind == JsonValueKind.String
                    ? valor.GetString() ?? string.Empty
                    : string.Empty;
        }
    }
}