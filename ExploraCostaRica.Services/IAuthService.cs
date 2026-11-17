using ExploraCostaRica.Models.Entities;

namespace ExploraCostaRica.Services
{
    public interface IAuthService
    {

        Task<(User? Usuario, string? Error)> RegistrarAsync(
            string nombre, string email, string password);


        Task<User?> ValidarCredencialesAsync(string email, string password);
    }
}
