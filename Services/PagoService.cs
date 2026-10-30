namespace SistemaVentasMovil.Services
{
    public class DatosTarjeta
    {
        public string Titular { get; set; } = string.Empty;
        public string Numero { get; set; } = string.Empty;      // solo dígitos
        public string Vencimiento { get; set; } = string.Empty; // MM/AA
        public string Cvv { get; set; } = string.Empty;
    }

    public class ResultadoPago
    {
        public bool Aprobado { get; init; }
        public string Mensaje { get; init; } = string.Empty;
        public string Referencia { get; init; } = string.Empty;
        public string TarjetaEnmascarada { get; init; } = string.Empty;
    }

    /// <summary>
    /// PAGO SIMULADO. Valida los datos de la tarjeta como lo haría un checkout real (largo, algoritmo de Luhn,
    /// vencimiento, CVV) y "aprueba" o "rechaza" sin conectarse a ninguna pasarela de pagos.
    /// Los datos de la tarjeta NO se envían a la API ni se guardan en ningún lado.
    ///
    /// Tarjetas de prueba:  4242 4242 4242 4242 (aprobada)  ·  4000 0000 0000 0002 (rechazada)
    /// </summary>
    public class PagoService
    {
        public static string Marca(string numero)
        {
            if (numero.StartsWith('4')) return "Visa";
            if (numero.Length >= 2 && int.TryParse(numero.AsSpan(0, 2), out var dos))
            {
                if (dos is >= 51 and <= 55) return "Mastercard";
                if (dos is 34 or 37) return "American Express";
            }
            return "Tarjeta";
        }

        public static bool LuhnValido(string numero)
        {
            if (numero.Length < 13 || numero.Any(c => !char.IsDigit(c))) return false;

            var suma = 0;
            var duplicar = false;

            for (var i = numero.Length - 1; i >= 0; i--)
            {
                var d = numero[i] - '0';
                if (duplicar) { d *= 2; if (d > 9) d -= 9; }
                suma += d;
                duplicar = !duplicar;
            }

            return suma % 10 == 0;
        }

        /// <summary>Devuelve la lista de problemas de los datos ingresados (vacía si está todo bien).</summary>
        public List<string> Validar(DatosTarjeta t)
        {
            var errores = new List<string>();

            if (string.IsNullOrWhiteSpace(t.Titular) || t.Titular.Trim().Length < 3)
                errores.Add("Ingresá el nombre del titular tal como figura en la tarjeta.");

            if (t.Numero.Length < 13 || t.Numero.Length > 19 || !LuhnValido(t.Numero))
                errores.Add("El número de tarjeta no es válido.");

            var venc = t.Vencimiento.Split('/');
            if (venc.Length != 2 || !int.TryParse(venc[0], out var mes) || !int.TryParse(venc[1], out var anio)
                || mes < 1 || mes > 12 || venc[1].Length != 2)
            {
                errores.Add("El vencimiento debe tener el formato MM/AA.");
            }
            else
            {
                var finDeMes = new DateTime(2000 + anio, mes, 1).AddMonths(1);
                if (finDeMes <= DateTime.Today) errores.Add("La tarjeta está vencida.");
            }

            var largoCvv = Marca(t.Numero) == "American Express" ? 4 : 3;
            if (t.Cvv.Length != largoCvv || t.Cvv.Any(c => !char.IsDigit(c)))
                errores.Add($"El código de seguridad debe tener {largoCvv} dígitos.");

            return errores;
        }

        public async Task<ResultadoPago> ProcesarAsync(DatosTarjeta t, decimal monto)
        {
            await Task.Delay(1500); // simula la demora de la pasarela

            var enmascarada = $"{Marca(t.Numero)} •••• {t.Numero[^4..]}";

            if (t.Numero.EndsWith("0002"))
            {
                return new ResultadoPago
                {
                    Aprobado = false,
                    Mensaje = "El banco rechazó la tarjeta (simulado). Probá con otra.",
                    TarjetaEnmascarada = enmascarada
                };
            }

            return new ResultadoPago
            {
                Aprobado = true,
                Mensaje = "Pago aprobado (simulado).",
                Referencia = "SIM-" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(),
                TarjetaEnmascarada = enmascarada
            };
        }
    }
}
