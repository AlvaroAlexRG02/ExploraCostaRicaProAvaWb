using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using ExploraCostaRica.Models.DTOs;
using Microsoft.Extensions.Configuration;

namespace ExploraCostaRica.Services
{
    public class FotografiasApiService : IFotografiasApiService
    {
        private readonly HttpClient _httpClient;
        private readonly string _accessKey;

        public FotografiasApiService(
            HttpClient httpClient,
            IConfiguration configuration)
        {
            _httpClient = httpClient;

            _accessKey = configuration["Unsplash:AccessKey"]
                ?? throw new InvalidOperationException(
                    "No se configuró la Access Key de Unsplash.");

            _httpClient.BaseAddress =
                new Uri("https://api.unsplash.com/");

            _httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue(
                    "application/json"));

            _httpClient.DefaultRequestHeaders.Add(
                "Accept-Version", "v1");
        }

        // Consulta fotografías reales desde Unsplash.
        // Conservamos este método para no afectar el buscador.
        public async Task<List<FotografiaDto>> BuscarFotografiasAsync(
            string busqueda,
            int pagina = 1)
        {
            var fotografias = new List<FotografiaDto>();

            if (string.IsNullOrWhiteSpace(busqueda))
            {
                return fotografias;
            }

            string consulta = Uri.EscapeDataString(busqueda.Trim());

            string url =
                $"search/photos?query={consulta}&page={Math.Max(1, pagina)}&per_page=12";

            using var solicitud =
                new HttpRequestMessage(HttpMethod.Get, url);

            solicitud.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Client-ID", _accessKey);

            using var respuesta =
                await _httpClient.SendAsync(solicitud);

            respuesta.EnsureSuccessStatusCode();

            await using var stream =
                await respuesta.Content.ReadAsStreamAsync();

            using var documento =
                await JsonDocument.ParseAsync(stream);

            foreach (var foto in documento.RootElement
                         .GetProperty("results").EnumerateArray())
            {
                var usuario = foto.GetProperty("user");
                var urls = foto.GetProperty("urls");
                var enlaces = foto.GetProperty("links");

                fotografias.Add(new FotografiaDto
                {
                    Id = foto.GetProperty("id").GetString()
                         ?? string.Empty,

                    Descripcion =
                        foto.TryGetProperty(
                            "alt_description", out var descripcion)
                        && descripcion.ValueKind == JsonValueKind.String
                            ? descripcion.GetString() ?? string.Empty
                            : string.Empty,

                    UrlImagen =
                        urls.GetProperty("small").GetString()
                        ?? string.Empty,

                    NombreFotografo =
                        usuario.GetProperty("name").GetString()
                        ?? string.Empty,

                    UrlFotografo =
                        usuario.GetProperty("links")
                            .GetProperty("html").GetString()
                        ?? string.Empty,

                    UrlUnsplash =
                        enlaces.GetProperty("html").GetString()
                        ?? string.Empty
                });
            }

            return fotografias;
        }

        // Convierte las fotografías al DTO común del proyecto.
        public async Task<List<MediaItemDto>> BuscarContenidoAsync(
            string busqueda,
            int pagina = 1)
        {
            var fotografias =
                await BuscarFotografiasAsync(busqueda, pagina);

            var contenidos = new List<MediaItemDto>();

            foreach (var foto in fotografias)
            {
                contenidos.Add(new MediaItemDto
                {
                    SchemaVersion = "1.0",

                    ExternalId = foto.Id,

                    Title = !string.IsNullOrWhiteSpace(foto.Descripcion)
                        ? foto.Descripcion
                        : $"Fotografía de {busqueda}",

                    Description = foto.Descripcion,

                    Author = foto.NombreFotografo,

                    Url = foto.UrlUnsplash,

                    ImageUrl = foto.UrlImagen,

                    Category = "Fotografía",

                    Destination = busqueda.Trim(),

                    ContentType = "Imagen",

                    Source = "Unsplash",

                    PublishedAt = null
                });
            }

            return contenidos;
        }
    }
}