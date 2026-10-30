namespace SistemaVentasMovil.Services
{
    /// <summary>Todo lo que toca archivos del teléfono: guardar/compartir/abrir PDFs y preparar fotos para subir.</summary>
    public class ArchivoService
    {
        private const int MaxBytesFoto = 5 * 1024 * 1024; // límite de la API
        private const int LadoMaximo = 1600;

        private readonly ErrorLog _log;

        public ArchivoService(ErrorLog log) => _log = log;

        /// <summary>Guarda el PDF en la carpeta temporal de la app y devuelve su ruta.</summary>
        public async Task<string> GuardarPdfAsync(string nombre, byte[] bytes)
        {
            var ruta = Path.Combine(FileSystem.CacheDirectory, nombre);
            await File.WriteAllBytesAsync(ruta, bytes);
            return ruta;
        }

        /// <summary>Abre el selector de Android para enviar el PDF por WhatsApp, Drive, correo, etc.</summary>
        public Task CompartirAsync(string ruta, string titulo) =>
            Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = titulo,
                File = new ShareFile(ruta, "application/pdf")
            });

        /// <summary>Abre el PDF con el visor que tenga el teléfono.</summary>
        public Task AbrirAsync(string ruta, string titulo) =>
            Launcher.Default.OpenAsync(new OpenFileRequest(titulo, new ReadOnlyFile(ruta, "application/pdf")));

        /// <summary>
        /// El usuario elige una foto de la galería. Se reduce (máx. 1600 px), se corrige la rotación y se pasa a JPG
        /// para que entre en el límite de 5 MB de la API. Devuelve null si cancela.
        /// </summary>
        public async Task<(byte[] Bytes, string Nombre, string Tipo)?> ElegirFotoAsync()
        {
            var fotos = await MediaPicker.Default.PickPhotosAsync(new MediaPickerOptions { Title = "Elegí una foto del producto" });
            var foto = fotos?.FirstOrDefault();
            if (foto == null) return null;

            await using var origen = await foto.OpenReadAsync();
            using var memoria = new MemoryStream();
            await origen.CopyToAsync(memoria);
            var original = memoria.ToArray();

            try
            {
                var reducida = Reducir(original);
                var nombre = Path.GetFileNameWithoutExtension(foto.FileName) + ".jpg";
                return (reducida, nombre, "image/jpeg");
            }
            catch (Exception ex)
            {
                _log.Registrar("Foto", ex, "No se pudo reducir la foto; se sube la original.");

                if (original.Length > MaxBytesFoto)
                    throw new InvalidOperationException("La foto pesa más de 5 MB y no se pudo reducir.");

                var ext = Path.GetExtension(foto.FileName).ToLowerInvariant();
                return (original, foto.FileName, ext == ".png" ? "image/png" : "image/jpeg");
            }
        }

        private static byte[] Reducir(byte[] original)
        {
#if ANDROID
            // Primero se leen solo las medidas, para no cargar en memoria una foto enorme completa
            var medidas = new Android.Graphics.BitmapFactory.Options { InJustDecodeBounds = true };
            Android.Graphics.BitmapFactory.DecodeByteArray(original, 0, original.Length, medidas);

            var muestreo = 1;
            while (Math.Max(medidas.OutWidth, medidas.OutHeight) / muestreo > LadoMaximo * 2) muestreo *= 2;

            var opciones = new Android.Graphics.BitmapFactory.Options { InSampleSize = muestreo };
            using var bitmap = Android.Graphics.BitmapFactory.DecodeByteArray(original, 0, original.Length, opciones)
                ?? throw new InvalidOperationException("No se pudo leer la imagen.");

            // Rotación que indica la cámara (EXIF)
            float grados = 0;
            using (var exif = new Android.Media.ExifInterface(new MemoryStream(original)))
            {
                grados = exif.GetAttributeInt(Android.Media.ExifInterface.TagOrientation, 1) switch
                {
                    6 => 90,
                    3 => 180,
                    8 => 270,
                    _ => 0
                };
            }

            var escala = Math.Min(1f, LadoMaximo / (float)Math.Max(bitmap.Width, bitmap.Height));
            var matriz = new Android.Graphics.Matrix();
            matriz.PostScale(escala, escala);
            if (grados != 0) matriz.PostRotate(grados);

            using var final = Android.Graphics.Bitmap.CreateBitmap(bitmap, 0, 0, bitmap.Width, bitmap.Height, matriz, true)
                ?? throw new InvalidOperationException("No se pudo reducir la imagen.");

            using var salida = new MemoryStream();
            final.Compress(Android.Graphics.Bitmap.CompressFormat.Jpeg!, 85, salida);
            return salida.ToArray();
#else
            return original;
#endif
        }
    }
}
