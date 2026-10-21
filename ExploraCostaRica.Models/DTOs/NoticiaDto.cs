
namespace ExploraCostaRica.Models.DTOs
{
    public class NoticiaDto
    {
        public string Id { get; set; } = "";

        public string Titulo { get; set; } = "";

        public string Descripcion { get; set; } = "";

        public string Contenido { get; set; } = "";

        public string Url { get; set; } = "";

        public string ImagenUrl { get; set; } = "";

        public string Fuente { get; set; } = "";

        public DateTime? FechaPublicacion { get; set; }
    }
}
