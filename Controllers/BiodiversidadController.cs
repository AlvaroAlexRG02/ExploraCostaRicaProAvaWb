using ExploraCostaRica.Models.DTOs;
using ExploraCostaRica.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExploraCostaRica.Controllers
{
    public class BiodiversidadController : Controller
    {
        private readonly IBiodiversidadApiService _biodiversidadService;
        private readonly IDestinationService _destinationService;

        public BiodiversidadController(
            IBiodiversidadApiService biodiversidadService,
            IDestinationService destinationService)
        {
            _biodiversidadService = biodiversidadService;
            _destinationService = destinationService;
        }

        // GET /api/biodiversidad?destinoId=1&radioKm=10
        [HttpGet("/api/biodiversidad")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(List<MediaItemDto>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(502)]
        [ProducesResponseType(504)]
        public async Task<IActionResult> ObtenerJson(
            [FromQuery] int destinoId,
            [FromQuery] int radioKm = 10)
        {
            if (destinoId <= 0)
            {
                return BadRequest(new
                {
                    mensaje = "Debes indicar un destino válido."
                });
            }

            var detalle = await _destinationService.GetDetailAsync(destinoId);

            if (detalle == null)
            {
                return NotFound(new
                {
                    mensaje = "El destino no existe o está inactivo."
                });
            }

            if (!detalle.HasCoordinates)
            {
                return BadRequest(new
                {
                    mensaje = "El destino no tiene coordenadas registradas."
                });
            }

            try
            {
                var resultados = await _biodiversidadService.BuscarContenidoAsync(
                    (double)detalle.Destination.Latitude!.Value,
                    (double)detalle.Destination.Longitude!.Value,
                    detalle.Destination.Name,
                    radioKm);

                return Ok(resultados);
            }
            catch (HttpRequestException)
            {
                return StatusCode(502, new
                {
                    mensaje = "No fue posible consultar iNaturalist."
                });
            }
            catch (TaskCanceledException)
            {
                return StatusCode(504, new
                {
                    mensaje = "iNaturalist tardó demasiado en responder."
                });
            }
        }
    }
}
