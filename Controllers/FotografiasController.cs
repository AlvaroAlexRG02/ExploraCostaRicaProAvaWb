using ExploraCostaRica.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExploraCostaRica.Controllers
{
    public class FotografiasController : Controller
    {
        private readonly IFotografiasApiService _fotografiasService;

        public FotografiasController(
            IFotografiasApiService fotografiasService)
        {
            _fotografiasService = fotografiasService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? busqueda)
        {
            ViewBag.Busqueda = busqueda ?? string.Empty;
            ViewBag.Error = null;

            var fotografias = new List<FotografiaDto>();

            if (!string.IsNullOrWhiteSpace(busqueda))
            {
                try
                {
                    fotografias =
                        await _fotografiasService.BuscarFotografiasAsync(
                            busqueda);
                }
                catch (HttpRequestException)
                {
                    ViewBag.Error =
                        "No fue posible consultar Unsplash. " +
                        "Verificá la conexión y la API Key.";
                }
                catch (InvalidOperationException)
                {
                    ViewBag.Error =
                        "No se pudo completar la consulta de fotografías.";
                }
            }

            return View(fotografias);
        }
    }
}