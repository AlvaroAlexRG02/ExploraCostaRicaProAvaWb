using ExploraCostaRica.Models.DTOs;
using ExploraCostaRica.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExploraCostaRica.API.Controllers
{
    // ============================================================
    // API 6 - OPEN LIBRARY
    // ------------------------------------------------------------
    // Endpoint propio de la aplicación. Recibe una búsqueda,
    // le pide los datos a OpenLibraryApiService y devuelve la
    // lista en el formato común MediaItemDto.
    //
    // Ejemplo: GET /api/libros?busqueda=Monteverde&cantidad=12
    // ============================================================
    [ApiController]
    [Route("api/libros")]
    public class LibrosController : ControllerBase
    {
        private readonly IOpenLibraryApiService _openLibraryService;

        // El servicio llega por inyección de dependencias (Program.cs).
        public LibrosController(IOpenLibraryApiService openLibraryService)
        {
            _openLibraryService = openLibraryService;
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
                    mensaje = "Debes indicar qué buscar. Ej.: Monteverde, aves, volcanes."
                });
            }

            try
            {
                var resultados =
                    await _openLibraryService.BuscarContenidoAsync(busqueda, cantidad);

                // 200: lista (puede venir vacía si no hay libros).
                return Ok(resultados);
            }
            catch (HttpRequestException)
            {
                // 502: Open Library respondió con error o no se pudo conectar.
                return StatusCode(502, new
                {
                    mensaje = "No fue posible consultar Open Library."
                });
            }
            catch (TaskCanceledException)
            {
                // 504: Open Library tardó más que el tiempo límite (timeout).
                return StatusCode(504, new
                {
                    mensaje = "Open Library tardó demasiado en responder."
                });
            }
        }
    }
}
