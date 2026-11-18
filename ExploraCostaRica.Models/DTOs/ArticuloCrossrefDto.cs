namespace ExploraCostaRica.Models.DTOs
{
    // ============================================================
    // API 7 - CROSSREF
    // ------------------------------------------------------------
    // Representa UN artículo científico (paper) tal como lo
    // devuelve Crossref, ya simplificado con los campos que usa
    // la aplicación.
    //
    // Después, el servicio convierte este DTO al formato común
    // MediaItemDto para mostrarlo en Explorar como cualquier
    // otro contenido.
    // ============================================================
    public class ArticuloCrossrefDto
    {
        // DOI del artículo: su identificador único.
        // Ej.: "10.1111/btp.12345".
        public string Doi { get; set; } = string.Empty;

        // Título del artículo.
        public string Titulo { get; set; } = string.Empty;

        // Autores ("Nombre Apellido").
        public List<string> Autores { get; set; } = new();

        // Revista donde se publicó. Ej.: "Biotropica".
        public string Revista { get; set; } = string.Empty;

        // Resumen del artículo, si Crossref lo trae (ya sin etiquetas).
        public string Resumen { get; set; } = string.Empty;

        // Fecha de publicación (a veces solo año o año y mes).
        public DateTime? FechaPublicacion { get; set; }

        // Enlace al artículo (https://doi.org/...).
        public string UrlArticulo { get; set; } = string.Empty;
    }
}
