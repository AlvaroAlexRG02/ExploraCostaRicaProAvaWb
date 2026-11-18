using ExploraCostaRica.Models.DTOs;
using ExploraCostaRica.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExploraCostaRica.API.Controllers
{
    // ============================================================
    // API 7 - CROSSREF
    // ------------------------------------------------------------
    // Endpoint propio de la aplicación. Recibe una búsqueda,
    // le pide los datos a CrossrefApiService y devuelve la
    // lista en el formato común MediaItemDto.
    //
    // Ejemplo: GET /api/papers?busqueda=Monteverde&cantidad=12
    // ============================================================
    [ApiController]
    [Route("api/papers")]
    public class PapersController : ControllerBase
    {
        private readonly ICrossrefApiService _crossrefService;

        // El servicio llega por inyección de dependencias (Program.cs).
        public PapersController(ICrossrefApiService crossrefService)
        {
            _crossrefService = crossrefService;
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
                    mensaje = "Debes indicar qué buscar. Ej.: Monteverde, quetzal, volcán Arenal."
                });
            }

            try
            {
                var resultados =
                    await _crossrefService.BuscarContenidoAsync(busqueda, cantidad);

                // 200: lista (puede venir vacía si no hay artículos).
                return Ok(resultados);
            }
            catch (HttpRequestException)
            {
                // 502: Crossref respondió con error o no se pudo conectar.
                return StatusCode(502, new
                {
                    mensaje = "No fue posible consultar Crossref."
                });
            }
            catch (TaskCanceledException)
            {
                // 504: Crossref tardó más que el tiempo límite (timeout).
                return StatusCode(504, new
                {
                    mensaje = "Crossref tardó demasiado en responder."
                });
            }
        }
    }
}
