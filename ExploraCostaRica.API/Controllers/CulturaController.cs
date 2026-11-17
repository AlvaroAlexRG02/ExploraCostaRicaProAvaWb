using ExploraCostaRica.Models.DTOs;
using ExploraCostaRica.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExploraCostaRica.API.Controllers
{
    [ApiController]
    [Route("api/cultura")]
    public class CulturaController : ControllerBase
    {
        private readonly IWikimediaApiService _wikimediaService;
        private readonly IDestinationService _destinationService;

        public CulturaController(
            IWikimediaApiService wikimediaService,
            IDestinationService destinationService)
        {
            _wikimediaService = wikimediaService;
            _destinationService = destinationService;
        }

        // GET: /api/cultura?busqueda=Arenal
        // GET: /api/cultura?destinoId=8
        [HttpGet]
        [Produces("application/json")]
        [ProducesResponseType(typeof(List<MediaItemDto>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(502)]
        [ProducesResponseType(504)]
        public async Task<IActionResult> Obtener(
            [FromQuery] string? busqueda,
            [FromQuery] int? destinoId,
            [FromQuery] int cantidad = 12)
        {
            string? nombreDestino = null;

            if (destinoId.HasValue)
            {
                var detalle =
                    await _destinationService.GetDetailAsync(destinoId.Value);

                if (detalle == null)
                {
                    return NotFound(new
                    {
                        mensaje = "El destino no existe o está inactivo."
                    });
                }

                nombreDestino = detalle.Destination.Name;

                if (string.IsNullOrWhiteSpace(busqueda))
                {
                    busqueda = nombreDestino + " Costa Rica";
                }
            }

            if (string.IsNullOrWhiteSpace(busqueda))
            {
                return BadRequest(new
                {
                    mensaje = "Indica una búsqueda o un destino."
                });
            }

            try
            {
                var resultados =
                    await _wikimediaService.BuscarContenidoAsync(
                        busqueda.Trim(),
                        nombreDestino,
                        cantidad);


                return Ok(resultados);
            }
            catch (HttpRequestException)
            {
                return StatusCode(502, new
                {
                    mensaje = "No fue posible consultar Wikimedia Commons."
                });
            }
            catch (TaskCanceledException)
            {
                return StatusCode(504, new
                {
                    mensaje = "Wikimedia Commons tardó demasiado en responder."
                });
            }
        }
    }
}
