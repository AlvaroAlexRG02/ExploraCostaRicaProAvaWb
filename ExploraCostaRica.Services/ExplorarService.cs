using ExploraCostaRica.Data;
using ExploraCostaRica.Models.ViewModels;
using ExploraCostaRica.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExploraCostaRica.Services
{
    // ============================================================
    // LÓGICA DE BÚSQUEDA Y FILTROS DE LA PANTALLA EXPLORAR
    // ============================================================
    // Lee la vista vw_MediaCatalog de SQL Server, que ya junta
    // el contenido con su tipo, categoría, destino y fuente.
    // ============================================================
    public class ExplorarService : IExplorarService
    {
        // Mismo usuario demo que usa Favoritos mientras no exista login
        private const string DemoUserEmail = "demo@exploracostarica.local";

        // Largo máximo de SearchText en la tabla SearchHistory
        private const int MaxSearchTextLength = 300;

        private readonly ExploraCostaRicaDbContext _context;

        // El DbContext llega por inyección de dependencias (Program.cs)
        public ExplorarService(ExploraCostaRicaDbContext context)
        {
            _context = context;
        }

        public async Task<ExplorarViewModel> BuscarAsync(ExplorarFiltro filtro)
        {
            // 1. Punto de partida: todo el catálogo.
            //    AsNoTracking = solo lectura, más rápido.
            IQueryable<VwMediaCatalog> query = _context.VwMediaCatalogs.AsNoTracking();

            // 2. Aplicar cada filtro SOLO si el usuario lo eligió.
            query = AplicarTexto(query, filtro.Texto);

            if (!string.IsNullOrWhiteSpace(filtro.Tipo))
                query = query.Where(m => m.ContentType == filtro.Tipo);

            if (filtro.CategoriaId.HasValue)
                query = query.Where(m => m.CategoryId == filtro.CategoriaId);

            if (filtro.DestinoId.HasValue)
                query = query.Where(m => m.DestinationId == filtro.DestinoId);

            if (filtro.FuenteId.HasValue)
                query = query.Where(m => m.SourceId == filtro.FuenteId);

            // 3. Ordenar
            query = Ordenar(query, filtro.Orden);

            // 4. Ejecutar la consulta
            var resultados = await query.ToListAsync();

            // 5. Guardar la búsqueda en el historial (SearchHistory)
            await GuardarHistorialAsync(filtro, resultados.Count);

            // 6. Armar lo que necesita la vista
            return new ExplorarViewModel
            {
                Filtro = filtro,
                Resultados = resultados,
                Tipos = await ObtenerTiposAsync(),
                Categorias = await ObtenerCategoriasAsync(),
                Destinos = await ObtenerDestinosAsync(),
                Fuentes = await ObtenerFuentesAsync()
            };
        }

        // ------------------------------------------------------------
        // HISTORIAL DE BÚSQUEDAS
        // Solo se guarda cuando la persona escribió algo en el buscador
        // (si solo entra a la página, no cuenta como búsqueda).
        // Si falla el guardado, NO se cae la pantalla: la búsqueda
        // sigue funcionando igual.
        // ------------------------------------------------------------
        private async Task GuardarHistorialAsync(ExplorarFiltro filtro, int cantidadResultados)
        {
            if (string.IsNullOrWhiteSpace(filtro.Texto))
                return;

            var texto = filtro.Texto.Trim();
            if (texto.Length > MaxSearchTextLength)
                texto = texto.Substring(0, MaxSearchTextLength);

            try
            {
                // UserId puede ir vacío (null) si no existe el usuario demo
                var userId = await _context.Users
                    .AsNoTracking()
                    .Where(u => u.Email == DemoUserEmail && u.IsActive)
                    .Select(u => (int?)u.UserId)
                    .FirstOrDefaultAsync();

                _context.SearchHistories.Add(new SearchHistory
                {
                    UserId = userId,
                    SearchText = texto,
                    CategoryId = filtro.CategoriaId,
                    DestinationId = filtro.DestinoId,
                    ResultCount = cantidadResultados,
                    SearchedAt = DateTime.Now
                });

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Limpiamos lo pendiente para no afectar otras operaciones
                _context.ChangeTracker.Clear();
            }
        }

        // ------------------------------------------------------------
        // Búsqueda por texto: busca la palabra en varios campos.
        // Ej.: "Monteverde" encuentra por título, destino o descripción.
        // ------------------------------------------------------------
        private static IQueryable<VwMediaCatalog> AplicarTexto(IQueryable<VwMediaCatalog> query, string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return query;

            texto = texto.Trim();

            return query.Where(m =>
                m.Title.Contains(texto) ||
                (m.Description != null && m.Description.Contains(texto)) ||
                (m.Destination != null && m.Destination.Contains(texto)) ||
                (m.Province != null && m.Province.Contains(texto)) ||
                (m.Category != null && m.Category.Contains(texto)) ||
                (m.Author != null && m.Author.Contains(texto)) ||
                m.Source.Contains(texto));
        }

        // ------------------------------------------------------------
        // Orden: "Más relevantes", "Más recientes" o "Título A-Z"
        // ------------------------------------------------------------
        private static IQueryable<VwMediaCatalog> Ordenar(IQueryable<VwMediaCatalog> query, string? orden)
        {
            return orden switch
            {
                "recientes" => query.OrderByDescending(m => m.PublishedAt ?? m.SavedAt),
                "titulo" => query.OrderBy(m => m.Title),
                _ => query.OrderBy(m => m.ContentType).ThenBy(m => m.Title)   // relevantes
            };
        }

        // ------------------------------------------------------------
        // Opciones de los filtros, con cuántos elementos hay de cada una.
        // GroupBy agrupa y Count cuenta: Foto (14), Video (6)...
        // ------------------------------------------------------------
        private Task<List<OpcionFiltro>> ObtenerTiposAsync()
        {
            return _context.VwMediaCatalogs.AsNoTracking()
                .GroupBy(m => m.ContentType)
                .OrderBy(g => g.Key)
                .Select(g => new OpcionFiltro { Valor = g.Key, Texto = g.Key, Cantidad = g.Count() })
                .ToListAsync();
        }

        private Task<List<OpcionFiltro>> ObtenerCategoriasAsync()
        {
            return _context.VwMediaCatalogs.AsNoTracking()
                .Where(m => m.CategoryId != null)
                .GroupBy(m => new { m.CategoryId, m.Category })
                .OrderBy(g => g.Key.Category)
                .Select(g => new OpcionFiltro
                {
                    Valor = g.Key.CategoryId!.Value.ToString(),
                    Texto = g.Key.Category ?? "",
                    Cantidad = g.Count()
                })
                .ToListAsync();
        }

        private Task<List<OpcionFiltro>> ObtenerDestinosAsync()
        {
            return _context.VwMediaCatalogs.AsNoTracking()
                .Where(m => m.DestinationId != null)
                .GroupBy(m => new { m.DestinationId, m.Destination })
                .OrderBy(g => g.Key.Destination)
                .Select(g => new OpcionFiltro
                {
                    Valor = g.Key.DestinationId!.Value.ToString(),
                    Texto = g.Key.Destination ?? "",
                    Cantidad = g.Count()
                })
                .ToListAsync();
        }

        private Task<List<OpcionFiltro>> ObtenerFuentesAsync()
        {
            return _context.VwMediaCatalogs.AsNoTracking()
                .GroupBy(m => new { m.SourceId, m.Source })
                .OrderBy(g => g.Key.Source)
                .Select(g => new OpcionFiltro
                {
                    Valor = g.Key.SourceId.ToString(),
                    Texto = g.Key.Source,
                    Cantidad = g.Count()
                })
                .ToListAsync();
        }
    }
}
