# Sistema de Ventas – App móvil "Changomas" (TP6)

Trabajo Práctico de Programación Móvil N° 6. Aplicación Android de e-commerce hecha con **.NET MAUI Blazor Hybrid**: catálogo tipo Mercado Libre con disponibilidad de stock, carrito, pago simulado con tarjeta, ingreso de mercadería, reportes PDF (consulta y envío por correo), control de acceso por roles y registro de auditoría y errores.
## Links para verificar el trabajo

| Qué | Link |
|---|---|
| Repositorio de la app móvil | https://github.com/TU-USUARIO/TP6-SistemaVentasMovil |
| API (backend, HTTPS) | https://tp3.runasp.net |
| Documentación de la API (Swagger) | https://tp3.runasp.net/swagger/index.html
Los usuarios de prueba (administrador y cajero) se entregan junto con el trabajo. Un cliente se crea desde la propia app con "Crear cuenta".

## Arquitectura

```
App Android (Blazor Hybrid / .NET MAUI)
        │
        └──HTTPS + token JWT──> API: ASP.NET Core (.NET 10, MonsterASP.NET)
                                      │
                                      └──> SQL Server (base única, 3FN)
```

La app reutiliza **la misma API y la misma base de datos** del TP3 (punto de venta web). No tiene base propia: todo el stock, las ventas y los usuarios son los de la API

## Roles y permisos en la app

| Acción | Sin sesión | Cliente | Cajero | Administrador |
|---|---|---|---|---|
| Ver catálogo y detalle | Sí | Sí | Sí | Sí |
| Usar el carrito | Sí | Sí | Sí | Sí |
| Pagar / registrar la compra | No | Sí, a su nombre | Sí | Sí |
| Ver historial | No | Sus compras | Las ventas que registró | Todas |
| Stock, ingreso, productos, reportes, auditoría | No | No | No | Sí |

## Seguridad

- Contraseñas con hash en el servidor; autenticación con token JWT de 2 horas.
- El token se guarda en **SecureStorage** (almacén cifrado de Android) y la sesión se cierra sola al vencer.
- Toda la comunicación usa HTTPS.
- El usuario de cada venta sale del token, no de lo que envía la app.
- No hay credenciales ni claves dentro de la app ni del repositorio.

- ## Cómo probarlo

1. ejecutar desde Visual Studio con el celular por USB.
2. **Cliente:** crear cuenta, agregar productos al carrito, pagar con la tarjeta de prueba.
   - `4242 4242 4242 4242` → pago aprobado
   - `4000 0000 0000 0002` → pago rechazado
   - Vencimiento futuro (ej. `12/30`) y CVV de 3 dígitos
3. **Administrador:** ingresar con el usuario `admin` y usar la pestaña **Admin** (stock, ingreso de mercadería, productos y fotos, reportes, auditoría).

> La API está en un hosting gratuito que "duerme" cuando no se usa: la primera llamada puede tardar unos segundos.
> El envío de reportes por correo depende del SMTP del hosting; si no está disponible, el PDF puede compartirse igual desde el teléfono.
>
> ## Ejecutarlo desde el código

Requisitos: Visual Studio con la carga de trabajo ".NET Multi-platform App UI development", SDK de .NET 10 y un celular Android con depuración USB (o un emulador).

1. Clonar el repositorio y abrir la solución.
2. Verificar la dirección de la API en `Services/AppConfig.cs` (`ApiBaseUrl`).
3. Elegir el celular como destino y ejecutar (F5).

Para generar el APK:
```
dotnet publish -f net10.0-android -c Release -p:AndroidPackageFormat=apk
```

## Estructura del proyecto

| Carpeta | Contenido |
|---|---|
| `Services/` | Cliente de la API, sesión segura, carrito, pago simulado, archivos/PDF/fotos y log de errores |
| `Models/` | Clases que reflejan el JSON de la API |
| `Components/Pages/` | Pantallas del cliente (catálogo, detalle, carrito, pago, compras, cuenta) |
| `Components/Pages/Admin/` | Pantallas del administrador (stock, ingreso, productos, reportes, auditoría, errores) |
| `Components/Layout/` | Barra superior y pestañas de navegación según el rol |
| `wwwroot/` | Página base y estilos pensados para pantalla de celular |

## Tecnologías

.NET MAUI Blazor Hybrid · .NET 10 · C# · ASP.NET Core Web API · Entity Framework Core · SQL Server · JWT · Serilog · QuestPDF · MailKit
