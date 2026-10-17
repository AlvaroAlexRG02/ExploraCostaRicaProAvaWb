using ExploraCostaRica.Models.ViewModels;

namespace ExploraCostaRica.Services
{
    // ============================================================
    // CONTRATO DEL SERVICIO DE DESTINOS Y SORPRÉNDEME
    // ============================================================
    // El controller solo conoce esta interfaz. Las consultas y
    // la selección aleatoria viven en DestinationService.
    //
    // Así cumplimos la regla del enunciado:
    // "No puede haber lógica de negocio en los controladores".
    // ============================================================
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