using ExploraCostaRica.Models;

namespace ExploraCostaRica.Services
{
    public interface IExplorarService
    {
        Task<ExplorarViewModel> BuscarAsync(ExplorarFiltro filtro);
    }
}
