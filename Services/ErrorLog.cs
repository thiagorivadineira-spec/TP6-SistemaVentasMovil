using System.Text;

namespace SistemaVentasMovil.Services
{
    /// <summary>
    /// Registro de errores de la app en un archivo del teléfono (errores-app.log).
    /// Los errores del servidor los registra la API (Serilog + tabla de auditoría);
    /// este log cubre lo que pasa dentro de la app: fallas de red, errores de pantalla, excepciones no controladas.
    /// </summary>
    public class ErrorLog
    {
        private static readonly object Candado = new();
        private const long TamanoMaximo = 512 * 1024; // 512 KB: si crece más, se reinicia

        private static string Ruta => Path.Combine(FileSystem.AppDataDirectory, "errores-app.log");

        public void Registrar(string origen, Exception? ex = null, string? mensaje = null)
        {
            try
            {
                var sb = new StringBuilder();
                sb.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")).Append(" [").Append(origen).Append("] ");
                if (!string.IsNullOrWhiteSpace(mensaje)) sb.Append(mensaje).Append(' ');
                if (ex != null) sb.Append(ex.GetType().Name).Append(": ").Append(ex.Message);
                sb.AppendLine();
                if (ex?.StackTrace != null) sb.AppendLine(ex.StackTrace);

                lock (Candado)
                {
                    var info = new FileInfo(Ruta);
                    if (info.Exists && info.Length > TamanoMaximo) File.Delete(Ruta);
                    File.AppendAllText(Ruta, sb.ToString());
                }

                System.Diagnostics.Debug.WriteLine(sb.ToString());
            }
            catch
            {
                // el log nunca debe romper la app
            }
        }

        public string Leer()
        {
            try
            {
                lock (Candado) return File.Exists(Ruta) ? File.ReadAllText(Ruta) : string.Empty;
            }
            catch { return string.Empty; }
        }

        public void Limpiar()
        {
            try { lock (Candado) { if (File.Exists(Ruta)) File.Delete(Ruta); } }
            catch { /* nada */ }
        }
    }
}
