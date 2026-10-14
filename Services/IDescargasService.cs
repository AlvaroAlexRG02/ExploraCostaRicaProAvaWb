
using ExploraCostaRica.Models.DTOs;
using ExploraCostaRica.Models.Entities;

namespace ExploraCostaRica.Services
{
    public interface IDescargasService
    {
        // Obtener usuario de demostración
        Task<User?> ObtenerUsuarioAsync();

        // Consultar contenidos activos
        Task<List<MediaItem>> ObtenerContenidosAsync();

        // Obtener historial de descargas
        Task<List<Download>> ObtenerHistorialAsync(
            int usuarioId);

        // Generar JSON de un contenido
        Task<string?> GenerarJsonAsync(
            long mediaId);

        // Registrar descarga en SQL Server
        Task RegistrarDescargaAsync(
            int usuarioId,
            long mediaId,
            string nombreArchivo);

        // Buscar descarga del usuario
        Task<Download?> ObtenerDescargaAsync(
            long descargaId,
            int usuarioId);

        // Eliminar descarga del historial
        Task<bool> EliminarDescargaAsync(
            long descargaId,
            int usuarioId);

        // Importar JSON y guardar contenido en SQL Server
        Task<ResultadoImportacionDto> ImportarJsonAsync(
            Stream archivo);
    }
}
