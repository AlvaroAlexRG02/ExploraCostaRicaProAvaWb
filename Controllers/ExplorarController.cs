using ExploraCostaRica.Extensions;
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
    //   2. Saber quién inició sesión (para su historial).
    //   3. Pasárselos al servicio.
    //   4. Mandar el resultado a la vista.
    //
    // NO filtra ni busca nada él mismo: eso lo hace ExplorarService.
    // (Regla del enunciado: sin lógica de negocio en controladores.)
    //
    // Explorar NO exige iniciar sesión: cualquiera puede buscar.
    // Si hay sesión, el historial queda a nombre de esa persona.
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
        // GET /Explorar?Texto=Monteverde&Tipo=Video&Orden=recientes
        // ========================================================
        // [FromQuery] toma los valores de la URL y los mete
        // automáticamente en el objeto "filtro" (Model Binding).
        //
        // User.GetUserId() lee el UserId guardado en la cookie de
        // login (ClaimsPrincipalExtensions). Devuelve null si no
        // hay sesión iniciada.
        // ========================================================
        [HttpGet]
        public async Task<IActionResult> Index([FromQuery] ExplorarFiltro filtro)
        {
            var modelo = await _explorarService.BuscarAsync(filtro, User.GetUserId());
            return View(modelo);
        }
    }
}