using KerniX.Api.Services.Configuracion;
using KerniX.Middleware;
using KerniX.Security;
using KerniX.Services.Auth;
using KerniX.Services.Empleados;
using KerniX.Services.Usuarios;
using KerniX.Services.Ventas;
using Microsoft.AspNetCore.HttpOverrides;
namespace KerniX;

public class Program
{
    public static void Main(
        string[] args)
    {
        var builder =
            WebApplication.CreateBuilder(args);

        // =========================================================
        // CONFIGURACIÓN API
        // =========================================================

        string? apiBaseUrl =
            builder.Configuration[
                "ApiSettings:BaseUrl"];

        if (string.IsNullOrWhiteSpace(
            apiBaseUrl))
        {
            throw new InvalidOperationException(
                "ApiSettings:BaseUrl no está configurado.");
        }

        if (!apiBaseUrl.EndsWith('/'))
        {
            apiBaseUrl += "/";
        }

        Uri apiUri =
            new(apiBaseUrl);

        // =========================================================
        // RAZOR PAGES
        // =========================================================

        builder.Services
            .AddRazorPages();
        // =========================================================
        // NUEVA VENTA - PRODUCTOS API
        // ENVÍA JWT
        // =========================================================

        builder.Services
            .AddHttpClient<
                IProductoVentaService,
                ProductoVentaService>(
                client =>
                {
                    client.BaseAddress =
                        apiUri;

                    client.Timeout =
                        TimeSpan.FromSeconds(30);

                    client.DefaultRequestHeaders
                        .Accept
                        .ParseAdd(
                            "application/json"
                        );
                })
            .AddHttpMessageHandler<
                AuthHeaderHandler>();
        // =========================================================
        // HTTP CONTEXT
        // =========================================================

        builder.Services
            .AddHttpContextAccessor();

        // =========================================================
        // SESSION
        // =========================================================

        builder.Services
            .AddDistributedMemoryCache();

        builder.Services
            .AddSession(
                options =>
                {
                    options.IdleTimeout =
                        TimeSpan.FromMinutes(30);

                    options.Cookie.Name =
                        ".KerniX.Session";

                    options.Cookie.HttpOnly =
                        true;

                    options.Cookie.IsEssential =
                        true;

                    options.Cookie.SameSite =
                        SameSiteMode.Lax;

                    options.Cookie.SecurePolicy =
                        CookieSecurePolicy.SameAsRequest;
                });

        // =========================================================
        // SEGURIDAD TOKEN
        // =========================================================

        builder.Services
            .AddScoped<TokenStorageService>();

        builder.Services
            .AddTransient<AuthHeaderHandler>();

        // =========================================================
        // SERVICIOS INTERNOS EXISTENTES
        // =========================================================

        builder.Services
            .AddScoped<
                IEmpleadoDbService,
                EmpleadoDbService>();

        // =========================================================
        // CONFIGURACIÓN PARÁMETROS DEL SISTEMA
        // =========================================================

        builder.Services
            .AddScoped<
                IParametroSistemaDbService,
                ParametroSistemaDbService>();

        // =========================================================
        // CONFIGURACIÓN SUCURSALES
        // =========================================================

        builder.Services
            .AddScoped<
                IConfiguracionSucursalDbService,
                ConfiguracionSucursalDbService>();

        // =========================================================
        // CONFIGURACIÓN MONEDAS
        // =========================================================

        builder.Services
            .AddScoped<
                IMonedaDbService,
                MonedaDbService>();

        // =========================================================
        // CONFIGURACIÓN TIPO DE CAMBIO
        // =========================================================

        builder.Services
            .AddScoped<
                ITipoCambioDbService,
                TipoCambioDbService>();

        // =========================================================
        // CONFIGURACIÓN IMPUESTOS
        // =========================================================

        builder.Services
            .AddScoped<
                IImpuestoDbService,
                ImpuestoDbService>();
        // =========================================================
        // CONFIGURACIÓN SERIES DE COMPROBANTE
        // =========================================================

        builder.Services
                .AddScoped<
                    ISerieComprobanteDbService,
                    SerieComprobanteDbService>();

        // =========================================================
        // CONFIGURACIÓN GENERAL
        // =========================================================

        builder.Services
            .AddScoped<
                IConfiguracionDbService,
                ConfiguracionDbService>();

        // =========================================================
        // CONFIGURACIÓN COPIAS DE SEGURIDAD
        // =========================================================

        builder.Services
            .AddScoped<
                ICopiaSeguridadDbService,
                CopiaSeguridadDbService>();

        // =========================================================
        // CONFIGURACIÓN RESTAURACIÓN
        // =========================================================

        builder.Services
            .AddScoped<
                IRestauracionSistemaDbService,
                RestauracionSistemaDbService>();

        // =========================================================
        // CONFIGURACIÓN PLANES DE MANTENIMIENTO
        // =========================================================

        builder.Services
            .AddScoped<
                IPlanMantenimientoDbService,
                PlanMantenimientoDbService>();

        // =========================================================
        // CONFIGURACIÓN EJECUCIONES DE MANTENIMIENTO
        // =========================================================

        builder.Services
            .AddScoped<
                IEjecucionMantenimientoDbService,
                EjecucionMantenimientoDbService>();

        // =========================================================
        // CONFIGURACIÓN ALERTAS DE MANTENIMIENTO
        // =========================================================

        builder.Services
            .AddScoped<
                IAlertaMantenimientoDbService,
                AlertaMantenimientoDbService>();

        // =========================================================
        // CONFIGURACIÓN ORQUESTADOR DE MANTENIMIENTO
        // =========================================================

        builder.Services
            .AddScoped<
                IMantenimientoDbService,
                MantenimientoDbService>();

        // =========================================================
        // GESTIÓN DE VENTAS
        // =========================================================

        builder.Services
            .AddScoped<
                IVentaDbService,
                VentaDbService>();

        builder.Services
            .AddScoped<
                IVentaClienteDbService,
                VentaClienteDbService>();

        builder.Services
            .AddScoped<
                IVentaProductoDbService,
                VentaProductoDbService>();

        builder.Services
            .AddScoped<
                IPagoDbService,
                PagoDbService>();

        builder.Services
            .AddScoped<
                IComprobanteDbService,
                ComprobanteDbService>();

        builder.Services
            .AddScoped<
                IVentaDashboardDbService,
                VentaDashboardDbService>();

        // =========================================================
        // NUEVA VENTA - CLIENTES API
        // ENVÍA JWT
        // =========================================================
        builder.Services
            .AddHttpClient<
                IClienteVentaService,
                ClienteVentaService>(
                client =>
                {
                    client.BaseAddress =
                        apiUri;

                    client.Timeout =
                        TimeSpan.FromSeconds(30);

                    client.DefaultRequestHeaders
                        .Accept
                        .ParseAdd(
                            "application/json");
                })
            .AddHttpMessageHandler<
                AuthHeaderHandler>();

        // =========================================================
        // AUTH API
        // LOGIN REFRESH
        // NO ENVÍA JWT
        // =========================================================

        builder.Services
            .AddHttpClient<
                IAuthApiService,
                AuthApiService>(
                client =>
                {
                    client.BaseAddress =
                        apiUri;

                    client.Timeout =
                        TimeSpan.FromSeconds(15);
                });

        // =========================================================
        // USUARIOS API
        // ENVÍA JWT
        // =========================================================

        builder.Services
            .AddHttpClient<
                IUsuarioService,
                UsuarioService>(
                client =>
                {
                    client.BaseAddress =
                        apiUri;

                    client.Timeout =
                        TimeSpan.FromSeconds(30);
                })
            .AddHttpMessageHandler<
                AuthHeaderHandler>();

        // =========================================================
        // API GENERAL CON JWT
        // =========================================================

        builder.Services
            .AddHttpClient(
                "KerniXApi",
                client =>
                {
                    client.BaseAddress =
                        apiUri;

                    client.Timeout =
                        TimeSpan.FromSeconds(30);
                })
            .AddHttpMessageHandler<
                AuthHeaderHandler>();

        // =========================================================
        // AUTORIZACIÓN
        // =========================================================

        builder.Services
            .AddAuthorization();

        // =========================================================
        // FORWARDED HEADERS
        // =========================================================

        builder.Services
            .Configure<
                ForwardedHeadersOptions>(
                options =>
                {
                    options.ForwardedHeaders =
                        ForwardedHeaders.XForwardedFor |
                        ForwardedHeaders.XForwardedProto;

                    options.KnownNetworks.Clear();

                    options.KnownProxies.Clear();
                });

        // =========================================================
        // BUILD
        // =========================================================

        var app =
            builder.Build();

        // =========================================================
        // MANEJO DE ERRORES
        // =========================================================

        if (app.Environment.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler(
                "/Error");

            app.UseHsts();
        }
        app.UseForwardedHeaders();
        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseSession();
        app.UseMiddleware<
            SessionGuardMiddleware>();
        app.UseAuthorization();
        app.MapRazorPages();

        app.Run();
    }
}