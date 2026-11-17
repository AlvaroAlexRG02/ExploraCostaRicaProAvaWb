using ExploraCostaRica.Models.DTOs;

namespace ExploraCostaRica.Services
{
    // ============================================================
    // API 10 - GBIF
    // ------------------------------------------------------------
    // Contrato del servicio que consulta registros científicos de
    // especies en Costa Rica. Los controllers solo conocen esta
    // interfaz; la clase GbifApiService hace el trabajo real.
    // ============================================================
    public interface IGbifApiService
    {
        // Devuelve los registros tal como vienen de GBIF (simplificados).
        Task<List<OcurrenciaGbifDto>> BuscarOcurrenciasAsync(
            string busqueda, int cantidad = 12);

        // Devuelve los mismos registros convertidos al formato común
        // MediaItemDto, listos para mostrarse en Explorar.
        Task<List<MediaItemDto>> BuscarContenidoAsync(
            string busqueda, int cantidad = 12);
    }
}