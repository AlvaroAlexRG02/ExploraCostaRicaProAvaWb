using ExploraCostaRica.Models.Entities;

namespace ExploraCostaRica.Services
{
    public interface IFavoritosService
    {
        Task<List<VwUserFavorite>> ObtenerFavoritosAsync(
            string tipo = "todos",
            string orden = "recientes");

        Task<List<long>> ObtenerIdsAsync();

        Task<(bool Exitoso, bool YaExistia, int Cantidad, string? Error)>
            AgregarAsync(long mediaId);

        Task<(bool Exitoso, int Cantidad, string? Error)>
            QuitarAsync(long mediaId);

        Task<(bool Exitoso, bool EsFavorito, int Cantidad, string? Error)>
            ToggleAsync(long mediaId);
    }
}