using ExploraCostaRica.Models.Entities;

namespace ExploraCostaRica.Services
{

    public interface IFavoritosService
    {
        Task<List<VwUserFavorite>> ObtenerFavoritosAsync(
            int userId,
            string tipo = "todos",
            string orden = "recientes");

        Task<List<long>> ObtenerIdsAsync(int userId);

        Task<(bool Exitoso, bool YaExistia, int Cantidad, string? Error)>
            AgregarAsync(int userId, long mediaId);

        Task<(bool Exitoso, int Cantidad, string? Error)>
            QuitarAsync(int userId, long mediaId);

        Task<(bool Exitoso, bool EsFavorito, int Cantidad, string? Error)>
            ToggleAsync(int userId, long mediaId);
    }
}
