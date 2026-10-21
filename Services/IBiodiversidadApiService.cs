using ExploraCostaRica.Models.DTOs;

namespace ExploraCostaRica.Services
{
    public interface IBiodiversidadApiService
    {
        Task<List<ObservacionInatDto>> BuscarObservacionesAsync(
            double latitud, double longitud, int radioKm = 10);

        Task<List<MediaItemDto>> BuscarContenidoAsync(
            double latitud, double longitud, string destino, int radioKm = 10);
    }
}