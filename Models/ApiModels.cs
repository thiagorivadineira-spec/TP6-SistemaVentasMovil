using System.Text.Json.Serialization;

namespace SistemaVentasMovil.Models
{
    // Clases que reflejan el JSON que devuelve / recibe la API (la misma de TP3)

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class RegistroRequest
    {
        public string Nombre { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Documento { get; set; }
        public string? Telefono { get; set; }
        public string? Direccion { get; set; }
    }

    public class AuthResponse
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
        public int? ClienteId { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime Expira { get; set; }
    }

    public class Paginado<T>
    {
        public int PaginaActual { get; set; }
        public int TamanoPagina { get; set; }
        public int TotalRegistros { get; set; }
        public int TotalPaginas { get; set; }
        public List<T> Datos { get; set; } = new();
    }

    public class ImagenProducto
    {
        public int Id { get; set; }
        public string Url { get; set; } = string.Empty;
    }

    public class Categoria
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public bool Activo { get; set; }
    }

    public class Producto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public decimal PrecioVenta { get; set; }
        public int StockActual { get; set; }
        public int PuntoReposicion { get; set; }
        public int StockMaximo { get; set; }
        public bool Activo { get; set; }
        public int CategoriaProductoId { get; set; }
        public string? CategoriaNombre { get; set; }
        public List<ImagenProducto> Imagenes { get; set; } = new();
    }

    /// <summary>Cuerpo para crear / editar un producto (solo Admin).</summary>
    public class ProductoRequest
    {
        public string Nombre { get; set; } = string.Empty;
        public string? Descripcion { get; set; }
        public decimal PrecioVenta { get; set; }
        public int StockActual { get; set; }
        public int PuntoReposicion { get; set; }
        public int StockMaximo { get; set; }
        public bool Activo { get; set; } = true;
        public int CategoriaProductoId { get; set; }
    }

    public class Proveedor
    {
        public int Id { get; set; }
        public string RazonSocial { get; set; } = string.Empty;
        public string? Cuit { get; set; }
        public bool Activo { get; set; }
    }

    // ---- Ventas ----
    public class DetalleVentaRequest
    {
        public int ProductoId { get; set; }
        public int Cantidad { get; set; }
    }

    public class VentaRequest
    {
        public int? ClienteId { get; set; }
        public List<DetalleVentaRequest> Detalles { get; set; } = new();
    }

    public class VentaCreada
    {
        public string? Mensaje { get; set; }
        public int VentaId { get; set; }
        public decimal Total { get; set; }
    }

    public class DetalleVentaDto
    {
        public int ProductoId { get; set; }
        public string NombreProducto { get; set; } = string.Empty;
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal { get; set; }
    }

    public class VentaDto
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public decimal Total { get; set; }
        public string NombreCliente { get; set; } = string.Empty;
        public string NombreUsuario { get; set; } = string.Empty;
        public List<DetalleVentaDto> Detalles { get; set; } = new();
    }

    // ---- Compras (ingreso de mercadería) ----
    public class DetalleCompraRequest
    {
        public int ProductoId { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
    }

    public class CompraRequest
    {
        public int ProveedorId { get; set; }
        public List<DetalleCompraRequest> Detalles { get; set; } = new();
    }

    public class CompraCreada
    {
        public int Id { get; set; }
        public decimal Total { get; set; }
        public string? ProveedorNombre { get; set; }
    }

    // ---- Auditoría, reportes, cuenta ----
    public class RegistroAuditoria
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public int? UsuarioId { get; set; }
        public string? Username { get; set; }
        public string Accion { get; set; } = string.Empty;
        public string? Entidad { get; set; }
        public int? EntidadId { get; set; }
        public string? Detalle { get; set; }
        public string? DireccionIp { get; set; }
    }

    public class EnviarReporteRequest
    {
        public string Tipo { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public DateTime? Desde { get; set; }
        public DateTime? Hasta { get; set; }
    }

    public class CambiarPasswordRequest
    {
        public string PasswordActual { get; set; } = string.Empty;
        public string PasswordNueva { get; set; } = string.Empty;
    }

    public class MensajeRespuesta
    {
        public string? Mensaje { get; set; }
    }

    // ---- Modelos solo de la app (no viajan a la API) ----

    /// <summary>Una línea del carrito. Guarda una "foto" del producto para poder mostrarlo sin conexión.</summary>
    public class LineaCarrito
    {
        public int ProductoId { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public decimal Precio { get; set; }
        public int Stock { get; set; }
        public string? ImagenUrl { get; set; }
        public int Cantidad { get; set; } = 1;

        [JsonIgnore]
        public decimal Subtotal => Cantidad * Precio;
    }

    /// <summary>Comprobante que se muestra al terminar una compra.</summary>
    public class Comprobante
    {
        public int VentaId { get; set; }
        public DateTime Fecha { get; set; }
        public decimal Total { get; set; }
        public string Pago { get; set; } = string.Empty;
        public string ReferenciaPago { get; set; } = string.Empty;
        public List<LineaCarrito> Lineas { get; set; } = new();
    }
}
