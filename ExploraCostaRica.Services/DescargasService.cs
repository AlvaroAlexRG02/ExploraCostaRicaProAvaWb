
using ExploraCostaRica.Data;
using ExploraCostaRica.Models.DTOs;
using ExploraCostaRica.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text.Encodings.Web;

namespace ExploraCostaRica.Services
{
    public class DescargasService : IDescargasService
    {
        private readonly ExploraCostaRicaDbContext _context;

        private const string CorreoUsuarioDemo =
            "demo@exploracostarica.local";

        private const long TamanoMaximo = 2 * 1024 * 1024;

        // Reutilizar opciones JSON para evitar crearlas
        // en cada solicitud.
        private static readonly JsonSerializerOptions OpcionesJson = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public DescargasService(
            ExploraCostaRicaDbContext context)
        {
            _context = context;
        }

        // ====================================================
        // USUARIO DEMO
        // ====================================================

        public async Task<User?> ObtenerUsuarioAsync()
        {
            return await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u =>
                    u.Email == CorreoUsuarioDemo &&
                    u.IsActive);
        }

        // ====================================================
        // CONTENIDOS DISPONIBLES
        // ====================================================

        public async Task<List<MediaItem>> ObtenerContenidosAsync()
        {
            return await _context.MediaItems
                .AsNoTracking()
                .Include(m => m.Category)
                .Include(m => m.Destination)
                .Where(m => m.IsActive)
                .OrderBy(m => m.Title)
                .ToListAsync();
        }

        // ====================================================
        // HISTORIAL DE DESCARGAS
        // ====================================================

        public async Task<List<Download>> ObtenerHistorialAsync(
            int usuarioId)
        {
            return await _context.Downloads
                .AsNoTracking()
                .Include(d => d.Media)
                .Where(d => d.UserId == usuarioId)
                .OrderByDescending(d => d.DownloadedAt)
                .ToListAsync();
        }

        // ====================================================
        // EXPORTAR JSON ESTANDARIZADO
        // ====================================================

        public async Task<string?> GenerarJsonAsync(long mediaId)
        {
            var contenido = await _context.MediaItems
                .AsNoTracking()
                .Include(m => m.Category)
                .Include(m => m.Destination)
                .Include(m => m.ContentType)
                .Include(m => m.Source)
                .FirstOrDefaultAsync(m =>
                    m.MediaId == mediaId &&
                    m.IsActive);

            if (contenido == null)
                return null;

            // DTO de intercambio entre aplicaciones.
            // No exportamos el MediaId interno de SQL Server.
            var dto = new MediaItemDto
            {
                SchemaVersion = "1.0",

                ExternalId = contenido.ExternalId,
                Title = contenido.Title,
                Description = contenido.Description,
                Author = contenido.Author,
                Url = contenido.Url,
                ImageUrl = contenido.ImageUrl,

                Category = contenido.Category?.Name,
                Destination = contenido.Destination?.Name,
                ContentType = contenido.ContentType?.Name,
                Source = contenido.Source?.Name,

                PublishedAt = contenido.PublishedAt
            };

            return JsonSerializer.Serialize(dto, OpcionesJson);
        }

        // ====================================================
        // REGISTRAR DESCARGA
        // ====================================================

        public async Task RegistrarDescargaAsync(
            int usuarioId,
            long mediaId,
            string nombreArchivo)
        {
            var descarga = new Download
            {
                UserId = usuarioId,
                MediaId = mediaId,
                FileFormat = "JSON",
                FileName = nombreArchivo,
                DownloadedAt = DateTime.Now
            };

            _context.Downloads.Add(descarga);
            await _context.SaveChangesAsync();
        }

        // ====================================================
        // OBTENER DESCARGA
        // ====================================================

        public async Task<Download?> ObtenerDescargaAsync(
            long descargaId,
            int usuarioId)
        {
            return await _context.Downloads
                .AsNoTracking()
                .FirstOrDefaultAsync(d =>
                    d.DownloadId == descargaId &&
                    d.UserId == usuarioId);
        }

        // ====================================================
        // ELIMINAR REGISTRO DEL HISTORIAL
        // ====================================================

        public async Task<bool> EliminarDescargaAsync(
            long descargaId,
            int usuarioId)
        {
            var descarga = await _context.Downloads
                .FirstOrDefaultAsync(d =>
                    d.DownloadId == descargaId &&
                    d.UserId == usuarioId);

            if (descarga == null)
                return false;

            _context.Downloads.Remove(descarga);
            await _context.SaveChangesAsync();

            return true;
        }

        // ====================================================
        // IMPORTAR JSON
        // ACEPTA EL NUEVO FORMATO Y EL ANTERIOR
        // ====================================================

        public async Task<ResultadoImportacionDto> ImportarJsonAsync(
            Stream archivo)
        {
            if (archivo == null || !archivo.CanRead)
            {
                return ResultadoImportacionDto.Error(
                    "No se pudo leer el archivo JSON.");
            }

            JsonImportado? datos;

            try
            {
                using var memoria = new MemoryStream();

                var buffer = new byte[81920];
                long total = 0;
                int leidos;

                while ((leidos = await archivo.ReadAsync(
                    buffer.AsMemory())) > 0)
                {
                    total += leidos;

                    if (total > TamanoMaximo)
                    {
                        return ResultadoImportacionDto.Error(
                            "El archivo supera el límite de 2 MB.");
                    }

                    await memoria.WriteAsync(
                        buffer.AsMemory(0, leidos));
                }

                if (total == 0)
                {
                    return ResultadoImportacionDto.Error(
                        "El archivo JSON está vacío.");
                }

                memoria.Position = 0;

                using var documento =
                    await JsonDocument.ParseAsync(memoria);

                if (documento.RootElement.ValueKind !=
                    JsonValueKind.Object)
                {
                    return ResultadoImportacionDto.Error(
                        "El JSON debe contener un objeto.");
                }

                var raiz = documento.RootElement;

                // Detectar si el archivo usa el formato nuevo
                // o el formato antiguo de Explora Costa Rica.
                bool formatoNuevo =
                    raiz.TryGetProperty(
                        "schemaVersion",
                        out _) ||
                    raiz.TryGetProperty(
                        "category",
                        out _) ||
                    raiz.TryGetProperty(
                        "destination",
                        out _) ||
                    raiz.TryGetProperty(
                        "contentType",
                        out _) ||
                    raiz.TryGetProperty(
                        "source",
                        out _);

                memoria.Position = 0;

                if (formatoNuevo)
                {
                    var dto = await JsonSerializer
                        .DeserializeAsync<MediaItemDto>(
                            memoria,
                            OpcionesJson);

                    if (dto == null)
                    {
                        return ResultadoImportacionDto.Error(
                            "No se pudo interpretar el archivo JSON.");
                    }

                    if (dto.SchemaVersion != "1.0")
                    {
                        return ResultadoImportacionDto.Error(
                            "La versión del formato JSON no es compatible.");
                    }

                    // Convertir al modelo que ya utiliza
                    // nuestro proceso de importación.
                    datos = new JsonImportado
                    {
                        ExternalId = dto.ExternalId,
                        Title = dto.Title,
                        Description = dto.Description,
                        Author = dto.Author,
                        Url = dto.Url,
                        ImageUrl = dto.ImageUrl,
                        Categoria = dto.Category,
                        Destino = dto.Destination,
                        TipoContenido = dto.ContentType,
                        Fuente = dto.Source,
                        PublishedAt = dto.PublishedAt
                    };
                }
                else
                {
                    datos = await JsonSerializer
                        .DeserializeAsync<JsonImportado>(
                            memoria,
                            OpcionesJson);
                }
            }
            catch (JsonException)
            {
                return ResultadoImportacionDto.Error(
                    "El archivo no contiene un JSON válido.");
            }
            catch (IOException)
            {
                return ResultadoImportacionDto.Error(
                    "Ocurrió un error al leer el archivo.");
            }

            // =================================================
            // VALIDAR INFORMACIÓN
            // =================================================

            if (datos == null ||
                string.IsNullOrWhiteSpace(datos.Title) ||
                datos.Title.Trim().Length > 300)
            {
                return ResultadoImportacionDto.Error(
                    "El JSON debe contener un título válido de máximo 300 caracteres.");
            }

            if ((datos.ExternalId?.Length ?? 0) > 250 ||
                (datos.Author?.Length ?? 0) > 200 ||
                (datos.Url?.Length ?? 0) > 1500 ||
                (datos.ImageUrl?.Length ?? 0) > 1500 ||
                (datos.Fuente?.Length ?? 0) > 150 ||
                (datos.TipoContenido?.Length ?? 0) > 80)
            {
                return ResultadoImportacionDto.Error(
                    "Algunos campos superan los límites permitidos.");
            }

            if ((datos.Categoria?.Trim().Length ?? 0) > 100 ||
                (datos.Destino?.Trim().Length ?? 0) > 150)
            {
                return ResultadoImportacionDto.Error(
                    "La categoría o el destino supera el límite permitido.");
            }

            var fuenteNombre =
                string.IsNullOrWhiteSpace(datos.Fuente)
                    ? "Archivo JSON importado"
                    : datos.Fuente.Trim();

            var tipoNombre =
                string.IsNullOrWhiteSpace(datos.TipoContenido)
                    ? "Importado"
                    : datos.TipoContenido.Trim();

            var externalId =
                string.IsNullOrWhiteSpace(datos.ExternalId)
                    ? null
                    : datos.ExternalId.Trim();

            // =================================================
            // TRANSACCIÓN SQL SERVER
            // =================================================

            await using var transaccion =
                await _context.Database.BeginTransactionAsync();

            try
            {
                // TIPO DE CONTENIDO
                var tipo = await _context.ContentTypes
                    .FirstOrDefaultAsync(t =>
                        t.Name == tipoNombre);

                if (tipo == null)
                {
                    tipo = new ExploraCostaRica.Models.Entities.ContentType
                    {
                        Name = tipoNombre,
                        Description =
                            "Contenido disponible para importar",
                        IsActive = true
                    };

                    _context.ContentTypes.Add(tipo);
                    await _context.SaveChangesAsync();
                }

                // FUENTE
                var fuente = await _context.ApiSources
                    .FirstOrDefaultAsync(s =>
                        s.Name == fuenteNombre);

                if (fuente == null)
                {
                    fuente = new ApiSource
                    {
                        Name = fuenteNombre,
                        Description =
                            "Fuente registrada mediante importación JSON",
                        ContentTypeId = tipo.ContentTypeId,
                        RequiresApiKey = false,
                        IsActive = true,
                        CreatedAt = DateTime.Now
                    };

                    _context.ApiSources.Add(fuente);
                    await _context.SaveChangesAsync();
                }

                // VERIFICACIÓN DE DUPLICADOS
                if (externalId != null)
                {
                    bool duplicado = await _context.MediaItems
                        .AnyAsync(m =>
                            m.SourceId == fuente.SourceId &&
                            m.ExternalId == externalId);

                    if (duplicado)
                    {
                        await transaccion.RollbackAsync();
                        _context.ChangeTracker.Clear();

                        return ResultadoImportacionDto.Error(
                            "Este elemento ya existe para la misma fuente.");
                    }
                }

                // CATEGORÍA
                Category? categoria = null;

                if (!string.IsNullOrWhiteSpace(datos.Categoria))
                {
                    var nombre = datos.Categoria.Trim();

                    categoria = await _context.Categories
                        .FirstOrDefaultAsync(c =>
                            c.Name == nombre);

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

                // DESTINO
                Destination? destino = null;

                if (!string.IsNullOrWhiteSpace(datos.Destino))
                {
                    var nombre = datos.Destino.Trim();

                    destino = await _context.Destinations
                        .FirstOrDefaultAsync(d =>
                            d.Name == nombre);

                    if (destino == null)
                    {
                        destino = new Destination
                        {
                            Name = nombre,
                            Province = "Sin especificar",
                            IsActive = true,
                            CreatedAt = DateTime.Now
                        };

                        _context.Destinations.Add(destino);
                        await _context.SaveChangesAsync();
                    }
                }

                // CONTENIDO
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

                return ResultadoImportacionDto.Exito(
                    $"El contenido '{contenido.Title}' se importó correctamente.",
                    contenido.MediaId);
            }
            catch (DbUpdateException)
            {
                await transaccion.RollbackAsync();
                _context.ChangeTracker.Clear();

                return ResultadoImportacionDto.Error(
                    "No se pudo guardar el contenido. Revisá si ya existe o si los datos son compatibles.");
            }
        }
    }
}
