using ExploraCostaRica.Models.DTOs;
using ExploraCostaRica.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExploraCostaRica.API.Controllers
{
    [ApiController]
    [Route("api/fotografias")]
    public class FotografiasController : ControllerBase
    {
        private readonly IFotografiasApiService _fotografiasService;

        public FotografiasController(
            IFotografiasApiService fotografiasService)
        {
            _fotografiasService = fotografiasService;
        }

        // GET: /api/fotografias?busqueda=Volcan Arenal Costa Rica
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
                    mensaje = "Debes indicar un destino para buscar."
                });
            }

            try
            {
                var resultados =
                    await _fotografiasService.BuscarContenidoAsync(
                        busqueda.Trim());

                return Ok(resultados);
            }
            catch (HttpRequestException)
            {
                return StatusCode(502, new
                {
                    mensaje = "No fue posible consultar Unsplash."
                });
            }
            catch (InvalidOperationException)
            {
                return StatusCode(503, new
                {
                    mensaje = "El servicio de fotografías no está configurado."
                });
            }
            catch (TaskCanceledException)
            {
                return StatusCode(504, new
                {
                    mensaje = "Unsplash tardó demasiado en responder."
                });
            }
        }
    }
}