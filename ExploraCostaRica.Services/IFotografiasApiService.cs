using ExploraCostaRica.Models.DTOs;

namespace ExploraCostaRica.Services
{
    public interface IFotografiasApiService
    {
        Task<List<FotografiaDto>> BuscarFotografiasAsync(
            string busqueda,
            int pagina = 1);

        Task<List<MediaItemDto>> BuscarContenidoAsync(
            string busqueda,
            int pagina = 1);
    }
}