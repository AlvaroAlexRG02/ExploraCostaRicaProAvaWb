namespace ExploraCostaRica.Models.ViewModels
{
    // ============================================================
    // FILTROS QUE ENVÍA EL USUARIO EN LA PANTALLA DESTINOS
    // ============================================================
    // Si una propiedad viene vacía, ese filtro no se aplica.
    // ============================================================
    public class DestinationFilter
    {
        // Texto del buscador (nombre, provincia o descripción)
        public string? Search { get; set; }

        // Provincia elegida en el filtro
        public string? Province { get; set; }

        public bool HasActiveFilter =>
            !string.IsNullOrWhiteSpace(Search) ||
            !string.IsNullOrWhiteSpace(Province);
    }

    // ============================================================
    // TODO LO QUE NECESITA LA VISTA Destinos/Index.cshtml
    // ============================================================
    public class DestinationIndexViewModel
    {
        // Filtros elegidos (para dejarlos marcados en el formulario)
        public DestinationFilter Filter { get; set; } = new();

        // Destinos destacados (IsFeatured = 1)
        public List<DestinationCardViewModel> Featured { get; set; } = new();

        // Destinos que cumplen los filtros
        public List<DestinationCardViewModel> Destinations { get; set; } = new();

        // Provincias disponibles para el filtro
        public List<string> Provinces { get; set; } = new();

        public int TotalResults => Destinations.Count;
    }
}
