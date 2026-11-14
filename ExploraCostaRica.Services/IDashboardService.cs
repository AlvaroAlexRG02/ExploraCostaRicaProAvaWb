using ExploraCostaRica.Models.DTOs;
using ExploraCostaRica.Models.Exchange;
using ExploraCostaRica.Models.ViewModels;

namespace ExploraCostaRica.Services
{
    public interface IDashboardService
    {
        // Pantalla principal: items filtrados y paginados + totales y fuentes.
        Task<DashboardViewModel> ObtenerAsync(
            DashboardFilter filtro,
            CancellationToken ct = default);

        // Importar un archivo del formato común v1.0 (pasos 1 a 4 de la sección 8).
        Task<DashboardImportResult> ImportarAsync(
            Stream archivo,
            CancellationToken ct = default);

        // Guarda en SQL Server las fuentes e items de un DashboardExport.
        // Si el item ya existe (misma fuente e id) se reemplaza.
        Task<DashboardImportResult> GuardarAsync(
            DashboardExport export,
            CancellationToken ct = default);

        // Exporta todos los items que cumplen el filtro (sin paginar).
        Task<DashboardExport> ExportarAsync(
            DashboardFilter? filtro,
            CancellationToken ct = default);

        // Exporta un solo item (con su fuente).
        Task<DashboardExport?> ExportarItemAsync(
            int itemId,
            CancellationToken ct = default);

        // JSON del item tal como se guardó (para verlo en el dashboard).
        Task<string?> ObtenerJsonItemAsync(
            int itemId,
            CancellationToken ct = default);

        // Encabezado de un archivo nuevo (version, exportedAt, exportedBy).
        DashboardExport NuevoExport();
    }
}
