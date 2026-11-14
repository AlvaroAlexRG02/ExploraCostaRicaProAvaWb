using ExploraCostaRica.Models.DTOs;

namespace ExploraCostaRica.Services
{
    public interface IDashboardSyncService
    {
        // Consulta las APIs propias del proyecto (GNews, Unsplash e iNaturalist),
        // las convierte al formato común v1.0 y las guarda en el dashboard.
        Task<DashboardImportResult> SincronizarAsync(
            CancellationToken ct = default);
    }
}
