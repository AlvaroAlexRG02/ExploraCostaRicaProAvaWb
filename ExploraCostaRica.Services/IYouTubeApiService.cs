using ExploraCostaRica.Models.DTOs;

namespace ExploraCostaRica.Services
{
    // ============================================================
    // API 4 - YOUTUBE
    // ------------------------------------------------------------
    // Contrato del servicio que busca videos sobre Costa Rica.
    // Los controllers solo conocen esta interfaz.
    // ============================================================
    public interface IYouTubeApiService
    {
        // Videos tal como vienen de YouTube (simplificados).
        Task<List<VideoYouTubeDto>> BuscarVideosAsync(
            string busqueda, int cantidad = 12);

        // Los mismos videos en el formato común MediaItemDto.
        Task<List<MediaItemDto>> BuscarContenidoAsync(
            string busqueda, int cantidad = 12);
    }
}