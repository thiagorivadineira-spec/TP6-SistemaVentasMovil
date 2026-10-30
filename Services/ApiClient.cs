using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using SistemaVentasMovil.Models;

namespace SistemaVentasMovil.Services
{
    /// <summary>Resultado de una llamada a la API: o trae datos, o trae un mensaje de error listo para mostrar.</summary>
    public class ApiResultado<T>
    {
        public bool Ok { get; init; }
        public T? Datos { get; init; }
        public string? Error { get; init; }

        public static ApiResultado<T> Exito(T? datos) => new() { Ok = true, Datos = datos };
        public static ApiResultado<T> Falla(string error) => new() { Ok = false, Error = error };
    }

    /// <summary>
    /// Único lugar donde la app habla con la API: arma las URL, agrega el token JWT,
    /// interpreta los errores, los registra en el log local y, si el token venció (401), vuelve al login.
    /// </summary>
    public class ApiClient
    {
        private readonly HttpClient _http;
        private readonly SesionService _sesion;
        private readonly AppAuthStateProvider _auth;
        private readonly NavigationManager _nav;
        private readonly ErrorLog _log;

        public ApiClient(HttpClient http, SesionService sesion, AppAuthStateProvider auth, NavigationManager nav, ErrorLog log)
        {
            _http = http;
            _sesion = sesion;
            _auth = auth;
            _nav = nav;
            _log = log;
        }

        // ---------------- Autenticación ----------------
        public Task<ApiResultado<AuthResponse>> LoginAsync(string username, string password) =>
            EnviarAsync<AuthResponse>(HttpMethod.Post, "api/Auth/Login",
                new LoginRequest { Username = username, Password = password }, requiereSesion: false);

        public Task<ApiResultado<MensajeRespuesta>> RegistrarAsync(RegistroRequest pedido) =>
            EnviarAsync<MensajeRespuesta>(HttpMethod.Post, "api/Auth/Registro", pedido, requiereSesion: false);

        public Task<ApiResultado<bool>> CambiarPasswordAsync(string actual, string nueva) =>
            EnviarSinRespuestaAsync(HttpMethod.Post, "api/Auth/CambiarPassword",
                new CambiarPasswordRequest { PasswordActual = actual, PasswordNueva = nueva });

        // ---------------- Catálogo ----------------
        public Task<ApiResultado<Paginado<Producto>>> GetProductosAsync(
            string? buscar, int pagina = 1, int tamano = 20, string ordenarPor = "nombre") =>
            EnviarAsync<Paginado<Producto>>(HttpMethod.Get, "api/Productos" + Query(
                ("pagina", pagina.ToString()),
                ("tamanoPagina", tamano.ToString()),
                ("buscar", buscar),
                ("ordenarPor", ordenarPor)), requiereSesion: false, enviarTokenSiHay: true);

        public Task<ApiResultado<Producto>> GetProductoAsync(int id) =>
            EnviarAsync<Producto>(HttpMethod.Get, $"api/Productos/{id}", requiereSesion: false, enviarTokenSiHay: true);

        public Task<ApiResultado<List<Categoria>>> GetCategoriasAsync() =>
            EnviarAsync<List<Categoria>>(HttpMethod.Get, "api/CategoriasProductos", requiereSesion: false);

        // ---------------- Productos (Admin) ----------------
        public Task<ApiResultado<Producto>> CrearProductoAsync(ProductoRequest pedido) =>
            EnviarAsync<Producto>(HttpMethod.Post, "api/Productos", pedido);

        public Task<ApiResultado<bool>> ActualizarProductoAsync(int id, ProductoRequest pedido) =>
            EnviarSinRespuestaAsync(HttpMethod.Put, $"api/Productos/{id}", pedido);

        /// <summary>Sube una foto (jpg/png, máx. 5 MB). El campo multipart se llama "archivo".</summary>
        public Task<ApiResultado<bool>> SubirImagenAsync(int productoId, byte[] bytes, string nombreArchivo, string tipoContenido)
        {
            var contenido = new MultipartFormDataContent();
            var archivo = new ByteArrayContent(bytes);
            archivo.Headers.ContentType = new MediaTypeHeaderValue(tipoContenido);
            contenido.Add(archivo, "archivo", nombreArchivo);
            return EnviarSinRespuestaAsync(HttpMethod.Post, $"api/Productos/{productoId}/imagenes", contenido);
        }

        public Task<ApiResultado<Paginado<Proveedor>>> GetProveedoresAsync() =>
            EnviarAsync<Paginado<Proveedor>>(HttpMethod.Get, "api/Proveedores" + Query(
                ("pagina", "1"), ("tamanoPagina", "50"), ("ordenarPor", "nombre")));

        // ---------------- Ventas y compras ----------------
        public Task<ApiResultado<VentaCreada>> CrearVentaAsync(VentaRequest pedido) =>
            EnviarAsync<VentaCreada>(HttpMethod.Post, "api/Ventas", pedido);

        public Task<ApiResultado<Paginado<VentaDto>>> GetVentasAsync(int pagina, int tamano = 10) =>
            EnviarAsync<Paginado<VentaDto>>(HttpMethod.Get, "api/Ventas" + Query(
                ("pagina", pagina.ToString()), ("tamanoPagina", tamano.ToString()), ("ordenarPor", "fecha_desc")));

        public Task<ApiResultado<CompraCreada>> CrearCompraAsync(CompraRequest pedido) =>
            EnviarAsync<CompraCreada>(HttpMethod.Post, "api/Compras", pedido);

        // ---------------- Auditoría ----------------
        public Task<ApiResultado<Paginado<RegistroAuditoria>>> GetAuditoriaAsync(
            int pagina, int tamano, string? accion, string? buscar) =>
            EnviarAsync<Paginado<RegistroAuditoria>>(HttpMethod.Get, "api/Auditoria" + Query(
                ("pagina", pagina.ToString()),
                ("tamanoPagina", tamano.ToString()),
                ("accion", accion),
                ("buscar", buscar)));

        // ---------------- Reportes ----------------
        public Task<ApiResultado<byte[]>> DescargarReporteAsync(string tipo, DateTime? desde, DateTime? hasta) =>
            EjecutarAsync<byte[]>(HttpMethod.Get,
                $"api/Reportes/{tipo}" + Query(("desde", Fecha(desde)), ("hasta", Fecha(hasta))),
                null, true, false,
                async contenido => await contenido.ReadAsByteArrayAsync());

        public Task<ApiResultado<MensajeRespuesta>> EnviarReporteAsync(
            string tipo, string email, DateTime? desde, DateTime? hasta) =>
            EnviarAsync<MensajeRespuesta>(HttpMethod.Post, "api/Reportes/enviar",
                new EnviarReporteRequest { Tipo = tipo, Email = email, Desde = desde, Hasta = hasta });

        // ---------------- Utilidades ----------------
        /// <summary>Convierte la ruta de una imagen de la API ("/uploads/x.png") en una URL completa.</summary>
        public string? UrlImagen(string? rutaRelativa) =>
            string.IsNullOrWhiteSpace(rutaRelativa)
                ? null
                : new Uri(_http.BaseAddress!, rutaRelativa.TrimStart('/')).ToString();

        public string? PrimeraImagen(Producto p) => UrlImagen(p.Imagenes.FirstOrDefault()?.Url);

        private static string? Fecha(DateTime? fecha) => fecha?.ToString("yyyy-MM-dd");

        private static string Query(params (string clave, string? valor)[] parametros) =>
            "?" + string.Join("&", parametros
                .Where(p => !string.IsNullOrWhiteSpace(p.valor))
                .Select(p => $"{p.clave}={Uri.EscapeDataString(p.valor!)}"));

        // ---------------- Núcleo de las llamadas ----------------
        private Task<ApiResultado<T>> EnviarAsync<T>(
            HttpMethod metodo, string url, object? cuerpo = null, bool requiereSesion = true, bool enviarTokenSiHay = false) =>
            EjecutarAsync<T>(metodo, url, cuerpo, requiereSesion, enviarTokenSiHay,
                async contenido => await contenido.ReadFromJsonAsync<T>());

        private async Task<ApiResultado<bool>> EnviarSinRespuestaAsync(HttpMethod metodo, string url, object? cuerpo = null)
        {
            var r = await EjecutarAsync<bool>(metodo, url, cuerpo, true, false, _ => Task.FromResult(true));
            return r.Ok ? ApiResultado<bool>.Exito(true) : ApiResultado<bool>.Falla(r.Error ?? "Error desconocido.");
        }

        private async Task<ApiResultado<T>> EjecutarAsync<T>(
            HttpMethod metodo, string url, object? cuerpo, bool requiereSesion, bool enviarTokenSiHay,
            Func<HttpContent, Task<T?>> leerContenido)
        {
            try
            {
                using var peticion = new HttpRequestMessage(metodo, url);

                if (cuerpo is HttpContent contenidoDirecto)
                {
                    peticion.Content = contenidoDirecto;
                }
                else if (cuerpo != null)
                {
                    peticion.Content = JsonContent.Create(cuerpo, cuerpo.GetType());
                }

                if (requiereSesion || enviarTokenSiHay)
                {
                    var sesion = await _sesion.ObtenerAsync();

                    if (sesion != null)
                    {
                        peticion.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sesion.Token);
                    }
                    else if (requiereSesion)
                    {
                        await _auth.CerrarSesionAsync();
                        _nav.NavigateTo("login");
                        return ApiResultado<T>.Falla("Necesitás iniciar sesión.");
                    }
                }

                using var respuesta = await _http.SendAsync(peticion);

                if (respuesta.IsSuccessStatusCode)
                {
                    if (respuesta.StatusCode == HttpStatusCode.NoContent)
                    {
                        return ApiResultado<T>.Exito(default);
                    }

                    return ApiResultado<T>.Exito(await leerContenido(respuesta.Content));
                }

                if (respuesta.StatusCode == HttpStatusCode.Unauthorized && (requiereSesion || enviarTokenSiHay)
                    && peticion.Headers.Authorization != null)
                {
                    await _auth.CerrarSesionAsync();
                    _nav.NavigateTo("login");
                    return ApiResultado<T>.Falla("Tu sesión expiró. Iniciá sesión nuevamente.");
                }

                var error = await LeerErrorAsync(respuesta);
                _log.Registrar("API", null, $"{metodo} {url} -> {(int)respuesta.StatusCode}: {error}");
                return ApiResultado<T>.Falla(error);
            }
            catch (HttpRequestException ex)
            {
                _log.Registrar("Red", ex, $"{metodo} {url}");
                return ApiResultado<T>.Falla("No se pudo conectar con el servidor. Revisá tu conexión a internet.");
            }
            catch (TaskCanceledException ex)
            {
                _log.Registrar("Red", ex, $"Tiempo agotado: {metodo} {url}");
                return ApiResultado<T>.Falla("El servidor tardó demasiado en responder. Probá de nuevo.");
            }
            catch (JsonException ex)
            {
                _log.Registrar("API", ex, $"Formato inesperado: {metodo} {url}");
                return ApiResultado<T>.Falla("La respuesta del servidor no tiene el formato esperado.");
            }
        }

        /// <summary>Extrae un mensaje legible de las distintas formas de error que devuelve la API.</summary>
        private static async Task<string> LeerErrorAsync(HttpResponseMessage respuesta)
        {
            var texto = string.Empty;

            try { texto = await respuesta.Content.ReadAsStringAsync(); }
            catch (Exception) { /* se usa el mensaje genérico de más abajo */ }

            if (!string.IsNullOrWhiteSpace(texto))
            {
                try
                {
                    using var documento = JsonDocument.Parse(texto);
                    var raiz = documento.RootElement;

                    if (raiz.ValueKind == JsonValueKind.Object)
                    {
                        if (raiz.TryGetProperty("mensaje", out var mensaje) && mensaje.ValueKind == JsonValueKind.String)
                            return mensaje.GetString()!;

                        if (raiz.TryGetProperty("errors", out var errores) && errores.ValueKind == JsonValueKind.Object)
                        {
                            var lista = new List<string>();
                            foreach (var campo in errores.EnumerateObject())
                            {
                                if (campo.Value.ValueKind != JsonValueKind.Array) continue;
                                foreach (var item in campo.Value.EnumerateArray())
                                    if (item.ValueKind == JsonValueKind.String) lista.Add(item.GetString()!);
                            }
                            if (lista.Count > 0) return string.Join(" ", lista);
                        }

                        if (raiz.TryGetProperty("title", out var titulo) && titulo.ValueKind == JsonValueKind.String)
                            return titulo.GetString()!;
                    }
                    else if (raiz.ValueKind == JsonValueKind.String)
                    {
                        return raiz.GetString()!;
                    }
                }
                catch (JsonException)
                {
                    if (texto.Length < 200) return texto;
                }
            }

            return respuesta.StatusCode switch
            {
                HttpStatusCode.Forbidden => "No tenés permiso para realizar esta acción.",
                HttpStatusCode.NotFound => "No se encontró el recurso solicitado.",
                _ => $"Error del servidor ({(int)respuesta.StatusCode})."
            };
        }
    }
}
