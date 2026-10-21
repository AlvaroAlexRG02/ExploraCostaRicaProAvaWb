using ExploraCostaRica.Models.DTOs;
using ExploraCostaRica.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExploraCostaRica.API.Controllers
{
    [ApiController]
    [Route("api/noticias")]
    public class NoticiasController : ControllerBase
    {
        private readonly INoticiasApiService _noticiasService;

        public NoticiasController(
            INoticiasApiService noticiasService)
        {
            _noticiasService = noticiasService;
        }

        // GET: /api/noticias?busqueda=Costa Rica turismo
        [HttpGet]
        [Produces("application/json")]
        [ProducesResponseType(typeof(List<MediaItemDto>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(502)]
        [ProducesResponseType(503)]
        [ProducesResponseType(504)]
        public async Task<IActionResult> Obtener(
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
                var resultados =
                    await _noticiasService.BuscarContenidoAsync(
                        busqueda.Trim());

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