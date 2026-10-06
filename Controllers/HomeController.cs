using System.Diagnostics;
using ExploraCostaRica.Data;
using ExploraCostaRica.Models;
using ExploraCostaRica.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ExploraCostaRica.Controllers
{
    public class HomeController : Controller
    {
        // Logger creado por ASP.NET Core
        private readonly ILogger<HomeController> _logger;

        // Contexto de nuestra base ExploraCostaRicaDB
        private readonly ExploraCostaRicaDbContext _context;


        // ====================================================
        // CONSTRUCTOR
        // ====================================================
        // ASP.NET Core nos entrega automáticamente:
        // - Logger
        // - DbContext
        //
        // gracias a lo que registramos en Program.cs.
        // ====================================================

        public HomeController(
            ILogger<HomeController> logger,
            ExploraCostaRicaDbContext context)
        {
            _logger = logger;
            _context = context;
        }


        // ====================================================
        // PÁGINA PRINCIPAL
        // ====================================================
        // Lee la vista vw_MediaCatalog directamente desde
        // SQL Server.
        //
        // Ya NO tenemos:
        //
        // new MediaItem
        // Volcán Arenal
        // Monteverde
        // Guanacaste
        //
        // escritos manualmente dentro del controlador.
        // ====================================================

        public async Task<IActionResult> Index()
        {
            var mediaItems = await _context
                .VwMediaCatalogs
                .AsNoTracking()
                .ToListAsync();

            return View(mediaItems);
        }


        // ====================================================
        // PRIVACY
        // ====================================================

        public IActionResult Privacy()
        {
            return View();
        }


        // ====================================================
        // ERROR
        // ====================================================

        [ResponseCache(
            Duration = 0,
            Location = ResponseCacheLocation.None,
            NoStore = true)]
        public IActionResult Error()
        {
            return View(
                new ErrorViewModel
                {
                    RequestId =
                        Activity.Current?.Id ??
                        HttpContext.TraceIdentifier
                }
            );
        }
    }
}