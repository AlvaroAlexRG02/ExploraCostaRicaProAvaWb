using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ExploraCostaRica.Data;
using ExploraCostaRica.Models.DTOs;
using ExploraCostaRica.Models.Exchange;
using Microsoft.EntityFrameworkCore;

namespace ExploraCostaRica.Services
{

    public class DashboardSyncService : IDashboardSyncService
    {
        private const string BusquedaGeneral = "Costa Rica";
        private const int MaxDestinosBiodiversidad = 3;

        private readonly IDashboardService _dashboard;
        private readonly INoticiasApiService _noticias;
        private readonly IFotografiasApiService _fotografias;
        private readonly IBiodiversidadApiService _biodiversidad;
        private readonly ExploraCostaRicaDbContext _context;

        public DashboardSyncService(
            IDashboardService dashboard,
            INoticiasApiService noticias,
            IFotografiasApiService fotografias,
            IBiodiversidadApiService biodiversidad,
            ExploraCostaRicaDbContext context)
        {
            _dashboard = dashboard;
            _noticias = noticias;
            _fotografias = fotografias;
            _biodiversidad = biodiversidad;
            _context = context;
        }

        public async Task<DashboardImportResult> SincronizarAsync(
            CancellationToken ct = default)
        {
            var export = _dashboard.NuevoExport();
            var notas = new List<string>();

            // ---- GNews: noticias (texto) ----
            await Consultar(notas, "GNews", async () =>
            {
                var dtos = await _noticias.BuscarContenidoAsync(BusquedaGeneral);

                return CrearFuente(
                    "gnews", "GNews", "https://gnews.io/api/v4/search",
                    "noticias", MediaTypes.Text, dtos);
            }, export);

            // ---- Unsplash: fotografías (imagen) ----
            await Consultar(notas, "Unsplash", async () =>
            {
                var dtos = await _fotografias.BuscarContenidoAsync(BusquedaGeneral);

                return CrearFuente(
                    "unsplash", "Unsplash", "https://api.unsplash.com/search/photos",
                    "imagenes", MediaTypes.Image, dtos);
            }, export);

            // ---- iNaturalist: observaciones cerca de los destinos (imagen) ----
            await Consultar(notas, "iNaturalist", async () =>
            {
                var destinos = await _context.Destinations
                    .AsNoTracking()
                    .Where(d => d.IsActive && d.Latitude != null && d.Longitude != null)
                    .OrderByDescending(d => d.IsFeatured)
                    .ThenBy(d => d.Name)
                    .Take(MaxDestinosBiodiversidad)
                    .Select(d => new { d.Name, d.Latitude, d.Longitude })
                    .ToListAsync(ct);

                if (destinos.Count == 0)
                {
                    throw new InvalidOperationException(
                        "no hay destinos con coordenadas");
                }

                var todos = new List<MediaItemDto>();

                foreach (var destino in destinos)
                {
                    todos.AddRange(await _biodiversidad.BuscarContenidoAsync(
                        (double)destino.Latitude!.Value,
                        (double)destino.Longitude!.Value,
                        destino.Name));
                }

                return CrearFuente(
                    "inaturalist", "iNaturalist",
                    "https://api.inaturalist.org/v1/observations",
                    "ciencia", MediaTypes.Image, todos);
            }, export);

            if (export.Sources.Count == 0)
            {
                return new DashboardImportResult
                {
                    Success = false,
                    Message = "No se pudo consultar ninguna fuente.",
                    Notes = notas
                };
            }

            var resultado = await _dashboard.GuardarAsync(export, ct);

            resultado.Notes = notas;

            if (resultado.Success)
            {
                // Marca cuáles fuentes necesitan API key (columna RequiresSecret).
                await _context.DashboardSources
                    .Where(s => s.SourceKey == "gnews" || s.SourceKey == "unsplash")
                    .ExecuteUpdateAsync(
                        s => s.SetProperty(x => x.RequiresSecret, true), ct);
            }

            return resultado;
        }


        private static async Task Consultar(
            List<string> notas,
            string nombre,
            Func<Task<ExportSource>> consulta,
            DashboardExport export)
        {
            try
            {
                var fuente = await consulta();

                if (fuente.Items.Count == 0)
                {
                    notas.Add($"{nombre}: la consulta no devolvió contenido.");
                    return;
                }

                export.Sources.Add(fuente);
                notas.Add($"{nombre}: {fuente.Items.Count} items.");
            }
            catch (Exception ex) when (
                ex is HttpRequestException ||
                ex is InvalidOperationException ||
                ex is TaskCanceledException ||
                ex is JsonException)
            {
                notas.Add($"{nombre}: no se pudo consultar ({ex.Message})");
            }
        }


        private static ExportSource CrearFuente(
            string id,
            string nombre,
            string url,
            string categoria,
            string mediaType,
            IEnumerable<MediaItemDto> dtos)
        {
            var fuente = new ExportSource
            {
                Id = id,
                Name = nombre,
                Url = url,
                Category = categoria
            };

            foreach (var dto in dtos.Where(d => !string.IsNullOrWhiteSpace(d.Title)))
            {
                fuente.Items.Add(new ExportItem
                {
                    Id = CrearId(dto),
                    Title = dto.Title.Trim(),
                    MediaType = mediaType,
                    CreatedAt = FechaUtc(dto.PublishedAt),
                    Category = null,
                    Url = dto.Url,
                    Thumbnail = dto.ImageUrl,
                    Description = dto.Description,
                    // Lo que devolvió nuestra capa de servicios para este item.
                    Data = JsonSerializer.SerializeToElement(dto)
                });
            }

            return fuente;
        }

        // El id debe ser único dentro de la fuente y caber en 250 caracteres.
        private static string CrearId(MediaItemDto dto)
        {
            var id = dto.ExternalId?.Trim();

            if (!string.IsNullOrEmpty(id) && id.Length <= 250)
            {
                return id;
            }

            var origen = !string.IsNullOrEmpty(id) ? id : dto.Url ?? dto.Title;

            return Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(origen))).ToLowerInvariant();
        }

        // Si la API no trae fecha, se usa el momento de la consulta.
        private static DateTime FechaUtc(DateTime? fecha)
        {
            var valor = fecha ?? DateTime.UtcNow;

            valor = valor.Kind switch
            {
                DateTimeKind.Utc => valor,
                DateTimeKind.Local => valor.ToUniversalTime(),
                _ => DateTime.SpecifyKind(valor, DateTimeKind.Utc)
            };

            return new DateTime(
                valor.Ticks - valor.Ticks % TimeSpan.TicksPerSecond,
                DateTimeKind.Utc);
        }
    }
}
