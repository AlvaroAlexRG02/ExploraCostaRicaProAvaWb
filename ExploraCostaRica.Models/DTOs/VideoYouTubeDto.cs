namespace ExploraCostaRica.Models.DTOs
{
    // ============================================================
    // API 4 - YOUTUBE DATA API v3
    // ------------------------------------------------------------
    // Representa UN video tal como lo devuelve YouTube, ya
    // simplificado con los campos que usa la aplicación.
    //
    // Después, el servicio lo convierte al formato común
    // MediaItemDto para mostrarlo en Explorar.
    // ============================================================
    public class VideoYouTubeDto
    {
        // Identificador del video. Ej.: "dQw4w9WgXcQ".
        public string VideoId { get; set; } = string.Empty;

        public string Titulo { get; set; } = string.Empty;

        public string Descripcion { get; set; } = string.Empty;

        // Nombre del canal que publicó el video.
        public string Canal { get; set; } = string.Empty;

        // Miniatura (imagen de portada) del video.
        public string UrlMiniatura { get; set; } = string.Empty;

        public DateTime? FechaPublicacion { get; set; }

        // Enlace para ver el video en YouTube.
        public string UrlVideo => $"https://www.youtube.com/watch?v={VideoId}";
    }
}