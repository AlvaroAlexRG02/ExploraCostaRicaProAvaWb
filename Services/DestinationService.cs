using System.Linq.Expressions;
using ExploraCostaRica.Data;
using ExploraCostaRica.Models.Entities;
using ExploraCostaRica.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ExploraCostaRica.Services
{
    // ============================================================
    // LÓGICA DE LA PANTALLA DESTINOS Y DEL BOTÓN SORPRÉNDEME
    // ============================================================
    // Lee la tabla Destinations y, para el contenido asociado,
    // la vista vw_MediaCatalog. Solo muestra destinos activos.
    // ============================================================
    public class DestinationService : IDestinationService
    {
        // Largo máximo del texto de búsqueda (igual que Name en SQL)
        private const int MaxSearchLength = 150;

        // Cuánto contenido relacionado mostramos en el detalle
        private const int MaxRelatedContent = 12;

        private readonly ExploraCostaRicaDbContext _context;

        // El DbContext llega por inyección de dependencias (Program.cs)
        public DestinationService(ExploraCostaRicaDbContext context)
        {
            _context = context;
        }

        // ------------------------------------------------------------
        // Convierte un Destination en tarjeta.
        // EF Core lo traduce a SQL, así ContentCount se calcula en
        // la base de datos y no trayendo todos los MediaItems.
        // ------------------------------------------------------------
        private static readonly Expression<Func<Destination, DestinationCardViewModel>> ToCard =
            d => new DestinationCardViewModel
            {
                DestinationId = d.DestinationId,
                Name = d.Name,
                Province = d.Province,
                Description = d.Description,
                ImageUrl = d.ImageUrl,
                IsFeatured = d.IsFeatured,
                ContentCount = d.MediaItems.Count(m => m.IsActive)
            };

        // ============================================================
        // PANTALLA PRINCIPAL DE DESTINOS
        // ============================================================
        public async Task<DestinationIndexViewModel> GetIndexAsync(DestinationFilter filter)
        {
            filter ??= new DestinationFilter();

            // 1. Limpiar lo que escribió el usuario
            filter.Search = CleanText(filter.Search, MaxSearchLength);
            filter.Province = CleanText(filter.Province, MaxSearchLength);

            // 2. Punto de partida: solo destinos activos (solo lectura)
            IQueryable<Destination> query = ActiveDestinations();

            // 3. Aplicar cada filtro SOLO si el usuario lo eligió
            if (filter.Search != null)
            {
                var search = filter.Search;

                query = query.Where(d =>
                    d.Name.Contains(search) ||
                    d.Province.Contains(search) ||
                    (d.Description != null && d.Description.Contains(search)));
            }

            if (filter.Province != null)
            {
                var province = filter.Province;

                query = query.Where(d => d.Province == province);
            }

            // 4. Ejecutar: destacados primero y luego por nombre
            var destinations = await query
                .OrderByDescending(d => d.IsFeatured)
                .ThenBy(d => d.Name)
                .Select(ToCard)
                .ToListAsync();

            // 5. Armar lo que necesita la vista
            return new DestinationIndexViewModel
            {
                Filter = filter,
                Destinations = destinations,
                Featured = await GetFeaturedAsync(),
                Provinces = await GetProvincesAsync()
            };
        }

        // ============================================================
        // DETALLE DE UN DESTINO
        // ============================================================
        public Task<DestinationDetailViewModel?> GetDetailAsync(int destinationId)
        {
            return BuildDetailAsync(destinationId, isSurprise: false);
        }

        // ============================================================
        // SORPRÉNDEME: SELECCIÓN ALEATORIA REAL
        // ============================================================
        // OrderBy(Guid.NewGuid()) se traduce en SQL Server a
        // ORDER BY NEWID(), así el azar lo hace la base de datos
        // y solo viaja un destino, no la tabla completa.
        // ============================================================
        public async Task<DestinationDetailViewModel?> GetRandomAsync(int? excludeDestinationId = null)
        {
            IQueryable<Destination> candidates = ActiveDestinations();

            // Evitar repetir el destino anterior, pero solo si existe
            // otro destino activo (si hay uno solo, se repite).
            if (excludeDestinationId.HasValue)
            {
                var excludedId = excludeDestinationId.Value;

                var others = candidates.Where(d => d.DestinationId != excludedId);

                if (await others.AnyAsync())
                    candidates = others;
            }

            var randomId = await candidates
                .OrderBy(d => Guid.NewGuid())
                .Select(d => (int?)d.DestinationId)
                .FirstOrDefaultAsync();

            if (randomId == null)
                return null;   // No hay destinos activos

            return await BuildDetailAsync(randomId.Value, isSurprise: true);
        }

        // ------------------------------------------------------------
        // Arma el detalle (lo usan Detalle y Sorpréndeme)
        // ------------------------------------------------------------
        private async Task<DestinationDetailViewModel?> BuildDetailAsync(int destinationId, bool isSurprise)
        {
            var destination = await ActiveDestinations()
                .FirstOrDefaultAsync(d => d.DestinationId == destinationId);

            if (destination == null)
                return null;   // No existe o está inactivo

            // Contenido asociado desde vw_MediaCatalog
            // (la vista ya filtra MediaItems activos)
            var relatedContent = await _context.VwMediaCatalogs
                .AsNoTracking()
                .Where(m => m.DestinationId == destinationId)
                .OrderByDescending(m => m.PublishedAt ?? m.SavedAt)
                .Take(MaxRelatedContent)
                .ToListAsync();

            return new DestinationDetailViewModel
            {
                Destination = destination,
                RelatedContent = relatedContent,
                IsSurprise = isSurprise
            };
        }

        // ------------------------------------------------------------
        // Destinos destacados (para la sección superior)
        // ------------------------------------------------------------
        private Task<List<DestinationCardViewModel>> GetFeaturedAsync()
        {
            return ActiveDestinations()
                .Where(d => d.IsFeatured)
                .OrderBy(d => d.Name)
                .Select(ToCard)
                .ToListAsync();
        }

        // ------------------------------------------------------------
        // Provincias que tienen al menos un destino activo
        // ------------------------------------------------------------
        private Task<List<string>> GetProvincesAsync()
        {
            return ActiveDestinations()
                .Select(d => d.Province)
                .Distinct()
                .OrderBy(p => p)
                .ToListAsync();
        }

        // ------------------------------------------------------------
        // Consulta base: destinos activos, solo lectura
        // ------------------------------------------------------------
        private IQueryable<Destination> ActiveDestinations()
        {
            return _context.Destinations
                .AsNoTracking()
                .Where(d => d.IsActive);
        }

        // ------------------------------------------------------------
        // Quita espacios y recorta. Devuelve null si queda vacío.
        // ------------------------------------------------------------
        private static string? CleanText(string? text, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            text = text.Trim();

            return text.Length > maxLength
                ? text.Substring(0, maxLength)
                : text;
        }
    }
}