using ExploraCostaRica.Models.DTOs;
using ExploraCostaRica.Models.Entities;

namespace ExploraCostaRica.Models.ViewModels
{
    public class DestinationDetailViewModel
    {
        // Destino que se muestra
        public Destination Destination { get; set; } = null!;

        // Contenido relacionado (fotos, videos, noticias...)
        // leído desde la vista vw_MediaCatalog
        public List<VwMediaCatalog> RelatedContent { get; set; } = new();

        // Observaciones de biodiversidad (iNaturalist) cerca del destino.
        // Puede venir vacío si la API no responde.
        public List<MediaItemDto> Biodiversidad { get; set; } = new();

        // true cuando el destino lo eligió el botón Sorpréndeme
        public bool IsSurprise { get; set; }

        // Para mostrar el mapa en el futuro
        public bool HasCoordinates =>
            Destination.Latitude.HasValue &&
            Destination.Longitude.HasValue;
    }
}