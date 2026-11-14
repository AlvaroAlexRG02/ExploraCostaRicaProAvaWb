using ExploraCostaRica.Models.DTOs;
using ExploraCostaRica.Models.Exchange;
using ExploraCostaRica.Models.ViewModels;
using ExploraCostaRica.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace ExploraCostaRica.Controllers;

[ApiExplorerSettings(IgnoreApi = true)]
public class DashboardController : Controller
{
    private const long TamanoMaximo = 10 * 1024 * 1024;


    private const int MaxErroresVisibles = 6;

    private readonly IDashboardService _dashboard;
    private readonly IDashboardSyncService _sync;

    public DashboardController(
        IDashboardService dashboard,
        IDashboardSyncService sync)
    {
        _dashboard = dashboard;
        _sync = sync;
    }


    [HttpGet]
    public async Task<IActionResult> Index(
        [FromQuery] DashboardFilter filtro,
        CancellationToken ct)
    {
        try
        {
            return View(await _dashboard.ObtenerAsync(filtro, ct));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {

            ViewBag.ErrorBaseDatos = ex.GetBaseException().Message;

            return View(new DashboardViewModel { Filter = filtro });
        }
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(10 * 1024 * 1024 + 65536)]
    public async Task<IActionResult> Importar(
        IFormFile? archivo,
        CancellationToken ct)
    {
        if (archivo == null || archivo.Length == 0)
        {
            TempData["Error"] = "Seleccioná un archivo JSON.";
            return RedirectToAction(nameof(Index));
        }

        if (!string.Equals(
            Path.GetExtension(archivo.FileName),
            ".json",
            StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "Solo se permiten archivos .json.";
            return RedirectToAction(nameof(Index));
        }

        if (archivo.Length > TamanoMaximo)
        {
            TempData["Error"] = "El archivo supera el límite de 10 MB.";
            return RedirectToAction(nameof(Index));
        }

        await using var stream = archivo.OpenReadStream();

        var resultado = await _dashboard.ImportarAsync(stream, ct);

        Avisar(resultado);

        return RedirectToAction(nameof(Index));
    }



    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Sincronizar(CancellationToken ct)
    {
        var resultado = await _sync.SincronizarAsync(ct);

        Avisar(resultado);

        return RedirectToAction(nameof(Index));
    }



    [HttpGet]
    public async Task<IActionResult> Exportar(
        [FromQuery] DashboardFilter filtro,
        CancellationToken ct)
    {
        var export = await _dashboard.ExportarAsync(filtro, ct);

        if (export.Sources.Count == 0)
        {
            TempData["Error"] = "No hay items para exportar con esos filtros.";
            return RedirectToAction(nameof(Index));
        }

        return Archivo(
            export,
            $"dashboard-export-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json");
    }



    [HttpGet]
    public async Task<IActionResult> ExportarItem(int id, CancellationToken ct)
    {
        var export = await _dashboard.ExportarItemAsync(id, ct);

        if (export == null || export.Sources.Count == 0)
        {
            return NotFound();
        }

        return Archivo(export, $"dashboard-item-{id}.json");
    }



    [HttpGet]
    public async Task<IActionResult> Item(int id, CancellationToken ct)
    {
        var json = await _dashboard.ObtenerJsonItemAsync(id, ct);

        if (json == null)
        {
            return NotFound();
        }

        return Content(json, "application/json", Encoding.UTF8);
    }


    private FileContentResult Archivo(DashboardExport export, string nombre)
    {
        return File(
            new UTF8Encoding(false).GetBytes(export.ToJson()),
            "application/json",
            nombre);
    }

    private void Avisar(DashboardImportResult resultado)
    {
        if (resultado.Success)
        {
            TempData["Exito"] = resultado.Message;
        }
        else
        {
            TempData["Error"] = resultado.Message;
        }

        if (resultado.Errors.Count > 0)
        {
            var visibles = resultado.Errors
                .Take(MaxErroresVisibles)
                .Select(e => e.Length > 220 ? e[..220] + "…" : e)
                .ToList();

            if (resultado.Errors.Count > MaxErroresVisibles)
            {
                visibles.Add(
                    $"… y {resultado.Errors.Count - MaxErroresVisibles} más.");
            }

            TempData["Detalle"] = string.Join("\n", visibles);
        }

        if (resultado.Notes.Count > 0)
        {
            TempData["Notas"] = string.Join("\n", resultado.Notes);
        }
    }
}
