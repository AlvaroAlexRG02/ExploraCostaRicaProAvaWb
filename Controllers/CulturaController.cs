using ExploraCostaRica.Models.DTOs;
using ExploraCostaRica.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExploraCostaRica.Controllers
{
    public class CulturaController : Controller
    {

        private const string BusquedaInicial = "Costa Rica cultura";

        private readonly IWikimediaApiService _wikimediaService;

        public CulturaController(IWikimediaApiService wikimediaService)
        {
            _wikimediaService = wikimediaService;
        }


        [HttpGet]
        [ApiExplorerSettings(IgnoreApi = true)]
        public async Task<IActionResult> Index(string? busqueda)
        {
            busqueda = string.IsNullOrWhiteSpace(busqueda)
                ? BusquedaInicial
                : busqueda.Trim();

            ViewBag.Busqueda = busqueda;
            ViewBag.Error = null;

            var items = new List<MediaItemDto>();

            try
            {
                items = await _wikimediaService.BuscarContenidoAsync(busqueda);
            }
            catch (HttpRequestException)
            {
                ViewBag.Error = "No fue posible consultar Wikimedia Commons.";
            }
            catch (TaskCanceledException)
            {
                ViewBag.Error = "Wikimedia Commons tardó demasiado en responder.";
            }

            return View(items);
        }
    }
}
