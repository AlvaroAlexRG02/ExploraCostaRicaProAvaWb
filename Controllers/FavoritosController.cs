using ExploraCostaRica.Data;
using ExploraCostaRica.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExploraCostaRica.Controllers
{
    public class FavoritosController : Controller
    {
        private const string DemoUserEmail = "demo@exploracostarica.local";


        private const string DestinationContentType = "Lugar";

        private readonly ExploraCostaRicaDbContext _context;

        public FavoritosController(ExploraCostaRicaDbContext context)
        {
            _context = context;
        }


        private Task<int?> GetCurrentUserIdAsync()
        {
            return _context.Users
                .AsNoTracking()
                .Where(u => u.Email == DemoUserEmail && u.IsActive)
                .Select(u => (int?)u.UserId)
                .FirstOrDefaultAsync();
        }

 
        [HttpGet]
        public async Task<IActionResult> Index(
            string tipo = "todos",
            string orden = "recientes")
        {
            tipo = (tipo ?? "todos").ToLowerInvariant();
            orden = (orden ?? "recientes").ToLowerInvariant();

            var userId = await GetCurrentUserIdAsync();

            var query = _context.VwUserFavorites
                .AsNoTracking()
                .Where(f => f.UserId == (userId ?? -1));

            if (tipo == "destinos")
                query = query.Where(f => f.ContentType == DestinationContentType);
            else if (tipo == "contenido")
                query = query.Where(f => f.ContentType != DestinationContentType);
            else
                tipo = "todos";

            query = orden switch
            {
                "antiguos" => query.OrderBy(f => f.FavoriteDate),
                "titulo"   => query.OrderBy(f => f.Title),
                _          => query.OrderByDescending(f => f.FavoriteDate)
            };

            ViewBag.Tipo = tipo;
            ViewBag.Orden = orden == "antiguos" || orden == "titulo" ? orden : "recientes";
            ViewBag.DestinationContentType = DestinationContentType;

            var items = await query.ToListAsync();
            return View(items);
        }


        [HttpGet]
        public async Task<IActionResult> Ids()
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null)
                return Json(Array.Empty<long>());

            var ids = await _context.Favorites
                .AsNoTracking()
                .Where(f => f.UserId == userId)
                .Select(f => f.MediaId)
                .ToListAsync();

            return Json(ids);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Agregar(long mediaId)
        {
            var result = await AddInternalAsync(mediaId);
            if (result.Error != null)
                return result.Error;

            return Json(new
            {
                isFavorite = true,
                alreadyExisted = result.AlreadyExisted,
                count = await CountAsync(result.UserId)
            });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Quitar(long mediaId)
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null)
                return BadRequest(new { error = "No existe el Usuario Demo en la base de datos." });

            await _context.Favorites
                .Where(f => f.UserId == userId && f.MediaId == mediaId)
                .ExecuteDeleteAsync();

            return Json(new
            {
                isFavorite = false,
                count = await CountAsync(userId.Value)
            });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(long mediaId)
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null)
                return BadRequest(new { error = "No existe el Usuario Demo en la base de datos." });

            var exists = await _context.Favorites
                .AnyAsync(f => f.UserId == userId && f.MediaId == mediaId);

            if (exists)
            {
                await _context.Favorites
                    .Where(f => f.UserId == userId && f.MediaId == mediaId)
                    .ExecuteDeleteAsync();

                return Json(new
                {
                    isFavorite = false,
                    count = await CountAsync(userId.Value)
                });
            }

            var result = await AddInternalAsync(mediaId);
            if (result.Error != null)
                return result.Error;

            return Json(new
            {
                isFavorite = true,
                count = await CountAsync(result.UserId)
            });
        }


        private async Task<(int UserId, bool AlreadyExisted, IActionResult? Error)>
            AddInternalAsync(long mediaId)
        {
            var userId = await GetCurrentUserIdAsync();
            if (userId == null)
                return (0, false, BadRequest(new { error = "No existe el Usuario Demo en la base de datos." }));

            var mediaExists = await _context.MediaItems
                .AnyAsync(m => m.MediaId == mediaId && m.IsActive);

            if (!mediaExists)
                return (userId.Value, false, NotFound(new { error = "El contenido no existe." }));

            var exists = await _context.Favorites
                .AnyAsync(f => f.UserId == userId && f.MediaId == mediaId);

            if (exists)
                return (userId.Value, true, null);

            _context.Favorites.Add(new Favorite
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
                return (userId.Value, true, null);
            }

            return (userId.Value, false, null);
        }

        private Task<int> CountAsync(int userId)
        {
            return _context.Favorites.CountAsync(f => f.UserId == userId);
        }
    }
}
