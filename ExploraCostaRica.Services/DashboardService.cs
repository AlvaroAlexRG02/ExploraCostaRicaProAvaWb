using System.Text;
using System.Text.Json;
using ExploraCostaRica.Data;
using ExploraCostaRica.Models.DTOs;
using ExploraCostaRica.Models.Entities;
using ExploraCostaRica.Models.Exchange;
using ExploraCostaRica.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace ExploraCostaRica.Services
{

    public class DashboardService : IDashboardService
    {
        private const long TamanoMaximo = 10 * 1024 * 1024;

        private static readonly string[] Ordenes = { "recientes", "antiguos", "titulo" };


        private static readonly JsonSerializerOptions OpcionesCompactas =
            new(DashboardExport.JsonOptions) { WriteIndented = false };

        private readonly ExploraCostaRicaDbContext _context;
        private readonly IConfiguration _configuration;

        public DashboardService(
            ExploraCostaRicaDbContext context,
            IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }



        public async Task<DashboardViewModel> ObtenerAsync(
            DashboardFilter filtro,
            CancellationToken ct = default)
        {
            filtro = Normalizar(filtro);

            var consulta = Aplicar(
                _context.DashboardSourceItems.AsNoTracking(), filtro);

            int total = await consulta.CountAsync(ct);

            int paginas = Math.Max(
                1, (int)Math.Ceiling(total / (double)DashboardFilter.PageSize));

            filtro.Page = Math.Clamp(filtro.Page, 1, paginas);

            var filas = await Ordenar(consulta, filtro.Sort)
                .Skip((filtro.Page - 1) * DashboardFilter.PageSize)
                .Take(DashboardFilter.PageSize)
                .Select(i => new
                {
                    i.Id,
                    SourceKey = i.Source.SourceKey,
                    SourceName = i.Source.Name,
                    i.ItemKey,
                    i.Title,
                    i.MediaType,
                    Category = i.Category ?? i.Source.Category,
                    i.CreatedAt,
                    i.Json
                })
                .ToListAsync(ct);

            var fuentes = await _context.DashboardSources
                .AsNoTracking()
                .OrderBy(s => s.Name)
                .Select(s => new DashboardSourceSummary
                {
                    Key = s.SourceKey,
                    Name = s.Name,
                    Category = s.Category,
                    Url = s.Url,
                    Count = s.Items.Count
                })
                .ToListAsync(ct);

            foreach (var fuente in fuentes)
            {
                fuente.Url = UrlSegura(fuente.Url);
            }

            var porTipo = await _context.DashboardSourceItems
                .AsNoTracking()
                .GroupBy(i => i.MediaType)
                .Select(g => new { Tipo = g.Key, Cantidad = g.Count() })
                .ToListAsync(ct);

            var modelo = new DashboardViewModel
            {
                Filter = filtro,
                Total = total,
                TotalPages = paginas,
                Sources = fuentes,
                TotalSources = fuentes.Count,
                TotalItems = fuentes.Sum(f => f.Count),
                ByMediaType = porTipo.ToDictionary(x => x.Tipo, x => x.Cantidad)
            };

            foreach (var fila in filas)
            {
                var tarjeta = new DashboardItemCard
                {
                    Id = fila.Id,
                    SourceKey = fila.SourceKey,
                    SourceName = fila.SourceName,
                    ItemKey = fila.ItemKey,
                    Title = fila.Title,
                    MediaType = fila.MediaType,
                    Category = fila.Category,
                    CreatedAt = EnUtc(fila.CreatedAt)
                };


                var item = LeerItem(fila.Json);

                if (item != null)
                {

                    tarjeta.Url = UrlSegura(item.Url);
                    tarjeta.Thumbnail = UrlSegura(item.Thumbnail);
                    tarjeta.Description = item.Description;
                }

                modelo.Items.Add(tarjeta);
            }

            return modelo;
        }



        public async Task<DashboardImportResult> ImportarAsync(
            Stream archivo,
            CancellationToken ct = default)
        {
            if (archivo == null || !archivo.CanRead)
            {
                return DashboardImportResult.Fail(
                    "No se pudo leer el archivo JSON.");
            }


            byte[] bytes;

            try
            {
                using var memoria = new MemoryStream();
                var buffer = new byte[81920];
                long total = 0;
                int leidos;

                while ((leidos = await archivo.ReadAsync(buffer.AsMemory(), ct)) > 0)
                {
                    total += leidos;

                    if (total > TamanoMaximo)
                    {
                        return DashboardImportResult.Fail(
                            "El archivo supera el límite de 10 MB.");
                    }

                    memoria.Write(buffer, 0, leidos);
                }

                if (total == 0)
                {
                    return DashboardImportResult.Fail("El archivo está vacío.");
                }

                bytes = memoria.ToArray();
            }
            catch (IOException)
            {
                return DashboardImportResult.Fail(
                    "Ocurrió un error al leer el archivo.");
            }

            string texto;

            try
            {

                texto = new UTF8Encoding(false, true).GetString(bytes)
                    .TrimStart('﻿');
            }
            catch (DecoderFallbackException)
            {
                return DashboardImportResult.Fail(
                    "El archivo no está codificado en UTF-8 válido.");
            }

            JsonDocument documento;

            try
            {
                documento = JsonDocument.Parse(texto);
            }
            catch (JsonException ex)
            {
                var donde = ex.LineNumber.HasValue
                    ? $" (línea {ex.LineNumber + 1}, posición {ex.BytePositionInLine + 1})"
                    : string.Empty;

                return DashboardImportResult.Fail(
                    $"El archivo no contiene un JSON válido{donde}.");
            }

            DashboardExport? export;

            using (documento)
            {
                var raiz = documento.RootElement;


                if (raiz.ValueKind == JsonValueKind.Object &&
                    raiz.TryGetProperty("version", out var version) &&
                    version.ValueKind == JsonValueKind.String &&
                    version.GetString() != DashboardExport.CurrentVersion)
                {
                    return DashboardImportResult.Fail(
                        $"La versión del archivo ('{version.GetString()}') no es compatible. " +
                        $"Este dashboard importa la versión {DashboardExport.CurrentVersion}; " +
                        "pídale al otro grupo que exporte con ese formato.");
                }


                var problemas = DashboardExportValidator.Validate(raiz);

                if (problemas.Count > 0)
                {
                    return DashboardImportResult.Fail(
                        "El archivo no cumple el formato común v1.0, así que no se cargó nada.",
                        problemas.Select(p => p.ToString()));
                }


                try
                {
                    export = JsonSerializer.Deserialize<DashboardExport>(
                        texto, DashboardExport.JsonOptions);
                }
                catch (JsonException ex)
                {
                    return DashboardImportResult.Fail(
                        "No se pudo interpretar el archivo.",
                        new[] { ex.Message });
                }
            }

            if (export == null)
            {
                return DashboardImportResult.Fail(
                    "No se pudo interpretar el archivo.");
            }


            int credenciales = 0;

            foreach (var fuente in export.Sources)
            {
                DashboardSecretScrubber.ScrubSource(fuente, ref credenciales);
            }


            var resultado = await GuardarAsync(export, ct);

            resultado.CredentialsRemoved = credenciales;

            if (resultado.Success && credenciales > 0)
            {
                resultado.Message +=
                    $" Se omitieron {credenciales} valores que parecían credenciales (API keys o tokens).";
            }

            return resultado;
        }

        // ====================================================
        // GUARDAR (insertar o reemplazar)
        // ====================================================

        public async Task<DashboardImportResult> GuardarAsync(
            DashboardExport export,
            CancellationToken ct = default)
        {
            var ahora = DateTime.UtcNow;
            int agregados = 0;
            int reemplazados = 0;

            // Una fuente puede venir repetida en el archivo: se unen por id.
            var grupos = export.Sources
                .GroupBy(s => s.Id, StringComparer.Ordinal)
                .ToList();

            var claves = grupos.Select(g => g.Key).ToList();

            try
            {
                await using var transaccion =
                    await _context.Database.BeginTransactionAsync(ct);

                // --- Fuentes: crear las nuevas, actualizar las existentes ---
                var fuentes = await _context.DashboardSources
                    .Where(s => claves.Contains(s.SourceKey))
                    .ToDictionaryAsync(s => s.SourceKey, StringComparer.Ordinal, ct);

                foreach (var grupo in grupos)
                {
                    var datos = grupo.Last();

                    if (!fuentes.TryGetValue(grupo.Key, out var fuente))
                    {
                        fuente = new DashboardSource { SourceKey = grupo.Key };
                        _context.DashboardSources.Add(fuente);
                        fuentes[grupo.Key] = fuente;
                    }

                    var nombre = datos.Name?.Trim();

                    fuente.Name = Cortar(
                        string.IsNullOrEmpty(nombre) ? grupo.Key : nombre, 200);

                    if (!string.IsNullOrWhiteSpace(datos.Url))
                    {
                        fuente.Url = Cortar(datos.Url.Trim(), 500);
                    }

                    if (!string.IsNullOrWhiteSpace(datos.Category))
                    {
                        fuente.Category = datos.Category;
                    }
                }

                await _context.SaveChangesAsync(ct);

                // --- Items: reemplazar los que ya existen, agregar el resto ---
                foreach (var grupo in grupos)
                {
                    var fuente = fuentes[grupo.Key];


                    var items = grupo
                        .SelectMany(s => s.Items)
                        .GroupBy(i => i.Id.TrimEnd(), StringComparer.Ordinal)
                        .Select(g => (Id: g.Key, Item: g.Last()))
                        .ToList();


                    var actuales = await _context.DashboardSourceItems
                        .Where(i => i.SourceId == fuente.Id)
                        .ToDictionaryAsync(i => i.ItemKey, StringComparer.Ordinal, ct);

                    foreach (var (id, item) in items)
                    {
                        item.Id = id;
                        item.CreatedAt = EnUtc(item.CreatedAt);

                        if (actuales.TryGetValue(id, out var fila))
                        {
                            Copiar(fila, item, ahora);
                            reemplazados++;
                        }
                        else
                        {
                            fila = new DashboardSourceItem { SourceId = fuente.Id };
                            Copiar(fila, item, ahora);
                            _context.DashboardSourceItems.Add(fila);
                            agregados++;
                        }
                    }
                }

                await _context.SaveChangesAsync(ct);
                await transaccion.CommitAsync(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _context.ChangeTracker.Clear();

                return DashboardImportResult.Fail(
                    "No se pudo guardar en la base de datos. ¿Ya ejecutó ScriptSQLDashboard.sql?",
                    new[] { ex.GetBaseException().Message });
            }

            return new DashboardImportResult
            {
                Success = true,
                Sources = grupos.Count,
                Added = agregados,
                Replaced = reemplazados,
                Message =
                    $"Se cargaron {agregados + reemplazados} items de {grupos.Count} fuente(s): " +
                    $"{agregados} nuevos y {reemplazados} reemplazados."
            };
        }

        // ====================================================
        // EXPORTAR
        // ====================================================

        public async Task<DashboardExport> ExportarAsync(
            DashboardFilter? filtro,
            CancellationToken ct = default)
        {
            filtro = Normalizar(filtro ?? new DashboardFilter());

            var filas = await Aplicar(
                    _context.DashboardSourceItems.AsNoTracking(), filtro)
                .OrderBy(i => i.SourceId)
                .ThenByDescending(i => i.CreatedAt)
                .ThenBy(i => i.Id)
                .Select(i => new { i.SourceId, i.Json })
                .ToListAsync(ct);

            return await ArmarAsync(
                filas.Select(f => (f.SourceId, f.Json)), ct);
        }

        public async Task<DashboardExport?> ExportarItemAsync(
            int itemId,
            CancellationToken ct = default)
        {
            var fila = await _context.DashboardSourceItems
                .AsNoTracking()
                .Where(i => i.Id == itemId)
                .Select(i => new { i.SourceId, i.Json })
                .FirstOrDefaultAsync(ct);

            if (fila == null)
            {
                return null;
            }

            return await ArmarAsync(new[] { (fila.SourceId, fila.Json) }, ct);
        }

        public async Task<string?> ObtenerJsonItemAsync(
            int itemId,
            CancellationToken ct = default)
        {
            return await _context.DashboardSourceItems
                .AsNoTracking()
                .Where(i => i.Id == itemId)
                .Select(i => i.Json)
                .FirstOrDefaultAsync(ct);
        }

        public DashboardExport NuevoExport()
        {
            return new DashboardExport
            {
                Version = DashboardExport.CurrentVersion,
                ExportedAt = AhoraUtc(),
                ExportedBy = _configuration["Dashboard:ExportedBy"] ?? "Explora Costa Rica"
            };
        }

        // Junta las filas por fuente y arma el archivo del formato común.
        private async Task<DashboardExport> ArmarAsync(
            IEnumerable<(int SourceId, string Json)> filas,
            CancellationToken ct)
        {
            var lista = filas.ToList();
            var ids = lista.Select(f => f.SourceId).Distinct().ToList();

            var fuentes = await _context.DashboardSources
                .AsNoTracking()
                .Where(s => ids.Contains(s.Id))
                .ToDictionaryAsync(s => s.Id, ct);

            var export = NuevoExport();

            foreach (var grupo in lista.GroupBy(f => f.SourceId))
            {
                if (!fuentes.TryGetValue(grupo.Key, out var fuente))
                {
                    continue;
                }

                var origen = new ExportSource
                {
                    Id = fuente.SourceKey,
                    Name = fuente.Name,
                    Url = string.IsNullOrWhiteSpace(fuente.Url) ? null : fuente.Url,
                    Category = fuente.Category
                };

                foreach (var fila in grupo)
                {
                    var item = LeerItem(fila.Json);

                    if (item != null)
                    {
                        item.CreatedAt = EnUtc(item.CreatedAt);
                        origen.Items.Add(item);
                    }
                }

                if (origen.Items.Count > 0)
                {
                    export.Sources.Add(origen);
                }
            }

            return export;
        }

        // ====================================================
        // AYUDAS
        // ====================================================

        private static DashboardFilter Normalizar(DashboardFilter f)
        {
            f.Q = Limpiar(f.Q, 100);
            f.Source = Limpiar(f.Source, 100)?.ToLowerInvariant();

            var tipo = Limpiar(f.MediaType, 10)?.ToLowerInvariant();
            f.MediaType = tipo != null && MediaTypes.All.Contains(tipo) ? tipo : null;

            var categoria = Limpiar(f.Category, 50)?.ToLowerInvariant();
            f.Category = categoria != null && ExportCategories.All.Contains(categoria)
                ? categoria
                : null;

            f.Sort = Ordenes.Contains(f.Sort) ? f.Sort : "recientes";
            f.Page = Math.Max(1, f.Page);

            f.From = f.From?.Date;
            f.To = f.To?.Date;

            if (f.From.HasValue && f.To.HasValue && f.From > f.To)
            {
                (f.From, f.To) = (f.To, f.From);
            }

            return f;
        }

        private static IQueryable<DashboardSourceItem> Aplicar(
            IQueryable<DashboardSourceItem> consulta,
            DashboardFilter f)
        {
            if (!string.IsNullOrEmpty(f.Source))
            {
                consulta = consulta.Where(i => i.Source.SourceKey == f.Source);
            }

            if (!string.IsNullOrEmpty(f.MediaType))
            {
                consulta = consulta.Where(i => i.MediaType == f.MediaType);
            }

            if (!string.IsNullOrEmpty(f.Category))
            {
                // Si el item no trae categoría se usa la de su fuente.
                consulta = consulta.Where(i =>
                    (i.Category ?? i.Source.Category) == f.Category);
            }

            if (f.From.HasValue)
            {
                var desde = f.From.Value;
                consulta = consulta.Where(i => i.CreatedAt >= desde);
            }

            if (f.To.HasValue)
            {
                var hasta = f.To.Value.AddDays(1);
                consulta = consulta.Where(i => i.CreatedAt < hasta);
            }

            if (!string.IsNullOrEmpty(f.Q))
            {
                var texto = f.Q;
                consulta = consulta.Where(i => i.Title.Contains(texto));
            }

            return consulta;
        }

        private static IQueryable<DashboardSourceItem> Ordenar(
            IQueryable<DashboardSourceItem> consulta,
            string orden)
        {
            return orden switch
            {
                "antiguos" => consulta.OrderBy(i => i.CreatedAt).ThenBy(i => i.Id),
                "titulo" => consulta.OrderBy(i => i.Title).ThenBy(i => i.Id),
                _ => consulta.OrderByDescending(i => i.CreatedAt).ThenByDescending(i => i.Id)
            };
        }

        // Copia el item del formato común a la fila de SQL Server.
        private static void Copiar(
            DashboardSourceItem fila, ExportItem item, DateTime ahora)
        {
            fila.ItemKey = item.Id;
            fila.Title = Cortar(item.Title, 500);
            fila.MediaType = item.MediaType;
            fila.Category = item.Category;
            fila.CreatedAt = item.CreatedAt;
            fila.SavedAt = ahora;
            fila.Json = JsonSerializer.Serialize(item, OpcionesCompactas);
        }

        private static ExportItem? LeerItem(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<ExportItem>(
                    json, DashboardExport.JsonOptions);
            }
            catch (JsonException)
            {
                return null;
            }
        }


        private static DateTime EnUtc(DateTime fecha) => fecha.Kind switch
        {
            DateTimeKind.Utc => fecha,
            DateTimeKind.Local => fecha.ToUniversalTime(),
            _ => DateTime.SpecifyKind(fecha, DateTimeKind.Utc)
        };

        private static DateTime AhoraUtc()
        {
            var ahora = DateTime.UtcNow;


            return new DateTime(
                ahora.Ticks - ahora.Ticks % TimeSpan.TicksPerSecond,
                DateTimeKind.Utc);
        }


        public static string? UrlSegura(string? url)
        {
            if (string.IsNullOrWhiteSpace(url))
            {
                return null;
            }

            return Uri.TryCreate(url.Trim(), UriKind.Absolute, out var uri) &&
                   (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
                ? uri.OriginalString
                : null;
        }

        private static string? Limpiar(string? texto, int maximo)
        {
            if (string.IsNullOrWhiteSpace(texto))
            {
                return null;
            }

            texto = texto.Trim();

            return texto.Length > maximo ? texto[..maximo] : texto;
        }

        private static string Cortar(string texto, int maximo) =>
            texto.Length <= maximo ? texto : texto[..maximo];
    }
}
