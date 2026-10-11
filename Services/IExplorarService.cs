using ExploraCostaRica.Models;

namespace ExploraCostaRica.Services
{
    // ============================================================
    // CONTRATO DEL SERVICIO DE EXPLORAR
    // ============================================================
    // El controller solo conoce esta interfaz.
    // No sabe cómo se busca ni de dónde salen los datos:
    // eso lo decide la clase que la implementa (ExplorarService).
    //
    // Así cumplimos la regla del enunciado:
    // "No puede haber lógica de negocio en los controladores".
    // ============================================================
    public interface IExplorarService
    {
        Task<ExplorarViewModel> BuscarAsync(ExplorarFiltro filtro);
    }
}
