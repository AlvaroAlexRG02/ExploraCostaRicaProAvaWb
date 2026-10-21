using ExploraCostaRica.Models.DTOs;
using ExploraCostaRica.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExploraCostaRica.Controllers
{
    public class NoticiasController : Controller
    {
        private readonly INoticiasApiService _noticiasService;

        public NoticiasController(INoticiasApiService noticiasService)
        {
            _noticiasService = noticiasService;
        }

        // Pantalla de noticias.
        [HttpGet]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> Index(string? busqueda)
        {
            ViewBag.Busqueda = busqueda ?? string.Empty;
            ViewBag.Error = null;

            var noticias = new List<MediaItemDto>();

            // Solo consulta GNews si la persona escribió algo,
            // para no gastar el límite diario de la API.
            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                try
                {
                    noticias = await _noticiasService
                        .BuscarContenidoAsync(busqueda.Trim());
                }
                catch (HttpRequestException)
                {
                    ViewBag.Error = "No fue posible consultar GNews.";
                }
                catch (InvalidOperationException)
                {
                    ViewBag.Error = "El servicio de noticias no está configurado.";
                }
                catch (TaskCanceledException)
                {
                    ViewBag.Error = "GNews tardó demasiado en responder.";
                }
            }

            return View(noticias);
        }
    }
}