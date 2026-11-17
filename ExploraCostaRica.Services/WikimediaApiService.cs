using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using ExploraCostaRica.Models.DTOs;

namespace ExploraCostaRica.Services
{

    public class WikimediaApiService : IWikimediaApiService
    {

        private static readonly HashSet<string> TiposImagen =
            new(StringComparer.OrdinalIgnoreCase)
            {
                "image/jpeg", "image/png", "image/webp", "image/gif"
            };

        private static readonly Regex EtiquetasHtml =
            new("<[^>]*>", RegexOptions.Compiled);

        private static readonly Regex Espacios =
            new(@"\s+", RegexOptions.Compiled);

        private readonly HttpClient _httpClient;

        public WikimediaApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;

            _httpClient.BaseAddress = new Uri("https://commons.wikimedia.org/w/");

            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));

            if (_httpClient.DefaultRequestHeaders.UserAgent.Count == 0)
            {
                _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
                    "ExploraCostaRica/1.0 (proyecto academico SC-701)");
            }
        }


        // ============================================================
        // BÚSQUEDA EN COMMONS
        // ============================================================

        public async Task<List<WikimediaImagenDto>> BuscarImagenesAsync(
            string busqueda, int cantidad = 12)
        {
            var lista = new List<WikimediaImagenDto>();

            busqueda = (busqueda ?? "").Trim();

            if (busqueda.Length == 0)
                return lista;

            if (busqueda.Length > 120)
                busqueda = busqueda.Substring(0, 120);

            cantidad = Math.Clamp(cantidad, 1, 30);


            string url = "api.php"
                + "?action=query"
                + "&format=json"
                + "&formatversion=2"
                + "&generator=search"
                + "&gsrnamespace=6"
                + "&gsrlimit=" + Math.Min(cantidad * 2, 40)
                + "&gsrsearch=" + Uri.EscapeDataString(busqueda + " filetype:bitmap")
                + "&prop=imageinfo"
                + "&iiprop=url%7Cmime%7Cextmetadata"
                + "&iiurlwidth=640"
                + "&iilimit=1";

            using var respuesta = await _httpClient.GetAsync(url);

            respuesta.EnsureSuccessStatusCode();

            await using var stream = await respuesta.Content.ReadAsStreamAsync();
            using var documento = await JsonDocument.ParseAsync(stream);

            if (!documento.RootElement.TryGetProperty("query", out var consulta)
                || !consulta.TryGetProperty("pages", out var paginas)
                || paginas.ValueKind != JsonValueKind.Array)
            {

                return lista;
            }

            var orden = new List<(int Indice, WikimediaImagenDto Imagen)>();

            foreach (var pagina in paginas.EnumerateArray())
            {
                if (!pagina.TryGetProperty("imageinfo", out var infos)
                    || infos.ValueKind != JsonValueKind.Array
                    || infos.GetArrayLength() == 0)
                {
                    continue;
                }

                var info = infos[0];

                string mime = Texto(info, "mime");

                if (!TiposImagen.Contains(mime))
                    continue;

                string urlImagen = Texto(info, "thumburl");

                if (urlImagen.Length == 0)
                    urlImagen = Texto(info, "url");

                string urlPagina = Texto(info, "descriptionurl");

                // Solo enlaces https hacia Wikimedia.
                if (!EsUrlWikimedia(urlImagen) || !EsUrlWikimedia(urlPagina))
                    continue;

                info.TryGetProperty("extmetadata", out var meta);

                string titulo = Limpiar(Meta(meta, "ObjectName"), 200);

                if (titulo.Length == 0)
                    titulo = TituloDesdeArchivo(Texto(pagina, "title"));

                string urlLicencia = Meta(meta, "LicenseUrl");

                if (urlLicencia.Length > 0 && !EsUrlHttp(urlLicencia))
                    urlLicencia = "";

                DateTime? fecha = null;

                if (DateTime.TryParse(Limpiar(Meta(meta, "DateTimeOriginal"), 40),
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                        out var fechaLeida)
                    && fechaLeida.Year >= 1753)
                {
                    fecha = fechaLeida;
                }

                int indice = pagina.TryGetProperty("index", out var idx)
                    && idx.ValueKind == JsonValueKind.Number
                        ? idx.GetInt32()
                        : orden.Count;

                orden.Add((indice, new WikimediaImagenDto
                {
                    Titulo = titulo,
                    Descripcion = Limpiar(Meta(meta, "ImageDescription"), 400),
                    UrlImagen = urlImagen,
                    UrlPagina = urlPagina,
                    Autor = Limpiar(Meta(meta, "Artist"), 150),
                    Licencia = Limpiar(Meta(meta, "LicenseShortName"), 60),
                    UrlLicencia = urlLicencia,
                    Fecha = fecha
                }));
            }


            return orden
                .OrderBy(o => o.Indice)
                .Select(o => o.Imagen)
                .Take(cantidad)
                .ToList();
        }


        // ============================================================
        // NORMALIZAR A MediaItemDto
        // ============================================================

        public async Task<List<MediaItemDto>> BuscarContenidoAsync(
            string busqueda, string? destino = null, int cantidad = 12)
        {
            busqueda = (busqueda ?? "").Trim();

            var imagenes = await BuscarImagenesAsync(busqueda, cantidad);

            return imagenes.Select(i =>
            {

                var partes = new List<string>();

                if (i.Descripcion.Length > 0)
                    partes.Add(i.Descripcion);

                if (i.Licencia.Length > 0)
                    partes.Add("Licencia: " + i.Licencia + ".");

                partes.Add("Fuente: Wikimedia Commons.");

                return new MediaItemDto
                {
                    SchemaVersion = "1.0",
                    ExternalId = i.UrlPagina,
                    Title = i.Titulo,
                    Description = string.Join(" ", partes),
                    Author = i.Autor.Length > 0 ? i.Autor : null,
                    Url = i.UrlPagina,
                    ImageUrl = i.UrlImagen,
                    Category = "Cultura",
                    Destination = destino,
                    ContentType = "Cultura",
                    Source = "Wikimedia Commons",
                    PublishedAt = i.Fecha
                };
            }).ToList();
        }


        // ============================================================
        // AUXILIARES
        // ============================================================


        private static string Meta(JsonElement meta, string campo)
        {
            return meta.ValueKind == JsonValueKind.Object
                && meta.TryGetProperty(campo, out var dato)
                    ? Texto(dato, "value")
                    : string.Empty;
        }

        private static string Texto(JsonElement elemento, string propiedad)
        {
            return elemento.ValueKind == JsonValueKind.Object
                && elemento.TryGetProperty(propiedad, out var valor)
                && valor.ValueKind == JsonValueKind.String
                    ? valor.GetString() ?? string.Empty
                    : string.Empty;
        }


        private static string Limpiar(string? html, int maximo)
        {
            if (string.IsNullOrWhiteSpace(html))
                return string.Empty;

            string texto = EtiquetasHtml.Replace(html, " ");
            texto = WebUtility.HtmlDecode(texto);
            texto = Espacios.Replace(texto, " ").Trim();

            return texto.Length > maximo
                ? texto.Substring(0, maximo - 1).TrimEnd() + "…"
                : texto;
        }


        private static string TituloDesdeArchivo(string titulo)
        {
            int dosPuntos = titulo.IndexOf(':');

            if (dosPuntos >= 0)
                titulo = titulo.Substring(dosPuntos + 1);

            int punto = titulo.LastIndexOf('.');

            if (punto > 0)
                titulo = titulo.Substring(0, punto);

            titulo = titulo.Replace('_', ' ').Trim();

            return titulo.Length > 0 ? titulo : "Imagen de Wikimedia Commons";
        }

        private static bool EsUrlHttp(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out var uri)
                && (uri.Scheme == Uri.UriSchemeHttps
                    || uri.Scheme == Uri.UriSchemeHttp);
        }

        private static bool EsUrlWikimedia(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out var uri)
                && uri.Scheme == Uri.UriSchemeHttps
                && (uri.Host.EndsWith(".wikimedia.org", StringComparison.OrdinalIgnoreCase)
                    || uri.Host.Equals("wikimedia.org", StringComparison.OrdinalIgnoreCase));
        }
    }
}