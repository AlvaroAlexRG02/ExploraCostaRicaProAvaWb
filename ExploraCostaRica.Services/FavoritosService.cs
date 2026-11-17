using ExploraCostaRica.Data;
using ExploraCostaRica.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExploraCostaRica.Services
{
    public class FavoritosService : IFavoritosService
    {
        private const string DestinationContentType =
            "Lugar";

        private readonly ExploraCostaRicaDbContext _context;

        public FavoritosService(
            ExploraCostaRicaDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // LISTAR FAVORITOS
        // ============================================================

        public async Task<List<VwUserFavorite>>
            ObtenerFavoritosAsync(
                int userId,
                string tipo = "todos",
                string orden = "recientes")
        {
            tipo = (tipo ?? "todos").ToLowerInvariant();
            orden = (orden ?? "recientes").ToLowerInvariant();

            var query = _context.VwUserFavorites
                .AsNoTracking()
                .Where(f =>
                    f.UserId == userId);

            if (tipo == "destinos")
            {
                query = query.Where(f =>
                    f.ContentType ==
                    DestinationContentType);
            }
            else if (tipo == "contenido")
            {
                query = query.Where(f =>
                    f.ContentType !=
                    DestinationContentType);
            }

            query = orden switch
            {
                "antiguos" =>
                    query.OrderBy(f =>
                        f.FavoriteDate),

                "titulo" =>
                    query.OrderBy(f =>
                        f.Title),

                _ =>
                    query.OrderByDescending(f =>
                        f.FavoriteDate)
            };

            return await query.ToListAsync();
        }


        // ============================================================
        // OBTENER IDS DE FAVORITOS
        // ============================================================

        public async Task<List<long>> ObtenerIdsAsync(int userId)
        {
            return await _context.Favorites
                .AsNoTracking()
                .Where(f =>
                    f.UserId == userId)
                .Select(f => f.MediaId)
                .ToListAsync();
        }


        // ============================================================
        // AGREGAR FAVORITO
        // ============================================================

        public async Task<(
            bool Exitoso,
            bool YaExistia,
            int Cantidad,
            string? Error)>
            AgregarAsync(int userId, long mediaId)
        {
            var mediaExiste =
                await _context.MediaItems.AnyAsync(m =>
                    m.MediaId == mediaId &&
                    m.IsActive);

            if (!mediaExiste)
            {
                return (
                    false,
                    false,
                    await ContarAsync(userId),
                    "El contenido no existe."
                );
            }

            var yaExiste =
                await _context.Favorites.AnyAsync(f =>
                    f.UserId == userId &&
                    f.MediaId == mediaId);

            if (yaExiste)
            {
                return (
                    true,
                    true,
                    await ContarAsync(userId),
                    null
                );
            }

            _context.Favorites.Add(
                new Favorite
                {
                    UserId = userId,
                    MediaId = mediaId
                });

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                _context.ChangeTracker.Clear();

                return (
                    true,
                    true,
                    await ContarAsync(userId),
                    null
                );
            }

            return (
                true,
                false,
                await ContarAsync(userId),
                null
            );
        }


        // ============================================================
        // QUITAR FAVORITO
        // ============================================================

        public async Task<(
            bool Exitoso,
            int Cantidad,
            string? Error)>
            QuitarAsync(int userId, long mediaId)
        {
            await _context.Favorites
                .Where(f =>
                    f.UserId == userId &&
                    f.MediaId == mediaId)
                .ExecuteDeleteAsync();

            return (
                true,
                await ContarAsync(userId),
                null
            );
        }


        // ============================================================
        // ACTIVAR / DESACTIVAR FAVORITO
        // ============================================================

        public async Task<(
            bool Exitoso,
            bool EsFavorito,
            int Cantidad,
            string? Error)>
            ToggleAsync(int userId, long mediaId)
        {
            var existe =
                await _context.Favorites.AnyAsync(f =>
                    f.UserId == userId &&
                    f.MediaId == mediaId);

            if (existe)
            {
                await _context.Favorites
                    .Where(f =>
                        f.UserId == userId &&
                        f.MediaId == mediaId)
                    .ExecuteDeleteAsync();

                return (
                    true,
                    false,
                    await ContarAsync(userId),
                    null
                );
            }

            var resultado =
                await AgregarAsync(userId, mediaId);

            return (
                resultado.Exitoso,
                resultado.Exitoso,
                resultado.Cantidad,
                resultado.Error
            );
        }


        // ============================================================
        // CONTAR FAVORITOS
        // ============================================================

        private Task<int> ContarAsync(int userId)
        {
            return _context.Favorites
                .CountAsync(f =>
                    f.UserId == userId);
        }
    }
}