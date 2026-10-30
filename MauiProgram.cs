using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;
using SistemaVentasMovil.Services;

namespace SistemaVentasMovil
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            // ---- Servicios de la app ----
            var log = new ErrorLog();
            builder.Services.AddSingleton(log);

            // Errores que nadie capturó: quedan en el log del teléfono en vez de pasar desapercibidos
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
                log.Registrar("Fatal", e.ExceptionObject as Exception, "Excepción no controlada.");
            TaskScheduler.UnobservedTaskException += (_, e) =>
            {
                log.Registrar("Tarea", e.Exception, "Excepción en segundo plano.");
                e.SetObserved();
            };

            builder.Services.AddSingleton<SesionService>();
            builder.Services.AddSingleton<CarritoService>();
            builder.Services.AddSingleton<PagoService>();
            builder.Services.AddSingleton<ArchivoService>();

            builder.Services.AddSingleton(_ => new HttpClient
            {
                BaseAddress = new Uri(AppConfig.ApiBaseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            });

            // Autenticación (para mostrar/ocultar pantallas según el rol)
            builder.Services.AddAuthorizationCore();
            builder.Services.AddScoped<AppAuthStateProvider>();
            builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<AppAuthStateProvider>());

            builder.Services.AddScoped<ApiClient>();

            return builder.Build();
        }
    }
}
