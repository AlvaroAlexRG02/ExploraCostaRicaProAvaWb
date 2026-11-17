using ExploraCostaRica.Models.ViewModels;

namespace ExploraCostaRica.Services
{
    public interface IExplorarService
    {
        // userId: el usuario que inició sesión (null si nadie inició sesión).
        // Se usa para guardar su historial de búsquedas (SearchHistory).
        Task<ExplorarViewModel> BuscarAsync(ExplorarFiltro filtro, int? userId);
    }
}