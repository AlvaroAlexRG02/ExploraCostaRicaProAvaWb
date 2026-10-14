
namespace ExploraCostaRica.Models.DTOs
{
    public class JsonImportado
    {
        public string? ExternalId { get; set; }

        public string? Title { get; set; }

        public string? Description { get; set; }

        public string? Author { get; set; }

        public string? Url { get; set; }

        public string? ImageUrl { get; set; }

        public string? Categoria { get; set; }

        public string? Destino { get; set; }

        public string? TipoContenido { get; set; }

        public string? Fuente { get; set; }

        public DateTime? PublishedAt { get; set; }
    }
}
