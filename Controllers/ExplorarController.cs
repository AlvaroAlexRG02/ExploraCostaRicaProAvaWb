using ExploraCostaRica.Models.ViewModels;
using ExploraCostaRica.Services;
using Microsoft.AspNetCore.Mvc;

namespace ExploraCostaRica.Controllers
{
    // ============================================================
    // CONTROLLER DE LA PANTALLA EXPLORAR
    // ============================================================
    // Su único trabajo es:
    //   1. Recibir los filtros que vienen en la URL.
    //   2. Pasárselos al servicio.
    //   3. Mandar el resultado a la vista.
    //
    // NO filtra ni busca nada él mismo: eso lo hace ExplorarService.
    // (Regla del enunciado: sin lógica de negocio en controladores.)
    // ============================================================
    public class ExplorarController : Controller
    {
        private readonly IExplorarService _explorarService;

        // El servicio llega por inyección de dependencias
        // (lo registramos en Program.cs con AddScoped).
        public ExplorarController(IExplorarService explorarService)
        {
            _explorarService = explorarService;
        }

        // ========================================================
        // GET /Explorar
        // GET /Explorar?Texto=Monteverde&Tipo=Foto&Orden=recientes
        // ========================================================
        // [FromQuery] toma los valores de la URL y los mete
        // automáticamente en el objeto "filtro" (Model Binding).
        // ========================================================
        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] ExplorarFiltro filtro)
        {
            var modelo = await _explorarService.BuscarAsync(filtro);
            return View(modelo);
        }
    }
}
