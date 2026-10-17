using ExploraCostaRica.Models.ViewModels;
using ExploraCostaRica.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExploraCostaRica.Controllers
{
    // ============================================================
    // CONTROLLER DE DESTINOS Y SORPRÉNDEME
    // ============================================================
    // No tiene lógica de negocio: recibe la petición, llama a
    // IDestinationService y devuelve la vista.
    //
    // URLs:
    //   /Destinations                    -> lista de destinos
    //   /Destinations/Detail/3           -> detalle del destino 3
    //   /Destinations/Surprise           -> destino aleatorio
    //   /Destinations/Surprise?exclude=3 -> otro destino distinto al 3
    // ============================================================
    public class DestinationsController : Controller
    {
        private readonly IDestinationService _destinationService;

        // El servicio llega por inyección de dependencias (Program.cs)
        public DestinationsController(IDestinationService destinationService)
        {
            _destinationService = destinationService;
        }

        // ============================================================
        // LISTA DE DESTINOS (con búsqueda y filtro por provincia)
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] DestinationFilter filter)
        {
            var model = await _destinationService.GetIndexAsync(filter);

            return View(model);
        }

        // ============================================================
        // DETALLE DE UN DESTINO
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Detail(int id)
        {
            var model = await _destinationService.GetDetailAsync(id);

            if (model == null)
                return NotFound();   // No existe o está inactivo

            return View(model);
        }

        // ============================================================
        // SORPRÉNDEME
        // ============================================================
        // exclude = id del destino que se mostró antes, para que
        // el botón "Otro destino" no repita el mismo.
        // ============================================================
        [HttpGet]
        public async Task<IActionResult> Surprise(int? exclude)
        {
            var model = await _destinationService.GetRandomAsync(exclude);

            if (model == null)
            {
                TempData["Error"] = "No hay destinos disponibles en este momento.";

                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }
    }
}