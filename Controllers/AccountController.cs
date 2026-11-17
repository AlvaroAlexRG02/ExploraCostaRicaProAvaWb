using System.Security.Claims;
using ExploraCostaRica.Models.Entities;
using ExploraCostaRica.Models.ViewModels;
using ExploraCostaRica.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ExploraCostaRica.Controllers
{
    public class AccountController : Controller
    {
        private readonly IAuthService _authService;

        public AccountController(IAuthService authService)
        {
            _authService = authService;
        }


        // ============================================================
        // INICIAR SESIÓN
        // ============================================================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectLocal(returnUrl);

            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel modelo)
        {
            if (!ModelState.IsValid)
                return View(modelo);

            var usuario = await _authService.ValidarCredencialesAsync(
                modelo.Email, modelo.Password);

            if (usuario == null)
            {

                ModelState.AddModelError(string.Empty,
                    "Correo o contraseña incorrectos.");

                modelo.Password = "";
                return View(modelo);
            }

            await IniciarSesionAsync(usuario, modelo.Recordarme);

            return RedirectLocal(modelo.ReturnUrl);
        }


        // ============================================================
        // REGISTRO
        // ============================================================

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
                return RedirectLocal(returnUrl);

            return View(new RegisterViewModel { ReturnUrl = returnUrl });
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel modelo)
        {
            if (!ModelState.IsValid)
                return View(modelo);

            var (usuario, error) = await _authService.RegistrarAsync(
                modelo.Name, modelo.Email, modelo.Password);

            if (usuario == null)
            {
                ModelState.AddModelError(string.Empty,
                    error ?? "No se pudo crear la cuenta.");

                modelo.Password = "";
                modelo.ConfirmPassword = "";
                return View(modelo);
            }


            await IniciarSesionAsync(usuario, recordarme: false);

            return RedirectLocal(modelo.ReturnUrl);
        }


        // ============================================================
        // CERRAR SESIÓN
        // ============================================================

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Index", "Home");
        }


        // ============================================================
        // SIN PERMISO
        // ============================================================

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }


        // ============================================================
        // AUXILIARES
        // ============================================================

        // Crea la cookie de sesión con UserId, nombre, correo y rol.
        private async Task IniciarSesionAsync(User usuario, bool recordarme)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, usuario.UserId.ToString()),
                new(ClaimTypes.Name, usuario.Name),
                new(ClaimTypes.Email, usuario.Email),
                new(ClaimTypes.Role, usuario.Role)
            };

            var identidad = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(identidad),
                new AuthenticationProperties
                {
                    IsPersistent = recordarme,
                    ExpiresUtc = recordarme
                        ? DateTimeOffset.UtcNow.AddDays(14)
                        : null
                });
        }


        private IActionResult RedirectLocal(string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }
    }
}
