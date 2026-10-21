using ExploraCostaRica.Models.ViewModels;

namespace ExploraCostaRica.Services
{
    public interface IDestinationService
    {
        // Pantalla principal: destacados + lista filtrada + provincias
        Task<DestinationIndexViewModel> GetIndexAsync(DestinationFilter filter);

        // Detalle de un destino activo con su contenido asociado.
        // Devuelve null si no existe o está inactivo.
        Task<DestinationDetailViewModel?> GetDetailAsync(int destinationId);

        // Sorpréndeme: destino activo elegido al azar en SQL Server.
        // excludeDestinationId evita repetir el destino anterior.
        // Devuelve null si no hay destinos activos.
        Task<DestinationDetailViewModel?> GetRandomAsync(int? excludeDestinationId = null);
    }
}