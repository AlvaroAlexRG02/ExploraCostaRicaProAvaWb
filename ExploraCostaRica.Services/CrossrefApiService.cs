using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using ExploraCostaRica.Models.DTOs;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;

namespace ExploraCostaRica.Services
{
    // ============================================================
    // API 7 - CROSSREF REST API
    // ------------------------------------------------------------
    // Busca artículos científicos (papers) sobre Costa Rica, su
    // naturaleza y su cultura. No necesita API Key ni registro.
    //
    // Documentación: https://www.crossref.org/documentation/retrieve-metadata/rest-api/
    // Endpoint usado: GET /works
    //
    // Crossref recomienda el modo "polite": mandar un correo de
    // contacto (mailto) y un User-Agent que identifique la app.
    // El correo se lee de configuración ("Crossref:Mailto"); si
    // no está, la consulta funciona igual, solo que sin ese modo.
    // ============================================================
    public class CrossrefApiService : ICrossrefApiService
    {
        // Los papers cambian poco: 1 día de caché.
        private static readonly TimeSpan DuracionCache = TimeSpan.FromDays(1);

        // Largo máximo del resumen que se muestra en la tarjeta.
        private const int LargoMaximoResumen = 400;

        // Solo pedimos los campos que usamos (respuesta más liviana).
        private const string Campos =
            "DOI,title,author,container-title,published,issued,abstract,URL";

        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly IConfiguration _configuration;

        public CrossrefApiService(
            HttpClient httpClient,
            IMemoryCache cache,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _cache = cache;
            _configuration = configuration;

            _httpClient.BaseAddress = new Uri("https://api.crossref.org/");

            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        // ========================================================
        // 1. CONSULTAR CROSSREF
        // ========================================================
        public async Task<List<ArticuloCrossrefDto>> BuscarArticulosAsync(
            string busqueda, int cantidad = 12)
        {
            var lista = new List<ArticuloCrossrefDto>();

            if (string.IsNullOrWhiteSpace(busqueda))
            {
                return lista;
            }

            string texto = busqueda.Trim();

            // Agregamos "Costa Rica" a la búsqueda para que los papers
            // más relevantes sean sobre el país (Crossref ordena por
            // relevancia, así que no deja la búsqueda vacía).
            if (!texto.Contains("costa rica", StringComparison.OrdinalIgnoreCase))
            {
                texto += " Costa Rica";
            }

            // query  -> texto libre (título, autores, revista...)
            // filter -> solo artículos de revista
            // select -> solo los campos que necesitamos
            // rows   -> cantidad de resultados (máximo 30 aquí)
            // mailto -> modo "polite" (opcional)
            string url = "works"
                + $"?query={Uri.EscapeDataString(texto)}"
                + "&filter=type:journal-article"
                + $"&select={Campos}"
                + $"&rows={Math.Clamp(cantidad, 1, 30)}";

            string? mailto = _configuration["Crossref:Mailto"];

            if (!string.IsNullOrWhiteSpace(mailto))
            {
                url += $"&mailto={Uri.EscapeDataString(mailto.Trim())}";
            }

            using var respuesta = await _httpClient.GetAsync(url);

            respuesta.EnsureSuccessStatusCode();

            await using var stream = await respuesta.Content.ReadAsStreamAsync();
            using var documento = await JsonDocument.ParseAsync(stream);

            if (!documento.RootElement.TryGetProperty("message", out var mensaje)
                || !mensaje.TryGetProperty("items", out var articulos)
                || articulos.ValueKind != JsonValueKind.Array)
            {
                return lista;
            }

            foreach (var articulo in articulos.EnumerateArray())
            {
                string doi = Texto(articulo, "DOI");

                // Crossref devuelve el título y la revista como listas.
                string titulo = PrimerTexto(articulo, "title");

                // Sin DOI o sin título no sirve para una tarjeta.
                if (string.IsNullOrWhiteSpace(doi)
                    || string.IsNullOrWhiteSpace(titulo))
                {
                    continue;
                }

                string urlArticulo = Texto(articulo, "URL");

                if (string.IsNullOrWhiteSpace(urlArticulo))
                {
                    urlArticulo = $"https://doi.org/{doi}";
                }

                lista.Add(new ArticuloCrossrefDto
                {
                    Doi = doi,
                    Titulo = LimpiarTexto(titulo),
                    Autores = LeerAutores(articulo, 3),
                    Revista = LimpiarTexto(PrimerTexto(articulo, "container-title")),
                    Resumen = LimpiarTexto(Texto(articulo, "abstract")),
                    // "published" es la fecha más temprana; si falta, "issued".
                    FechaPublicacion = LeerFecha(articulo, "published")
                        ?? LeerFecha(articulo, "issued"),
                    UrlArticulo = urlArticulo
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

            string clave = $"crossref:{busqueda.Trim().ToLowerInvariant()}:{cantidad}";

            // Si ya buscamos esto hace poco, devolvemos lo guardado.
            if (_cache.TryGetValue(clave, out List<MediaItemDto>? enCache)
                && enCache != null)
            {
                return enCache;
            }

            var articulos = await BuscarArticulosAsync(busqueda, cantidad);

            var contenidos = articulos.Select(a => new MediaItemDto
            {
                SchemaVersion = "1.0",
                ExternalId = a.Doi,
                Title = a.Titulo,
                Description = ArmarDescripcion(a),
                Author = a.Autores.Count > 0
                    ? string.Join(", ", a.Autores)
                    : "Autor desconocido",
                Url = a.UrlArticulo,
                // Los papers no traen imagen: la tarjeta usa el ícono.
                ImageUrl = null,
                Category = "Investigación",
                Destination = "Costa Rica",
                ContentType = "Paper",
                Source = "Crossref",
                PublishedAt = a.FechaPublicacion
            }).ToList();

            _cache.Set(clave, contenidos, DuracionCache);

            return contenidos;
        }

        // ========================================================
        // MÉTODOS DE APOYO
        // ========================================================

        // Revista + resumen (si existe). Ej.:
        // "Revista: Biotropica. Estudio de aves en Monteverde..."
        private static string ArmarDescripcion(ArticuloCrossrefDto a)
        {
            var partes = new List<string>();

            if (!string.IsNullOrWhiteSpace(a.Revista))
            {
                partes.Add($"Revista: {a.Revista}.");
            }

            if (!string.IsNullOrWhiteSpace(a.Resumen))
            {
                string resumen = a.Resumen.Length > LargoMaximoResumen
                    ? a.Resumen.Substring(0, LargoMaximoResumen).TrimEnd() + "..."
                    : a.Resumen;

                partes.Add(resumen);
            }

            return partes.Count > 0
                ? string.Join(" ", partes)
                : $"Artículo científico (DOI {a.Doi}).";
        }

        // Autores como "Nombre Apellido" (Crossref separa given y family).
        private static List<string> LeerAutores(JsonElement articulo, int maximo)
        {
            var autores = new List<string>();

            if (!articulo.TryGetProperty("author", out var lista)
                || lista.ValueKind != JsonValueKind.Array)
            {
                return autores;
            }

            foreach (var autor in lista.EnumerateArray())
            {
                string nombre = $"{Texto(autor, "given")} {Texto(autor, "family")}".Trim();

                // Algunos autores son organizaciones: vienen en "name".
                if (string.IsNullOrWhiteSpace(nombre))
                {
                    nombre = Texto(autor, "name");
                }

                if (!string.IsNullOrWhiteSpace(nombre))
                {
                    autores.Add(nombre);

                    if (autores.Count == maximo)
                    {
                        break;
                    }
                }
            }

            return autores;
        }

        // Crossref da las fechas así: { "date-parts": [[2019, 5, 1]] }.
        // A veces solo trae año, o año y mes.
        private static DateTime? LeerFecha(JsonElement articulo, string propiedad)
        {
            if (!articulo.TryGetProperty(propiedad, out var fecha)
                || !fecha.TryGetProperty("date-parts", out var partes)
                || partes.ValueKind != JsonValueKind.Array
                || partes.GetArrayLength() == 0)
            {
                return null;
            }

            var numeros = partes[0];

            // Algunos registros traen [[null]]: se revisa que sea número.
            if (numeros.ValueKind != JsonValueKind.Array
                || !Numero(numeros, 0, out int anio)
                || anio < 1 || anio > 9999)
            {
                return null;
            }

            int mes = Numero(numeros, 1, out int m)
                ? Math.Clamp(m, 1, 12)
                : 1;

            int dia = Numero(numeros, 2, out int d)
                ? Math.Clamp(d, 1, DateTime.DaysInMonth(anio, mes))
                : 1;

            return new DateTime(anio, mes, dia);
        }

        // Lee el número en la posición indicada, solo si existe y es número.
        private static bool Numero(JsonElement lista, int posicion, out int valor)
        {
            valor = 0;

            return lista.GetArrayLength() > posicion
                && lista[posicion].ValueKind == JsonValueKind.Number
                && lista[posicion].TryGetInt32(out valor);
        }

        // Quita etiquetas (el resumen viene en formato JATS: <jats:p>...)

        // y espacios de más.
        private static string LimpiarTexto(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                return string.Empty;
            }

            texto = Regex.Replace(texto, "<[^>]+>", " ");
            texto = WebUtility.HtmlDecode(texto);
            texto = Regex.Replace(texto, @"\s+", " ");

            return texto.Trim();
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

        // Lee el primer texto de una lista del JSON (título, revista...).
        private static string PrimerTexto(JsonElement elemento, string propiedad)
        {
            if (elemento.TryGetProperty(propiedad, out var valores)
                && valores.ValueKind == JsonValueKind.Array)
            {
                foreach (var valor in valores.EnumerateArray())
                {
                    if (valor.ValueKind == JsonValueKind.String
                        && !string.IsNullOrWhiteSpace(valor.GetString()))
                    {
                        return valor.GetString()!;
                    }
                }
            }

            return string.Empty;
        }
    }
}
