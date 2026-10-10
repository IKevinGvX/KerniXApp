using System.Data;

using KerniX.Models.Inventario.Enterprise;

using Microsoft.Data.SqlClient;

namespace KerniX.Services.Inventario;

public sealed class InventarioEnterpriseDbService
    : IInventarioEnterpriseDbService
{
    private readonly string _connectionString;
    private readonly ILogger<InventarioEnterpriseDbService> _logger;


    public InventarioEnterpriseDbService(
        IConfiguration configuration,
        ILogger<InventarioEnterpriseDbService> logger)
    {
        _connectionString =
            configuration.GetConnectionString("KerniXDb")
            ?? throw new InvalidOperationException(
                "No existe ConnectionStrings:KerniXDb."
            );

        _logger = logger;
    }


    public async Task<InventarioEnterpriseCatalogos>
        ObtenerCatalogosAsync(
            int idEmpresa,
            int? idSucursal = null,
            CancellationToken cancellationToken = default)
    {
        InventarioEnterpriseCatalogos resultado = new();

        await using SqlConnection cn = CrearConexion();
        await using SqlCommand cmd =
            CrearProcedimiento(
                cn,
                "inv.sp_inventario_operaciones_catalogos"
            );

        Add(cmd, "@id_empresa", SqlDbType.Int, idEmpresa);
        AddNullable(cmd, "@id_sucursal", SqlDbType.Int, idSucursal);

        await cn.OpenAsync(cancellationToken);

        await using SqlDataReader rd =
            await cmd.ExecuteReaderAsync(cancellationToken);

        while (await rd.ReadAsync(cancellationToken))
        {
            resultado.Almacenes.Add(
                new InventarioAlmacenItem
                {
                    IdAlmacen = I(rd, "IdAlmacen"),
                    IdSucursal = I(rd, "IdSucursal"),
                    Sucursal = S(rd, "Sucursal"),
                    Codigo = S(rd, "Codigo"),
                    Nombre = S(rd, "Nombre"),
                    Ubicacion = SN(rd, "Ubicacion")
                }
            );
        }

        if (await rd.NextResultAsync(cancellationToken))
        {
            while (await rd.ReadAsync(cancellationToken))
            {
                resultado.Productos.Add(
                    new InventarioProductoItem
                    {
                        IdProducto = I(rd, "IdProducto"),
                        Codigo = S(rd, "Codigo"),
                        Nombre = S(rd, "Nombre"),
                        StockMinimo = D(rd, "StockMinimo"),
                        StockMaximo = DN(rd, "StockMaximo"),
                        PrecioCompra = D(rd, "PrecioCompra"),
                        PrecioVenta = D(rd, "PrecioVenta")
                    }
                );
            }
        }

        return resultado;
    }


    public async Task<List<MovimientoReversibleItem>>
        ListarReversiblesAsync(
            int idEmpresa,
            int? idSucursal = null,
            int top = 100,
            CancellationToken cancellationToken = default)
    {
        List<MovimientoReversibleItem> items = new();

        await using SqlConnection cn = CrearConexion();
        await using SqlCommand cmd =
            CrearProcedimiento(
                cn,
                "inv.sp_movimientos_reversibles_listar"
            );

        Add(cmd, "@id_empresa", SqlDbType.Int, idEmpresa);
        AddNullable(cmd, "@id_sucursal", SqlDbType.Int, idSucursal);
        Add(cmd, "@top", SqlDbType.Int, top);

        await cn.OpenAsync(cancellationToken);

        await using SqlDataReader rd =
            await cmd.ExecuteReaderAsync(cancellationToken);

        while (await rd.ReadAsync(cancellationToken))
        {
            items.Add(
                new MovimientoReversibleItem
                {
                    IdMovimientoInventario =
                        L(rd, "IdMovimientoInventario"),

                    FechaMovimiento =
                        F(rd, "FechaMovimiento"),

                    CodigoMovimiento =
                        S(rd, "CodigoMovimiento"),

                    TipoMovimiento =
                        S(rd, "TipoMovimiento"),

                    CodigoProducto =
                        S(rd, "CodigoProducto"),

                    Producto =
                        S(rd, "Producto"),

                    Almacen =
                        S(rd, "Almacen"),

                    Cantidad =
                        D(rd, "Cantidad"),

                    StockAnterior =
                        D(rd, "StockAnterior"),

                    StockNuevo =
                        D(rd, "StockNuevo"),

                    Referencia =
                        SN(rd, "Referencia"),

                    PermiteReversionGenerica =
                        B(rd, "PermiteReversionGenerica")
                }
            );
        }

        return items;
    }


    public async Task<ReversionMovimientoResultado>
        RevertirMovimientoAsync(
            long idMovimiento,
            int idEmpleado,
            string motivo,
            bool permitirDocumentoComercial = false,
            CancellationToken cancellationToken = default)
    {
        await using SqlConnection cn = CrearConexion();
        await using SqlCommand cmd =
            CrearProcedimiento(
                cn,
                "inv.sp_movimiento_revertir"
            );

        Add(
            cmd,
            "@id_movimiento_origen",
            SqlDbType.BigInt,
            idMovimiento
        );

        Add(
            cmd,
            "@id_empleado",
            SqlDbType.Int,
            idEmpleado
        );

        AddText(cmd, "@motivo", motivo, 300);

        Add(
            cmd,
            "@permitir_documento_comercial",
            SqlDbType.Bit,
            permitirDocumentoComercial
        );

        SqlParameter output =
            cmd.Parameters.Add(
                "@id_movimiento_reversa",
                SqlDbType.BigInt
            );

        output.Direction =
            ParameterDirection.Output;

        await cn.OpenAsync(cancellationToken);

        await using SqlDataReader rd =
            await cmd.ExecuteReaderAsync(cancellationToken);

        if (!await rd.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException(
                "La reversión no devolvió resultado."
            );
        }

        ReversionMovimientoResultado resultado =
            new()
            {
                IdMovimientoOrigen =
                    L(rd, "IdMovimientoOrigen"),

                IdMovimientoReversa =
                    L(rd, "IdMovimientoReversa"),

                CodigoOrigen =
                    S(rd, "CodigoOrigen"),

                CodigoReversa =
                    S(rd, "CodigoReversa"),

                Cantidad =
                    D(rd, "Cantidad"),

                StockOrigenAnterior =
                    D(rd, "StockOrigenAnterior"),

                StockOrigenNuevo =
                    D(rd, "StockOrigenNuevo"),

                StockReversaAnterior =
                    D(rd, "StockReversaAnterior"),

                StockReversaNuevo =
                    D(rd, "StockReversaNuevo"),

                FechaReversa =
                    F(rd, "FechaReversa"),

                Motivo =
                    S(rd, "Motivo")
            };

        _logger.LogInformation(
            "[INVENTARIO] Movimiento revertido. Origen={Origen} Reversa={Reversa}",
            resultado.IdMovimientoOrigen,
            resultado.IdMovimientoReversa
        );

        return resultado;
    }


    public async Task<List<TransferenciaListaItem>>
        ListarTransferenciasAsync(
            int idEmpresa,
            string? estado = null,
            CancellationToken cancellationToken = default)
    {
        List<TransferenciaListaItem> items = new();

        await using SqlConnection cn = CrearConexion();
        await using SqlCommand cmd =
            CrearProcedimiento(
                cn,
                "inv.sp_transferencia_listar"
            );

        Add(cmd, "@id_empresa", SqlDbType.Int, idEmpresa);
        AddTextNullable(cmd, "@estado", estado, 30);

        await cn.OpenAsync(cancellationToken);

        await using SqlDataReader rd =
            await cmd.ExecuteReaderAsync(cancellationToken);

        while (await rd.ReadAsync(cancellationToken))
        {
            items.Add(
                new TransferenciaListaItem
                {
                    IdTransferencia =
                        L(rd, "IdTransferencia"),

                    NumeroTransferencia =
                        S(rd, "NumeroTransferencia"),

                    AlmacenOrigen =
                        S(rd, "AlmacenOrigen"),

                    AlmacenDestino =
                        S(rd, "AlmacenDestino"),

                    Estado =
                        S(rd, "Estado"),

                    Observacion =
                        SN(rd, "Observacion"),

                    FechaSolicitud =
                        F(rd, "FechaSolicitud"),

                    FechaAprobacion =
                        FN(rd, "FechaAprobacion"),

                    FechaDespacho =
                        FN(rd, "FechaDespacho"),

                    FechaRecepcion =
                        FN(rd, "FechaRecepcion"),

                    Lineas =
                        I(rd, "Lineas"),

                    CantidadTotal =
                        D(rd, "CantidadTotal")
                }
            );
        }

        return items;
    }


    public async Task<long>
        CrearTransferenciaAsync(
            int idEmpresa,
            int idAlmacenOrigen,
            int idAlmacenDestino,
            int idEmpleado,
            string? observacion = null,
            CancellationToken cancellationToken = default)
    {
        await using SqlConnection cn = CrearConexion();
        await using SqlCommand cmd =
            CrearProcedimiento(
                cn,
                "inv.sp_transferencia_crear"
            );

        Add(cmd, "@id_empresa", SqlDbType.Int, idEmpresa);
        Add(cmd, "@id_almacen_origen", SqlDbType.Int, idAlmacenOrigen);
        Add(cmd, "@id_almacen_destino", SqlDbType.Int, idAlmacenDestino);
        Add(cmd, "@id_empleado", SqlDbType.Int, idEmpleado);
        AddTextNullable(cmd, "@observacion", observacion, 500);

        SqlParameter output =
            cmd.Parameters.Add(
                "@id_transferencia",
                SqlDbType.BigInt
            );

        output.Direction =
            ParameterDirection.Output;

        await cn.OpenAsync(cancellationToken);
        await cmd.ExecuteNonQueryAsync(cancellationToken);

        return Convert.ToInt64(output.Value);
    }


    public Task AgregarDetalleTransferenciaAsync(
        long idTransferencia,
        int idProducto,
        decimal cantidad,
        CancellationToken cancellationToken = default)
    {
        return EjecutarAsync(
            "inv.sp_transferencia_detalle_agregar",
            cancellationToken,
            p =>
            {
                Add(p, "@id_transferencia", SqlDbType.BigInt, idTransferencia);
                Add(p, "@id_producto", SqlDbType.Int, idProducto);
                AddDecimal(p, "@cantidad", cantidad);
            }
        );
    }


    public Task AprobarTransferenciaAsync(
        long idTransferencia,
        int idEmpleado,
        CancellationToken cancellationToken = default)
    {
        return EjecutarAsync(
            "inv.sp_transferencia_aprobar",
            cancellationToken,
            p =>
            {
                Add(p, "@id_transferencia", SqlDbType.BigInt, idTransferencia);
                Add(p, "@id_empleado", SqlDbType.Int, idEmpleado);
            }
        );
    }


    public Task DespacharTransferenciaAsync(
        long idTransferencia,
        int idEmpleado,
        CancellationToken cancellationToken = default)
    {
        return EjecutarAsync(
            "inv.sp_transferencia_despachar",
            cancellationToken,
            p =>
            {
                Add(p, "@id_transferencia", SqlDbType.BigInt, idTransferencia);
                Add(p, "@id_empleado", SqlDbType.Int, idEmpleado);
            }
        );
    }


    public Task RecibirTransferenciaAsync(
        long idTransferencia,
        int idEmpleado,
        CancellationToken cancellationToken = default)
    {
        return EjecutarAsync(
            "inv.sp_transferencia_recibir",
            cancellationToken,
            p =>
            {
                Add(p, "@id_transferencia", SqlDbType.BigInt, idTransferencia);
                Add(p, "@id_empleado", SqlDbType.Int, idEmpleado);
            }
        );
    }


    public Task CancelarTransferenciaAsync(
        long idTransferencia,
        int idEmpleado,
        string motivo,
        CancellationToken cancellationToken = default)
    {
        return EjecutarAsync(
            "inv.sp_transferencia_cancelar",
            cancellationToken,
            p =>
            {
                Add(p, "@id_transferencia", SqlDbType.BigInt, idTransferencia);
                Add(p, "@id_empleado", SqlDbType.Int, idEmpleado);
                AddText(p, "@motivo", motivo, 300);
            }
        );
    }


    public async Task<TransferenciaDetalleResultado?>
        ObtenerTransferenciaAsync(
            long idTransferencia,
            int idEmpresa,
            CancellationToken cancellationToken = default)
    {
        await using SqlConnection cn = CrearConexion();
        await using SqlCommand cmd =
            CrearProcedimiento(
                cn,
                "inv.sp_transferencia_obtener"
            );

        Add(cmd, "@id_transferencia", SqlDbType.BigInt, idTransferencia);
        Add(cmd, "@id_empresa", SqlDbType.Int, idEmpresa);

        await cn.OpenAsync(cancellationToken);

        await using SqlDataReader rd =
            await cmd.ExecuteReaderAsync(cancellationToken);

        if (!await rd.ReadAsync(cancellationToken))
        {
            return null;
        }

        TransferenciaDetalleResultado resultado =
            new()
            {
                IdTransferencia =
                    L(rd, "IdTransferencia"),

                NumeroTransferencia =
                    S(rd, "NumeroTransferencia"),

                IdEmpresa =
                    I(rd, "IdEmpresa"),

                IdAlmacenOrigen =
                    I(rd, "IdAlmacenOrigen"),

                AlmacenOrigen =
                    S(rd, "AlmacenOrigen"),

                IdAlmacenDestino =
                    I(rd, "IdAlmacenDestino"),

                AlmacenDestino =
                    S(rd, "AlmacenDestino"),

                Estado =
                    S(rd, "Estado"),

                Observacion =
                    SN(rd, "Observacion"),

                FechaSolicitud =
                    F(rd, "FechaSolicitud"),

                FechaAprobacion =
                    FN(rd, "FechaAprobacion"),

                FechaDespacho =
                    FN(rd, "FechaDespacho"),

                FechaRecepcion =
                    FN(rd, "FechaRecepcion"),

                FechaCancelacion =
                    FN(rd, "FechaCancelacion"),

                MotivoCancelacion =
                    SN(rd, "MotivoCancelacion")
            };

        if (await rd.NextResultAsync(cancellationToken))
        {
            while (await rd.ReadAsync(cancellationToken))
            {
                resultado.Detalles.Add(
                    new TransferenciaDetalleItem
                    {
                        IdTransferenciaDetalle =
                            L(rd, "IdTransferenciaDetalle"),

                        IdProducto =
                            I(rd, "IdProducto"),

                        CodigoProducto =
                            S(rd, "CodigoProducto"),

                        Producto =
                            S(rd, "Producto"),

                        Cantidad =
                            D(rd, "Cantidad"),

                        Estado =
                            S(rd, "Estado"),

                        IdMovimientoSalida =
                            LN(rd, "IdMovimientoSalida"),

                        IdMovimientoEntrada =
                            LN(rd, "IdMovimientoEntrada")
                    }
                );
            }
        }

        return resultado;
    }


    public async Task<List<InventarioFisicoListaItem>>
        ListarInventariosFisicosAsync(
            int idEmpresa,
            string? estado = null,
            CancellationToken cancellationToken = default)
    {
        List<InventarioFisicoListaItem> items = new();

        await using SqlConnection cn = CrearConexion();
        await using SqlCommand cmd =
            CrearProcedimiento(
                cn,
                "inv.sp_inventario_fisico_listar"
            );

        Add(cmd, "@id_empresa", SqlDbType.Int, idEmpresa);
        AddTextNullable(cmd, "@estado", estado, 30);

        await cn.OpenAsync(cancellationToken);

        await using SqlDataReader rd =
            await cmd.ExecuteReaderAsync(cancellationToken);

        while (await rd.ReadAsync(cancellationToken))
        {
            items.Add(
                new InventarioFisicoListaItem
                {
                    IdInventarioFisico =
                        L(rd, "IdInventarioFisico"),

                    NumeroInventario =
                        S(rd, "NumeroInventario"),

                    IdAlmacen =
                        I(rd, "IdAlmacen"),

                    Almacen =
                        S(rd, "Almacen"),

                    Estado =
                        S(rd, "Estado"),

                    Observacion =
                        SN(rd, "Observacion"),

                    FechaCreacion =
                        F(rd, "FechaCreacion"),

                    FechaCierre =
                        FN(rd, "FechaCierre"),

                    FechaAplicacion =
                        FN(rd, "FechaAplicacion"),

                    TotalProductos =
                        I(rd, "TotalProductos"),

                    PendientesConteo =
                        I(rd, "PendientesConteo"),

                    DiferenciaAbsoluta =
                        D(rd, "DiferenciaAbsoluta")
                }
            );
        }

        return items;
    }


    public async Task<long>
        IniciarInventarioFisicoAsync(
            int idEmpresa,
            int idAlmacen,
            int idEmpleado,
            string? observacion = null,
            CancellationToken cancellationToken = default)
    {
        await using SqlConnection cn = CrearConexion();
        await using SqlCommand cmd =
            CrearProcedimiento(
                cn,
                "inv.sp_inventario_fisico_iniciar"
            );

        Add(cmd, "@id_empresa", SqlDbType.Int, idEmpresa);
        Add(cmd, "@id_almacen", SqlDbType.Int, idAlmacen);
        Add(cmd, "@id_empleado", SqlDbType.Int, idEmpleado);
        AddTextNullable(cmd, "@observacion", observacion, 500);

        SqlParameter output =
            cmd.Parameters.Add(
                "@id_inventario_fisico",
                SqlDbType.BigInt
            );

        output.Direction =
            ParameterDirection.Output;

        await cn.OpenAsync(cancellationToken);
        await cmd.ExecuteNonQueryAsync(cancellationToken);

        return Convert.ToInt64(output.Value);
    }


    public async Task<InventarioFisicoDetalleResultado?>
        ObtenerInventarioFisicoAsync(
            long idInventarioFisico,
            int idEmpresa,
            CancellationToken cancellationToken = default)
    {
        await using SqlConnection cn = CrearConexion();
        await using SqlCommand cmd =
            CrearProcedimiento(
                cn,
                "inv.sp_inventario_fisico_obtener"
            );

        Add(cmd, "@id_inventario_fisico", SqlDbType.BigInt, idInventarioFisico);
        Add(cmd, "@id_empresa", SqlDbType.Int, idEmpresa);

        await cn.OpenAsync(cancellationToken);

        await using SqlDataReader rd =
            await cmd.ExecuteReaderAsync(cancellationToken);

        if (!await rd.ReadAsync(cancellationToken))
        {
            return null;
        }

        InventarioFisicoDetalleResultado resultado =
            new()
            {
                IdInventarioFisico =
                    L(rd, "IdInventarioFisico"),

                NumeroInventario =
                    S(rd, "NumeroInventario"),

                IdAlmacen =
                    I(rd, "IdAlmacen"),

                Almacen =
                    S(rd, "Almacen"),

                Estado =
                    S(rd, "Estado"),

                Observacion =
                    SN(rd, "Observacion"),

                FechaCreacion =
                    F(rd, "FechaCreacion"),

                FechaCierre =
                    FN(rd, "FechaCierre"),

                FechaAplicacion =
                    FN(rd, "FechaAplicacion")
            };

        if (await rd.NextResultAsync(cancellationToken))
        {
            while (await rd.ReadAsync(cancellationToken))
            {
                resultado.Detalles.Add(
                    new InventarioFisicoDetalleItem
                    {
                        IdInventarioFisicoDetalle =
                            L(rd, "IdInventarioFisicoDetalle"),

                        IdProducto =
                            I(rd, "IdProducto"),

                        CodigoProducto =
                            S(rd, "CodigoProducto"),

                        Producto =
                            S(rd, "Producto"),

                        StockSistema =
                            D(rd, "StockSistema"),

                        CantidadContada =
                            DN(rd, "CantidadContada"),

                        Diferencia =
                            DN(rd, "Diferencia"),

                        IdMovimientoAjuste =
                            LN(rd, "IdMovimientoAjuste")
                    }
                );
            }
        }

        return resultado;
    }


    public Task GuardarConteoFisicoAsync(
        long idInventarioFisico,
        int idProducto,
        decimal cantidadContada,
        CancellationToken cancellationToken = default)
    {
        return EjecutarAsync(
            "inv.sp_inventario_fisico_conteo_guardar",
            cancellationToken,
            p =>
            {
                Add(p, "@id_inventario_fisico", SqlDbType.BigInt, idInventarioFisico);
                Add(p, "@id_producto", SqlDbType.Int, idProducto);
                AddDecimal(p, "@cantidad_contada", cantidadContada);
            }
        );
    }


    public Task CerrarInventarioFisicoAsync(
        long idInventarioFisico,
        int idEmpleado,
        CancellationToken cancellationToken = default)
    {
        return EjecutarAsync(
            "inv.sp_inventario_fisico_cerrar",
            cancellationToken,
            p =>
            {
                Add(p, "@id_inventario_fisico", SqlDbType.BigInt, idInventarioFisico);
                Add(p, "@id_empleado", SqlDbType.Int, idEmpleado);
            }
        );
    }


    public Task AplicarInventarioFisicoAsync(
        long idInventarioFisico,
        int idEmpleado,
        CancellationToken cancellationToken = default)
    {
        return EjecutarAsync(
            "inv.sp_inventario_fisico_aplicar",
            cancellationToken,
            p =>
            {
                Add(p, "@id_inventario_fisico", SqlDbType.BigInt, idInventarioFisico);
                Add(p, "@id_empleado", SqlDbType.Int, idEmpleado);
            }
        );
    }


    public Task RegistrarAjusteAsync(
        int idEmpresa,
        int idAlmacen,
        int idProducto,
        decimal diferencia,
        int idEmpleado,
        string motivo,
        CancellationToken cancellationToken = default)
    {
        return EjecutarAsync(
            "inv.sp_ajuste_inventario_registrar",
            cancellationToken,
            p =>
            {
                Add(p, "@id_empresa", SqlDbType.Int, idEmpresa);
                Add(p, "@id_almacen", SqlDbType.Int, idAlmacen);
                Add(p, "@id_producto", SqlDbType.Int, idProducto);
                AddDecimal(p, "@diferencia", diferencia);
                Add(p, "@id_empleado", SqlDbType.Int, idEmpleado);
                AddText(p, "@motivo", motivo, 300);
            }
        );
    }


    public async Task<List<AjusteInventarioItem>>
        ListarAjustesAsync(
            int idEmpresa,
            CancellationToken cancellationToken = default)
    {
        List<AjusteInventarioItem> items = new();

        await using SqlConnection cn = CrearConexion();
        await using SqlCommand cmd =
            CrearProcedimiento(
                cn,
                "inv.sp_ajuste_inventario_listar"
            );

        Add(cmd, "@id_empresa", SqlDbType.Int, idEmpresa);

        await cn.OpenAsync(cancellationToken);

        await using SqlDataReader rd =
            await cmd.ExecuteReaderAsync(cancellationToken);

        while (await rd.ReadAsync(cancellationToken))
        {
            items.Add(
                new AjusteInventarioItem
                {
                    IdAjusteInventario =
                        L(rd, "IdAjusteInventario"),

                    NumeroAjuste =
                        S(rd, "NumeroAjuste"),

                    FechaRegistro =
                        F(rd, "FechaRegistro"),

                    Almacen =
                        S(rd, "Almacen"),

                    CodigoProducto =
                        S(rd, "CodigoProducto"),

                    Producto =
                        S(rd, "Producto"),

                    Diferencia =
                        D(rd, "Diferencia"),

                    Motivo =
                        S(rd, "Motivo"),

                    IdMovimientoInventario =
                        LN(rd, "IdMovimientoInventario")
                }
            );
        }

        return items;
    }


    public Task RegistrarMermaAsync(
        int idEmpresa,
        int idAlmacen,
        int idProducto,
        decimal cantidad,
        string causa,
        string motivo,
        int idEmpleado,
        CancellationToken cancellationToken = default)
    {
        return EjecutarAsync(
            "inv.sp_merma_registrar",
            cancellationToken,
            p =>
            {
                Add(p, "@id_empresa", SqlDbType.Int, idEmpresa);
                Add(p, "@id_almacen", SqlDbType.Int, idAlmacen);
                Add(p, "@id_producto", SqlDbType.Int, idProducto);
                AddDecimal(p, "@cantidad", cantidad);
                AddText(p, "@causa", causa, 50);
                AddText(p, "@motivo", motivo, 300);
                Add(p, "@id_empleado", SqlDbType.Int, idEmpleado);
            }
        );
    }


    public async Task<List<MermaItem>>
        ListarMermasAsync(
            int idEmpresa,
            CancellationToken cancellationToken = default)
    {
        List<MermaItem> items = new();

        await using SqlConnection cn = CrearConexion();
        await using SqlCommand cmd =
            CrearProcedimiento(
                cn,
                "inv.sp_merma_listar"
            );

        Add(cmd, "@id_empresa", SqlDbType.Int, idEmpresa);

        await cn.OpenAsync(cancellationToken);

        await using SqlDataReader rd =
            await cmd.ExecuteReaderAsync(cancellationToken);

        while (await rd.ReadAsync(cancellationToken))
        {
            items.Add(
                new MermaItem
                {
                    IdMerma =
                        L(rd, "IdMerma"),

                    NumeroMerma =
                        S(rd, "NumeroMerma"),

                    FechaRegistro =
                        F(rd, "FechaRegistro"),

                    Almacen =
                        S(rd, "Almacen"),

                    CodigoProducto =
                        S(rd, "CodigoProducto"),

                    Producto =
                        S(rd, "Producto"),

                    Cantidad =
                        D(rd, "Cantidad"),

                    Causa =
                        S(rd, "Causa"),

                    Motivo =
                        S(rd, "Motivo"),

                    IdMovimientoInventario =
                        LN(rd, "IdMovimientoInventario")
                }
            );
        }

        return items;
    }


    public Task RegistrarDevolucionAsync(
        string tipo,
        int idEmpresa,
        int idAlmacen,
        int idProducto,
        decimal cantidad,
        string? origenTipo,
        long? origenId,
        string? documentoOrigen,
        string? terceroDocumento,
        string? terceroNombre,
        string motivo,
        int idEmpleado,
        CancellationToken cancellationToken = default)
    {
        return EjecutarAsync(
            "inv.sp_devolucion_inventario_registrar",
            cancellationToken,
            p =>
            {
                AddText(p, "@tipo", tipo, 20);
                Add(p, "@id_empresa", SqlDbType.Int, idEmpresa);
                Add(p, "@id_almacen", SqlDbType.Int, idAlmacen);
                Add(p, "@id_producto", SqlDbType.Int, idProducto);
                AddDecimal(p, "@cantidad", cantidad);
                AddTextNullable(p, "@origen_tipo", origenTipo, 50);
                AddNullable(p, "@origen_id", SqlDbType.BigInt, origenId);
                AddTextNullable(p, "@documento_origen", documentoOrigen, 100);
                AddTextNullable(p, "@tercero_documento", terceroDocumento, 30);
                AddTextNullable(p, "@tercero_nombre", terceroNombre, 200);
                AddText(p, "@motivo", motivo, 300);
                Add(p, "@id_empleado", SqlDbType.Int, idEmpleado);
            }
        );
    }


    public async Task<List<DevolucionInventarioItem>>
        ListarDevolucionesAsync(
            int idEmpresa,
            string? tipo = null,
            CancellationToken cancellationToken = default)
    {
        List<DevolucionInventarioItem> items = new();

        await using SqlConnection cn = CrearConexion();
        await using SqlCommand cmd =
            CrearProcedimiento(
                cn,
                "inv.sp_devolucion_inventario_listar"
            );

        Add(cmd, "@id_empresa", SqlDbType.Int, idEmpresa);
        AddTextNullable(cmd, "@tipo", tipo, 20);

        await cn.OpenAsync(cancellationToken);

        await using SqlDataReader rd =
            await cmd.ExecuteReaderAsync(cancellationToken);

        while (await rd.ReadAsync(cancellationToken))
        {
            items.Add(
                new DevolucionInventarioItem
                {
                    IdDevolucion =
                        L(rd, "IdDevolucion"),

                    NumeroDevolucion =
                        S(rd, "NumeroDevolucion"),

                    Tipo =
                        S(rd, "Tipo"),

                    FechaRegistro =
                        F(rd, "FechaRegistro"),

                    Almacen =
                        S(rd, "Almacen"),

                    CodigoProducto =
                        S(rd, "CodigoProducto"),

                    Producto =
                        S(rd, "Producto"),

                    Cantidad =
                        D(rd, "Cantidad"),

                    OrigenTipo =
                        SN(rd, "OrigenTipo"),

                    OrigenId =
                        LN(rd, "OrigenId"),

                    DocumentoOrigen =
                        SN(rd, "DocumentoOrigen"),

                    TerceroDocumento =
                        SN(rd, "TerceroDocumento"),

                    TerceroNombre =
                        SN(rd, "TerceroNombre"),

                    Motivo =
                        S(rd, "Motivo"),

                    IdMovimientoInventario =
                        LN(rd, "IdMovimientoInventario")
                }
            );
        }

        return items;
    }


    public async Task<KardexResultado>
        ListarKardexAsync(
            KardexFiltro filtro,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filtro);

        filtro.Pagina =
            filtro.Pagina <= 0
                ? 1
                : filtro.Pagina;

        filtro.TamanoPagina =
            filtro.TamanoPagina <= 0
                ? 50
                : Math.Min(
                    filtro.TamanoPagina,
                    200
                );

        KardexResultado resultado =
            new()
            {
                Pagina = filtro.Pagina,
                TamanoPagina = filtro.TamanoPagina
            };

        await using SqlConnection cn = CrearConexion();
        await using SqlCommand cmd =
            CrearProcedimiento(
                cn,
                "inv.sp_kardex_avanzado_listar"
            );

        Add(cmd, "@id_empresa", SqlDbType.Int, filtro.IdEmpresa);
        AddNullable(cmd, "@id_sucursal", SqlDbType.Int, filtro.IdSucursal);
        AddNullable(cmd, "@id_almacen", SqlDbType.Int, filtro.IdAlmacen);
        AddNullable(cmd, "@id_producto", SqlDbType.Int, filtro.IdProducto);
        AddTextNullable(cmd, "@codigo_movimiento", filtro.CodigoMovimiento, 50);
        AddDateNullable(cmd, "@fecha_desde", filtro.FechaDesde);
        AddDateNullable(cmd, "@fecha_hasta", filtro.FechaHasta);
        AddTextNullable(cmd, "@buscar", filtro.Buscar, 200);
        Add(cmd, "@pagina", SqlDbType.Int, filtro.Pagina);
        Add(cmd, "@tamano_pagina", SqlDbType.Int, filtro.TamanoPagina);

        await cn.OpenAsync(cancellationToken);

        await using SqlDataReader rd =
            await cmd.ExecuteReaderAsync(cancellationToken);

        while (await rd.ReadAsync(cancellationToken))
        {
            resultado.Items.Add(
                new KardexItem
                {
                    IdMovimientoInventario =
                        L(rd, "IdMovimientoInventario"),

                    FechaMovimiento =
                        F(rd, "FechaMovimiento"),

                    IdSucursal =
                        I(rd, "IdSucursal"),

                    Sucursal =
                        S(rd, "Sucursal"),

                    IdAlmacen =
                        I(rd, "IdAlmacen"),

                    Almacen =
                        S(rd, "Almacen"),

                    IdProducto =
                        I(rd, "IdProducto"),

                    CodigoProducto =
                        S(rd, "CodigoProducto"),

                    Producto =
                        S(rd, "Producto"),

                    CodigoMovimiento =
                        S(rd, "CodigoMovimiento"),

                    TipoMovimiento =
                        S(rd, "TipoMovimiento"),

                    Naturaleza =
                        S(rd, "Naturaleza"),

                    Cantidad =
                        D(rd, "Cantidad"),

                    StockAnterior =
                        D(rd, "StockAnterior"),

                    StockNuevo =
                        D(rd, "StockNuevo"),

                    Referencia =
                        SN(rd, "Referencia"),

                    Motivo =
                        SN(rd, "Motivo"),

                    IdEmpleado =
                        I(rd, "IdEmpleado"),

                    Modulo =
                        SN(rd, "Modulo"),

                    OrigenTipo =
                        SN(rd, "OrigenTipo"),

                    OrigenId =
                        LN(rd, "OrigenId"),

                    DocumentoOrigen =
                        SN(rd, "DocumentoOrigen"),

                    EstaRevertido =
                        B(rd, "EstaRevertido"),

                    IdMovimientoReversa =
                        LN(rd, "IdMovimientoReversa")
                }
            );
        }

        if (
            await rd.NextResultAsync(cancellationToken)
            &&
            await rd.ReadAsync(cancellationToken)
        )
        {
            resultado.TotalRegistros =
                L(rd, "TotalRegistros");
        }

        return resultado;
    }


    private async Task EjecutarAsync(
        string procedimiento,
        CancellationToken cancellationToken,
        Action<SqlCommand> configurar)
    {
        await using SqlConnection cn = CrearConexion();
        await using SqlCommand cmd =
            CrearProcedimiento(
                cn,
                procedimiento
            );

        configurar(cmd);

        await cn.OpenAsync(cancellationToken);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }


    private SqlConnection CrearConexion()
    {
        return new SqlConnection(_connectionString);
    }


    private static SqlCommand CrearProcedimiento(
        SqlConnection cn,
        string nombre)
    {
        return new SqlCommand(nombre, cn)
        {
            CommandType = CommandType.StoredProcedure,
            CommandTimeout = 90
        };
    }


    private static void Add(
        SqlCommand cmd,
        string nombre,
        SqlDbType tipo,
        object valor)
    {
        SqlParameter p =
            cmd.Parameters.Add(nombre, tipo);

        p.Value = valor;
    }


    private static void AddNullable(
        SqlCommand cmd,
        string nombre,
        SqlDbType tipo,
        object? valor)
    {
        SqlParameter p =
            cmd.Parameters.Add(nombre, tipo);

        p.Value =
            valor
            ?? DBNull.Value;
    }


    private static void AddDecimal(
        SqlCommand cmd,
        string nombre,
        decimal valor)
    {
        SqlParameter p =
            cmd.Parameters.Add(
                nombre,
                SqlDbType.Decimal
            );

        p.Precision = 18;
        p.Scale = 4;
        p.Value = valor;
    }


    private static void AddText(
        SqlCommand cmd,
        string nombre,
        string valor,
        int longitud)
    {
        SqlParameter p =
            cmd.Parameters.Add(
                nombre,
                SqlDbType.NVarChar,
                longitud
            );

        p.Value =
            valor.Trim();
    }


    private static void AddTextNullable(
        SqlCommand cmd,
        string nombre,
        string? valor,
        int longitud)
    {
        SqlParameter p =
            cmd.Parameters.Add(
                nombre,
                SqlDbType.NVarChar,
                longitud
            );

        p.Value =
            string.IsNullOrWhiteSpace(valor)
                ? DBNull.Value
                : valor.Trim();
    }


    private static void AddDateNullable(
        SqlCommand cmd,
        string nombre,
        DateTime? valor)
    {
        SqlParameter p =
            cmd.Parameters.Add(
                nombre,
                SqlDbType.Date
            );

        p.Value =
            valor.HasValue
                ? valor.Value.Date
                : DBNull.Value;
    }


    private static int O(
        SqlDataReader rd,
        string nombre)
    {
        for (int i = 0; i < rd.FieldCount; i++)
        {
            if (
                string.Equals(
                    rd.GetName(i),
                    nombre,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return i;
            }
        }

        return -1;
    }


    private static int I(
        SqlDataReader rd,
        string nombre)
    {
        int o = O(rd, nombre);

        return o < 0 || rd.IsDBNull(o)
            ? 0
            : Convert.ToInt32(rd.GetValue(o));
    }


    private static long L(
        SqlDataReader rd,
        string nombre)
    {
        int o = O(rd, nombre);

        return o < 0 || rd.IsDBNull(o)
            ? 0L
            : Convert.ToInt64(rd.GetValue(o));
    }


    private static long? LN(
        SqlDataReader rd,
        string nombre)
    {
        int o = O(rd, nombre);

        return o < 0 || rd.IsDBNull(o)
            ? null
            : Convert.ToInt64(rd.GetValue(o));
    }


    private static decimal D(
        SqlDataReader rd,
        string nombre)
    {
        int o = O(rd, nombre);

        return o < 0 || rd.IsDBNull(o)
            ? 0M
            : Convert.ToDecimal(rd.GetValue(o));
    }


    private static decimal? DN(
        SqlDataReader rd,
        string nombre)
    {
        int o = O(rd, nombre);

        return o < 0 || rd.IsDBNull(o)
            ? null
            : Convert.ToDecimal(rd.GetValue(o));
    }


    private static bool B(
        SqlDataReader rd,
        string nombre)
    {
        int o = O(rd, nombre);

        return o >= 0
            && !rd.IsDBNull(o)
            && Convert.ToBoolean(rd.GetValue(o));
    }


    private static string S(
        SqlDataReader rd,
        string nombre)
    {
        return SN(rd, nombre)
            ?? string.Empty;
    }


    private static string? SN(
        SqlDataReader rd,
        string nombre)
    {
        int o = O(rd, nombre);

        return o < 0 || rd.IsDBNull(o)
            ? null
            : Convert.ToString(rd.GetValue(o));
    }


    private static DateTime F(
        SqlDataReader rd,
        string nombre)
    {
        int o = O(rd, nombre);

        return o < 0 || rd.IsDBNull(o)
            ? DateTime.MinValue
            : Convert.ToDateTime(rd.GetValue(o));
    }


    private static DateTime? FN(
        SqlDataReader rd,
        string nombre)
    {
        int o = O(rd, nombre);

        return o < 0 || rd.IsDBNull(o)
            ? null
            : Convert.ToDateTime(rd.GetValue(o));
    }
}
