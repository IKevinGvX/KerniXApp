
using System.Data;
using KerniX.Models.Pedido;
using Microsoft.Data.SqlClient;

namespace KerniX.Services.Pedido;

public sealed class PedidoService : IPedidoService
{
    private readonly string _connectionString;

    private const int IdEmpresa = 1;
    private const int IdSucursal = 1;

    private const decimal MaximoSql =
        99999999999999.9999m;

    public async Task CambiarEstadoAsync(
        long idPedido,
        int idEmpresa,
        int idSucursal,
        string nuevoEstado,
        CancellationToken cancellationToken = default)
    {
        // ========================================
        // VALIDAR PARÁMETROS
        // ========================================

        if (idPedido <= 0)
        {
            throw new ArgumentException(
                "El identificador del pedido no es válido."
            );
        }

        if (idEmpresa <= 0 || idSucursal <= 0)
        {
            throw new ArgumentException(
                "El contexto comercial no es válido."
            );
        }

        nuevoEstado = (nuevoEstado ?? string.Empty)
            .Trim()
            .ToLowerInvariant();

        if (
            nuevoEstado != "aprobado" &&
            nuevoEstado != "cancelado"
        )
        {
            throw new ArgumentException(
                "La operación solicitada no está permitida."
            );
        }

        // ========================================
        // CONEXIÓN
        // ========================================

        await using var conexion =
            new SqlConnection(_connectionString);

        await conexion.OpenAsync(cancellationToken);

        await using var transaccion =
            (SqlTransaction)await conexion.BeginTransactionAsync(
                IsolationLevel.ReadCommitted,
                cancellationToken
            );

        try
        {
            // ====================================
            // VALIDAR EMPRESA Y SUCURSAL
            // ====================================

            const string sqlValidacion = """
            SELECT COUNT_BIG(1)
            FROM ven.pedido WITH (UPDLOCK, HOLDLOCK)
            WHERE id_pedido = @id_pedido
              AND id_empresa = @id_empresa
              AND id_sucursal = @id_sucursal;
            """;

            await using (
                var validar = new SqlCommand(
                    sqlValidacion,
                    conexion,
                    transaccion
                )
            )
            {
                validar.Parameters.Add(
                    "@id_pedido",
                    SqlDbType.BigInt
                ).Value = idPedido;

                validar.Parameters.Add(
                    "@id_empresa",
                    SqlDbType.Int
                ).Value = idEmpresa;

                validar.Parameters.Add(
                    "@id_sucursal",
                    SqlDbType.Int
                ).Value = idSucursal;

                object? resultado =
                    await validar.ExecuteScalarAsync(
                        cancellationToken
                    );

                long encontrados =
                    resultado is null || resultado == DBNull.Value
                        ? 0L
                        : Convert.ToInt64(resultado);

                if (encontrados != 1)
                {
                    throw new ArgumentException(
                        "El pedido no existe o no pertenece a la sucursal autorizada."
                    );
                }
            }

            // ====================================
            // EJECUTAR PROCEDIMIENTO EXISTENTE
            // ====================================

            await using var comando =
                new SqlCommand(
                    "ven.sp_pedido_cambiar_estado",
                    conexion,
                    transaccion
                )
                {
                    CommandType = CommandType.StoredProcedure,
                    CommandTimeout = 30
                };

            comando.Parameters.Add(
                "@id_pedido",
                SqlDbType.BigInt
            ).Value = idPedido;

            comando.Parameters.Add(
                "@estado",
                SqlDbType.NVarChar,
                30
            ).Value = nuevoEstado;

            // El procedimiento SQL valida:
            // - Estado actual.
            // - Transiciones permitidas.
            // - Ventas vinculadas.
            // - Cantidades atendidas.

            await comando.ExecuteNonQueryAsync(
                cancellationToken
            );

            // ====================================
            // CONFIRMAR
            // ====================================

            await transaccion.CommitAsync(
                cancellationToken
            );
        }
        catch
        {
            try
            {
                await transaccion.RollbackAsync(
                    CancellationToken.None
                );
            }
            catch
            {
                // Si SQL Server ya revirtió la
                // transacción, conservamos el
                // error original.
            }

            throw;
        }
    }
    public async Task<PedidoDetalle?> ObtenerAsync(
        long idPedido,
        int idEmpresa,
        int idSucursal,
        CancellationToken cancellationToken = default)
    {
        // ========================================
        // VALIDAR PARÁMETROS
        // ========================================

        if (idPedido <= 0)
        {
            throw new ArgumentException(
                "El identificador del pedido no es válido."
            );
        }

        if (idEmpresa <= 0 || idSucursal <= 0)
        {
            throw new ArgumentException(
                "El contexto comercial no es válido."
            );
        }

        // ========================================
        // CONEXIÓN
        // ========================================

        await using var conexion =
            new SqlConnection(_connectionString);

        await using var comando =
            new SqlCommand(
                "ven.sp_pedido_obtener",
                conexion
            )
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 30
            };

        comando.Parameters.Add(
            "@id_pedido",
            SqlDbType.BigInt
        ).Value = idPedido;

        await conexion.OpenAsync(cancellationToken);

        await using var lector =
            await comando.ExecuteReaderAsync(
                cancellationToken
            );

        // ========================================
        // PRIMER RESULTADO: CABECERA
        // ========================================

        if (!await lector.ReadAsync(cancellationToken))
        {
            return null;
        }

        // El procedimiento no filtra por empresa
        // ni por sucursal. Verificamos el contexto
        // antes de devolver cualquier dato.

        int empresaPedido =
            lector.GetInt32(
                lector.GetOrdinal("id_empresa")
            );

        int sucursalPedido =
            lector.GetInt32(
                lector.GetOrdinal("id_sucursal")
            );

        if (
            empresaPedido != idEmpresa ||
            sucursalPedido != idSucursal
        )
        {
            return null;
        }

        // ========================================
        // LECTURAS SEGURAS
        // ========================================

        string LeerTexto(string columna)
        {
            int ordinal = lector.GetOrdinal(columna);

            return lector.IsDBNull(ordinal)
                ? string.Empty
                : lector.GetString(ordinal);
        }

        decimal LeerDecimal(string columna)
        {
            int ordinal = lector.GetOrdinal(columna);

            return lector.IsDBNull(ordinal)
                ? 0m
                : lector.GetDecimal(ordinal);
        }

        DateTime? LeerFechaNullable(string columna)
        {
            int ordinal = lector.GetOrdinal(columna);

            return lector.IsDBNull(ordinal)
                ? null
                : lector.GetDateTime(ordinal);
        }

        // ========================================
        // MAPEAR CABECERA
        // ========================================

        var pedido = new PedidoDetalle
        {
            IdPedido = lector.GetInt64(
                lector.GetOrdinal("id_pedido")
            ),

            IdEmpresa = empresaPedido,

            IdSucursal = sucursalPedido,

            IdCliente = lector.GetInt32(
                lector.GetOrdinal("id_cliente")
            ),

            IdEmpleado = lector.GetInt32(
                lector.GetOrdinal("id_empleado")
            ),

            IdCotizacion =
                lector.IsDBNull(
                    lector.GetOrdinal("id_cotizacion")
                )
                    ? null
                    : lector.GetInt64(
                        lector.GetOrdinal("id_cotizacion")
                    ),

            NumeroPedido =
                LeerTexto("numero_pedido"),

            FechaPedido =
                lector.GetDateTime(
                    lector.GetOrdinal("fecha_pedido")
                ),

            FechaEntregaEstimada =
                LeerFechaNullable("fecha_entrega_estimada"),

            Cliente = LeerTexto("cliente"),

            Vendedor = LeerTexto("vendedor"),

            Sucursal = LeerTexto("sucursal"),

            Estado = LeerTexto("estado"),

            Observacion = LeerTexto("observacion"),

            Subtotal = LeerDecimal("subtotal"),

            Descuento = LeerDecimal("descuento"),

            Impuesto = LeerDecimal("impuesto"),

            Total = LeerDecimal("total")
        };

        // ========================================
        // SEGUNDO RESULTADO: PRODUCTOS
        // ========================================

        if (!await lector.NextResultAsync(
            cancellationToken))
        {
            throw new InvalidOperationException(
                "El procedimiento no devolvió el detalle del pedido."
            );
        }

        while (await lector.ReadAsync(
            cancellationToken))
        {
            var producto = new PedidoDetalleProducto
            {
                IdDetallePedido =
                    lector.GetInt64(
                        lector.GetOrdinal(
                            "id_detalle_pedido"
                        )
                    ),

                IdProducto =
                    lector.GetInt32(
                        lector.GetOrdinal("id_producto")
                    ),

                Codigo =
                    LeerTexto("codigo"),

                Producto =
                    LeerTexto("producto"),

                CantidadSolicitada =
                    LeerDecimal("cantidad_solicitada"),

                CantidadAtendida =
                    LeerDecimal("cantidad_atendida"),

                PrecioUnitario =
                    LeerDecimal("precio_unitario"),

                Descuento =
                    LeerDecimal("descuento"),

                Impuesto =
                    LeerDecimal("impuesto"),

                Subtotal =
                    LeerDecimal("subtotal"),

                Total =
                    LeerDecimal("total")
            };

            pedido.Productos.Add(producto);
        }

        return pedido;
    }
    public PedidoService(
        IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("KerniXDb")
            ?? throw new InvalidOperationException(
                "Configura ConnectionStrings:KerniXDb."
            );
    }

    // ============================================
    // LISTAR PEDIDOS
    // ============================================

    public async Task<IReadOnlyList<PedidoFila>> ListarAsync(
        PedidoFiltro filtro,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filtro);

        if (filtro.IdEmpresa <= 0)
        {
            throw new ArgumentException(
                "La empresa no es válida."
            );
        }

        if (filtro.IdSucursal is <= 0)
        {
            throw new ArgumentException(
                "La sucursal no es válida."
            );
        }

        var resultado = new List<PedidoFila>();

        await using var conexion =
            new SqlConnection(_connectionString);

        await using var comando =
            new SqlCommand(
                "ven.sp_pedido_listar",
                conexion
            )
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 30
            };

        // EMPRESA

        comando.Parameters.Add(
            "@id_empresa",
            SqlDbType.Int
        ).Value = filtro.IdEmpresa;

        // SUCURSAL

        comando.Parameters.Add(
            "@id_sucursal",
            SqlDbType.Int
        ).Value = filtro.IdSucursal.HasValue
            ? filtro.IdSucursal.Value : DBNull.Value;

        // ESTADO

        comando.Parameters.Add(
            "@estado",
            SqlDbType.NVarChar,
            30
        ).Value =
            string.IsNullOrWhiteSpace(filtro.Estado)
                ? DBNull.Value
                : filtro.Estado.Trim().ToLowerInvariant();

        // BUSCADOR

        comando.Parameters.Add(
            "@buscar",
            SqlDbType.NVarChar,
            100
        ).Value =
            string.IsNullOrWhiteSpace(filtro.Buscar)
                ? DBNull.Value
                : filtro.Buscar.Trim()[..Math.Min(100, filtro.Buscar.Trim().Length)];

        await conexion.OpenAsync(cancellationToken);

        await using var lector =
            await comando.ExecuteReaderAsync(
                cancellationToken
            );

        // COLUMNAS

        int id =
            lector.GetOrdinal("id_pedido");


        int numero = lector.GetOrdinal("numero_pedido");

        int cliente =
            lector.GetOrdinal("cliente");

        int fecha =
            lector.GetOrdinal("fecha_pedido");

        int total =
            lector.GetOrdinal("total");

        int estado =
            lector.GetOrdinal("estado");

        int solicitada =
            lector.GetOrdinal("cantidad_solicitada");

        int atendida =
            lector.GetOrdinal("cantidad_atendida");

        int ventas =
            lector.GetOrdinal("ventas_vinculadas");

        int venta =
            lector.GetOrdinal("id_venta");

        int cotizacion =
            lector.GetOrdinal("id_cotizacion");

        // LEER RESULTADOS

        while (
            await lector.ReadAsync(cancellationToken)
        )
        {
            resultado.Add(
                new PedidoFila
                {
                    IdPedido =
                        lector.GetInt64(id),

                    Numero =
                        lector.GetString(numero),

                    Cliente =
                        lector.GetString(cliente),

                    Fecha =
                        lector.GetDateTime(fecha),

                    Total =
                        lector.GetDecimal(total),

                    Estado =
                        lector.GetString(estado),

                    CantidadSolicitada =
                        lector.GetDecimal(solicitada),

                    CantidadAtendida =
                        lector.GetDecimal(atendida),

                    VentasVinculadas =
                        lector.GetInt32(ventas),

                    IdVenta =
                        lector.IsDBNull(venta)
                            ? null
                            : lector.GetInt64(venta),

                    IdCotizacion =
                        lector.IsDBNull(cotizacion)
                            ? null
                            : lector.GetInt64(cotizacion)
                }
            );
        }

        return resultado;
    }

    // ============================================
    // REGISTRAR PEDIDO
    // ============================================

    public async Task<long> RegistrarAsync(
        PedidoRegistrarRequest request,
        int idEmpleado,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        // ========================================
        // VALIDAR EMPLEADO
        // ========================================

        if (idEmpleado <= 0)
        {
            throw new ArgumentException(
                "No se pudo identificar al empleado."
            );
        }

        // ========================================
        // VALIDAR CLIENTE
        // ========================================

        if (request.IdCliente <= 0)
        {
            throw new ArgumentException(
                "Selecciona un cliente válido."
            );
        }

        // ========================================
        // VALIDAR COTIZACIÓN
        // ========================================

        if (request.IdCotizacion is <= 0)
        {
            throw new ArgumentException(
                "La cotización seleccionada no es válida."
            );
        }

        // ========================================
        // VALIDAR OBSERVACIÓN
        // ========================================

        string? observacion =
            request.Observacion?.Trim();

        if (observacion?.Length > 500)
        {
            throw new ArgumentException(
                "La observación supera los 500 caracteres."
            );
        }

        // ========================================
        // VALIDAR PRODUCTOS
        // ========================================

        if (request.Detalles is null ||
            request.Detalles.Count == 0)
        {
            throw new ArgumentException(
                "Agrega al menos un producto."
            );
        }

        // PRODUCTOS DUPLICADOS

        if (request.Detalles
            .GroupBy(x => x.IdProducto)
            .Any(x => x.Count() > 1))
        {
            throw new ArgumentException(
                "No se permiten productos duplicados."
            );
        }

        decimal subtotalGeneral = 0m;
        decimal descuentoGeneral = 0m;
        decimal impuestoGeneral = 0m;

        foreach (var detalle in request.Detalles)
        {
            // IDENTIFICADOR

            if (detalle.IdProducto <= 0)
            {
                throw new ArgumentException(
                    "Existe un producto inválido."
                );
            }

            // CANTIDAD

            if (!DecimalValido(detalle.CantidadSolicitada) ||
                detalle.CantidadSolicitada <= 0)
            {
                throw new ArgumentException(
                    "La cantidad solicitada no es válida."
                );
            }

            // PRECIO

            if (!DecimalValido(detalle.PrecioUnitario))
            {
                throw new ArgumentException(
                    "El precio del producto no es válido."
                );
            }

            // DESCUENTO

            if (!DecimalValido(detalle.Descuento))
            {
                throw new ArgumentException(
                    "El descuento no es válido."
                );
            }

            // IMPUESTO

            if (!DecimalValido(detalle.Impuesto))
            {
                throw new ArgumentException(
                    "El impuesto no es válido."
                );
            }

            // CALCULAR SUBTOTAL

            decimal subtotal = checked(
                detalle.CantidadSolicitada *
                detalle.PrecioUnitario
            );

            if (subtotal > MaximoSql)
            {
                throw new ArgumentException(
                    "El subtotal supera el importe permitido."
                );
            }

            // VALIDAR DESCUENTO

            if (detalle.Descuento > subtotal)
            {
                throw new ArgumentException(
                    "El descuento supera el subtotal."
                );
            }

            // TOTAL DEL DETALLE

            decimal totalDetalle = checked(
                subtotal
                - detalle.Descuento
                + detalle.Impuesto
            );

            if (totalDetalle < 0 ||
                totalDetalle > MaximoSql)
            {
                throw new ArgumentException(
                    "El total del producto no es válido."
                );
            }

            subtotalGeneral = checked(
                subtotalGeneral + subtotal
            );

            descuentoGeneral = checked(
                descuentoGeneral + detalle.Descuento
            );

            impuestoGeneral = checked(
                impuestoGeneral + detalle.Impuesto
            );
        }

        decimal totalGeneral = checked(
            subtotalGeneral
            - descuentoGeneral
            + impuestoGeneral
        );

        // VALIDAR TOTALES GENERALES

        if (subtotalGeneral > MaximoSql ||
            descuentoGeneral > MaximoSql ||
            impuestoGeneral > MaximoSql ||
            totalGeneral < 0 ||
            totalGeneral > MaximoSql)
        {
            throw new ArgumentException(
                "Los importes totales superan los límites permitidos."
            );
        }

        // SQL registra a escala 4; comprueba el límite acumulado.
        // Los precios, impuestos y descuentos requieren validación
        // contra reglas comerciales autorizadas antes de producción.

        // ========================================
        // CONEXIÓN SQL
        // ========================================

        await using var conexion =
            new SqlConnection(_connectionString);

        await using var comando =
            new SqlCommand(
                "ven.sp_pedido_registrar",
                conexion
            )
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 60
            };

        // ========================================
        // EMPRESA
        // ========================================

        comando.Parameters.Add(
            "@id_empresa",
            SqlDbType.Int
        ).Value = IdEmpresa;

        // ========================================
        // SUCURSAL
        // ========================================

        comando.Parameters.Add(
            "@id_sucursal",
            SqlDbType.Int
        ).Value = IdSucursal;

        // ========================================
        // CLIENTE
        // ========================================

        comando.Parameters.Add(
            "@id_cliente",
            SqlDbType.Int
        ).Value = request.IdCliente;

        // ========================================
        // EMPLEADO
        // ========================================

        comando.Parameters.Add(
            "@id_empleado",
            SqlDbType.Int
        ).Value = idEmpleado;

        // ========================================
        // COTIZACIÓN
        // ========================================

        comando.Parameters.Add(
            "@id_cotizacion",
            SqlDbType.BigInt
        ).Value =
            request.IdCotizacion.HasValue
                ? request.IdCotizacion.Value
                : DBNull.Value;

        // ========================================
        // NÚMERO
        // ========================================

        comando.Parameters.Add(
            "@numero_pedido",
            SqlDbType.NVarChar,
            30
        ).Value = DBNull.Value;

        // ========================================
        // FECHA ENTREGA
        // ========================================

        comando.Parameters.Add(
            "@fecha_entrega_estimada",
            SqlDbType.DateTime2
        ).Value =
            request.FechaEntregaEstimada.HasValue
                ? request.FechaEntregaEstimada.Value
                : DBNull.Value;

        // ========================================
        // OBSERVACIÓN
        // ========================================

        comando.Parameters.Add(
            "@observacion",
            SqlDbType.NVarChar,
            500
        ).Value =
            string.IsNullOrWhiteSpace(observacion)
                ? DBNull.Value
                : observacion;

        // ========================================
        // DETALLE DE PRODUCTOS
        // ========================================

        DataTable tablaDetalles =
            CrearDetallePedido(request.Detalles);

        var parametroDetalles =
            comando.Parameters.Add(
                "@detalles",
                SqlDbType.Structured
            );

        parametroDetalles.TypeName =
            "ven.tt_detalle_pedido";

        parametroDetalles.Value =
            tablaDetalles;

        // ========================================
        // EJECUTAR PROCEDIMIENTO
        // ========================================

        await conexion.OpenAsync(cancellationToken);

        await using var lector =
            await comando.ExecuteReaderAsync(
                cancellationToken
            );

        // El procedimiento ejecuta sp_pedido_obtener.
        // El primer result set devuelve la cabecera.

        if (!await lector.ReadAsync(cancellationToken))
        {
            throw new InvalidOperationException(
                "SQL Server no devolvió el pedido registrado."
            );
        }

        int ordinal =
            lector.GetOrdinal("id_pedido");

        long idPedido =
            lector.GetInt64(ordinal);

        // Consumir los resultados antes de cerrar el lector.

        while (await lector.ReadAsync(cancellationToken))
        {
        }

        while (await lector.NextResultAsync(cancellationToken))
        {
            while (await lector.ReadAsync(cancellationToken))
            {
            }
        }

        return idPedido;
    }

    // ============================================
    // CLIENTES: empresa 1; solo activos
    // ============================================
    public async Task<IReadOnlyList<PedidoClienteBusqueda>> BuscarClientesAsync(
        string buscar, CancellationToken cancellationToken = default)
    {
        buscar = (buscar ?? string.Empty).Trim();
        if (buscar.Length < 2) return Array.Empty<PedidoClienteBusqueda>();
        if (buscar.Length > 100) buscar = buscar[..100];

        const string sql = """
            SELECT TOP (15) c.id_cliente,
                COALESCE(NULLIF(LTRIM(RTRIM(c.razon_social)), N''),
                  NULLIF(LTRIM(RTRIM(CONCAT(c.nombres, N' ', c.apellidos))), N''),
                  N'Cliente sin nombre') AS nombre
            FROM ven.cliente AS c
            WHERE c.id_empresa = @empresa AND c.estado = 1
              AND (c.razon_social LIKE @buscar
                   OR CONCAT(c.nombres, N' ', c.apellidos) LIKE @buscar)
            ORDER BY nombre, c.id_cliente;
            """;
        var resultado = new List<PedidoClienteBusqueda>();
        await using var conexion = new SqlConnection(_connectionString);
        await using var comando = new SqlCommand(sql, conexion);
        comando.Parameters.Add("@empresa", SqlDbType.Int).Value = IdEmpresa;
        comando.Parameters.Add("@buscar", SqlDbType.NVarChar, 202).Value =
            "%" + EscaparLike(buscar) + "%";
        await conexion.OpenAsync(cancellationToken);
        await using var lector = await comando.ExecuteReaderAsync(cancellationToken);
        while (await lector.ReadAsync(cancellationToken))
        {
            resultado.Add(new PedidoClienteBusqueda
            {
                IdCliente = lector.GetInt32(0),
                Nombre = lector.GetString(1)
            });
        }

        return resultado;
    }

    // ============================================
    // PRODUCTOS: empresa 1; solo activos
    // No inventamos precios ni stock no verificados.
    // ============================================

    // ============================================
    // BUSCAR PRODUCTOS CON PRECIO DE VENTA
    // ============================================

    public async Task<IReadOnlyList<PedidoProductoBusqueda>>
        BuscarProductosAsync(
            string buscar,
            CancellationToken cancellationToken = default)
    {
        buscar = (buscar ?? string.Empty).Trim();

        if (buscar.Length < 2)
        {
            return Array.Empty<PedidoProductoBusqueda>();
        }

        if (buscar.Length > 100)
        {
            buscar = buscar[..100];
        }

        const string sql = """
        SELECT TOP (15)
            p.id_producto,
            p.codigo,
            p.nombre,
            p.precio_venta
        FROM inv.producto AS p
        WHERE p.id_empresa = @empresa
          AND p.estado = 1
          AND (
              p.codigo LIKE @buscar
              OR p.nombre LIKE @buscar
              OR p.codigo_barras LIKE @buscar
          )
        ORDER BY p.nombre, p.id_producto;
        """;

        var resultado = new List<PedidoProductoBusqueda>();

        await using var conexion =
            new SqlConnection(_connectionString);

        await using var comando =
            new SqlCommand(sql, conexion)
            {
                CommandTimeout = 30
            };

        comando.Parameters.Add(
            "@empresa",
            SqlDbType.Int
        ).Value = IdEmpresa;

        comando.Parameters.Add(
            "@buscar",
            SqlDbType.NVarChar,
            202
        ).Value =
            "%" + EscaparLike(buscar) + "%";

        await conexion.OpenAsync(cancellationToken);

        await using var lector =
            await comando.ExecuteReaderAsync(
                cancellationToken
            );

        while (await lector.ReadAsync(cancellationToken))
        {
            resultado.Add(
                new PedidoProductoBusqueda
                {
                    IdProducto = lector.GetInt32(0),

                    Codigo = lector.IsDBNull(1)
                        ? string.Empty
                        : lector.GetString(1),

                    Nombre = lector.IsDBNull(2)
                        ? string.Empty
                        : lector.GetString(2),

                    PrecioUnitario = lector.IsDBNull(3)
                        ? null
                        : lector.GetDecimal(3)
                }
            );
        }

        return resultado;
    }
    // Cotizaciones aprobadas del cliente seleccionado, sin pedido ni venta.
    public async Task<IReadOnlyList<PedidoCotizacionBusqueda>> BuscarCotizacionesAsync(
        int idCliente, CancellationToken cancellationToken = default)
    {
        if (idCliente <= 0) return Array.Empty<PedidoCotizacionBusqueda>();
        const string sql = """
            SELECT TOP (20) c.id_cotizacion
            FROM ven.cotizacion AS c
            WHERE c.id_empresa = @empresa AND c.id_sucursal = @sucursal
              AND c.id_cliente = @cliente
              AND UPPER(LTRIM(RTRIM(c.estado))) = N'APROBADA'
              AND NOT EXISTS (SELECT 1 FROM ven.pedido p WHERE p.id_cotizacion = c.id_cotizacion)
              AND NOT EXISTS (SELECT 1 FROM ven.venta v WHERE v.id_cotizacion = c.id_cotizacion)
            ORDER BY c.id_cotizacion DESC;
            """;
        var resultado = new List<PedidoCotizacionBusqueda>();
        await using var conexion = new SqlConnection(_connectionString);
        await using var comando = new SqlCommand(sql, conexion);
        comando.Parameters.Add("@empresa", SqlDbType.Int).Value = IdEmpresa;
        comando.Parameters.Add("@sucursal", SqlDbType.Int).Value = IdSucursal;
        comando.Parameters.Add("@cliente", SqlDbType.Int).Value = idCliente;
        await conexion.OpenAsync(cancellationToken);
        await using var lector = await comando.ExecuteReaderAsync(cancellationToken);
        while (await lector.ReadAsync(cancellationToken))
            resultado.Add(new PedidoCotizacionBusqueda { IdCotizacion = lector.GetInt64(0) });
        return resultado;
    }

    private static string EscaparLike(string valor) => valor
        .Replace("[", "[[]")
        .Replace("%", "[%]")
        .Replace("_", "[_]");

    // ============================================
    // VALIDAR DECIMAL(18,4)
    // ============================================

    private static bool DecimalValido(decimal valor)
    {
        return valor >= 0m
            && valor <= MaximoSql
            && decimal.Round(valor, 4) == valor;
    }

    // ============================================
    // CONSTRUIR DATATABLE
    // ============================================

    private static DataTable CrearDetallePedido(
        IReadOnlyList<PedidoDetalleRequest> detalles)
    {
        var tabla = new DataTable();

        // 1. PRODUCTO

        tabla.Columns.Add(
            "id_producto",
            typeof(int)
        );

        // 2. CANTIDAD SOLICITADA

        tabla.Columns.Add(
            "cantidad_solicitada",
            typeof(decimal)
        );

        // 3. CANTIDAD ATENDIDA

        tabla.Columns.Add(
            "cantidad_atendida",
            typeof(decimal)
        );

        // 4. PRECIO UNITARIO

        tabla.Columns.Add(
            "precio_unitario",
            typeof(decimal)
        );

        // 5. DESCUENTO

        tabla.Columns.Add(
            "descuento",
            typeof(decimal)
        );

        // 6. IMPUESTO

        tabla.Columns.Add(
            "impuesto",
            typeof(decimal)
        );

        // ========================================
        // AGREGAR PRODUCTOS
        // ========================================

        foreach (var detalle in detalles)
        {
            tabla.Rows.Add(
                detalle.IdProducto,
                detalle.CantidadSolicitada,
                0m,
                detalle.PrecioUnitario,
                detalle.Descuento,
                detalle.Impuesto
            );
        }

        return tabla;
    }
}