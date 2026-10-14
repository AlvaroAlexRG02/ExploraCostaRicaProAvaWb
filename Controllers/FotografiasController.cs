using ExploraCostaRica.Models.DTOs;
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

        // Página con el buscador de fotografías.
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
                        "No fue posible consultar Unsplash.";
                }
                catch (InvalidOperationException)
                {
                    ViewBag.Error =
                        "No se pudo completar la consulta.";
                }
            }

            return View(fotografias);
        }

        // Endpoint propio que devuelve fotografías como JSON.
        // GET /api/fotografias?busqueda=Volcan Arenal Costa Rica
        [HttpGet("/api/fotografias")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(List<MediaItemDto>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(502)]
        public async Task<IActionResult> ObtenerJson(
            [FromQuery] string? busqueda)
        {
            if (string.IsNullOrWhiteSpace(busqueda))
            {
                return BadRequest(new
                {
                    mensaje = "Debés indicar un destino para buscar."
                });
            }

            try
            {
                var resultados =
                    await _fotografiasService.BuscarContenidoAsync(
                        busqueda);

                return Ok(resultados);
            }
            catch (HttpRequestException)
            {
                return StatusCode(502, new
                {
                    mensaje = "No fue posible consultar Unsplash."
                });
            }
        }
    }
}