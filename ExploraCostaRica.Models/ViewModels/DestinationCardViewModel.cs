namespace ExploraCostaRica.Models.ViewModels
{
    // ============================================================
    // TARJETA DE UN DESTINO
    // ============================================================
    // Solo los datos que necesita una tarjeta en la pantalla
    // Destinos. No enviamos la entidad completa a la vista.
    // ============================================================
    public class DestinationCardViewModel
    {
        public int DestinationId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Province { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? ImageUrl { get; set; }

        public bool IsFeatured { get; set; }

        // Cantidad de contenido activo (MediaItems) asociado al destino
        public int ContentCount { get; set; }
    }
}