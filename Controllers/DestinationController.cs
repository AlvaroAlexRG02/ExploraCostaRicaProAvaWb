using ExploraCostaRica.Models.ViewModels;
using ExploraCostaRica.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExploraCostaRica.Controllers
{
    public class DestinationsController : Controller
    {
        private readonly IDestinationService _destinationService;

       
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