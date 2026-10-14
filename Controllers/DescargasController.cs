
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

    private const long TamanoMaximo = 2 * 1024 * 1024;

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

        TempData["Exito"] = "Registro eliminado del historial.";

        return RedirectToAction(nameof(Index));
    }

    // IMPORTAR UN ARCHIVO JSON
    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(2 * 1024 * 1024 + 65536)]
    public async Task<IActionResult> SubirJson(IFormFile? archivo)
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
            TempData["Error"] = "El archivo supera el límite de 2 MB.";
            return RedirectToAction(nameof(Index));
        }

        JsonImportado? datos;

        try
        {
            using var stream = archivo.OpenReadStream();

            datos = await JsonSerializer.DeserializeAsync<JsonImportado>(
                stream,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
        }
        catch (JsonException)
        {
            TempData["Error"] = "El archivo no contiene un JSON válido.";
            return RedirectToAction(nameof(Index));
        }
        catch (DecoderFallbackException)
        {
            TempData["Error"] = "La codificación del archivo no es válida.";
            return RedirectToAction(nameof(Index));
        }

        if (datos == null ||
            string.IsNullOrWhiteSpace(datos.Title) ||
            datos.Title.Length > 300)
        {
            TempData["Error"] =
                "El JSON debe contener un Title válido de máximo 300 caracteres.";

            return RedirectToAction(nameof(Index));
        }

        if ((datos.ExternalId?.Length ?? 0) > 250 ||
            (datos.Author?.Length ?? 0) > 200 ||
            (datos.Url?.Length ?? 0) > 1500 ||
            (datos.ImageUrl?.Length ?? 0) > 1500 ||
            (datos.Fuente?.Length ?? 0) > 150 ||
            (datos.TipoContenido?.Length ?? 0) > 80)
        {
            TempData["Error"] =
                "El archivo contiene campos que exceden los límites permitidos.";

            return RedirectToAction(nameof(Index));
        }

        // La identidad del MediaId original no se reutiliza.
        // SQL Server genera una nueva identidad local.
        var fuenteNombre = string.IsNullOrWhiteSpace(datos.Fuente)
            ? "Archivo JSON importado"
            : datos.Fuente.Trim();

        var tipoNombre = string.IsNullOrWhiteSpace(datos.TipoContenido)
            ? "Importado"
            : datos.TipoContenido.Trim();

        var externalId = string.IsNullOrWhiteSpace(datos.ExternalId)
            ? null
            : datos.ExternalId.Trim();

        await using var transaccion =
            await _context.Database.BeginTransactionAsync();

        try
        {
            var tipo = await _context.ContentTypes
                .FirstOrDefaultAsync(t => t.Name == tipoNombre);

            if (tipo == null)
            {
                tipo = new ExploraCostaRica.Models.Entities.ContentType
                {
                    Name = tipoNombre,
                    Description = "Contenido disponible para importar",
                    IsActive = true
                };

                _context.ContentTypes.Add(tipo);
                await _context.SaveChangesAsync();
            }

            var fuente = await _context.ApiSources
                .FirstOrDefaultAsync(s => s.Name == fuenteNombre);

            if (fuente == null)
            {
                fuente = new ApiSource
                {
                    Name = fuenteNombre,
                    Description = "Fuente registrada mediante importación JSON",
                    ContentTypeId = tipo.ContentTypeId,
                    RequiresApiKey = false,
                    IsActive = true,
                    CreatedAt = DateTime.Now
                };

                _context.ApiSources.Add(fuente);
                await _context.SaveChangesAsync();
            }

            if (externalId != null)
            {
                bool duplicado = await _context.MediaItems
                    .AnyAsync(m =>
                        m.SourceId == fuente.SourceId &&
                        m.ExternalId == externalId);

                if (duplicado)
                {
                    await transaccion.RollbackAsync();

                    TempData["Error"] =
                        "Este elemento ya existe para la misma fuente.";

                    return RedirectToAction(nameof(Index));
                }
            }

            Category? categoria = null;

            if (!string.IsNullOrWhiteSpace(datos.Categoria))
            {
                var nombre = datos.Categoria.Trim();

                if (nombre.Length > 100)
                {
                    await transaccion.RollbackAsync();
                    TempData["Error"] = "La categoría supera 100 caracteres.";
                    return RedirectToAction(nameof(Index));
                }

                categoria = await _context.Categories
                    .FirstOrDefaultAsync(c => c.Name == nombre);

                if (categoria == null)
                {
                    categoria = new Category
                    {
                        Name = nombre,
                        IsActive = true
                    };

                    _context.Categories.Add(categoria);
                    await _context.SaveChangesAsync();
                }
            }

            Destination? destino = null;

            if (!string.IsNullOrWhiteSpace(datos.Destino))
            {
                var nombre = datos.Destino.Trim();

                if (nombre.Length > 150)
                {
                    await transaccion.RollbackAsync();
                    TempData["Error"] = "El destino supera 150 caracteres.";
                    return RedirectToAction(nameof(Index));
                }

                destino = await _context.Destinations
                    .FirstOrDefaultAsync(d => d.Name == nombre);

                if (destino == null)
                {
                    destino = new Destination
                    {
                        Name = nombre,
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    };

                    _context.Destinations.Add(destino);
                    await _context.SaveChangesAsync();
                }
            }

            var contenido = new MediaItem
            {
                ExternalId = externalId,
                SourceId = fuente.SourceId,
                ContentTypeId = tipo.ContentTypeId,
                CategoryId = categoria?.CategoryId,
                DestinationId = destino?.DestinationId,
                Title = datos.Title.Trim(),
                Description = datos.Description,
                Author = datos.Author,
                Url = datos.Url,
                ImageUrl = datos.ImageUrl,
                PublishedAt = datos.PublishedAt,
                SavedAt = DateTime.Now,
                IsActive = true
            };

            _context.MediaItems.Add(contenido);
            await _context.SaveChangesAsync();

            await transaccion.CommitAsync();

            TempData["Exito"] =
                $"El contenido '{contenido.Title}' se importó correctamente.";
        }
        catch (DbUpdateException)
        {
            await transaccion.RollbackAsync();

            TempData["Error"] =
                "No se pudo guardar el contenido. Revisá si ya existe o si los datos son compatibles.";
        }

        return RedirectToAction(nameof(Index));
    }
}

// MODELO UTILIZADO POR LA VISTA
public class DescargasViewModel
{
    public int? UsuarioDemoId { get; set; }

    public string NombreUsuario { get; set; } = "";

    public List<MediaItem> Contenidos { get; set; } = new();

    public List<Download> Historial { get; set; } = new();
}

// MODELO DEL ARCHIVO JSON IMPORTADO
public class JsonImportado
{
    public string? ExternalId { get; set; }

    public string? Title { get; set; }

    public string? Description { get; set; }

    public string? Author { get; set; }

    public string? Url { get; set; }

    public string? ImageUrl { get; set; }

    public string? Categoria { get; set; }

    public string? Destino { get; set; }

    public string? TipoContenido { get; set; }

    public string? Fuente { get; set; }

    public DateTime? PublishedAt { get; set; }
}
