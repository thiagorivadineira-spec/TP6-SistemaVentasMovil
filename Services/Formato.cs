using System.Globalization;
using SistemaVentasMovil.Models;

namespace SistemaVentasMovil.Services
{
    /// <summary>Formatos de dinero y fechas en español de Argentina, y estado de stock de un producto.</summary>
    public static class Formato
    {
        private static readonly CultureInfo Cultura = Obtener();

        public static string Dinero(decimal valor) => "$ " + valor.ToString("N2", Cultura);

        public static string Fecha(DateTime fecha) => fecha.ToString("dd/MM/yyyy HH:mm", Cultura);

        /// <summary>Clase CSS y texto para mostrar la disponibilidad como en un e-commerce.</summary>
        public static (string Clase, string Texto) EstadoStock(Producto p)
        {
            if (p.StockActual <= 0) return ("sin", "Sin stock");
            if (p.StockActual <= Math.Max(p.PuntoReposicion, 3)) return ("bajo", $"¡Últimas {p.StockActual} unidades!");
            return ("ok", $"Disponible · {p.StockActual} en stock");
        }

        private static CultureInfo Obtener()
        {
            try { return new CultureInfo("es-AR"); }
            catch (CultureNotFoundException) { return CultureInfo.InvariantCulture; }
        }
    }
}
