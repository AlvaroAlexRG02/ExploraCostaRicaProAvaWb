using ExploraCostaRica.Data;
using ExploraCostaRica.Models.DTOs;
using ExploraCostaRica.Models.Entities;
using ExploraCostaRica.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace ExploraCostaRica.Services
{
    // ============================================================
    // LÓGICA DE BÚSQUEDA Y FILTROS DE LA PANTALLA EXPLORAR
    // ============================================================
    // Combina dos orígenes de contenido:
    //
    //   1. LOCAL: la vista vw_MediaCatalog de SQL Server, que ya
    //      junta el contenido con su tipo, categoría, destino y fuente.
    //
    //   2. EXTERNO (en vivo): YouTube (API 4) y GBIF (API 10).
    //      Solo se consultan cuando la persona escribe algo en el
    //      buscador, para no gastar la cuota de YouTube en cada
    //      visita a la página. Ambos servicios tienen caché.
    // ============================================================
    public class ExplorarService : IExplorarService
    {
        // Largo máximo de SearchText en la tabla SearchHistory
        private const int MaxSearchTextLength = 300;

        // Nombres de las fuentes externas (iguales a la tabla ApiSources)
        private const string FuenteYouTube = "YouTube";
        private const string FuenteGbif = "GBIF";

        // Tipos de contenido que traen esas fuentes
        private const string TipoVideo = "Video";
        private const string TipoBiodiversidad = "Biodiversidad";

        // Cuántos resultados pedir a cada API externa
        private const int CantidadPorApi = 8;

        // Fotos de respaldo: las encontradas en Wikimedia se guardan
        // 1 día, así cada destino se busca una sola vez al día.
        private static readonly TimeSpan DuracionCacheFotos = TimeSpan.FromDays(1);

        // Tiempo máximo para esperar una foto de respaldo. Si tarda
        // más, la tarjeta usa el fondo de color y la página no espera.
        private static readonly TimeSpan EsperaMaximaFoto = TimeSpan.FromSeconds(4);

        // Fotos locales (wwwroot/images/home) que ya usa Inicio.
        // Clave = palabra del destino; valor = ruta de la imagen.
        private static readonly Dictionary<string, string> FotosLocales = new()
        {
            ["arenal"] = "/images/home/arenalunsplash.jpg",
            ["monteverde"] = "/images/home/monteverdeunsplash.jpg",
            ["guanacaste"] = "/images/home/guanacasteunsplash.jpg"
        };

        private readonly ExploraCostaRicaDbContext _context;
        private readonly IYouTubeApiService _youTubeService;
        private readonly IGbifApiService _gbifService;
        private readonly IWikimediaApiService _wikimediaService;
        private readonly IMemoryCache _cache;

        // Todo llega por inyección de dependencias (Program.cs)
        public ExplorarService(
            ExploraCostaRicaDbContext context,
            IYouTubeApiService youTubeService,
            IGbifApiService gbifService,
            IWikimediaApiService wikimediaService,
            IMemoryCache cache)
        {
            _context = context;
            _youTubeService = youTubeService;
            _gbifService = gbifService;
            _wikimediaService = wikimediaService;
            _cache = cache;
        }

        public async Task<ExplorarViewModel> BuscarAsync(ExplorarFiltro filtro, int? userId)
        {
            var modelo = new ExplorarViewModel { Filtro = filtro };

            // Ids de las fuentes registradas (para saber cuál es YouTube y cuál GBIF)
            var fuentes = await _context.ApiSources
                .AsNoTracking()
                .Where(s => s.IsActive)
                .Select(s => new { s.SourceId, s.Name })
                .ToListAsync();

            int? idYouTube = fuentes.FirstOrDefault(s => s.Name == FuenteYouTube)?.SourceId;
            int? idGbif = fuentes.FirstOrDefault(s => s.Name == FuenteGbif)?.SourceId;

            // ----------------------------------------------------
            // 1. CONTENIDO LOCAL (SQL Server)
            // ----------------------------------------------------
            IQueryable<VwMediaCatalog> query = _context.VwMediaCatalogs.AsNoTracking();

            query = AplicarTexto(query, filtro.Texto);

            if (!string.IsNullOrWhiteSpace(filtro.Tipo))
                query = query.Where(m => m.ContentType == filtro.Tipo);

            if (filtro.CategoriaId.HasValue)
                query = query.Where(m => m.CategoryId == filtro.CategoriaId);

            if (filtro.DestinoId.HasValue)
                query = query.Where(m => m.DestinationId == filtro.DestinoId);

            if (filtro.FuenteId.HasValue)
                query = query.Where(m => m.SourceId == filtro.FuenteId);

            modelo.Resultados = await Ordenar(query, filtro.Orden).ToListAsync();

            // Fotos de respaldo para el contenido que no trae imagen
            await AsignarFotosDeRespaldoAsync(modelo);

            // ----------------------------------------------------
            // 2. CONTENIDO EXTERNO (YouTube + GBIF)
            // ----------------------------------------------------
            await BuscarEnApisAsync(filtro, modelo, idYouTube, idGbif);

            modelo.ResultadosExternos = OrdenarExternos(modelo.ResultadosExternos, filtro.Orden);

            // ----------------------------------------------------
            // 3. HISTORIAL Y OPCIONES DE LOS FILTROS
            // ----------------------------------------------------
            await GuardarHistorialAsync(filtro, modelo.TotalResultados, userId);

            modelo.Tipos = await ObtenerTiposAsync(modelo.ResultadosExternos);
            modelo.Categorias = await ObtenerCategoriasAsync();
            modelo.Destinos = await ObtenerDestinosAsync();
            modelo.Fuentes = await ObtenerFuentesAsync(modelo.ResultadosExternos);

            return modelo;
        }

        // ------------------------------------------------------------
        // BÚSQUEDA EN APIS EXTERNAS
        // Se consulta cada API solo si tiene sentido con los filtros:
        //   - Debe haber texto en el buscador.
        //   - Categoría y destino son filtros de nuestra base de
        //     datos; si se usan, no se consultan APIs externas.
        //   - Tipo "Video" -> solo YouTube; "Biodiversidad" -> solo GBIF.
        //   - Si se eligió una fuente, solo se consulta esa.
        // Si una API falla, se muestra un aviso y la página sigue
        // funcionando con lo demás.
        // ------------------------------------------------------------
        private async Task BuscarEnApisAsync(
            ExplorarFiltro filtro,
            ExplorarViewModel modelo,
            int? idYouTube,
            int? idGbif)
        {
            if (string.IsNullOrWhiteSpace(filtro.Texto)
                || filtro.CategoriaId.HasValue
                || filtro.DestinoId.HasValue)
            {
                return;
            }

            bool incluirYouTube =
                (string.IsNullOrWhiteSpace(filtro.Tipo) || filtro.Tipo == TipoVideo)
                && (!filtro.FuenteId.HasValue || filtro.FuenteId == idYouTube);

            bool incluirGbif =
                (string.IsNullOrWhiteSpace(filtro.Tipo) || filtro.Tipo == TipoBiodiversidad)
                && (!filtro.FuenteId.HasValue || filtro.FuenteId == idGbif);

            if (!incluirYouTube && !incluirGbif)
            {
                return;
            }

            modelo.SeConsultaronApis = true;

            string texto = filtro.Texto.Trim();

            // Las dos APIs se consultan AL MISMO TIEMPO (Task.WhenAll),
            // así la página espera lo que tarde la más lenta, no la suma.
            var tareaYouTube = incluirYouTube
                ? ConsultarSeguroAsync(
                    () => _youTubeService.BuscarContenidoAsync(texto, CantidadPorApi),
                    FuenteYouTube, modelo.Avisos)
                : Task.FromResult(new List<MediaItemDto>());

            var tareaGbif = incluirGbif
                ? ConsultarSeguroAsync(
                    () => _gbifService.BuscarContenidoAsync(texto, CantidadPorApi),
                    FuenteGbif, modelo.Avisos)
                : Task.FromResult(new List<MediaItemDto>());

            await Task.WhenAll(tareaYouTube, tareaGbif);

            modelo.ResultadosExternos.AddRange(tareaYouTube.Result);
            modelo.ResultadosExternos.AddRange(tareaGbif.Result);
        }

        // Ejecuta la consulta a una API y, si falla, devuelve una lista
        // vacía y agrega un aviso en vez de romper la página.
        private static async Task<List<MediaItemDto>> ConsultarSeguroAsync(
            Func<Task<List<MediaItemDto>>> consulta,
            string fuente,
            List<string> avisos)
        {
            try
            {
                return await consulta();
            }
            catch (InvalidOperationException)
            {
                lock (avisos)
                {
                    avisos.Add($"{fuente} no está configurado en este equipo (falta la API Key).");
                }
            }
            catch (HttpRequestException)
            {
                lock (avisos)
                {
                    avisos.Add($"No fue posible consultar {fuente} en este momento.");
                }
            }
            catch (TaskCanceledException)
            {
                lock (avisos)
                {
                    avisos.Add($"{fuente} tardó demasiado en responder.");
                }
            }
            catch (System.Text.Json.JsonException)
            {
                lock (avisos)
                {
                    avisos.Add($"{fuente} devolvió una respuesta que no se pudo leer.");
                }
            }

            return new List<MediaItemDto>();
        }

        // Mismo criterio de orden que el contenido local.
        private static List<MediaItemDto> OrdenarExternos(List<MediaItemDto> lista, string? orden)
        {
            return orden switch
            {
                "recientes" => lista.OrderByDescending(m => m.PublishedAt).ToList(),
                "titulo" => lista.OrderBy(m => m.Title).ToList(),
                _ => lista   // relevantes: el orden que da cada API
            };
        }

        // ------------------------------------------------------------
        // FOTOS DE RESPALDO PARA EL CONTENIDO LOCAL
        // Muchos registros de la base no traen ImageUrl. Para que
        // ninguna tarjeta quede sin foto se sigue este orden:
        //   1. La imagen propia del contenido (si tiene).
        //   2. Una foto local del destino (las mismas de Inicio).
        //   3. Una foto del destino en Wikimedia Commons (sin API Key),
        //      guardada en caché 1 día y con crédito de autor/licencia.
        //   4. Si nada funciona, la vista muestra un fondo de color.
        // ------------------------------------------------------------
        private async Task AsignarFotosDeRespaldoAsync(ExplorarViewModel modelo)
        {
            var sinFoto = modelo.Resultados
                .Where(r => string.IsNullOrWhiteSpace(r.ImageUrl))
                .ToList();

            if (sinFoto.Count == 0)
            {
                return;
            }

            // Paso 2: foto local según el destino
            foreach (var item in sinFoto)
            {
                string? local = BuscarFotoLocal(item.Destination);

                if (local != null)
                {
                    item.ImageUrl = local;
                    modelo.CreditosImagen[item.MediaId] = "Foto: Unsplash";
                }
            }

            // Paso 3: Wikimedia, una búsqueda por destino (no por tarjeta)
            var destinos = sinFoto
                .Where(i => string.IsNullOrWhiteSpace(i.ImageUrl)
                         && !string.IsNullOrWhiteSpace(i.Destination))
                .Select(i => i.Destination!)
                .Distinct()
                .ToList();

            var tareas = destinos.ToDictionary(d => d, d => BuscarFotoWikimediaAsync(d));

            await Task.WhenAll(tareas.Values);

            foreach (var item in sinFoto.Where(i => string.IsNullOrWhiteSpace(i.ImageUrl)
                                                 && !string.IsNullOrWhiteSpace(i.Destination)))
            {
                var foto = tareas[item.Destination!].Result;

                if (foto != null)
                {
                    item.ImageUrl = foto.UrlImagen;
                    modelo.CreditosImagen[item.MediaId] = ArmarCredito(foto);
                }
            }
        }

        private static string? BuscarFotoLocal(string? destino)
        {
            if (string.IsNullOrWhiteSpace(destino))
            {
                return null;
            }

            return FotosLocales
                .Where(f => destino.Contains(f.Key, StringComparison.OrdinalIgnoreCase))
                .Select(f => f.Value)
                .FirstOrDefault();
        }

        // Busca UNA foto del destino en Wikimedia. Nunca rompe la página:
        // si falla o tarda más de 4 segundos, devuelve null.
        private async Task<WikimediaImagenDto?> BuscarFotoWikimediaAsync(string destino)
        {
            string clave = $"foto-destino:{destino.ToLowerInvariant()}";

            if (_cache.TryGetValue(clave, out WikimediaImagenDto? enCache))
            {
                return enCache;
            }

            try
            {
                var busqueda = _wikimediaService.BuscarImagenesAsync($"{destino} Costa Rica", 3);
                var ganadora = await Task.WhenAny(busqueda, Task.Delay(EsperaMaximaFoto));

                if (ganadora != busqueda)
                {
                    return null;   // tardó demasiado: no se guarda en caché
                }

                var foto = (await busqueda)
                    .FirstOrDefault(f => !string.IsNullOrWhiteSpace(f.UrlImagen));

                _cache.Set(clave, foto, DuracionCacheFotos);

                return foto;
            }
            catch (Exception ex) when (ex is HttpRequestException
                                    or TaskCanceledException
                                    or System.Text.Json.JsonException)
            {
                return null;
            }
        }

        // Ej.: "Foto: Juan Pérez · CC BY-SA 4.0 · Wikimedia Commons"
        private static string ArmarCredito(WikimediaImagenDto foto)
        {
            var partes = new List<string>();

            if (!string.IsNullOrWhiteSpace(foto.Autor))
            {
                partes.Add(foto.Autor);
            }

            if (!string.IsNullOrWhiteSpace(foto.Licencia))
            {
                partes.Add(foto.Licencia);
            }

            partes.Add("Wikimedia Commons");

            return "Foto: " + string.Join(" · ", partes);
        }

        // ------------------------------------------------------------
        // HISTORIAL DE BÚSQUEDAS
        // Solo se guarda cuando la persona escribió algo en el buscador
        // (si solo entra a la página, no cuenta como búsqueda).
        //
        // Se guarda a nombre del usuario que inició sesión (userId,
        // que viene de la cookie de login). Si nadie inició sesión,
        // UserId queda vacío (null): la tabla lo permite.
        //
        // Si falla el guardado, NO se cae la pantalla: la búsqueda
        // sigue funcionando igual.
        // ------------------------------------------------------------
        private async Task GuardarHistorialAsync(
            ExplorarFiltro filtro, int cantidadResultados, int? userId)
        {
            if (string.IsNullOrWhiteSpace(filtro.Texto))
                return;

            var texto = filtro.Texto.Trim();
            if (texto.Length > MaxSearchTextLength)
                texto = texto.Substring(0, MaxSearchTextLength);

            try
            {
                _context.SearchHistories.Add(new SearchHistory
                {
                    UserId = userId,
                    SearchText = texto,
                    CategoryId = filtro.CategoriaId,
                    DestinationId = filtro.DestinoId,
                    ResultCount = cantidadResultados,
                    SearchedAt = DateTime.Now
                });

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                // Limpiamos lo pendiente para no afectar otras operaciones
                _context.ChangeTracker.Clear();
            }
        }

        // ------------------------------------------------------------
        // Búsqueda por texto: busca la palabra en varios campos.
        // Ej.: "Monteverde" encuentra por título, destino o descripción.
        // ------------------------------------------------------------
        private static IQueryable<VwMediaCatalog> AplicarTexto(IQueryable<VwMediaCatalog> query, string? texto)
        {
            if (string.IsNullOrWhiteSpace(texto))
                return query;

            texto = texto.Trim();

            return query.Where(m =>
                m.Title.Contains(texto) ||
                (m.Description != null && m.Description.Contains(texto)) ||
                (m.Destination != null && m.Destination.Contains(texto)) ||
                (m.Province != null && m.Province.Contains(texto)) ||
                (m.Category != null && m.Category.Contains(texto)) ||
                (m.Author != null && m.Author.Contains(texto)) ||
                m.Source.Contains(texto));
        }

        // ------------------------------------------------------------
        // Orden: "Más relevantes", "Más recientes" o "Título A-Z"
        // ------------------------------------------------------------
        private static IQueryable<VwMediaCatalog> Ordenar(IQueryable<VwMediaCatalog> query, string? orden)
        {
            return orden switch
            {
                "recientes" => query.OrderByDescending(m => m.PublishedAt ?? m.SavedAt),
                "titulo" => query.OrderBy(m => m.Title),
                _ => query.OrderBy(m => m.ContentType).ThenBy(m => m.Title)   // relevantes
            };
        }

        // ------------------------------------------------------------
        // Opciones de los filtros, con cuántos elementos hay de cada una.
        // GroupBy agrupa y Count cuenta: Foto (14), Video (6)...
        // ------------------------------------------------------------

        // Tipos: los de la base + los que trajeron las APIs externas.
        // "Video" y "Biodiversidad" siempre aparecen para poder filtrar.
        private async Task<List<OpcionFiltro>> ObtenerTiposAsync(List<MediaItemDto> externos)
        {
            var tipos = await _context.VwMediaCatalogs.AsNoTracking()
                .GroupBy(m => m.ContentType)
                .Select(g => new OpcionFiltro { Valor = g.Key, Texto = g.Key, Cantidad = g.Count() })
                .ToListAsync();

            foreach (var nombre in new[] { TipoVideo, TipoBiodiversidad })
            {
                if (!tipos.Any(t => t.Valor == nombre))
                {
                    tipos.Add(new OpcionFiltro { Valor = nombre, Texto = nombre });
                }
            }

            foreach (var grupo in externos
                .Where(e => !string.IsNullOrWhiteSpace(e.ContentType))
                .GroupBy(e => e.ContentType!))
            {
                var opcion = tipos.First(t => t.Valor == grupo.Key);
                opcion.Cantidad += grupo.Count();
            }

            return tipos.OrderBy(t => t.Texto).ToList();
        }

        private Task<List<OpcionFiltro>> ObtenerCategoriasAsync()
        {
            return _context.VwMediaCatalogs.AsNoTracking()
                .Where(m => m.CategoryId != null)
                .GroupBy(m => new { m.CategoryId, m.Category })
                .OrderBy(g => g.Key.Category)
                .Select(g => new OpcionFiltro
                {
                    Valor = g.Key.CategoryId!.Value.ToString(),
                    Texto = g.Key.Category ?? "",
                    Cantidad = g.Count()
                })
                .ToListAsync();
        }

        private Task<List<OpcionFiltro>> ObtenerDestinosAsync()
        {
            return _context.VwMediaCatalogs.AsNoTracking()
                .Where(m => m.DestinationId != null)
                .GroupBy(m => new { m.DestinationId, m.Destination })
                .OrderBy(g => g.Key.Destination)
                .Select(g => new OpcionFiltro
                {
                    Valor = g.Key.DestinationId!.Value.ToString(),
                    Texto = g.Key.Destination ?? "",
                    Cantidad = g.Count()
                })
                .ToListAsync();
        }

        // Fuentes: todas las registradas en ApiSources (así YouTube y
        // GBIF aparecen aunque no haya contenido guardado de ellas),
        // con lo guardado en la base + lo que trajo esta búsqueda.
        private async Task<List<OpcionFiltro>> ObtenerFuentesAsync(List<MediaItemDto> externos)
        {
            var locales = await _context.VwMediaCatalogs.AsNoTracking()
                .GroupBy(m => m.SourceId)
                .Select(g => new { SourceId = g.Key, Cantidad = g.Count() })
                .ToListAsync();

            var fuentes = await _context.ApiSources.AsNoTracking()
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .Select(s => new { s.SourceId, s.Name })
                .ToListAsync();

            return fuentes.Select(s => new OpcionFiltro
            {
                Valor = s.SourceId.ToString(),
                Texto = s.Name,
                Cantidad = (locales.FirstOrDefault(l => l.SourceId == s.SourceId)?.Cantidad ?? 0)
                         + externos.Count(e => e.Source == s.Name)
            }).ToList();
        }
    }
}