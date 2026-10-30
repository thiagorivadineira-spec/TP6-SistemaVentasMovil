using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace SistemaVentasMovil.Services
{
    /// <summary>
    /// Le dice a Blazor quién es el usuario actual y cuál es su rol, a partir de la sesión guardada.
    /// Sirve para mostrar u ocultar pantallas; la seguridad real la aplica la API con el token.
    /// </summary>
    public class AppAuthStateProvider : AuthenticationStateProvider
    {
        private readonly SesionService _sesion;

        public AppAuthStateProvider(SesionService sesion) => _sesion = sesion;

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var sesion = await _sesion.ObtenerAsync();

            if (sesion == null)
            {
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, sesion.Id.ToString()),
                new Claim(ClaimTypes.Name, sesion.Nombre),
                new Claim(ClaimTypes.Role, sesion.Rol)
            };

            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt")));
        }

        public async Task IniciarSesionAsync(SesionUsuario sesion)
        {
            await _sesion.GuardarAsync(sesion);
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        public async Task CerrarSesionAsync()
        {
            await _sesion.CerrarAsync();
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }
    }
}
