using ExploraCostaRica.Models.ViewModels;

namespace ExploraCostaRica.Services
{
    public interface IExplorarService
    {
        Task<ExplorarViewModel> BuscarAsync(ExplorarFiltro filtro);
    }
}
