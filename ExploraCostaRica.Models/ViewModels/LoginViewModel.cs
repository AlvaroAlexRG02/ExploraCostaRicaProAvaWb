using System.ComponentModel.DataAnnotations;

namespace ExploraCostaRica.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Escribe tu correo.")]
        [EmailAddress(ErrorMessage = "El correo no tiene un formato válido.")]
        [StringLength(150)]
        [Display(Name = "Correo electrónico")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Escribe tu contraseña.")]
        [StringLength(100)]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; } = "";

        [Display(Name = "Mantener la sesión iniciada")]
        public bool Recordarme { get; set; }

        // Página a la que volver después de iniciar sesión.
        public string? ReturnUrl { get; set; }
    }
}
