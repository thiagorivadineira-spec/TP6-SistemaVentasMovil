using System.Text.Json;
using SistemaVentasMovil.Models;

namespace SistemaVentasMovil.Services
{
    /// <summary>
    /// Carrito de compras. Se guarda en el teléfono (Preferences), así que sobrevive si se cierra la app.
    /// Nunca deja agregar más unidades que el stock conocido del producto.
    /// </summary>
    public class CarritoService
    {
        private const string Clave = "sistemaventas.carrito";
        private readonly ErrorLog _log;
        private List<LineaCarrito> _lineas = new();

        public event Action? Cambio;

        /// <summary>Último comprobante generado (para mostrarlo después de pagar).</summary>
        public Comprobante? UltimoComprobante { get; set; }

        public CarritoService(ErrorLog log)
        {
            _log = log;
            Cargar();
        }

        public IReadOnlyList<LineaCarrito> Lineas => _lineas;
        public int CantidadTotal => _lineas.Sum(l => l.Cantidad);
        public decimal Total => _lineas.Sum(l => l.Subtotal);

        /// <summary>Agrega unidades. Devuelve un mensaje de error, o null si salió bien.</summary>
        public string? Agregar(Producto p, string? imagenUrl, int cantidad = 1)
        {
            if (!p.Activo || p.StockActual <= 0) return "Este producto no tiene stock.";

            var linea = _lineas.FirstOrDefault(l => l.ProductoId == p.Id);
            var actual = linea?.Cantidad ?? 0;

            if (actual + cantidad > p.StockActual)
            {
                return actual >= p.StockActual
                    ? $"Ya tenés en el carrito todo el stock disponible ({p.StockActual})."
                    : $"Solo quedan {p.StockActual} unidades.";
            }

            if (linea == null)
            {
                _lineas.Add(new LineaCarrito
                {
                    ProductoId = p.Id, Nombre = p.Nombre, Precio = p.PrecioVenta,
                    Stock = p.StockActual, ImagenUrl = imagenUrl, Cantidad = cantidad
                });
            }
            else
            {
                linea.Cantidad += cantidad;
                linea.Stock = p.StockActual;
                linea.Precio = p.PrecioVenta;
            }

            Guardar();
            return null;
        }

        /// <summary>Cambia la cantidad de una línea sin pasarse del stock. Devuelve un mensaje si se limitó.</summary>
        public string? CambiarCantidad(int productoId, int nueva)
        {
            var linea = _lineas.FirstOrDefault(l => l.ProductoId == productoId);
            if (linea == null) return null;

            string? aviso = null;
            if (nueva > linea.Stock) { nueva = linea.Stock; aviso = $"Solo quedan {linea.Stock} unidades."; }

            if (nueva <= 0) _lineas.Remove(linea);
            else linea.Cantidad = nueva;

            Guardar();
            return aviso;
        }

        public void Quitar(int productoId)
        {
            _lineas.RemoveAll(l => l.ProductoId == productoId);
            Guardar();
        }

        public void Vaciar()
        {
            _lineas.Clear();
            Guardar();
        }

        /// <summary>
        /// Actualiza precio y stock con los datos reales del servidor.
        /// Devuelve la lista de avisos (productos que ya no están o cuya cantidad se ajustó).
        /// </summary>
        public List<string> Sincronizar(IDictionary<int, Producto?> actuales)
        {
            var avisos = new List<string>();

            foreach (var linea in _lineas.ToList())
            {
                actuales.TryGetValue(linea.ProductoId, out var p);

                if (p == null || !p.Activo || p.StockActual <= 0)
                {
                    avisos.Add($"«{linea.Nombre}» ya no tiene stock y se quitó del carrito.");
                    _lineas.Remove(linea);
                    continue;
                }

                if (linea.Precio != p.PrecioVenta)
                    avisos.Add($"El precio de «{linea.Nombre}» cambió a {Formato.Dinero(p.PrecioVenta)}.");

                linea.Precio = p.PrecioVenta;
                linea.Stock = p.StockActual;
                linea.Nombre = p.Nombre;

                if (linea.Cantidad > p.StockActual)
                {
                    avisos.Add($"De «{linea.Nombre}» solo quedan {p.StockActual}: se ajustó la cantidad.");
                    linea.Cantidad = p.StockActual;
                }
            }

            Guardar();
            return avisos;
        }

        private void Guardar()
        {
            try { Preferences.Default.Set(Clave, JsonSerializer.Serialize(_lineas)); }
            catch (Exception ex) { _log.Registrar("Carrito", ex); }
            Cambio?.Invoke();
        }

        private void Cargar()
        {
            try
            {
                var json = Preferences.Default.Get(Clave, string.Empty);
                _lineas = string.IsNullOrWhiteSpace(json)
                    ? new List<LineaCarrito>()
                    : JsonSerializer.Deserialize<List<LineaCarrito>>(json) ?? new List<LineaCarrito>();
            }
            catch (Exception ex)
            {
                _log.Registrar("Carrito", ex, "Carrito guardado ilegible: se reinicia.");
                _lineas = new List<LineaCarrito>();
            }
        }
    }
}
