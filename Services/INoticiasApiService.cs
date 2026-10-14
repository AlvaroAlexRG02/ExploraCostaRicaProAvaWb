
using ExploraCostaRica.Models;

namespace ExploraCostaRica.Services
{
    public interface INoticiasApiService
    {
        Task<List<NoticiaDto>> ObtenerNoticiasAsync(
            string busqueda = "Costa Rica");
    }
}
