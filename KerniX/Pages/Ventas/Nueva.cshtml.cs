using System.Globalization;
using KerniX.Api.Models.Configuracion.Impuesto;
using KerniX.Api.Services.Configuracion;
using KerniX.Models.Ventas.Venta;
using KerniX.Models.Ventas.Comprobante;
using KerniX.Models.Ventas.Pago;
using Microsoft.Data.SqlClient;
using KerniX.Security;
using KerniX.Services.Ventas;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KerniX.Pages.Ventas;

public sealed class NuevaModel
    : PageModel
{
    private readonly IClienteVentaService
        _clienteVentaService;

    private readonly IProductoVentaService
        _productoVentaService;

    private readonly IParametroSistemaDbService
        _parametroSistemaDbService;

    private readonly IImpuestoDbService
        _impuestoDbService;

    private readonly ILogger<NuevaModel>
        _logger;
    private readonly IVentaDbService
    _ventaDbService;

    private readonly IPagoDbService
        _pagoDbService;

    private readonly IComprobanteDbService
        _comprobanteDbService;
    public NuevaModel(
        IClienteVentaService clienteVentaService,
        IProductoVentaService productoVentaService,
        IParametroSistemaDbService parametroSistemaDbService,
        IImpuestoDbService impuestoDbService,
        IVentaDbService ventaDbService,
        IPagoDbService pagoDbService,
        IComprobanteDbService comprobanteDbService,
        ILogger<NuevaModel> logger)
    {
        _clienteVentaService =
            clienteVentaService;

        _productoVentaService =
            productoVentaService;

        _parametroSistemaDbService =
            parametroSistemaDbService;

        _impuestoDbService =
            impuestoDbService;

        _ventaDbService =
            ventaDbService;

        _pagoDbService =
            pagoDbService;

        _comprobanteDbService =
            comprobanteDbService;

        _logger =
            logger;
    }


    // =========================================================
    // EMPRESA / SUCURSAL / ALMACÉN
    // =========================================================

    public int IdEmpresa
    {
        get;
        private set;
    }


    public int IdSucursal
    {
        get;
        private set;
    }


    public int IdAlmacen
    {
        get;
        private set;
    }


    // =========================================================
    // CONFIGURACIÓN VENTA
    // =========================================================

    public decimal DescuentoMaximo
    {
        get;
        private set;
    }


    public bool PermitirVentaSinStock
    {
        get;
        private set;
    }


    public bool ValidarStockAntesVenta
    {
        get;
        private set;
    } = true;


    public bool PermitirCambioPrecio
    {
        get;
        private set;
    }


    public bool PermitirVentaCredito
    {
        get;
        private set;
    }


    // =========================================================
    // IMPUESTO
    // =========================================================

    public int IdImpuesto
    {
        get;
        private set;
    }


    public string CodigoImpuesto
    {
        get;
        private set;
    } = "IGV";


    public string NombreImpuesto
    {
        get;
        private set;
    } =
        "Impuesto General a las Ventas";


    public decimal PorcentajeImpuesto
    {
        get;
        private set;
    }


    // =========================================================
    // GET PÁGINA
    // =========================================================

    public async Task<IActionResult>
        OnGetAsync()
    {
        int? idUsuario =
            HttpContext.Session.GetInt32(
                SessionKeys.IdUsuario);


        if (
            !idUsuario.HasValue ||
            idUsuario.Value <= 0
        )
        {
            return RedirectToPage(
                "/Account/Login");
        }


        IdEmpresa =
            HttpContext.Session.GetInt32(
                "IdEmpresa")
            ?? 1;


        IdSucursal =
            HttpContext.Session.GetInt32(
                "IdSucursal")
            ?? 1;


        IdAlmacen =
            HttpContext.Session.GetInt32(
                "IdAlmacen")
            ?? 1;


        try
        {
            await CargarConfiguracionVentaAsync();

            await CargarImpuestoVigenteAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error cargando configuración de Nueva Venta. " +
                "Empresa={IdEmpresa}",
                IdEmpresa);


            DescuentoMaximo =
                0m;

            PermitirVentaSinStock =
                false;

            ValidarStockAntesVenta =
                true;

            PermitirCambioPrecio =
                false;

            PermitirVentaCredito =
                false;


            IdImpuesto =
                0;

            CodigoImpuesto =
                "IGV";

            NombreImpuesto =
                "Impuesto General a las Ventas";

            PorcentajeImpuesto =
                0m;
        }


        return Page();
    }


    // =========================================================
    // BUSCAR CLIENTES
    // =========================================================

    public async Task<IActionResult>
        OnGetBuscarClientesAsync(
            string? termino,
            CancellationToken cancellationToken)
    {
        try
        {
            var clientes =
                await _clienteVentaService
                    .BuscarAsync(
                        termino,
                        cancellationToken);


            return new JsonResult(
                new
                {
                    success = true,
                    data = clientes
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error buscando clientes. Termino={Termino}",
                termino);


            return new JsonResult(
                new
                {
                    success = false,
                    message =
                        "No se pudieron cargar los clientes."
                });
        }
    }


    // =========================================================
    // CREAR CLIENTE
    // =========================================================

    public async Task<IActionResult>
        OnPostCrearClienteAsync(
            [FromBody]
            ClienteVentaCrearRequest request,
            CancellationToken cancellationToken)
    {
        int? idUsuario =
            HttpContext.Session.GetInt32(
                SessionKeys.IdUsuario);


        if (
            !idUsuario.HasValue ||
            idUsuario.Value <= 0
        )
        {
            return new JsonResult(
                new
                {
                    success = false,
                    message =
                        "La sesión ha finalizado."
                })
            {
                StatusCode =
                    StatusCodes.Status401Unauthorized
            };
        }


        if (request is null)
        {
            return new JsonResult(
                new
                {
                    success = false,
                    message =
                        "No se recibieron los datos del cliente."
                })
            {
                StatusCode =
                    StatusCodes.Status400BadRequest
            };
        }


        // =====================================================
        // EMPRESA DE LA SESIÓN
        // NO CONFIAR EN EL ID ENVIADO POR JAVASCRIPT
        // =====================================================

        int idEmpresa =
            HttpContext.Session.GetInt32(
                "IdEmpresa")
            ?? 1;


        request.IdEmpresa =
            idEmpresa;


        // =====================================================
        // NORMALIZAR DATOS
        // =====================================================

        request.TipoCliente =
            request.TipoCliente?
                .Trim()
                .ToUpperInvariant()
            ?? string.Empty;


        request.TipoDocumento =
            request.TipoDocumento?
                .Trim()
                .ToUpperInvariant()
            ?? string.Empty;


        request.NumeroDocumento =
            request.NumeroDocumento?
                .Trim()
            ?? string.Empty;


        request.RazonSocial =
            LimpiarTexto(
                request.RazonSocial);


        request.Nombres =
            LimpiarTexto(
                request.Nombres);


        request.Apellidos =
            LimpiarTexto(
                request.Apellidos);


        request.Telefono =
            LimpiarTexto(
                request.Telefono);


        request.Correo =
            LimpiarTexto(
                request.Correo);


        // =====================================================
        // VALIDACIONES GENERALES
        // =====================================================

        if (string.IsNullOrWhiteSpace(
            request.TipoCliente))
        {
            return ErrorCliente(
                "Selecciona el tipo de cliente.");
        }


        if (
            request.TipoCliente != "NATURAL" &&
            request.TipoCliente != "JURIDICO"
        )
        {
            return ErrorCliente(
                "El tipo de cliente no es válido.");
        }


        if (string.IsNullOrWhiteSpace(
            request.TipoDocumento))
        {
            return ErrorCliente(
                "Selecciona el tipo de documento.");
        }


        if (string.IsNullOrWhiteSpace(
            request.NumeroDocumento))
        {
            return ErrorCliente(
                "Ingresa el número de documento.");
        }


        // =====================================================
        // VALIDAR DNI
        // =====================================================

        if (
            request.TipoDocumento == "DNI" &&
            (
                request.NumeroDocumento.Length != 8 ||
                !request.NumeroDocumento.All(
                    char.IsDigit)
            )
        )
        {
            return ErrorCliente(
                "El DNI debe contener 8 dígitos.");
        }


        // =====================================================
        // VALIDAR RUC
        // =====================================================

        if (
            request.TipoDocumento == "RUC" &&
            (
                request.NumeroDocumento.Length != 11 ||
                !request.NumeroDocumento.All(
                    char.IsDigit)
            )
        )
        {
            return ErrorCliente(
                "El RUC debe contener 11 dígitos.");
        }


        // =====================================================
        // PERSONA NATURAL
        // =====================================================

        if (
            request.TipoCliente == "NATURAL" &&
            string.IsNullOrWhiteSpace(
                request.Nombres)
        )
        {
            return ErrorCliente(
                "Ingresa los nombres del cliente.");
        }


        // =====================================================
        // PERSONA JURÍDICA
        // =====================================================

        if (
            request.TipoCliente == "JURIDICO" &&
            string.IsNullOrWhiteSpace(
                request.RazonSocial)
        )
        {
            return ErrorCliente(
                "Ingresa la razón social.");
        }


        // =====================================================
        // CORREO
        // =====================================================

        if (
            !string.IsNullOrWhiteSpace(
                request.Correo)
            &&
            !request.Correo.Contains('@')
        )
        {
            return ErrorCliente(
                "El correo ingresado no es válido.");
        }


        // =====================================================
        // CONTACTOS
        // =====================================================

        request.Contactos ??=
            new List<ClienteContactoCrearRequest>();


        foreach (
            ClienteContactoCrearRequest contacto
            in request.Contactos)
        {
            contacto.Nombres =
                LimpiarTexto(
                    contacto.Nombres);

            contacto.Cargo =
                LimpiarTexto(
                    contacto.Cargo);

            contacto.Telefono =
                LimpiarTexto(
                    contacto.Telefono);

            contacto.Correo =
                LimpiarTexto(
                    contacto.Correo);
        }


        // =====================================================
        // DIRECCIONES
        // =====================================================

        request.Direcciones ??=
            new List<ClienteDireccionCrearRequest>();


        foreach (
            ClienteDireccionCrearRequest direccion
            in request.Direcciones)
        {
            direccion.TipoDireccion =
                LimpiarTexto(
                    direccion.TipoDireccion);

            direccion.Direccion =
                LimpiarTexto(
                    direccion.Direccion);

            direccion.Referencia =
                LimpiarTexto(
                    direccion.Referencia);

            direccion.Distrito =
                LimpiarTexto(
                    direccion.Distrito);

            direccion.Provincia =
                LimpiarTexto(
                    direccion.Provincia);

            direccion.Departamento =
                LimpiarTexto(
                    direccion.Departamento);

            direccion.Ubigeo =
                LimpiarTexto(
                    direccion.Ubigeo);
        }


        // =====================================================
        // API CLIENTES
        // =====================================================

        try
        {
            var cliente =
                await _clienteVentaService
                    .CrearAsync(
                        request,
                        cancellationToken);


            if (cliente is null)
            {
                return new JsonResult(
                    new
                    {
                        success = false,
                        message =
                            "La API no devolvió el cliente creado."
                    })
                {
                    StatusCode =
                        StatusCodes.Status400BadRequest
                };
            }


            _logger.LogInformation(
                "Cliente creado correctamente. " +
                "Empresa={IdEmpresa} | Documento={Documento}",
                idEmpresa,
                request.NumeroDocumento);


            return new JsonResult(
                new
                {
                    success = true,
                    message =
                        "Cliente registrado correctamente.",
                    data = cliente
                });
        }
        catch (OperationCanceledException)
        {
            return new JsonResult(
                new
                {
                    success = false,
                    message =
                        "La operación fue cancelada."
                })
            {
                StatusCode =
                    StatusCodes.Status408RequestTimeout
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error registrando cliente. " +
                "Empresa={IdEmpresa} | Documento={Documento}",
                idEmpresa,
                request.NumeroDocumento);


            return new JsonResult(
                new
                {
                    success = false,
                    message =
                        ex.Message
                })
            {
                StatusCode =
                    StatusCodes.Status400BadRequest
            };
        }
    }


    // =========================================================
    // CONFIRMAR VENTA
    // =========================================================

    public async Task<IActionResult>
        OnPostConfirmarVentaAsync(
            [FromBody]
            VentaConfirmarRequest request,
            CancellationToken cancellationToken)
    {
        int? idUsuario =
            HttpContext.Session.GetInt32(
                SessionKeys.IdUsuario);

        int? idEmpleado =
            HttpContext.Session.GetInt32(
                "IdEmpleado");

        int idEmpresa =
            HttpContext.Session.GetInt32(
                "IdEmpresa")
            ?? 0;

        int idSucursal =
            HttpContext.Session.GetInt32(
                "IdSucursal")
            ?? 0;

        int idAlmacen =
            HttpContext.Session.GetInt32(
                "IdAlmacen")
            ?? 0;


        if (
            !idUsuario.HasValue ||
            idUsuario.Value <= 0
        )
        {
            return new JsonResult(
                new
                {
                    success = false,
                    message =
                        "La sesión ha finalizado."
                })
            {
                StatusCode =
                    StatusCodes.Status401Unauthorized
            };
        }


        if (
            !idEmpleado.HasValue ||
            idEmpleado.Value <= 0
        )
        {
            return ErrorVenta(
                "No se encontró el empleado de la sesión.");
        }


        if (
            idEmpresa <= 0 ||
            idSucursal <= 0 ||
            idAlmacen <= 0
        )
        {
            return ErrorVenta(
                "La empresa, sucursal o almacén de la sesión no son válidos.");
        }


        if (request is null)
        {
            return ErrorVenta(
                "No se recibieron los datos de la venta.");
        }


        if (request.IdCliente <= 0)
        {
            return ErrorVenta(
                "Selecciona un cliente.");
        }


        if (
            request.Detalles is null ||
            request.Detalles.Count == 0
        )
        {
            return ErrorVenta(
                "Agrega al menos un producto.");
        }


        string metodoPago =
            request.MetodoPago?
                .Trim()
                .ToUpperInvariant()
            ?? string.Empty;


        if (string.IsNullOrWhiteSpace(
            metodoPago))
        {
            return ErrorVenta(
                "Selecciona un método de pago.");
        }


        string tipoComprobante =
            request.TipoComprobante?
                .Trim()
                .ToUpperInvariant()
            ?? "SIN_COMPROBANTE";


        string[] tiposPermitidos =
        {
            "BOLETA",
            "FACTURA",
            "NOTA_VENTA",
            "SIN_COMPROBANTE"
        };


        if (!tiposPermitidos.Contains(
            tipoComprobante))
        {
            return ErrorVenta(
                "El tipo de comprobante no es válido.");
        }


        foreach (
            VentaConfirmarDetalleRequest detalle
            in request.Detalles)
        {
            if (detalle.IdProducto <= 0)
            {
                return ErrorVenta(
                    "Existe un producto inválido.");
            }

            if (detalle.Cantidad <= 0)
            {
                return ErrorVenta(
                    "La cantidad debe ser mayor que cero.");
            }

            if (detalle.PrecioUnitario < 0)
            {
                return ErrorVenta(
                    "El precio no puede ser negativo.");
            }

            if (detalle.Descuento < 0)
            {
                return ErrorVenta(
                    "El descuento no puede ser negativo.");
            }
        }


        try
        {
            string numeroVenta =
                $"V-{DateTime.Now:yyyyMMddHHmmssfff}";


            var ventaRequest =
                new VentaCrearRequest
                {
                    IdEmpresa =
                        idEmpresa,

                    IdSucursal =
                        idSucursal,

                    IdCliente =
                        request.IdCliente,

                    IdEmpleado =
                        idEmpleado.Value,

                    IdAlmacen =
                        idAlmacen,

                    IdPedido =
                        null,

                    NumeroVenta =
                        numeroVenta,

                    Observacion =
                        string.IsNullOrWhiteSpace(
                            request.Observacion)
                            ? null
                            : request.Observacion.Trim(),

                    Detalles =
                        request.Detalles
                            .Select(
                                x =>
                                    new VentaDetalleCrearRequest
                                    {
                                        IdProducto =
                                            x.IdProducto,

                                        Cantidad =
                                            x.Cantidad,

                                        PrecioUnitario =
                                            x.PrecioUnitario,

                                        Descuento =
                                            x.Descuento,

                                        Impuesto =
                                            0m
                                    })
                            .ToList()
                };


            VentaResponse venta =
                await _ventaDbService
                    .RegistrarAsync(
                        ventaRequest);


            var pagoRequest =
                new PagoCrearRequest
                {
                    IdVenta =
                        venta.IdVenta,

                    IdEmpleado =
                        idEmpleado.Value,

                    MetodoPago =
                        metodoPago,

                    Moneda =
                        "PEN",

                    Monto =
                        venta.Total,

                    NumeroOperacion =
                        string.IsNullOrWhiteSpace(
                            request.NumeroOperacion)
                            ? null
                            : request.NumeroOperacion.Trim()
                };


            PagoResponse pago =
                await _pagoDbService
                    .RegistrarAsync(
                        pagoRequest);


            ComprobanteResponse? comprobante =
                null;


            if (
                tipoComprobante !=
                "SIN_COMPROBANTE"
            )
            {
                var comprobanteRequest =
                    new ComprobanteCrearRequest
                    {
                        IdVenta =
                            venta.IdVenta,

                        IdEmpresa =
                            idEmpresa,

                        IdSucursal =
                            idSucursal,

                        TipoComprobante =
                            tipoComprobante,

                        EstadoSunat =
                            tipoComprobante ==
                            "NOTA_VENTA"
                                ? "NO_APLICA"
                                : "pendiente",

                        CodigoHash =
                            null,

                        RutaXml =
                            null,

                        RutaPdf =
                            null
                    };


                comprobante =
                    await _comprobanteDbService
                        .EmitirAsync(
                            comprobanteRequest);
            }


            _logger.LogInformation(
                "Venta confirmada correctamente. " +
                "IdVenta={IdVenta} | " +
                "Numero={NumeroVenta} | " +
                "Comprobante={TipoComprobante}",
                venta.IdVenta,
                venta.NumeroVenta,
                tipoComprobante);


            return new JsonResult(
                new
                {
                    success = true,
                    message =
                        "Venta registrada correctamente.",
                    data =
                        new
                        {
                            venta,
                            pago,
                            comprobante
                        }
                });
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            return new JsonResult(
                new
                {
                    success = false,
                    message =
                        "La operación fue cancelada."
                })
            {
                StatusCode =
                    StatusCodes.Status408RequestTimeout
            };
        }
        catch (SqlException ex)
        {
            _logger.LogError(
                ex,
                "Error SQL confirmando venta. " +
                "Empresa={IdEmpresa} | Cliente={IdCliente}",
                idEmpresa,
                request.IdCliente);

            return new JsonResult(
                new
                {
                    success = false,
                    message = ex.Message
                })
            {
                StatusCode =
                    StatusCodes.Status400BadRequest
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error confirmando venta. " +
                "Empresa={IdEmpresa} | Cliente={IdCliente}",
                idEmpresa,
                request.IdCliente);

            return new JsonResult(
                new
                {
                    success = false,
                    message = ex.Message
                })
            {
                StatusCode =
                    StatusCodes.Status400BadRequest
            };
        }
    }


    // =========================================================
    // BUSCAR PRODUCTOS
    // =========================================================

    public async Task<IActionResult>
        OnGetBuscarProductosAsync(
            string? termino,
            CancellationToken cancellationToken)
    {
        try
        {
            var productos =
                await _productoVentaService
                    .BuscarAsync(
                        termino,
                        cancellationToken);


            return new JsonResult(
                new
                {
                    success = true,
                    data = productos
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error buscando productos. Termino={Termino}",
                termino);


            return new JsonResult(
                new
                {
                    success = false,
                    message =
                        "No se pudieron cargar los productos."
                });
        }
    }


    // =========================================================
    // OBTENER PRODUCTO
    // =========================================================

    public async Task<IActionResult>
        OnGetProductoAsync(
            int idProducto,
            CancellationToken cancellationToken)
    {
        if (idProducto <= 0)
        {
            return new JsonResult(
                new
                {
                    success = false,
                    message =
                        "Producto inválido."
                });
        }


        try
        {
            var producto =
                await _productoVentaService
                    .ObtenerPorIdAsync(
                        idProducto,
                        cancellationToken);


            if (producto is null)
            {
                return new JsonResult(
                    new
                    {
                        success = false,
                        message =
                            "Producto no encontrado."
                    });
            }


            return new JsonResult(
                new
                {
                    success = true,
                    data = producto
                });
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error obteniendo producto. " +
                "IdProducto={IdProducto}",
                idProducto);


            return new JsonResult(
                new
                {
                    success = false,
                    message =
                        "No se pudo obtener el producto."
                });
        }
    }


    // =========================================================
    // CONFIGURACIÓN VENTA
    // =========================================================

    private async Task
        CargarConfiguracionVentaAsync()
    {
        var parametros =
            await _parametroSistemaDbService
                .ListarAsync(
                    IdEmpresa);


        string ObtenerValor(
            string codigo,
            string valorPredeterminado)
        {
            var parametro =
                parametros.FirstOrDefault(
                    x =>
                        string.Equals(
                            x.Codigo,
                            codigo,
                            StringComparison.OrdinalIgnoreCase));


            if (
                parametro is null ||
                string.IsNullOrWhiteSpace(
                    parametro.Valor)
            )
            {
                return valorPredeterminado;
            }


            return parametro.Valor.Trim();
        }


        DescuentoMaximo =
            ConvertirDecimal(
                ObtenerValor(
                    "DESCUENTO_MAXIMO",
                    "0"),
                0m);


        PermitirVentaSinStock =
            ConvertirBooleano(
                ObtenerValor(
                    "PERMITIR_VENTA_SIN_STOCK",
                    "false"));


        ValidarStockAntesVenta =
            ConvertirBooleano(
                ObtenerValor(
                    "VALIDAR_STOCK_ANTES_VENTA",
                    "true"));


        PermitirCambioPrecio =
            ConvertirBooleano(
                ObtenerValor(
                    "PERMITIR_CAMBIO_PRECIO",
                    "false"));


        PermitirVentaCredito =
            ConvertirBooleano(
                ObtenerValor(
                    "PERMITIR_VENTA_CREDITO",
                    "false"));


        if (!PermitirVentaSinStock)
        {
            ValidarStockAntesVenta =
                true;
        }
    }


    // =========================================================
    // IMPUESTO VIGENTE
    // =========================================================

    private async Task
        CargarImpuestoVigenteAsync()
    {
        IReadOnlyList<ImpuestoResponse> impuestos =
            await _impuestoDbService
                .ListarVigentesAsync(
                    IdEmpresa);


        var impuesto =
            impuestos
                .Where(
                    x =>
                        x.Estado
                        &&
                        string.Equals(
                            x.Codigo,
                            "IGV",
                            StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(
                    x =>
                        x.FechaInicio)
                .ThenByDescending(
                    x =>
                        x.IdImpuesto)
                .FirstOrDefault();


        if (impuesto is null)
        {
            IdImpuesto =
                0;

            CodigoImpuesto =
                "IGV";

            NombreImpuesto =
                "Impuesto General a las Ventas";

            PorcentajeImpuesto =
                0m;


            _logger.LogWarning(
                "No existe IGV vigente para la empresa {IdEmpresa}.",
                IdEmpresa);


            return;
        }


        IdImpuesto =
            impuesto.IdImpuesto;

        CodigoImpuesto =
            impuesto.Codigo;

        NombreImpuesto =
            impuesto.Nombre;

        PorcentajeImpuesto =
            impuesto.Porcentaje;


        _logger.LogInformation(
            "Impuesto vigente cargado. " +
            "Empresa={IdEmpresa} | " +
            "Codigo={Codigo} | " +
            "Porcentaje={Porcentaje}",
            IdEmpresa,
            CodigoImpuesto,
            PorcentajeImpuesto);
    }


    // =========================================================
    // ERROR VENTA
    // =========================================================

    private JsonResult ErrorVenta(
        string mensaje)
    {
        return new JsonResult(
            new
            {
                success = false,
                message = mensaje
            })
        {
            StatusCode =
                StatusCodes.Status400BadRequest
        };
    }


    // =========================================================
    // ERROR CLIENTE
    // =========================================================

    private JsonResult ErrorCliente(
        string mensaje)
    {
        return new JsonResult(
            new
            {
                success = false,
                message = mensaje
            })
        {
            StatusCode =
                StatusCodes.Status400BadRequest
        };
    }


    // =========================================================
    // LIMPIAR TEXTO
    // =========================================================

    private static string?
        LimpiarTexto(
            string? valor)
    {
        if (string.IsNullOrWhiteSpace(
            valor))
        {
            return null;
        }


        return valor.Trim();
    }


    // =========================================================
    // BOOL CONFIG
    // =========================================================

    private static bool
        ConvertirBooleano(
            string? valor)
    {
        if (string.IsNullOrWhiteSpace(
            valor))
        {
            return false;
        }


        string texto =
            valor.Trim();


        if (
            bool.TryParse(
                texto,
                out bool resultado)
        )
        {
            return resultado;
        }


        return
            texto == "1"
            ||
            texto.Equals(
                "SI",
                StringComparison.OrdinalIgnoreCase)
            ||
            texto.Equals(
                "SÍ",
                StringComparison.OrdinalIgnoreCase)
            ||
            texto.Equals(
                "ACTIVO",
                StringComparison.OrdinalIgnoreCase);
    }


    // =========================================================
    // DECIMAL CONFIG
    // =========================================================

    private static decimal
        ConvertirDecimal(
            string? valor,
            decimal predeterminado)
    {
        if (string.IsNullOrWhiteSpace(
            valor))
        {
            return predeterminado;
        }


        if (
            decimal.TryParse(
                valor,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out decimal resultado)
        )
        {
            return resultado;
        }


        if (
            decimal.TryParse(
                valor,
                NumberStyles.Number,
                CultureInfo.GetCultureInfo(
                    "es-PE"),
                out decimal resultadoPeru)
        )
        {
            return resultadoPeru;
        }


        return predeterminado;
    }
}