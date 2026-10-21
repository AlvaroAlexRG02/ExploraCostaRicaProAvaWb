using ExploraCostaRica.Models.Entities;

namespace ExploraCostaRica.Models.ViewModels
{
    // ============================================================
    // TODO LO QUE NECESITAN LAS VISTAS
    // Destinos/Detalle.cshtml y Destinos/Sorprendeme.cshtml
    // ============================================================
    // Las dos pantallas muestran lo mismo: un destino y su
    // contenido asociado. Por eso comparten este ViewModel.
    // ============================================================
    public class DestinationDetailViewModel
    {
        // Destino que se muestra
        public Destination Destination { get; set; } = null!;

        // Contenido relacionado (fotos, videos, noticias...)
        // leído desde la vista vw_MediaCatalog
        public List<VwMediaCatalog> RelatedContent { get; set; } = new();

        // true cuando el destino lo eligió el botón Sorpréndeme
        public bool IsSurprise { get; set; }

        // Para mostrar el mapa en el futuro
        public bool HasCoordinates =>
            Destination.Latitude.HasValue &&
            Destination.Longitude.HasValue;
    }
}
