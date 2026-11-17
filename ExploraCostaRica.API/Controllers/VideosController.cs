using ExploraCostaRica.Models.DTOs;
using ExploraCostaRica.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExploraCostaRica.API.Controllers
{
    // ============================================================
    // API 4 - YOUTUBE
    // ------------------------------------------------------------
    // Endpoint propio de la aplicación. Recibe una búsqueda,
    // le pide los videos a YouTubeApiService y devuelve la lista
    // en el formato común MediaItemDto.
    //
    // Ejemplo: GET /api/videos?busqueda=Monteverde&cantidad=12
    // ============================================================
    [ApiController]
    [Route("api/videos")]
    public class VideosController : ControllerBase
    {
        private readonly IYouTubeApiService _youTubeService;

        // El servicio llega por inyección de dependencias (Program.cs).
        public VideosController(IYouTubeApiService youTubeService)
        {
            _youTubeService = youTubeService;
        }

        [HttpGet]
        [Produces("application/json")]
        [ProducesResponseType(typeof(List<MediaItemDto>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [ProducesResponseType(502)]
        [ProducesResponseType(504)]
        public async Task<IActionResult> Buscar(
            [FromQuery] string? busqueda,
            [FromQuery] int cantidad = 12)
        {
            // 400: no se puede buscar sin texto.
            if (string.IsNullOrWhiteSpace(busqueda))
            {
                return BadRequest(new
                {
                    mensaje = "Debes indicar qué buscar. Ej.: Monteverde, Arenal, playas."
                });
            }

            try
            {
                var resultados =
                    await _youTubeService.BuscarContenidoAsync(busqueda, cantidad);

                // 200: lista de videos (puede venir vacía).
                return Ok(resultados);
            }
            catch (InvalidOperationException)
            {
                // 500: falta la API Key en User Secrets.
                return StatusCode(500, new
                {
                    mensaje = "Falta configurar la API Key de YouTube."
                });
            }
            catch (HttpRequestException)
            {
                // 502: YouTube respondió con error (clave inválida,
                // cuota agotada o sin conexión).
                return StatusCode(502, new
                {
                    mensaje = "No fue posible consultar YouTube. Revisá la API Key o la cuota diaria."
                });
            }
            catch (TaskCanceledException)
            {
                // 504: YouTube tardó más que el tiempo límite.
                return StatusCode(504, new
                {
                    mensaje = "YouTube tardó demasiado en responder."
                });
            }
        }
    }
}