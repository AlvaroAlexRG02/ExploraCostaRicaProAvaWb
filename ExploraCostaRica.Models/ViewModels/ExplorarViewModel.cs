using ExploraCostaRica.Models.DTOs;
using ExploraCostaRica.Models.Entities;

namespace ExploraCostaRica.Models.ViewModels
{
    // ============================================================
    // FILTROS QUE ENVÍA EL USUARIO
    // ============================================================
    // Cada propiedad corresponde a un campo del formulario de
    // la pantalla Explorar. Si viene vacío, ese filtro no se aplica.
    // ============================================================
    public class ExplorarFiltro
    {
        public string? Texto { get; set; }      // lo que escribe en el buscador
        public string? Tipo { get; set; }       // Foto, Video, Biodiversidad...
        public int? CategoriaId { get; set; }   // Naturaleza, Cultura...
        public int? DestinoId { get; set; }     // Monteverde, Arenal...
        public int? FuenteId { get; set; }      // de qué API / fuente vino
        public string Orden { get; set; } = "relevantes";
    }

    // ============================================================
    // UNA OPCIÓN DE FILTRO (para los checkbox y los combos)
    // ============================================================
    // Ejemplo: Valor = "Foto", Texto = "Foto", Cantidad = 14
    // ============================================================
    public class OpcionFiltro
    {
        public string Valor { get; set; } = string.Empty;
        public string Texto { get; set; } = string.Empty;
        public int Cantidad { get; set; }
    }

    // ============================================================
    // TODO LO QUE NECESITA LA VISTA Explorar/Index.cshtml
    // ============================================================
    public class ExplorarViewModel
    {
        // Los filtros que el usuario eligió (para dejarlos marcados)
        public ExplorarFiltro Filtro { get; set; } = new();

        // Tarjetas que vienen de nuestra base de datos (SQL Server)
        public List<VwMediaCatalog> Resultados { get; set; } = new();

        // Tarjetas que vienen de APIs externas en vivo
        // (YouTube - API 4 y GBIF - API 10), ya en el formato común.
        public List<MediaItemDto> ResultadosExternos { get; set; } = new();

        // Mensajes amigables si alguna API externa falló.
        // Ej.: "No fue posible consultar YouTube en este momento."
        public List<string> Avisos { get; set; } = new();

        // true si en esta búsqueda se consultaron APIs externas
        public bool SeConsultaronApis { get; set; }

        // Opciones para llenar los filtros del panel izquierdo
        public List<OpcionFiltro> Tipos { get; set; } = new();
        public List<OpcionFiltro> Categorias { get; set; } = new();
        public List<OpcionFiltro> Destinos { get; set; } = new();
        public List<OpcionFiltro> Fuentes { get; set; } = new();

        // Total = contenido local + contenido de APIs externas
        public int TotalResultados => Resultados.Count + ResultadosExternos.Count;
    }
}