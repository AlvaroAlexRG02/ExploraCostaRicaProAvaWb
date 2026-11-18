using ExploraCostaRica.Models.DTOs;

namespace ExploraCostaRica.Services
{
    // ============================================================
    // API 6 - OPEN LIBRARY
    // ------------------------------------------------------------
    // Contrato del servicio que busca libros sobre Costa Rica,
    // su naturaleza y su cultura. Los controllers solo conocen
    // esta interfaz; la clase OpenLibraryApiService hace el
    // trabajo real.
    // ============================================================
    public interface IOpenLibraryApiService
    {
        // Devuelve los libros tal como vienen de Open Library (simplificados).
        Task<List<LibroOpenLibraryDto>> BuscarLibrosAsync(
            string busqueda, int cantidad = 12);

        // Devuelve los mismos libros convertidos al formato común
        // MediaItemDto, listos para mostrarse en Explorar.
        Task<List<MediaItemDto>> BuscarContenidoAsync(
            string busqueda, int cantidad = 12);
    }
}
