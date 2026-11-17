using ExploraCostaRica.Data;
using ExploraCostaRica.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ExploraCostaRica.Services
{
    public class AuthService : IAuthService
    {
        private const string ErrorGenerico =
            "No se pudo crear la cuenta. Revisa los datos e intenta de nuevo.";

        private readonly ExploraCostaRicaDbContext _context;
        private readonly PasswordHasher<User> _hasher = new();

        public AuthService(ExploraCostaRicaDbContext context)
        {
            _context = context;
        }

        // ============================================================
        // REGISTRO
        // ============================================================

        public async Task<(User? Usuario, string? Error)> RegistrarAsync(
            string nombre, string email, string password)
        {
            nombre = (nombre ?? "").Trim();
            email = Normalizar(email);

            if (nombre.Length < 2 || nombre.Length > 100)
                return (null, "El nombre debe tener entre 2 y 100 caracteres.");

            if (email.Length == 0 || email.Length > 150)
                return (null, "El correo no es válido.");

            var errorPassword = ValidarPassword(password);
            if (errorPassword != null)
                return (null, errorPassword);

            var existe = await _context.Users
                .AnyAsync(u => u.Email == email);

            if (existe)
                return (null, "Ya existe una cuenta con ese correo.");

            var usuario = new User
            {
                Name = nombre,
                Email = email,
                IsActive = true,
                Role = Roles.Usuario,
                CreatedAt = DateTime.Now
            };

            usuario.PasswordHash = _hasher.HashPassword(usuario, password);

            _context.Users.Add(usuario);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {

                _context.ChangeTracker.Clear();
                return (null, "Ya existe una cuenta con ese correo.");
            }

            return (usuario, null);
        }


        // ============================================================
        // INICIO DE SESIÓN
        // ============================================================

        public async Task<User?> ValidarCredencialesAsync(
            string email, string password)
        {
            email = Normalizar(email);

            var usuario = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email && u.IsActive);

            if (usuario == null || string.IsNullOrEmpty(usuario.PasswordHash))
            {

                _hasher.HashPassword(new User(), password ?? "");
                return null;
            }

            var resultado = _hasher.VerifyHashedPassword(
                usuario, usuario.PasswordHash, password ?? "");

            if (resultado == PasswordVerificationResult.Failed)
                return null;

            if (resultado == PasswordVerificationResult.SuccessRehashNeeded)
            {

                usuario.PasswordHash = _hasher.HashPassword(usuario, password!);

                try { await _context.SaveChangesAsync(); }
                catch (DbUpdateException) { _context.ChangeTracker.Clear(); }
            }

            return usuario;
        }


        // ============================================================
        // AUXILIARES
        // ============================================================

        private static string Normalizar(string? email)
        {
            return (email ?? "").Trim().ToLowerInvariant();
        }


        private static string? ValidarPassword(string? password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 8)
                return "La contraseña debe tener al menos 8 caracteres.";

            if (password.Length > 100)
                return "La contraseña no puede pasar de 100 caracteres.";

            if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit))
                return "La contraseña debe tener al menos una letra y un número.";

            return null;
        }
    }
}
