
using ExploraCostaRica.Data;
using ExploraCostaRica.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;

namespace ExploraCostaRica.Controllers;

public class DescargasController : Controller
{
    private readonly ExploraCostaRicaDbContext _context;

    public DescargasController(ExploraCostaRicaDbContext context)
    {
        _context = context;
    }

    private async Task<User?> ObtenerUsuarioAsync()
    {
        return await _context.Users
            .Where(u => u.IsActive)
            .OrderBy(u => u.UserId)
            .FirstOrDefaultAsync();
    }

    public async Task<IActionResult> Index()
    {
        var usuario = await ObtenerUsuarioAsync();

        var modelo = new DescargasViewModel
        {
            UsuarioDemoId = usuario?.UserId,
            NombreUsuario = usuario?.Name ?? "Sin usuario demo",

            Contenidos = await _context.MediaItems
                .AsNoTracking()
                .Include(m => m.Category)
                .Include(m => m.Destination)
                .Where(m => m.IsActive)
                .OrderBy(m => m.Title)
                .ToListAsync()
        };

        if (usuario != null)
        {
            modelo.Historial = await _context.Downloads
                .AsNoTracking()
                .Include(d => d.Media)
                .Where(d => d.UserId == usuario.UserId)
                .OrderByDescending(d => d.DownloadedAt)
                .ToListAsync();
        }

        return View(modelo);
    }

    private async Task<string?> GenerarJsonAsync(long mediaId)
    {
        var contenido = await _context.MediaItems
            .AsNoTracking()
            .Include(m => m.Category)
            .Include(m => m.Destination)
            .Include(m => m.ContentType)
            .Include(m => m.Source)
            .FirstOrDefaultAsync(m => m.MediaId == mediaId);

        if (contenido == null)
            return null;

        var datos = new
        {
            contenido.MediaId,
            contenido.ExternalId,
            contenido.Title,
            contenido.Description,
            contenido.Author,
            contenido.Url,
            contenido.ImageUrl,
            Categoria = contenido.Category?.Name,
            Destino = contenido.Destination?.Name,
            TipoContenido = contenido.ContentType?.Name,
            Fuente = contenido.Source?.Name,
            contenido.PublishedAt
        };

        return JsonSerializer.Serialize(
            datos,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DescargarJson(long mediaId)
    {
        var usuario = await ObtenerUsuarioAsync();

        if (usuario == null)
        {
            TempData["Error"] = "No existe un usuario activo.";
            return RedirectToAction(nameof(Index));
        }

        var contenido = await _context.MediaItems
            .AsNoTracking()
            .FirstOrDefaultAsync(m =>
                m.MediaId == mediaId && m.IsActive);

        if (contenido == null)
            return NotFound();

        var json = await GenerarJsonAsync(mediaId);

        if (json == null)
            return NotFound();

        var nombre = $"ExploraCostaRica_{mediaId}.json";

        _context.Downloads.Add(new Download
        {
            UserId = usuario.UserId,
            MediaId = mediaId,
            FileFormat = "JSON",
            FileName = nombre,
            DownloadedAt = DateTime.Now
        });

        await _context.SaveChangesAsync();

        return File(
            Encoding.UTF8.GetBytes(json),
            "application/json",
            nombre);
    }

    [HttpGet]
    public async Task<IActionResult> Abrir(long id)
    {
        var usuario = await ObtenerUsuarioAsync();

        if (usuario == null)
            return NotFound();

        var descarga = await _context.Downloads
            .AsNoTracking()
            .FirstOrDefaultAsync(d =>
                d.DownloadId == id &&
                d.UserId == usuario.UserId);

        if (descarga == null)
            return NotFound();

        var json = await GenerarJsonAsync(descarga.MediaId);

        if (json == null)
            return NotFound();

        return Content(
            json,
            "application/json",
            Encoding.UTF8);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(long id)
    {
        var usuario = await ObtenerUsuarioAsync();

        if (usuario == null)
            return NotFound();

        var descarga = await _context.Downloads
            .FirstOrDefaultAsync(d =>
                d.DownloadId == id &&
                d.UserId == usuario.UserId);

        if (descarga == null)
            return NotFound();

        _context.Downloads.Remove(descarga);

        await _context.SaveChangesAsync();

        TempData["Exito"] =
            "Registro eliminado del historial.";

        return RedirectToAction(nameof(Index));
    }
}

public class DescargasViewModel
{
    public int? UsuarioDemoId { get; set; }

    public string NombreUsuario { get; set; } = "";

    public List<MediaItem> Contenidos { get; set; } = new();

    public List<Download> Historial { get; set; } = new();
}
