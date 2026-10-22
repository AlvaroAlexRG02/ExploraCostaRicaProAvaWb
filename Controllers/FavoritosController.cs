using ExploraCostaRica.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExploraCostaRica.Controllers
{
    public class FavoritosController : Controller
    {
        private const string DestinationContentType = "Lugar";

        private readonly IFavoritosService _favoritosService;

        public FavoritosController(
            IFavoritosService favoritosService)
        {
            _favoritosService = favoritosService;
        }


        // ============================================================
        // PANTALLA DE FAVORITOS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Index(
            string tipo = "todos",
            string orden = "recientes")
        {
            tipo = (tipo ?? "todos").ToLowerInvariant();
            orden = (orden ?? "recientes").ToLowerInvariant();

            if (tipo != "destinos" &&
                tipo != "contenido")
            {
                tipo = "todos";
            }

            if (orden != "antiguos" &&
                orden != "titulo")
            {
                orden = "recientes";
            }

            var items =
                await _favoritosService.ObtenerFavoritosAsync(
                    tipo,
                    orden);

            ViewBag.Tipo = tipo;
            ViewBag.Orden = orden;
            ViewBag.DestinationContentType =
                DestinationContentType;

            return View(items);
        }


        // ============================================================
        // IDS DE LOS FAVORITOS
        // ============================================================

        [HttpGet]
        public async Task<IActionResult> Ids()
        {
            var ids =
                await _favoritosService.ObtenerIdsAsync();

            return Json(ids);
        }


        // ============================================================
        // AGREGAR FAVORITO
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Agregar(
            long mediaId)
        {
            var resultado =
                await _favoritosService.AgregarAsync(
                    mediaId);

            if (!resultado.Exitoso)
            {
                if (resultado.Error ==
                    "El contenido no existe.")
                {
                    return NotFound(new
                    {
                        error = resultado.Error
                    });
                }

                return BadRequest(new
                {
                    error = resultado.Error
                });
            }

            return Json(new
            {
                isFavorite = true,
                alreadyExisted =
                    resultado.YaExistia,
                count =
                    resultado.Cantidad
            });
        }


        // ============================================================
        // QUITAR FAVORITO
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Quitar(
            long mediaId)
        {
            var resultado =
                await _favoritosService.QuitarAsync(
                    mediaId);

            if (!resultado.Exitoso)
            {
                return BadRequest(new
                {
                    error = resultado.Error
                });
            }

            return Json(new
            {
                isFavorite = false,
                count =
                    resultado.Cantidad
            });
        }


        // ============================================================
        // ACTIVAR / DESACTIVAR FAVORITO
        // ============================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Toggle(
            long mediaId)
        {
            var resultado =
                await _favoritosService.ToggleAsync(
                    mediaId);

            if (!resultado.Exitoso)
            {
                return BadRequest(new
                {
                    error = resultado.Error
                });
            }

            return Json(new
            {
                isFavorite =
                    resultado.EsFavorito,
                count =
                    resultado.Cantidad
            });
        }
    }
}