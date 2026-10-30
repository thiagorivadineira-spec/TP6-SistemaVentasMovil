using System.Text.Json;

namespace SistemaVentasMovil.Services
{
    public class SesionUsuario
    {
        public string Token { get; set; } = string.Empty;
        public DateTime Expira { get; set; }
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public int? ClienteId { get; set; }
    }

    /// <summary>
    /// Guarda la sesión (token JWT + datos del usuario) en SecureStorage, el almacén cifrado de Android.
    /// Si el token venció, la sesión se descarta sola.
    /// </summary>
    public class SesionService
    {
        private const string Clave = "sistemaventas.sesion";
        private readonly ErrorLog _log;
        private SesionUsuario? _sesion;
        private bool _cargada;

        public SesionService(ErrorLog log) => _log = log;

        public async Task<SesionUsuario?> ObtenerAsync()
        {
            if (!_cargada)
            {
                _cargada = true;
                try
                {
                    var json = await SecureStorage.Default.GetAsync(Clave);
                    _sesion = string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<SesionUsuario>(json);
                }
                catch (Exception ex)
                {
                    // Puede pasar tras restaurar un backup o cambiar de bloqueo de pantalla: se empieza de cero
                    _log.Registrar("Sesion", ex, "No se pudo leer la sesión guardada.");
                    SecureStorage.Default.RemoveAll();
                    _sesion = null;
                }
            }

            if (_sesion != null && _sesion.Expira.ToUniversalTime() <= DateTime.UtcNow)
            {
                await CerrarAsync();
            }

            return _sesion;
        }

        public async Task GuardarAsync(SesionUsuario sesion)
        {
            _sesion = sesion;
            _cargada = true;
            try { await SecureStorage.Default.SetAsync(Clave, JsonSerializer.Serialize(sesion)); }
            catch (Exception ex) { _log.Registrar("Sesion", ex, "No se pudo guardar la sesión (queda solo en memoria)."); }
        }

        public Task CerrarAsync()
        {
            _sesion = null;
            _cargada = true;
            try { SecureStorage.Default.Remove(Clave); }
            catch (Exception ex) { _log.Registrar("Sesion", ex); }
            return Task.CompletedTask;
        }
    }
}
