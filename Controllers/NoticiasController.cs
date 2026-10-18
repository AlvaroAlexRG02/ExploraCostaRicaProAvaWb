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

        // Endpoint propio. GET /api/noticias?busqueda=Costa Rica turismo
        [HttpGet("/api/noticias")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(List<MediaItemDto>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(502)]
        [ProducesResponseType(503)]
        [ProducesResponseType(504)]
        public async Task<IActionResult> ObtenerJson(
            [FromQuery] string? busqueda)
        {
            if (string.IsNullOrWhiteSpace(busqueda))
            {
                return BadRequest(new
                {
                    mensaje = "Debes indicar un tema para buscar."
                });
            }

            try
            {
                var resultados = await _noticiasService
                    .BuscarContenidoAsync(busqueda.Trim());

                return Ok(resultados);
            }
            catch (HttpRequestException)
            {
                return StatusCode(502, new
                {
                    mensaje = "No fue posible consultar GNews."
                });
            }
            catch (InvalidOperationException)
            {
                return StatusCode(503, new
                {
                    mensaje = "El servicio de noticias no está configurado."
                });
            }
            catch (TaskCanceledException)
            {
                return StatusCode(504, new
                {
                    mensaje = "GNews tardó demasiado en responder."
                });
            }
        }
    }
}
