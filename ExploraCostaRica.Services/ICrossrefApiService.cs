using ExploraCostaRica.Models.DTOs;

namespace ExploraCostaRica.Services
{
    // ============================================================
    // API 7 - CROSSREF
    // ------------------------------------------------------------
    // Contrato del servicio que busca artículos científicos
    // (papers) sobre Costa Rica. Los controllers solo conocen
    // esta interfaz; la clase CrossrefApiService hace el
    // trabajo real.
    // ============================================================
    public interface ICrossrefApiService
    {
        // Devuelve los artículos tal como vienen de Crossref (simplificados).
        Task<List<ArticuloCrossrefDto>> BuscarArticulosAsync(
            string busqueda, int cantidad = 12);

        // Devuelve los mismos artículos convertidos al formato común
        // MediaItemDto, listos para mostrarse en Explorar.
        Task<List<MediaItemDto>> BuscarContenidoAsync(
            string busqueda, int cantidad = 12);
    }
}
