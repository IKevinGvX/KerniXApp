using KerniX.Api.Services.Configuracion;
using KerniX.Middleware;
using KerniX.Security;
using KerniX.Services.Auth;
using KerniX.Services.Configuracion.Usuarios;
using KerniX.Services.Cotizacion;
using KerniX.Services.Empleados;
using KerniX.Services.Inventario;
using KerniX.Services.Inventario.Catalogos;
using KerniX.Services.Pedido;
using KerniX.Services.Security;
using KerniX.Services.Sesiones;
using KerniX.Services.Seguridad;
using KerniX.Services.Sunat;
using KerniX.Services.Usuarios;
using KerniX.Services.Ventas;
using Microsoft.AspNetCore.DataProtection;
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
        builder.Services.AddScoped<
    ICotizacionDbService,
    CotizacionDbService
>();
        builder.Services.AddScoped<
    IProductoDbService,
    ProductoDbService
>();

        builder.Services.AddScoped<
            KerniX.Services.IA.IAgenteCatalogoService,
            KerniX.Services.IA.AgenteCatalogoService>();

        builder.Services.AddScoped<
            KerniX.Services.IA.IKerniXIaService,
            KerniX.Services.IA.KerniXIaService>();

        builder.Services.AddScoped<
            KerniX.Services.Ventas.Operaciones.IVentasRegistroService,
            KerniX.Services.Ventas.Operaciones.VentasRegistroService>();

        builder.Services.AddScoped<
            KerniX.Services.Ventas.Panel.IVentasPanelService,
            KerniX.Services.Ventas.Panel.VentasPanelService>();

        builder.Services.AddScoped<
            KerniX.Services.Proveedores.IProveedorDbService,
            KerniX.Services.Proveedores.ProveedorDbService>();

        builder.Services.AddScoped<
            KerniX.Services.Proveedores.ProveedorContextService>();
        builder.Services.AddHttpClient<
    KerniX.Services.IA.IConversacionIaService,
    KerniX.Services.IA.ConversacionIaService>(client =>
    {
        var raw = builder.Configuration["IaMotor:BaseUrl"];
        if (string.IsNullOrWhiteSpace(raw)
            || !Uri.TryCreate(raw, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttps
                && !(uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback)))
        {
            throw new InvalidOperationException(
                "Configura IaMotor:BaseUrl. HTTP solo se permite para loopback; usa HTTPS en red.");
        }
        client.BaseAddress = new Uri(uri.AbsoluteUri.TrimEnd('/') + "/");
        client.Timeout = TimeSpan.FromSeconds(115);
    });

        builder.Services.AddScoped<
    ICatalogoInventarioDbService,
    CatalogoInventarioDbService>();

        builder.Services.AddScoped<IEntradaStockDbService, EntradaStockDbService>();
        builder.Services.AddScoped<ISalidaStockDbService, SalidaStockDbService>();
        builder.Services.AddScoped<IInventarioEnterpriseDbService, InventarioEnterpriseDbService>();

        Uri apiUri =
            new(apiBaseUrl);
        builder.Services.AddScoped<IPedidoService, PedidoService>();
        // =========================================================
        // RAZOR PAGES
        // =========================================================
        builder.Services.AddScoped<
    IInventarioEnterpriseDbService,
    InventarioEnterpriseDbService>();
        builder.Services.AddScoped<
    IUsuarioAdminDbService,
    UsuarioAdminDbService>();
        builder.Services
            .AddRazorPages();
        // =========================================================
        // NUEVA VENTA - PRODUCTOS API
        // ENVÍA JWT
        // =========================================================
        builder.Services.AddScoped<IVentaConfirmacionIntegralDbService, VentaConfirmacionIntegralDbService>();
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
        var dataProtectionPath =
    Path.Combine(
        builder.Environment.ContentRootPath,
        "App_Data",
        "DataProtectionKeys"
    ); builder.Services.AddScoped<
    ISunatConfiguracionDbService,
    SunatConfiguracionDbService
>();
        builder.Services.AddScoped<
    IInventarioEnterpriseDbService,
    InventarioEnterpriseDbService>();
        builder.Services.AddScoped<ProductoDbService>();
        Directory.CreateDirectory(
            dataProtectionPath
        );
        builder.Services.AddScoped<
    ISalidaStockDbService,
    SalidaStockDbService>();

        builder.Services.AddScoped<
    IEntradaStockDbService,
    EntradaStockDbService>();

        builder.Services
            .AddDataProtection()
            .SetApplicationName(
                "KerniXERP"
            )
            .PersistKeysToFileSystem(
                new DirectoryInfo(
                    dataProtectionPath
                )
            );
        builder.Services.AddScoped<ProductoDbService>();

        builder.Services.AddSingleton<
            ISecretProtectionService,
            SecretProtectionService
        >();
        // =========================================================
        // HTTP CONTEXT
        // =========================================================
        builder.Services.AddScoped<
    IVentaConfirmacionIntegralDbService,
    VentaConfirmacionIntegralDbService>();

        builder.Services.AddScoped<
            IPedidoService,
            PedidoService
        >();
        builder.Services
            .AddHttpContextAccessor();
        // ==========================================
        // CLIENTES - DEPENDENCY INJECTION
        // ==========================================

        builder.Services.AddHttpClient<
            KerniX.Services.Clientes.IClienteService,
            KerniX.Services.Clientes.ClienteService>(
            (serviceProvider, client) =>
            {
                var configuration =
                    serviceProvider.GetRequiredService<IConfiguration>();

                var baseUrl =
                    configuration["ApiSettings:BaseUrl"]
                    ?? throw new InvalidOperationException(
                        "Falta ApiSettings:BaseUrl.");

                client.BaseAddress =
                    new Uri(baseUrl);

                client.Timeout =
                    TimeSpan.FromSeconds(30);
            });
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
                        builder.Environment.IsDevelopment()
                            ? CookieSecurePolicy.SameAsRequest
                            : CookieSecurePolicy.Always;
                });

        // =========================================================
        // SEGURIDAD TOKEN
        // =========================================================

        builder.Services
            .AddScoped<TokenStorageService>();

        // Sesiones por dispositivo y cierre remoto.
        builder.Services.AddScoped<ISesionWebDbService, SesionWebDbService>();

        // Fase 4: recuperación de contraseña por correo y token temporal.
        builder.Services.AddScoped<IRecuperacionClaveService, RecuperacionClaveService>();

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
