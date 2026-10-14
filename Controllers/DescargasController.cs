
using ExploraCostaRica.Models.ViewModels;
using ExploraCostaRica.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace ExploraCostaRica.Controllers;

public class DescargasController : Controller
{
    private readonly IDescargasService _descargasService;

    private const long TamanoMaximo = 2 * 1024 * 1024;

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
        var usuario =
            await _descargasService.ObtenerUsuarioAsync();

        var modelo = new DescargasViewModel
        {
            UsuarioDemoId = usuario?.UserId,
            NombreUsuario = usuario?.Name ?? "Sin usuario demo",

            Contenidos =
                await _descargasService.ObtenerContenidosAsync()
        };

        if (usuario != null)
        {
            modelo.Historial =
                await _descargasService.ObtenerHistorialAsync(
                    usuario.UserId);
        }

        return View(modelo);
    }

    // ====================================================
    // DESCARGAR JSON
    // ====================================================

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DescargarJson(long mediaId)
    {
        var usuario =
            await _descargasService.ObtenerUsuarioAsync();

        if (usuario == null)
        {
            TempData["Error"] =
                "No existe un usuario demo activo.";

            return RedirectToAction(nameof(Index));
        }

        var json =
            await _descargasService.GenerarJsonAsync(mediaId);

        if (json == null)
            return NotFound();

        var nombre =
            $"ExploraCostaRica_{mediaId}.json";

        await _descargasService.RegistrarDescargaAsync(
            usuario.UserId,
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
        var usuario =
            await _descargasService.ObtenerUsuarioAsync();

        if (usuario == null)
            return NotFound();

        var descarga =
            await _descargasService.ObtenerDescargaAsync(
                id,
                usuario.UserId);

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
        var usuario =
            await _descargasService.ObtenerUsuarioAsync();

        if (usuario == null)
            return NotFound();

        var eliminado =
            await _descargasService.EliminarDescargaAsync(
                id,
                usuario.UserId);

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
