
using ExploraCostaRica.Models.Entities;

namespace ExploraCostaRica.Models.ViewModels
{
    public class DescargasViewModel
    {
        public int? UsuarioDemoId { get; set; }

        public string NombreUsuario { get; set; } = "";

        public List<ExploraCostaRica.Models.Entities.MediaItem>
            Contenidos
        { get; set; } = new();

        public List<Download> Historial { get; set; } = new();
    }
}
