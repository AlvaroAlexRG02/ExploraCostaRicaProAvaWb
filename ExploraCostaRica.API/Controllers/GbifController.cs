using ExploraCostaRica.Models.DTOs;
using ExploraCostaRica.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExploraCostaRica.API.Controllers
{
    // ============================================================
    // API 10 - GBIF
    // ------------------------------------------------------------
    // Endpoint propio de la aplicación. Recibe una búsqueda,
    // le pide los datos a GbifApiService y devuelve la lista en
    // el formato común MediaItemDto.
    //
    // Ejemplo: GET /api/gbif?busqueda=Monteverde&cantidad=12
    // ============================================================
    [ApiController]
    [Route("api/gbif")]
    public class GbifController : ControllerBase
    {
        private readonly IGbifApiService _gbifService;

        // El servicio llega por inyección de dependencias (Program.cs).
        public GbifController(IGbifApiService gbifService)
        {
            _gbifService = gbifService;
        }

        [HttpGet]
        [Produces("application/json")]
        [ProducesResponseType(typeof(List<MediaItemDto>), 200)]
        [ProducesResponseType(400)]
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
                    mensaje = "Debes indicar qué buscar. Ej.: Monteverde, quetzal, orquídea."
                });
            }

            try
            {
                var resultados =
                    await _gbifService.BuscarContenidoAsync(busqueda, cantidad);

                // 200: lista (puede venir vacía si no hay registros con foto).
                return Ok(resultados);
            }
            catch (HttpRequestException)
            {
                // 502: GBIF respondió con error o no se pudo conectar.
                return StatusCode(502, new
                {
                    mensaje = "No fue posible consultar GBIF."
                });
            }
            catch (TaskCanceledException)
            {
                // 504: GBIF tardó más que el tiempo límite (timeout).
                return StatusCode(504, new
                {
                    mensaje = "GBIF tardó demasiado en responder."
                });
            }
        }
    }
}