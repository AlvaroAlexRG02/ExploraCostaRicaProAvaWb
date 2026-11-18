namespace ExploraCostaRica.Models.DTOs
{
    // ============================================================
    // API 6 - OPEN LIBRARY
    // ------------------------------------------------------------
    // Representa UN libro tal como lo devuelve la búsqueda de
    // Open Library, ya simplificado con los campos que usa la
    // aplicación.
    //
    // Después, el servicio convierte este DTO al formato común
    // MediaItemDto para mostrarlo en Explorar como cualquier
    // otro contenido.
    // ============================================================
    public class LibroOpenLibraryDto
    {
        // Identificador de la obra en Open Library.
        // Ej.: "/works/OL1234567W".
        public string Id { get; set; } = string.Empty;

        // Título del libro.
        public string Titulo { get; set; } = string.Empty;

        // Autores (Open Library devuelve una lista).
        public List<string> Autores { get; set; } = new();

        // Año de la primera publicación, si se conoce.
        public int? AnioPublicacion { get; set; }

        // Primera editorial que aparece en el registro.
        public string Editorial { get; set; } = string.Empty;

        // Algunos temas del libro. Ej.: "Natural history", "Costa Rica".
        public List<string> Temas { get; set; } = new();

        // Portada del libro (si tiene).
        public string UrlPortada { get; set; } = string.Empty;

        // Enlace a la ficha del libro en openlibrary.org.
        public string UrlLibro { get; set; } = string.Empty;
    }
}
