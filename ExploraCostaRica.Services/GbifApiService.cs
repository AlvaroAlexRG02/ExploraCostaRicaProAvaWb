using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using ExploraCostaRica.Models.DTOs;
using Microsoft.Extensions.Caching.Memory;

namespace ExploraCostaRica.Services
{
    // ============================================================
    // API 10 - GBIF (Global Biodiversity Information Facility)
    // ------------------------------------------------------------
    // Consulta registros científicos de especies observadas en
    // Costa Rica. No necesita API Key.
    //
    // Documentación: https://www.gbif.org/developer/occurrence
    // Endpoint usado: GET /v1/occurrence/search
    //
    // Complementa a iNaturalist (API 3): iNaturalist busca por
    // coordenadas de un destino; GBIF busca por texto (lugar,
    // especie, familia...) dentro de todo Costa Rica.
    // ============================================================
    public class GbifApiService : IGbifApiService
    {
        // Los registros científicos cambian poco: 60 minutos de caché
        // es suficiente y evita llamar a GBIF en cada búsqueda.
        private static readonly TimeSpan DuracionCache = TimeSpan.FromMinutes(60);

        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;

        public GbifApiService(HttpClient httpClient, IMemoryCache cache)
        {
            _httpClient = httpClient;
            _cache = cache;

            _httpClient.BaseAddress = new Uri("https://api.gbif.org/v1/");

            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        // ========================================================
        // 1. CONSULTAR GBIF
        // ========================================================
        public async Task<List<OcurrenciaGbifDto>> BuscarOcurrenciasAsync(
            string busqueda, int cantidad = 12)
        {
            var lista = new List<OcurrenciaGbifDto>();

            if (string.IsNullOrWhiteSpace(busqueda))
            {
                return lista;
            }

            // country=CR      -> solo registros de Costa Rica
            // mediaType       -> solo registros con fotografía
            // q               -> texto libre (lugar, especie, familia...)
            // limit           -> cantidad de resultados (máximo 30 aquí)
            string url = "occurrence/search"
                + "?country=CR"
                + "&mediaType=StillImage"
                + $"&q={Uri.EscapeDataString(busqueda.Trim())}"
                + $"&limit={Math.Clamp(cantidad, 1, 30)}";

            using var respuesta = await _httpClient.GetAsync(url);

            respuesta.EnsureSuccessStatusCode();

            await using var stream = await respuesta.Content.ReadAsStreamAsync();
            using var documento = await JsonDocument.ParseAsync(stream);

            if (!documento.RootElement.TryGetProperty("results", out var resultados)
                || resultados.ValueKind != JsonValueKind.Array)
            {
                return lista;
            }

            foreach (var registro in resultados.EnumerateArray())
            {
                if (!registro.TryGetProperty("key", out var claveElemento)
                    || claveElemento.ValueKind != JsonValueKind.Number)
                {
                    continue;
                }

                long clave = claveElemento.GetInt64();

                // Primera foto del registro y su licencia.
                string urlImagen = string.Empty;
                string licencia = string.Empty;

                if (registro.TryGetProperty("media", out var medios)
                    && medios.ValueKind == JsonValueKind.Array)
                {
                    foreach (var medio in medios.EnumerateArray())
                    {
                        string identificador = Texto(medio, "identifier");

                        if (!string.IsNullOrWhiteSpace(identificador))
                        {
                            urlImagen = identificador;
                            licencia = Texto(medio, "license");
                            break;
                        }
                    }
                }

                // GBIF a veces trae solo la fecha y a veces fecha + hora.
                DateTime? fecha = null;
                string fechaTexto = Texto(registro, "eventDate");

                if (fechaTexto.Length >= 10
                    && DateTime.TryParse(fechaTexto.Substring(0, 10),
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out var fechaLeida))
                {
                    fecha = fechaLeida;
                }

                // "species" es el nombre limpio; "scientificName" incluye autor.
                string nombreCientifico = Texto(registro, "species");

                if (string.IsNullOrWhiteSpace(nombreCientifico))
                {
                    nombreCientifico = Texto(registro, "scientificName");
                }

                var ocurrencia = new OcurrenciaGbifDto
                {
                    Id = clave.ToString(CultureInfo.InvariantCulture),
                    NombreCientifico = nombreCientifico,
                    NombreComun = Texto(registro, "vernacularName"),
                    Reino = Texto(registro, "kingdom"),
                    Familia = Texto(registro, "family"),
                    Lugar = Texto(registro, "locality"),
                    Provincia = Texto(registro, "stateProvince"),
                    UrlImagen = urlImagen,
                    Licencia = licencia,
                    RegistradoPor = Texto(registro, "recordedBy"),
                    Institucion = Texto(registro, "institutionCode"),
                    FechaRegistro = fecha,
                    UrlRegistro = $"https://www.gbif.org/occurrence/{clave}"
                };

                // Sin foto o sin nombre no sirve para una tarjeta.
                if (string.IsNullOrWhiteSpace(ocurrencia.UrlImagen)
                    || string.IsNullOrWhiteSpace(ocurrencia.NombreCientifico))
                {
                    continue;
                }

                lista.Add(ocurrencia);
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

            string clave = $"gbif:{busqueda.Trim().ToLowerInvariant()}:{cantidad}";

            // Si ya buscamos esto hace poco, devolvemos lo guardado.
            if (_cache.TryGetValue(clave, out List<MediaItemDto>? enCache)
                && enCache != null)
            {
                return enCache;
            }

            var ocurrencias = await BuscarOcurrenciasAsync(busqueda, cantidad);

            var contenidos = ocurrencias.Select(o =>
            {
                // Título: nombre común si existe; si no, el científico.
                string titulo = !string.IsNullOrWhiteSpace(o.NombreComun)
                    ? o.NombreComun
                    : o.NombreCientifico;

                titulo = char.ToUpper(titulo[0]) + titulo.Substring(1);

                return new MediaItemDto
                {
                    SchemaVersion = "1.0",
                    ExternalId = o.Id,
                    Title = titulo,
                    Description = ArmarDescripcion(o),
                    Author = ArmarAutor(o),
                    Url = o.UrlRegistro,
                    ImageUrl = o.UrlImagen,
                    Category = "Biodiversidad",
                    Destination = string.IsNullOrWhiteSpace(o.Provincia)
                        ? "Costa Rica"
                        : o.Provincia,
                    ContentType = "Biodiversidad",
                    Source = "GBIF",
                    PublishedAt = o.FechaRegistro
                };
            }).ToList();

            _cache.Set(clave, contenidos, DuracionCache);

            return contenidos;
        }

        // ========================================================
        // MÉTODOS DE APOYO
        // ========================================================

        // Ej.: "Pharomachrus mocinno · Familia Trogonidae (Animalia).
        //       Registrado en: Monteverde, Puntarenas."
        private static string ArmarDescripcion(OcurrenciaGbifDto o)
        {
            string texto = o.NombreCientifico;

            if (!string.IsNullOrWhiteSpace(o.Familia))
            {
                texto += $" · Familia {o.Familia}";
            }

            if (!string.IsNullOrWhiteSpace(o.Reino))
            {
                texto += $" ({o.Reino})";
            }

            texto += ".";

            string lugar = string.Join(", ", new[] { o.Lugar, o.Provincia }
                .Where(x => !string.IsNullOrWhiteSpace(x)));

            if (!string.IsNullOrWhiteSpace(lugar))
            {
                texto += $" Registrado en: {lugar}.";
            }

            return texto;
        }

        // Autor + institución + licencia (atribución que pide GBIF).
        private static string ArmarAutor(OcurrenciaGbifDto o)
        {
            var partes = new List<string>();

            if (!string.IsNullOrWhiteSpace(o.RegistradoPor))
            {
                partes.Add(o.RegistradoPor);
            }

            if (!string.IsNullOrWhiteSpace(o.Institucion))
            {
                partes.Add(o.Institucion);
            }

            if (!string.IsNullOrWhiteSpace(o.Licencia))
            {
                partes.Add($"Licencia: {o.Licencia}");
            }

            return partes.Count > 0
                ? string.Join(" · ", partes)
                : "GBIF";
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