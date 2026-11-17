using ExploraCostaRica.Models.DTOs;

namespace ExploraCostaRica.Services
{
    public interface IWikimediaApiService
    {
        // Busca imágenes en Wikimedia Commons.
        Task<List<WikimediaImagenDto>> BuscarImagenesAsync(
            string busqueda, int cantidad = 12);


        Task<List<MediaItemDto>> BuscarContenidoAsync(
            string busqueda, string? destino = null, int cantidad = 12);
    }
}
