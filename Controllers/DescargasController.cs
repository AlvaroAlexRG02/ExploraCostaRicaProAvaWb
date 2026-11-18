
using ExploraCostaRica.Extensions;
using ExploraCostaRica.Models.ViewModels;
using ExploraCostaRica.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text;

namespace ExploraCostaRica.Controllers;

// Las descargas son personales: hay que iniciar sesión.
[Authorize]
public class DescargasController : Controller
{
    private readonly IDescargasService _descargasService;

    private const long TamanoMaximo = 2 * 1024 * 1024;

    // Usuario autenticado, tomado de la cookie de sesión.
    private int UserId => User.GetUserId()
        ?? throw new InvalidOperationException(
            "La sesión no tiene un UserId válido.");

    public DescargasController(
        IDescargasService descargasService)
    {
        _descargasService = descargasService;
    }

    // ====================================================
    // PANTALLA PRINCIPAL DE DESCARGAS
    // ====================================================

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var modelo = new DescargasViewModel
        {
            UsuarioId = UserId,
            NombreUsuario = User.FindFirstValue(ClaimTypes.Name) ?? "",

            Contenidos =
                await _descargasService.ObtenerContenidosAsync(),

            Historial =
                await _descargasService.ObtenerHistorialAsync(UserId)
        };

        return View(modelo);
    }

    // ====================================================
    // DESCARGAR JSON
    // ====================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DescargarJson(long mediaId)
    {
        var json =
            await _descargasService.GenerarJsonAsync(mediaId);

        if (json == null)
            return NotFound();

        var nombre =
            $"ExploraCostaRica_{mediaId}.json";

        await _descargasService.RegistrarDescargaAsync(
            UserId,
            mediaId,
            nombre);

        return File(
            Encoding.UTF8.GetBytes(json),
            "application/json",
            nombre);
    }

    // ====================================================
    // ABRIR DESCARGA DEL HISTORIAL
    // ====================================================

    [HttpGet]
    public async Task<IActionResult> Abrir(long id)
    {
        var descarga =
            await _descargasService.ObtenerDescargaAsync(
                id,
                UserId);

        if (descarga == null)
            return NotFound();

        var json =
            await _descargasService.GenerarJsonAsync(
                descarga.MediaId);

        if (json == null)
            return NotFound();

        return Content(
            json,
            "application/json",
            Encoding.UTF8);
    }

    // ====================================================
    // ELIMINAR REGISTRO DEL HISTORIAL
    // ====================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(long id)
    {
        var eliminado =
            await _descargasService.EliminarDescargaAsync(
                id,
                UserId);


        if (!eliminado)
            return NotFound();

        TempData["Exito"] =
            "Registro eliminado del historial.";

        return RedirectToAction(nameof(Index));
    }

    // ====================================================
    // SUBIR E IMPORTAR ARCHIVO JSON
    // ====================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(2 * 1024 * 1024 + 65536)]
    public async Task<IActionResult> SubirJson(IFormFile? archivo)
    {
        if (archivo == null || archivo.Length == 0)
        {
            TempData["Error"] =
                "Seleccioná un archivo JSON.";

            return RedirectToAction(nameof(Index));
        }

        if (!string.Equals(
            Path.GetExtension(archivo.FileName),
            ".json",
            StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] =
                "Solo se permiten archivos .json.";

            return RedirectToAction(nameof(Index));
        }

        if (archivo.Length > TamanoMaximo)
        {
            TempData["Error"] =
                "El archivo supera el límite de 2 MB.";

            return RedirectToAction(nameof(Index));
        }

        using var stream = archivo.OpenReadStream();

        var resultado =
            await _descargasService.ImportarJsonAsync(stream);

        if (resultado.Exitoso)
        {
            TempData["Exito"] = resultado.Mensaje;
        }
        else
        {
            TempData["Error"] = resultado.Mensaje;
        }

        return RedirectToAction(nameof(Index));
    }
}
