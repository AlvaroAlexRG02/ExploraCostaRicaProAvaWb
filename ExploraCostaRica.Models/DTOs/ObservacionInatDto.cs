namespace ExploraCostaRica.Models.DTOs
{
    public class ObservacionInatDto
    {
        public string Id { get; set; } = string.Empty;
        public string NombreComun { get; set; } = string.Empty;
        public string NombreCientifico { get; set; } = string.Empty;
        public string UrlImagen { get; set; } = string.Empty;
        public string UrlObservacion { get; set; } = string.Empty;
        public string Observador { get; set; } = string.Empty;
        public string Lugar { get; set; } = string.Empty;
        public DateTime? FechaObservacion { get; set; }
    }
}
