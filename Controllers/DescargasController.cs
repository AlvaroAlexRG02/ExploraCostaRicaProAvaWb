
using ExploraCostaRica.Data;
using ExploraCostaRica.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ExploraCostaRica.Controllers;

public class DescargasController : Controller
{
    private readonly ExploraCostaRicaDbContext _context;

    public DescargasController(ExploraCostaRicaDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var usuario = await _context.Users
            .AsNoTracking()
            .Where(u => u.IsActive)
            .OrderBy(u => u.UserId)
            .FirstOrDefaultAsync();

        var modelo = new DescargasViewModel
        {
            UsuarioDemoId = usuario?.UserId,
            NombreUsuario = usuario?.Name ?? "Sin usuario demo"
        };

        modelo.Contenidos = await _context.MediaItems
            .AsNoTracking()
            .Include(m => m.Destination)
            .Include(m => m.Category)
            .Where(m => m.IsActive)
            .OrderBy(m => m.Title)
            .ToListAsync();

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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DescargarJson(long mediaId)
    {
        var usuario = await _context.Users
            .Where(u => u.IsActive)
            .OrderBy(u => u.UserId)
            .FirstOrDefaultAsync();

        if (usuario == null)
        {
            TempData["Error"] =
                "No existe un usuario activo para registrar la descarga.";
            return RedirectToAction(nameof(Index));
        }

        var contenido = await _context.MediaItems
            .AsNoTracking()
            .Include(m => m.Category)
            .Include(m => m.Destination)
            .Include(m => m.ContentType)
            .Include(m => m.Source)
            .FirstOrDefaultAsync(m => m.MediaId == mediaId && m.IsActive);

        if (contenido == null)
            return NotFound("Contenido no encontrado.");

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
            TipoContenido = contenido.ContentType.Name,
            Fuente = contenido.Source.Name,
            contenido.PublishedAt
        };

        var json = JsonSerializer.Serialize(
            datos,
            new JsonSerializerOptions
            {
                WriteIndented = true
            });

        var nombreArchivo = $"ExploraCostaRica_{mediaId}.json";

        var registro = new Download
        {
            UserId = usuario.UserId,
            MediaId = contenido.MediaId,
            FileFormat = "JSON",
            FileName = nombreArchivo,
            DownloadedAt = DateTime.Now
        };

        _context.Downloads.Add(registro);
        await _context.SaveChangesAsync();

        var bytes = System.Text.Encoding.UTF8.GetBytes(json);

        return File(
            bytes,
            "application/json",
            nombreArchivo
        );
    }
}

public class DescargasViewModel
{
    public int? UsuarioDemoId { get; set; }

    public string NombreUsuario { get; set; } = "";

    public List<MediaItem> Contenidos { get; set; } = new();

    public List<Download> Historial { get; set; } = new();
}
