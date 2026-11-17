namespace ExploraCostaRica.Models.DTOs
{
    // ============================================================
    // API 10 - GBIF (Global Biodiversity Information Facility)
    // ------------------------------------------------------------
    // Representa UN registro científico de una especie
    // (una "ocurrencia") tal como lo devuelve GBIF, ya
    // simplificado con los campos que usa la aplicación.
    //
    // Después, el servicio convierte este DTO al formato común
    // MediaItemDto para mostrarlo en Explorar como cualquier
    // otro contenido.
    // ============================================================
    public class OcurrenciaGbifDto
    {
        // Identificador del registro en GBIF ("key").
        public string Id { get; set; } = string.Empty;

        // Nombre científico. Ej.: "Pharomachrus mocinno".
        public string NombreCientifico { get; set; } = string.Empty;

        // Nombre común si GBIF lo trae. Ej.: "Quetzal".
        public string NombreComun { get; set; } = string.Empty;

        // Clasificación: reino (Animalia, Plantae...) y familia.
        public string Reino { get; set; } = string.Empty;
        public string Familia { get; set; } = string.Empty;

        // Dónde se registró la especie.
        public string Lugar { get; set; } = string.Empty;
        public string Provincia { get; set; } = string.Empty;

        // Foto del registro (si tiene).
        public string UrlImagen { get; set; } = string.Empty;

        // Licencia de la foto. GBIF pide respetar la atribución.
        public string Licencia { get; set; } = string.Empty;

        // Quién hizo el registro y qué institución lo publica.
        public string RegistradoPor { get; set; } = string.Empty;
        public string Institucion { get; set; } = string.Empty;

        // Fecha en que se observó la especie.
        public DateTime? FechaRegistro { get; set; }

        // Enlace a la ficha del registro en gbif.org.
        public string UrlRegistro { get; set; } = string.Empty;
    }
}