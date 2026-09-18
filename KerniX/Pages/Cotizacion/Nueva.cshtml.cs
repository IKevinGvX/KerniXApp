using KerniX.Models.Cotizacion;
using KerniX.Security;
using KerniX.Services.Cotizacion;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;

namespace KerniX.Pages.Cotizacion;

public sealed class NuevaModel : PageModel
{
    private readonly ICotizacionDbService _cotizacionDbService;
    private readonly ILogger<NuevaModel> _logger;

    public NuevaModel(
        ICotizacionDbService cotizacionDbService,
        ILogger<NuevaModel> logger)
    {
        _cotizacionDbService = cotizacionDbService;
        _logger = logger;
    }

    // =========================================================
    // CATÁLOGOS
    // =========================================================

    public List<ClienteCotizacionItem> Clientes { get; private set; }
        = new();

    public List<ProductoCotizacionItem> Productos { get; private set; }
        = new();

    // =========================================================
    // FORMULARIO
    // =========================================================

    [BindProperty]
    public int IdCliente { get; set; }

    [BindProperty]
    public DateTime? FechaVencimiento { get; set; }

    [BindProperty]
    public decimal DescuentoGeneral { get; set; }

    [BindProperty]
    public string? Observacion { get; set; }

    [BindProperty]
    public string DetalleJson { get; set; } = string.Empty;

    public string? ErrorMensaje { get; private set; }

    // =========================================================
    // GET
    // =========================================================

    public async Task<IActionResult> OnGetAsync()
    {
        try
        {
            int idEmpresa = ObtenerIdEmpresa();

            FechaVencimiento = DateTime.Today.AddDays(15);

            await CargarCatalogosAsync(idEmpresa);

            _logger.LogInformation(
                "Nueva Cotización cargada. Empresa={Empresa} | Clientes={Clientes} | Productos={Productos}",
                idEmpresa,
                Clientes.Count,
                Productos.Count
            );

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error cargando Nueva Cotización."
            );

            ErrorMensaje = ex.Message;

            return Page();
        }
    }

    // =========================================================
    // POST
    // =========================================================

    public async Task<IActionResult> OnPostAsync()
    {
        _logger.LogInformation(
            "POST Nueva Cotización iniciado. Cliente={Cliente} | Descuento={Descuento} | DetalleLength={DetalleLength}",
            IdCliente,
            DescuentoGeneral,
            DetalleJson?.Length ?? 0
        );

        int idEmpresa = 0;

        try
        {
            // =================================================
            // CONTEXTO
            // =================================================

            idEmpresa = ObtenerIdEmpresa();

            int idSucursal = ObtenerIdSucursal();

            int idEmpleado = ObtenerIdEmpleado();

            _logger.LogInformation(
                "Contexto Cotización. Empresa={Empresa} | Sucursal={Sucursal} | Empleado={Empleado}",
                idEmpresa,
                idSucursal,
                idEmpleado
            );

            // DIAGNÓSTICO DE FECHA
            _logger.LogInformation(
                "DEBUG FECHA | Modelo={Fecha} | Formulario={Valor}",
                FechaVencimiento,
                Request.Form["FechaVencimiento"].ToString()
            );

            // DIAGNÓSTICO DE PRODUCTOS
            _logger.LogInformation(
                "DEBUG DETALLE | Longitud={Longitud} | Productos={Detalle}",
                DetalleJson?.Length ?? 0,
                DetalleJson
            );
            if (idEmpresa <= 0)
            {
                throw new InvalidOperationException(
                    "No se pudo identificar la empresa."
                );
            }

            if (idSucursal <= 0)
            {
                throw new InvalidOperationException(
                    "No se pudo identificar la sucursal."
                );
            }

            if (idEmpleado <= 0)
            {
                throw new InvalidOperationException(
                    "No se pudo identificar el empleado."
                );
            }

            // =================================================
            // ERRORES DE MODEL BINDING
            // =================================================

            if (!ModelState.IsValid)
            {
                foreach (var campo in ModelState)
                {
                    foreach (var error in campo.Value.Errors)
                    {
                        _logger.LogWarning(
                            "ModelState inválido. Campo={Campo} | Error={Error}",
                            campo.Key,
                            error.ErrorMessage
                        );
                    }
                }
            }

            // =================================================
            // VALIDAR CLIENTE
            // =================================================

            if (IdCliente <= 0)
            {
                throw new InvalidOperationException(
                    "Debe seleccionar un cliente."
                );
            }

            // =================================================
            // VALIDAR FECHA
            // =================================================

            if (!FechaVencimiento.HasValue)
            {
                throw new InvalidOperationException(
                    "Debe seleccionar una fecha de vencimiento."
                );
            }

            if (FechaVencimiento.Value.Date < DateTime.Today)
            {
                throw new InvalidOperationException(
                    "La fecha de vencimiento no puede ser anterior a hoy."
                );
            }

            // =================================================
            // VALIDAR DESCUENTO GENERAL
            // =================================================

            if (DescuentoGeneral < 0)
            {
                throw new InvalidOperationException(
                    "El descuento general no puede ser negativo."
                );
            }

            // =================================================
            // VALIDAR JSON
            // =================================================

            if (string.IsNullOrWhiteSpace(DetalleJson))
            {
                throw new InvalidOperationException(
                    "Debe agregar al menos un producto."
                );
            }

            List<NuevaCotizacionDetalleRequest> detalles;

            try
            {
                detalles =
                    JsonSerializer.Deserialize<
                        List<NuevaCotizacionDetalleRequest>
                    >(
                        DetalleJson,
                        new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        }
                    ) ?? new();
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(
                    ex,
                    "JSON inválido al registrar cotización."
                );

                throw new InvalidOperationException(
                    "No se pudo procesar el detalle de productos.",
                    ex
                );
            }

            if (detalles.Count == 0)
            {
                throw new InvalidOperationException(
                    "Debe agregar al menos un producto."
                );
            }

            // =================================================
            // VALIDAR PRODUCTOS
            // =================================================

            foreach (var detalle in detalles)
            {
                if (detalle.IdProducto <= 0)
                {
                    throw new InvalidOperationException(
                        "Existe un producto inválido."
                    );
                }

                if (detalle.Cantidad <= 0)
                {
                    throw new InvalidOperationException(
                        $"La cantidad de {detalle.Producto} debe ser mayor que cero."
                    );
                }

                if (detalle.Cantidad != decimal.Truncate(detalle.Cantidad))
                {
                    throw new InvalidOperationException(
                        $"La cantidad de {detalle.Producto} debe ser un número entero."
                    );
                }

                if (detalle.PrecioUnitario < 0)
                {
                    throw new InvalidOperationException(
                        $"El precio de {detalle.Producto} no es válido."
                    );
                }

                if (detalle.Descuento < 0)
                {
                    throw new InvalidOperationException(
                        $"El descuento de {detalle.Producto} no puede ser negativo."
                    );
                }

                decimal subtotal = Math.Round(
                    detalle.Cantidad * detalle.PrecioUnitario,
                    4
                );

                if (detalle.Descuento > subtotal)
                {
                    throw new InvalidOperationException(
                        $"El descuento de {detalle.Producto} supera su subtotal."
                    );
                }

                if (detalle.Impuesto < 0)
                {
                    throw new InvalidOperationException(
                        $"El impuesto de {detalle.Producto} no puede ser negativo."
                    );
                }

                // El subtotal se vuelve a calcular en el servidor.
                detalle.Subtotal = subtotal;
            }

            decimal baseDisponible =
                detalles.Sum(x => x.Subtotal - x.Descuento);

            if (DescuentoGeneral > baseDisponible)
            {
                throw new InvalidOperationException(
                    "El descuento general supera el importe disponible."
                );
            }

            // =================================================
            // REQUEST
            // =================================================

            NuevaCotizacionRequest request = new()
            {
                IdEmpresa = idEmpresa,

                IdSucursal = idSucursal,

                IdCliente = IdCliente,

                IdEmpleado = idEmpleado,

                FechaVencimiento = FechaVencimiento,

                Descuento = DescuentoGeneral,

                Observacion = Observacion,

                Detalles = detalles
            };

            // =================================================
            // GUARDAR EN SQL
            // =================================================

            _logger.LogInformation(
                "Registrando cotización. Cliente={Cliente} | Productos={CantidadProductos}",
                IdCliente,
                detalles.Count
            );

            CotizacionRegistroResultado resultado =
                await _cotizacionDbService.RegistrarAsync(request);

            _logger.LogInformation(
                "Cotización registrada. Id={IdCotizacion} | Número={Numero} | Total={Total}",
                resultado.IdCotizacion,
                resultado.NumeroCotizacion,
                resultado.Total
            );

            // Redirige al listado, que ya existe.
            // Cuando implementemos Detalle.cshtml podremos cambiarlo.

            TempData["CotizacionRegistrada"] =
                $"Cotización {resultado.NumeroCotizacion} registrada correctamente.";

            return Redirect("/Cotizacion/Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error registrando Nueva Cotización. Cliente={Cliente}",
                IdCliente
            );

            ErrorMensaje = ex.Message;

            if (idEmpresa > 0)
            {
                try
                {
                    await CargarCatalogosAsync(idEmpresa);
                }
                catch (Exception catalogoEx)
                {
                    _logger.LogError(
                        catalogoEx,
                        "Error recargando los catálogos tras fallar el registro."
                    );
                }
            }

            return Page();
        }
    }

    // =========================================================
    // CARGAR CATÁLOGOS
    // =========================================================

    private async Task CargarCatalogosAsync(int idEmpresa)
    {
        if (idEmpresa <= 0)
        {
            throw new InvalidOperationException(
                "No se pudo determinar la empresa."
            );
        }

        var clientesTask =
            _cotizacionDbService.ListarClientesAsync(idEmpresa);

        var productosTask =
            _cotizacionDbService.ListarProductosAsync(
                idEmpresa,
                null
            );

        await Task.WhenAll(
            clientesTask,
            productosTask
        );

        Clientes = await clientesTask;

        Productos = await productosTask;
    }

    // =========================================================
    // EMPRESA
    // =========================================================

    private int ObtenerIdEmpresa()
    {
        int id = ObtenerEnteroSesion("IdEmpresa");

        // Temporal para el entorno de pruebas.
        // Sustituir por el contexto real del login.

        return id > 0 ? id : 1;
    }

    // =========================================================
    // SUCURSAL
    // =========================================================

    private int ObtenerIdSucursal()
    {
        int id = ObtenerEnteroSesion("IdSucursal");

        // Temporal para el entorno de pruebas.

        return id > 0 ? id : 1;
    }

    // =========================================================
    // EMPLEADO
    // =========================================================

    private int ObtenerIdEmpleado()
    {
        int idEmpleado =
            ObtenerEnteroSesion(SessionKeys.IdEmpleado);

        if (idEmpleado <= 0)
        {
            _logger.LogWarning(
                "No se encontró IdEmpleado en sesión. Usuario={Usuario}",
                HttpContext.Session.GetString(SessionKeys.Usuario)
            );
        }

        return idEmpleado;
    }

    // =========================================================
    // SESIÓN
    // =========================================================

    private int ObtenerEnteroSesion(string clave)
    {
        int? valorInt = HttpContext.Session.GetInt32(clave);

        if (valorInt.HasValue)
        {
            return valorInt.Value;
        }

        string? valorString =
            HttpContext.Session.GetString(clave);

        if (int.TryParse(valorString, out int valor))
        {
            return valor;
        }

        return 0;
    }
}