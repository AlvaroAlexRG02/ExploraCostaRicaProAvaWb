using ExploraCostaRica.Data;
using ExploraCostaRica.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ExploraCostaRica.Services
{
    public class FavoritosService : IFavoritosService
    {
        private const string DemoUserEmail =
            "demo@exploracostarica.local";

        private const string DestinationContentType =
            "Lugar";

        private readonly ExploraCostaRicaDbContext _context;

        public FavoritosService(
            ExploraCostaRicaDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // OBTENER USUARIO DEMO
        // ============================================================

        private Task<int?> ObtenerUsuarioIdAsync()
        {
            return _context.Users
                .AsNoTracking()
                .Where(u =>
                    u.Email == DemoUserEmail &&
                    u.IsActive)
                .Select(u => (int?)u.UserId)
                .FirstOrDefaultAsync();
        }


        // ============================================================
        // LISTAR FAVORITOS
        // ============================================================

        public async Task<List<VwUserFavorite>>
            ObtenerFavoritosAsync(
                string tipo = "todos",
                string orden = "recientes")
        {
            tipo = (tipo ?? "todos").ToLowerInvariant();
            orden = (orden ?? "recientes").ToLowerInvariant();

            var userId = await ObtenerUsuarioIdAsync();

            var query = _context.VwUserFavorites
                .AsNoTracking()
                .Where(f =>
                    f.UserId == (userId ?? -1));

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

        public async Task<List<long>> ObtenerIdsAsync()
        {
            var userId = await ObtenerUsuarioIdAsync();

            if (userId == null)
            {
                return new List<long>();
            }

            return await _context.Favorites
                .AsNoTracking()
                .Where(f =>
                    f.UserId == userId.Value)
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
            AgregarAsync(long mediaId)
        {
            var userId = await ObtenerUsuarioIdAsync();

            if (userId == null)
            {
                return (
                    false,
                    false,
                    0,
                    "No existe el Usuario Demo en la base de datos."
                );
            }

            var mediaExiste =
                await _context.MediaItems.AnyAsync(m =>
                    m.MediaId == mediaId &&
                    m.IsActive);

            if (!mediaExiste)
            {
                return (
                    false,
                    false,
                    await ContarAsync(userId.Value),
                    "El contenido no existe."
                );
            }

            var yaExiste =
                await _context.Favorites.AnyAsync(f =>
                    f.UserId == userId.Value &&
                    f.MediaId == mediaId);

            if (yaExiste)
            {
                return (
                    true,
                    true,
                    await ContarAsync(userId.Value),
                    null
                );
            }

            _context.Favorites.Add(
                new Favorite
                {
                    UserId = userId.Value,
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
                    await ContarAsync(userId.Value),
                    null
                );
            }

            return (
                true,
                false,
                await ContarAsync(userId.Value),
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
            QuitarAsync(long mediaId)
        {
            var userId = await ObtenerUsuarioIdAsync();

            if (userId == null)
            {
                return (
                    false,
                    0,
                    "No existe el Usuario Demo en la base de datos."
                );
            }

            await _context.Favorites
                .Where(f =>
                    f.UserId == userId.Value &&
                    f.MediaId == mediaId)
                .ExecuteDeleteAsync();

            return (
                true,
                await ContarAsync(userId.Value),
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
            ToggleAsync(long mediaId)
        {
            var userId = await ObtenerUsuarioIdAsync();

            if (userId == null)
            {
                return (
                    false,
                    false,
                    0,
                    "No existe el Usuario Demo en la base de datos."
                );
            }

            var existe =
                await _context.Favorites.AnyAsync(f =>
                    f.UserId == userId.Value &&
                    f.MediaId == mediaId);

            if (existe)
            {
                await _context.Favorites
                    .Where(f =>
                        f.UserId == userId.Value &&
                        f.MediaId == mediaId)
                    .ExecuteDeleteAsync();

                return (
                    true,
                    false,
                    await ContarAsync(userId.Value),
                    null
                );
            }

            var resultado =
                await AgregarAsync(mediaId);

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