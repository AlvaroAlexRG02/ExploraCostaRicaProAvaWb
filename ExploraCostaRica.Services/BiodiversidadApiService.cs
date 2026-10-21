using System.Globalization;
using System.Net.Http.Headers;
using System.Text.Json;
using ExploraCostaRica.Models.DTOs;
using Microsoft.Extensions.Caching.Memory;

namespace ExploraCostaRica.Services
{
    public class BiodiversidadApiService : IBiodiversidadApiService
    {
        // Guardamos resultados un rato para no exceder el límite de iNaturalist.
        private static readonly TimeSpan DuracionCache = TimeSpan.FromMinutes(30);

        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;

        public BiodiversidadApiService(HttpClient httpClient, IMemoryCache cache)
        {
            _httpClient = httpClient;
            _cache = cache;

            _httpClient.BaseAddress = new Uri("https://api.inaturalist.org/v1/");

            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        // Consulta observaciones reales cerca de unas coordenadas.
        public async Task<List<ObservacionInatDto>> BuscarObservacionesAsync(
            double latitud, double longitud, int radioKm = 10)
        {
            var lista = new List<ObservacionInatDto>();

            // InvariantCulture: en es-CR el decimal usa coma y rompería la URL.
            var cultura = CultureInfo.InvariantCulture;

            string url = "observations"
                + $"?lat={latitud.ToString("F5", cultura)}"
                + $"&lng={longitud.ToString("F5", cultura)}"
                + $"&radius={Math.Clamp(radioKm, 1, 50)}"
                + "&photos=true"
                + "&quality_grade=research"
                + "&per_page=12"
                + "&locale=es";

            using var respuesta = await _httpClient.GetAsync(url);

            respuesta.EnsureSuccessStatusCode();

            await using var stream = await respuesta.Content.ReadAsStreamAsync();
            using var documento = await JsonDocument.ParseAsync(stream);

            if (!documento.RootElement.TryGetProperty("results", out var resultados)
                || resultados.ValueKind != JsonValueKind.Array)
            {
                return lista;
            }

            foreach (var obs in resultados.EnumerateArray())
            {
                if (!obs.TryGetProperty("id", out var idElemento)
                    || idElemento.ValueKind != JsonValueKind.Number)
                {
                    continue;
                }

                long id = idElemento.GetInt64();

                obs.TryGetProperty("taxon", out var taxon);
                obs.TryGetProperty("user", out var usuario);

                string urlFoto = string.Empty;

                if (obs.TryGetProperty("photos", out var fotos)
                    && fotos.ValueKind == JsonValueKind.Array
                    && fotos.GetArrayLength() > 0)
                {
                    // La API entrega la miniatura "square"; pedimos la "medium".
                    urlFoto = Texto(fotos[0], "url").Replace("square", "medium");
                }

                string uri = Texto(obs, "uri");

                DateTime? fecha = null;

                if (DateTime.TryParse(Texto(obs, "observed_on"), cultura,
                        DateTimeStyles.None, out var fechaLeida))
                {
                    fecha = fechaLeida;
                }

                var observacion = new ObservacionInatDto
                {
                    Id = id.ToString(cultura),
                    NombreComun = Texto(taxon, "preferred_common_name"),
                    NombreCientifico = Texto(taxon, "name"),
                    UrlImagen = urlFoto,
                    UrlObservacion = string.IsNullOrWhiteSpace(uri)
                        ? $"https://www.inaturalist.org/observations/{id}"
                        : uri,
                    Observador = Texto(usuario, "login"),
                    Lugar = Texto(obs, "place_guess"),
                    FechaObservacion = fecha
                };

                // Sin foto o sin nombre no sirve para una tarjeta.
                if (string.IsNullOrWhiteSpace(observacion.UrlImagen)
                    || (string.IsNullOrWhiteSpace(observacion.NombreComun)
                        && string.IsNullOrWhiteSpace(observacion.NombreCientifico)))
                {
                    continue;
                }

                lista.Add(observacion);
            }

            return lista;
        }

        // Convierte las observaciones al DTO común del proyecto.
        public async Task<List<MediaItemDto>> BuscarContenidoAsync(
            double latitud, double longitud, string destino, int radioKm = 10)
        {
            string clave = string.Format(CultureInfo.InvariantCulture,
                "inat:{0:F3}:{1:F3}:{2}:{3}", latitud, longitud, radioKm, destino);

            if (_cache.TryGetValue(clave, out List<MediaItemDto>? enCache)
                && enCache != null)
            {
                return enCache;
            }

            var observaciones = await BuscarObservacionesAsync(latitud, longitud, radioKm);

            var contenidos = observaciones.Select(o =>
            {
                string titulo = !string.IsNullOrWhiteSpace(o.NombreComun)
                    ? o.NombreComun
                    : o.NombreCientifico;

                titulo = char.ToUpper(titulo[0]) + titulo.Substring(1);

                string descripcion = $"Nombre científico: {o.NombreCientifico}.";

                if (!string.IsNullOrWhiteSpace(o.Lugar))
                {
                    descripcion += $" Observado en: {o.Lugar}.";
                }

                return new MediaItemDto
                {
                    SchemaVersion = "1.0",
                    ExternalId = o.Id,
                    Title = titulo,
                    Description = descripcion,
                    Author = o.Observador,
                    Url = o.UrlObservacion,
                    ImageUrl = o.UrlImagen,
                    Category = "Biodiversidad",
                    Destination = destino,
                    ContentType = "Biodiversidad",
                    Source = "iNaturalist",
                    PublishedAt = o.FechaObservacion
                };
            }).ToList();

            _cache.Set(clave, contenidos, DuracionCache);

            return contenidos;
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