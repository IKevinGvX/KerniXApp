using KerniX.Api.Models.Configuracion.Impuesto;
using KerniX.Api.Services.Configuracion;
using KerniX.Models.Pedido;
using KerniX.Models.Ventas.Venta;
using KerniX.Security;
using KerniX.Services.Pedido;
using KerniX.Services.Ventas;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Globalization;
using static KerniX.Models.Ventas.Venta.VentaConfirmarRequest;


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

    private readonly IPedidoService
        _pedidoService;

    private readonly IVentaConfirmacionIntegralDbService
        _ventaConfirmacionIntegralDbService;

    private readonly IConfiguration
        _configuration;

    public PedidoDetalle? PedidoOrigen { get; private set; }

    public string? ErrorPedidoOrigen { get; private set; }

    public bool VentaDesdePedido =>
        PedidoOrigen is not null;

    // =========================================================
    // CLIENTE DEL PEDIDO PARA PRECARGA EN NUEVA VENTA
    // =========================================================
    public int PedidoClienteId { get; private set; }
    public string PedidoClienteNombre { get; private set; } = string.Empty;
    public string PedidoClienteTipoDocumento { get; private set; } = string.Empty;
    public string PedidoClienteNumeroDocumento { get; private set; } = string.Empty;
    public string PedidoClienteTelefono { get; private set; } = string.Empty;
    public string PedidoClienteCorreo { get; private set; } = string.Empty;

    public NuevaModel(
        IClienteVentaService clienteVentaService,
        IProductoVentaService productoVentaService,
        IParametroSistemaDbService parametroSistemaDbService,
        IImpuestoDbService impuestoDbService,
        IConfiguration configuration,
        ILogger<NuevaModel> logger,
        IPedidoService pedidoService,
        IVentaConfirmacionIntegralDbService ventaConfirmacionIntegralDbService)
    {
        _clienteVentaService =
            clienteVentaService;

        _productoVentaService =
            productoVentaService;

        _parametroSistemaDbService =
            parametroSistemaDbService;

        _impuestoDbService =
            impuestoDbService;

        _configuration =
            configuration;

        _logger =
            logger;

        _pedidoService =
            pedidoService;

        _ventaConfirmacionIntegralDbService =
            ventaConfirmacionIntegralDbService;
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
     OnGetAsync(long? idPedido, CancellationToken cancellationToken)
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


        _logger.LogInformation("Nueva venta GET. IdPedido recibido={IdPedido}", idPedido);

        // Proyecto monempresa y monosucursal: contexto fijo de demostración.
        // Cambiar por el contexto autorizado del usuario antes de habilitar múltiples empresas.
        int? empresaAutorizada = 1;
        int? sucursalAutorizada = 1;

        IdEmpresa = 1;
        IdSucursal = 1;


        IdAlmacen =
            HttpContext.Session.GetInt32(
                "IdAlmacen")
            ?? 1;


        // Guardar valores válidos en sesión
        HttpContext.Session.SetInt32(
            "IdEmpresa",
            IdEmpresa);


        HttpContext.Session.SetInt32(
            "IdSucursal",
            IdSucursal);


        HttpContext.Session.SetInt32(
            "IdAlmacen",
            IdAlmacen);


        _logger.LogInformation(
            "Nueva Venta cargada. " +
            "Usuario={IdUsuario} | " +
            "Empresa={IdEmpresa} | " +
            "Sucursal={IdSucursal} | " +
            "Almacen={IdAlmacen}",
            idUsuario.Value,
            IdEmpresa,
            IdSucursal,
            IdAlmacen);


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


        // =========================================================
        // PEDIDO DE ORIGEN: CONSULTA DE SOLO LECTURA
        // =========================================================
        if (idPedido.HasValue)
        {
            if (idPedido.Value <= 0)
            {
                ErrorPedidoOrigen = "El identificador del pedido no es válido.";
            }
            else if (empresaAutorizada is null or <= 0 ||
                     sucursalAutorizada is null or <= 0)
            {
                ErrorPedidoOrigen =
                    "No hay empresa o sucursal autorizada en la sesión para consultar el pedido.";
            }
            else
            {
                try
                {
                    PedidoDetalle? pedido = await _pedidoService.ObtenerAsync(
                        idPedido.Value,
                        empresaAutorizada.Value,
                        sucursalAutorizada.Value,
                        cancellationToken);

                    if (pedido is null)
                    {
                        ErrorPedidoOrigen =
                            "El pedido no existe o no corresponde a la empresa y sucursal de la sesión.";
                    }
                    else
                    {
                        string estadoPedido = (pedido.Estado ?? string.Empty)
                            .Trim().ToLowerInvariant();

                        if (estadoPedido != "aprobado" && estadoPedido != "parcial")
                        {
                            ErrorPedidoOrigen =
                                "Solo se admiten pedidos aprobados o parcialmente atendidos.";
                        }
                        else if (pedido.CantidadPendiente <= 0)
                        {
                            ErrorPedidoOrigen =
                                "El pedido no contiene cantidades pendientes por atender.";
                        }
                        else
                        {
                            PedidoOrigen = pedido;

                            await CargarClientePedidoAsync(
                                pedido.IdCliente,
                                empresaAutorizada.Value,
                                cancellationToken);

                            _logger.LogInformation(
                                "Pedido de origen consultado. Pedido={IdPedido}, Empresa={Empresa}, Sucursal={Sucursal}, Cliente={IdCliente}",
                                pedido.IdPedido,
                                empresaAutorizada.Value,
                                sucursalAutorizada.Value,
                                pedido.IdCliente);
                        }
                    }
                }
                catch (OperationCanceledException)
                    when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al consultar el pedido {IdPedido}", idPedido.Value);
                    ErrorPedidoOrigen = "No se pudo consultar el pedido de origen.";
                }
            }
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

    public async Task<IActionResult> OnPostConfirmarVentaAsync(
        [FromBody] VentaConfirmarRequest request,
        CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return ErrorVenta(
                "No se recibieron los datos de la venta.");
        }

        int? idUsuario =
            HttpContext.Session.GetInt32(
                SessionKeys.IdUsuario);

        if (!idUsuario.HasValue ||
            idUsuario.Value <= 0)
        {
            return new JsonResult(new
            {
                success = false,
                message = "La sesión ha finalizado."
            })
            {
                StatusCode =
                    StatusCodes.Status401Unauthorized
            };
        }

        int? idEmpleado =
            HttpContext.Session.GetInt32(
                "IdEmpleado");

        if (!idEmpleado.HasValue ||
            idEmpleado.Value <= 0)
        {
            idEmpleado =
                await ObtenerIdEmpleadoUsuarioAsync(
                    idUsuario.Value,
                    cancellationToken);

            if (idEmpleado.HasValue &&
                idEmpleado.Value > 0)
            {
                HttpContext.Session.SetInt32(
                    "IdEmpleado",
                    idEmpleado.Value);
            }
        }

        if (!idEmpleado.HasValue ||
            idEmpleado.Value <= 0)
        {
            return ErrorVenta(
                "El usuario actual no tiene un empleado asociado.");
        }

        // El contexto de empresa/sucursal/almacén se toma de la sesión.
        // Los valores del navegador solo se usan como respaldo del flujo
        // monempresa actual.
        int idEmpresa =
            HttpContext.Session.GetInt32(
                "IdEmpresa")
            ?? request.IdEmpresa;

        int idSucursal =
            HttpContext.Session.GetInt32(
                "IdSucursal")
            ?? request.IdSucursal;

        int idAlmacen =
            HttpContext.Session.GetInt32(
                "IdAlmacen")
            ?? request.IdAlmacen;

        if (idEmpresa <= 0)
            idEmpresa = 1;

        if (idSucursal <= 0)
            idSucursal = 1;

        if (idAlmacen <= 0)
            idAlmacen = 1;

        HttpContext.Session.SetInt32(
            "IdEmpresa",
            idEmpresa);

        HttpContext.Session.SetInt32(
            "IdSucursal",
            idSucursal);

        HttpContext.Session.SetInt32(
            "IdAlmacen",
            idAlmacen);

        if (request.IdCliente <= 0)
        {
            return ErrorVenta(
                "Selecciona un cliente válido.");
        }

        if (request.Detalles is null ||
            request.Detalles.Count == 0)
        {
            return ErrorVenta(
                "Agrega al menos un producto.");
        }

        string metodoPago =
            (request.MetodoPago ?? string.Empty)
                .Trim()
                .ToUpperInvariant();

        string[] metodosPermitidos =
        {
            "EFECTIVO",
            "YAPE",
            "PLIN",
            "TARJETA"
        };

        if (!metodosPermitidos.Contains(
            metodoPago))
        {
            return ErrorVenta(
                "El método de pago no es válido.");
        }

        string tipoComprobante =
            (request.TipoComprobante ??
             "SIN_COMPROBANTE")
                .Trim()
                .ToUpperInvariant();

        string[] comprobantesPermitidos =
        {
            "BOLETA",
            "FACTURA",
            "NOTA_VENTA",
            "SIN_COMPROBANTE"
        };

        if (!comprobantesPermitidos.Contains(
            tipoComprobante))
        {
            return ErrorVenta(
                "El tipo de comprobante no es válido.");
        }

        if (request.IdPedido.HasValue &&
            request.IdPedido.Value <= 0)
        {
            return ErrorVenta(
                "El identificador del pedido no es válido.");
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

            if (detalle.PrecioUnitario <= 0)
            {
                return ErrorVenta(
                    "El precio debe ser mayor que cero.");
            }

            // Contrato del navegador:
            // detalle.Descuento llega como PORCENTAJE.
            // El servicio integral lo transforma a monto monetario
            // antes de crear el TVP de SQL Server.
            if (detalle.Descuento < 0 ||
                detalle.Descuento > 100)
            {
                return ErrorVenta(
                    "El descuento debe estar entre 0 y 100%.");
            }
        }

        if (metodoPago == "EFECTIVO" &&
            request.MontoRecibido <= 0)
        {
            return ErrorVenta(
                "Ingresa el monto recibido.");
        }

        try
        {
            string numeroVenta =
                $"V-{DateTime.Now:yyyyMMddHHmmssfff}";

            decimal montoRecibido =
                metodoPago == "EFECTIVO"
                    ? request.MontoRecibido
                    : 0m;

            VentaConfirmacionIntegralResponse resultado =
                await _ventaConfirmacionIntegralDbService
                    .ConfirmarAsync(
                        new VentaConfirmacionIntegralRequest
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
                                request.IdPedido,

                            NumeroVenta =
                                numeroVenta,

                            Observacion =
                                string.IsNullOrWhiteSpace(
                                    request.Observacion)
                                    ? null
                                    : request.Observacion.Trim(),

                            MetodoPago =
                                metodoPago,

                            NumeroOperacion =
                                string.IsNullOrWhiteSpace(
                                    request.NumeroOperacion)
                                    ? null
                                    : request.NumeroOperacion.Trim(),

                            MontoRecibido =
                                montoRecibido,

                            TipoComprobante =
                                tipoComprobante,

                            Detalles =
                                request.Detalles
                        },
                        cancellationToken);

            _logger.LogInformation(
                "Venta integral confirmada. " +
                "IdVenta={IdVenta} | " +
                "Pedido={IdPedido} | " +
                "Total={Total}",
                resultado.IdVenta,
                request.IdPedido,
                resultado.VentaTotal);

            return new JsonResult(new
            {
                success = true,

                message =
                    "Venta, pago y comprobante registrados correctamente.",

                data = new
                {
                    venta = new
                    {
                        idVenta =
                            resultado.IdVenta,

                        numeroVenta =
                            resultado.NumeroVenta,

                        subtotal =
                            resultado.VentaSubtotal,

                        descuento =
                            resultado.VentaDescuento,

                        impuesto =
                            resultado.VentaImpuesto,

                        total =
                            resultado.VentaTotal,

                        estado =
                            resultado.VentaEstado
                    },

                    pago = new
                    {
                        idPago =
                            resultado.IdPago,

                        monto =
                            resultado.PagoMonto,

                        metodoPago =
                            resultado.MetodoPago,

                        estado =
                            resultado.PagoEstado
                    },

                    comprobante =
                        resultado.IdComprobante.HasValue
                            ? new
                            {
                                idComprobante =
                                    resultado.IdComprobante,

                                tipoComprobante =
                                    resultado.TipoComprobante,

                                serie =
                                    resultado.Serie,

                                numero =
                                    resultado.Numero,

                                numeroComprobante =
                                    resultado.NumeroCompleto,

                                subtotal =
                                    resultado.ComprobanteSubtotal,

                                impuesto =
                                    resultado.ComprobanteImpuesto,

                                total =
                                    resultado.ComprobanteTotal,

                                estadoSunat =
                                    resultado.EstadoSunat
                            }
                            : null
                }
            });
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            return new JsonResult(new
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
                "Error SQL en confirmación integral. " +
                "Pedido={IdPedido} | Cliente={IdCliente}",
                request.IdPedido,
                request.IdCliente);

            return new JsonResult(new
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
                "Error confirmando venta integral. " +
                "Pedido={IdPedido} | Cliente={IdCliente}",
                request.IdPedido,
                request.IdCliente);

            return new JsonResult(new
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
    // STOCK REAL DEL PRODUCTO EN EL ALMACÉN DE SESIÓN
    // =========================================================
    public async Task<IActionResult> OnGetStockProductoAsync(
        int idProducto, CancellationToken cancellationToken)
    {
        int? usuario = HttpContext.Session.GetInt32(SessionKeys.IdUsuario);
        if (usuario is null or <= 0)
            return new JsonResult(new { success = false, message = "Sesión finalizada." })
            { StatusCode = StatusCodes.Status401Unauthorized };

        int empresa = HttpContext.Session.GetInt32("IdEmpresa") ?? 1;
        int sucursal = HttpContext.Session.GetInt32("IdSucursal") ?? 1;
        int almacen = HttpContext.Session.GetInt32("IdAlmacen") ?? 1;
        if (idProducto <= 0 || empresa <= 0 || sucursal <= 0 || almacen <= 0)
            return ErrorVenta("Producto o almacén no válido.");

        const string sql = """
            SELECT p.controla_stock,
                   COALESCE(s.cantidad_actual, 0) AS cantidad_actual,
                   COALESCE(s.cantidad_reservada, 0) AS cantidad_reservada
            FROM inv.producto AS p
            INNER JOIN inv.almacen AS a
                ON a.id_almacen = @almacen AND a.id_sucursal = @sucursal AND a.estado = 1
            LEFT JOIN inv.stock AS s
                ON s.id_producto = p.id_producto AND s.id_almacen = a.id_almacen
            WHERE p.id_producto = @producto AND p.id_empresa = @empresa AND p.estado = 1;
            """;
        try
        {
            string connectionString = _configuration.GetConnectionString("KerniXDb")
                ?? throw new InvalidOperationException("Falta ConnectionStrings:KerniXDb.");
            await using var cn = new SqlConnection(connectionString);
            await using var cmd = new SqlCommand(sql, cn);
            cmd.Parameters.Add("@empresa", System.Data.SqlDbType.Int).Value = empresa;
            cmd.Parameters.Add("@sucursal", System.Data.SqlDbType.Int).Value = sucursal;
            cmd.Parameters.Add("@almacen", System.Data.SqlDbType.Int).Value = almacen;
            cmd.Parameters.Add("@producto", System.Data.SqlDbType.Int).Value = idProducto;
            await cn.OpenAsync(cancellationToken);
            await using var rd = await cmd.ExecuteReaderAsync(cancellationToken);
            if (!await rd.ReadAsync(cancellationToken))
                return new JsonResult(new { success = false, message = "Producto o almacén no encontrado." })
                { StatusCode = StatusCodes.Status404NotFound };

            bool controla = Convert.ToBoolean(rd["controla_stock"]);
            decimal actual = Convert.ToDecimal(rd["cantidad_actual"]);
            decimal reservado = Convert.ToDecimal(rd["cantidad_reservada"]);
            return new JsonResult(new
            {
                success = true,
                data = new
                {
                    idProducto,
                    idAlmacen = almacen,
                    controlaStock = controla,
                    stockDisponible = actual - reservado
                }
            });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consultando stock. Producto={Producto} Almacen={Almacen}",
                idProducto, almacen);
            return new JsonResult(new { success = false, message = "No se pudo verificar el stock." })
            { StatusCode = StatusCodes.Status500InternalServerError };
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
    // OBTENER EMPLEADO DEL USUARIO
    // =========================================================

    private async Task<int?>
        ObtenerIdEmpleadoUsuarioAsync(
            int idUsuario,
            CancellationToken cancellationToken)
    {
        if (idUsuario <= 0)
        {
            return null;
        }

        string connectionString =
            _configuration.GetConnectionString(
                "KerniXDb")
            ?? throw new InvalidOperationException(
                "No existe la cadena de conexión 'KerniXDb'.");

        const string sql = """
            SELECT TOP (1)
                u.id_empleado
            FROM seg.usuario u
            WHERE u.id_usuario = @id_usuario
              AND u.estado = 1;
            """;

        await using var connection =
            new SqlConnection(
                connectionString);

        await connection.OpenAsync(
            cancellationToken);

        await using var command =
            new SqlCommand(
                sql,
                connection);

        command.Parameters.Add(
            "@id_usuario",
            System.Data.SqlDbType.Int)
            .Value =
            idUsuario;

        object? resultado =
            await command.ExecuteScalarAsync(
                cancellationToken);

        if (
            resultado is null ||
            resultado is DBNull
        )
        {
            _logger.LogWarning(
                "No se encontró id_empleado para el usuario {IdUsuario}.",
                idUsuario);

            return null;
        }

        int idEmpleado =
            Convert.ToInt32(
                resultado);

        if (idEmpleado <= 0)
        {
            return null;
        }

        return idEmpleado;
    }


    // =========================================================
    // CARGAR CLIENTE REAL DEL PEDIDO
    // =========================================================
    private async Task CargarClientePedidoAsync(
        int idCliente,
        int idEmpresa,
        CancellationToken cancellationToken)
    {
        PedidoClienteId = 0;
        PedidoClienteNombre = string.Empty;
        PedidoClienteTipoDocumento = string.Empty;
        PedidoClienteNumeroDocumento = string.Empty;
        PedidoClienteTelefono = string.Empty;
        PedidoClienteCorreo = string.Empty;

        if (idCliente <= 0 || idEmpresa <= 0)
        {
            return;
        }

        string connectionString =
            _configuration.GetConnectionString("KerniXDb")
            ?? throw new InvalidOperationException(
                "No existe la cadena de conexión 'KerniXDb'.");

        const string sql = """
            SELECT TOP (1)
                c.id_cliente,
                COALESCE(
                    NULLIF(LTRIM(RTRIM(c.razon_social)), N''),
                    NULLIF(LTRIM(RTRIM(CONCAT(c.nombres, N' ', c.apellidos))), N''),
                    N'Cliente'
                ) AS nombre_cliente,
                COALESCE(c.tipo_documento, N'') AS tipo_documento,
                COALESCE(c.numero_documento, N'') AS numero_documento,
                COALESCE(c.telefono, N'') AS telefono,
                COALESCE(c.correo, N'') AS correo
            FROM ven.cliente AS c
            WHERE c.id_cliente = @id_cliente
              AND c.id_empresa = @id_empresa
              AND c.estado = 1;
            """;

        await using var connection =
            new SqlConnection(connectionString);

        await using var command =
            new SqlCommand(sql, connection);

        command.Parameters.Add(
            "@id_cliente",
            System.Data.SqlDbType.Int
        ).Value = idCliente;

        command.Parameters.Add(
            "@id_empresa",
            System.Data.SqlDbType.Int
        ).Value = idEmpresa;

        await connection.OpenAsync(cancellationToken);

        await using var reader =
            await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
        {
            _logger.LogWarning(
                "No se encontró el cliente activo del pedido. Cliente={IdCliente} Empresa={IdEmpresa}",
                idCliente,
                idEmpresa);

            return;
        }

        PedidoClienteId =
            reader.GetInt32(
                reader.GetOrdinal("id_cliente"));

        PedidoClienteNombre =
            reader.GetString(
                reader.GetOrdinal("nombre_cliente"));

        PedidoClienteTipoDocumento =
            reader.GetString(
                reader.GetOrdinal("tipo_documento"))
                .Trim()
                .ToUpperInvariant();

        PedidoClienteNumeroDocumento =
            reader.GetString(
                reader.GetOrdinal("numero_documento"))
                .Trim();

        PedidoClienteTelefono =
            reader.GetString(
                reader.GetOrdinal("telefono"))
                .Trim();

        PedidoClienteCorreo =
            reader.GetString(
                reader.GetOrdinal("correo"))
                .Trim();
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