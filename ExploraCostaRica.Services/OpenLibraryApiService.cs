using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using ExploraCostaRica.Models.DTOs;
using Microsoft.Extensions.Caching.Memory;

namespace ExploraCostaRica.Services
{
    // ============================================================
    // API 6 - OPEN LIBRARY
    // ------------------------------------------------------------
    // Busca libros, autores y portadas sobre Costa Rica, su
    // naturaleza y su cultura. No necesita API Key.
    //
    // Documentación: https://openlibrary.org/dev/docs/api/search
    // Endpoint usado: GET /search.json
    // Portadas:       https://covers.openlibrary.org/b/id/{id}-M.jpg
    //
    // Open Library pide uso de bajo volumen, caché y un
    // User-Agent que identifique la aplicación (se configura
    // en Program.cs). Por eso los resultados se guardan 1 día.
    // ============================================================
    public class OpenLibraryApiService : IOpenLibraryApiService
    {
        // Los libros cambian muy poco: 1 día de caché.
        private static readonly TimeSpan DuracionCache = TimeSpan.FromDays(1);

        // Solo pedimos los campos que usamos (respuesta más liviana).
        private const string Campos =
            "key,title,author_name,first_publish_year,cover_i,subject,publisher";

        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;

        public OpenLibraryApiService(HttpClient httpClient, IMemoryCache cache)
        {
            _httpClient = httpClient;
            _cache = cache;

            _httpClient.BaseAddress = new Uri("https://openlibrary.org/");

            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        // ========================================================
        // 1. CONSULTAR OPEN LIBRARY
        // ========================================================
        public async Task<List<LibroOpenLibraryDto>> BuscarLibrosAsync(
            string busqueda, int cantidad = 12)
        {
            if (string.IsNullOrWhiteSpace(busqueda))
            {
                return new List<LibroOpenLibraryDto>();
            }

            string texto = busqueda.Trim();
            cantidad = Math.Clamp(cantidad, 1, 30);

            // Primero buscamos el texto junto con "Costa Rica" para que
            // los libros sean del país (ej.: "quetzal" -> aves de CR).
            // Si así no hay nada, buscamos solo el texto.
            var lista = new List<LibroOpenLibraryDto>();

            if (!texto.Contains("costa rica", StringComparison.OrdinalIgnoreCase))
            {
                lista = await ConsultarAsync($"{texto} Costa Rica", cantidad);
            }

            if (lista.Count == 0)
            {
                lista = await ConsultarAsync(texto, cantidad);
            }

            return lista;
        }

        private async Task<List<LibroOpenLibraryDto>> ConsultarAsync(
            string texto, int cantidad)
        {
            var lista = new List<LibroOpenLibraryDto>();

            // q      -> texto libre (título, autor, tema...)
            // fields -> solo los campos que necesitamos
            // limit  -> cantidad de resultados (máximo 30 aquí)
            string url = "search.json"
                + $"?q={Uri.EscapeDataString(texto)}"
                + $"&fields={Campos}"
                + $"&limit={cantidad}";

            using var respuesta = await _httpClient.GetAsync(url);

            respuesta.EnsureSuccessStatusCode();

            await using var stream = await respuesta.Content.ReadAsStreamAsync();
            using var documento = await JsonDocument.ParseAsync(stream);

            if (!documento.RootElement.TryGetProperty("docs", out var libros)
                || libros.ValueKind != JsonValueKind.Array)
            {
                return lista;
            }

            foreach (var libro in libros.EnumerateArray())
            {
                string clave = Texto(libro, "key");
                string titulo = Texto(libro, "title");

                // Sin identificador o sin título no sirve para una tarjeta.
                if (string.IsNullOrWhiteSpace(clave)
                    || string.IsNullOrWhiteSpace(titulo))
                {
                    continue;
                }

                // Portada: Open Library da un número (cover_i) y la
                // imagen se arma con la URL de covers.openlibrary.org.
                string urlPortada = string.Empty;

                if (libro.TryGetProperty("cover_i", out var portada)
                    && portada.ValueKind == JsonValueKind.Number
                    && portada.TryGetInt64(out long idPortada))
                {
                    urlPortada = "https://covers.openlibrary.org/b/id/"
                        + idPortada.ToString(CultureInfo.InvariantCulture)
                        + "-M.jpg";
                }

                int? anio = null;

                if (libro.TryGetProperty("first_publish_year", out var anioElemento)
                    && anioElemento.ValueKind == JsonValueKind.Number
                    && anioElemento.TryGetInt32(out int anioLeido))
                {
                    anio = anioLeido;
                }

                lista.Add(new LibroOpenLibraryDto
                {
                    Id = clave,
                    Titulo = titulo,
                    Autores = Lista(libro, "author_name", 3),
                    AnioPublicacion = anio,
                    Editorial = Lista(libro, "publisher", 1).FirstOrDefault() ?? string.Empty,
                    Temas = Lista(libro, "subject", 3),
                    UrlPortada = urlPortada,
                    UrlLibro = $"https://openlibrary.org{clave}"
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

            string clave = $"openlibrary:{busqueda.Trim().ToLowerInvariant()}:{cantidad}";

            // Si ya buscamos esto hace poco, devolvemos lo guardado.
            if (_cache.TryGetValue(clave, out List<MediaItemDto>? enCache)
                && enCache != null)
            {
                return enCache;
            }

            var libros = await BuscarLibrosAsync(busqueda, cantidad);

            var contenidos = libros.Select(l => new MediaItemDto
            {
                SchemaVersion = "1.0",
                ExternalId = l.Id,
                Title = l.Titulo,
                Description = ArmarDescripcion(l),
                Author = l.Autores.Count > 0
                    ? string.Join(", ", l.Autores)
                    : "Autor desconocido",
                Url = l.UrlLibro,
                ImageUrl = l.UrlPortada,
                Category = "Libros",
                Destination = "Costa Rica",
                ContentType = "Libro",
                Source = "Open Library",
                // Open Library solo da el año: usamos el 1 de enero.
                PublishedAt = l.AnioPublicacion.HasValue
                    ? new DateTime(l.AnioPublicacion.Value, 1, 1)
                    : null
            }).ToList();

            _cache.Set(clave, contenidos, DuracionCache);

            return contenidos;
        }

        // ========================================================
        // MÉTODOS DE APOYO
        // ========================================================

        // Ej.: "Publicado en 1998 por Zona Tropical.
        //       Temas: Birds, Costa Rica, Field guides."
        private static string ArmarDescripcion(LibroOpenLibraryDto l)
        {
            var partes = new List<string>();

            if (l.AnioPublicacion.HasValue && !string.IsNullOrWhiteSpace(l.Editorial))
            {
                partes.Add($"Publicado en {l.AnioPublicacion} por {l.Editorial}.");
            }
            else if (l.AnioPublicacion.HasValue)
            {
                partes.Add($"Publicado en {l.AnioPublicacion}.");
            }
            else if (!string.IsNullOrWhiteSpace(l.Editorial))
            {
                partes.Add($"Editorial: {l.Editorial}.");
            }

            if (l.Temas.Count > 0)
            {
                partes.Add($"Temas: {string.Join(", ", l.Temas)}.");
            }

            return partes.Count > 0
                ? string.Join(" ", partes)
                : "Libro disponible en Open Library.";
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

        // Lee los primeros textos de una lista del JSON (autores, temas...).
        private static List<string> Lista(JsonElement elemento, string propiedad, int maximo)
        {
            var lista = new List<string>();

            if (elemento.TryGetProperty(propiedad, out var valores)
                && valores.ValueKind == JsonValueKind.Array)
            {
                foreach (var valor in valores.EnumerateArray())
                {
                    if (valor.ValueKind == JsonValueKind.String
                        && !string.IsNullOrWhiteSpace(valor.GetString()))
                    {
                        lista.Add(valor.GetString()!);

                        if (lista.Count == maximo)
                        {
                            break;
                        }
                    }
                }
            }

            return lista;
        }
    }
}
