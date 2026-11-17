using System.Security.Claims;

namespace ExploraCostaRica.Extensions
{
    public static class ClaimsPrincipalExtensions
    {

        public static int? GetUserId(this ClaimsPrincipal user)
        {
            var valor = user.FindFirstValue(ClaimTypes.NameIdentifier);

            return int.TryParse(valor, out var id) ? id : null;
        }
    }
}
