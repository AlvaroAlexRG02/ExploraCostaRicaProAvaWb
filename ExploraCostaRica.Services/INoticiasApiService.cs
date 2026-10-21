
using ExploraCostaRica.Models.DTOs;

namespace ExploraCostaRica.Services
{
    public interface INoticiasApiService
    {
        Task<List<NoticiaDto>> ObtenerNoticiasAsync(
            string busqueda = "Costa Rica");

        Task<List<MediaItemDto>> BuscarContenidoAsync(
            string busqueda = "Costa Rica");
    }
}