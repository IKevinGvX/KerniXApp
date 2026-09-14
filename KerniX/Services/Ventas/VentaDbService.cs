using System.Data;
using KerniX.Models.Ventas.Venta;
using Microsoft.Data.SqlClient;

namespace KerniX.Services.Ventas;

public sealed class VentaDbService
    : IVentaDbService
{
    private readonly string _connectionString;
    private readonly ILogger<VentaDbService> _logger;

    public VentaDbService(
        IConfiguration configuration,
        ILogger<VentaDbService> logger)
    {
        _connectionString =
            configuration.GetConnectionString(
                "KerniXDb")
            ?? throw new InvalidOperationException(
                "No se encontró la conexión KerniXDb.");

        _logger = logger;
    }

    // =========================================================
    // REGISTRAR VENTA
    // =========================================================

    public async Task<VentaResponse> RegistrarAsync(
        VentaCrearRequest request)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        ValidarVenta(
            request);

        await using var connection =
            new SqlConnection(
                _connectionString);

        await connection.OpenAsync();

        await using var command =
            new SqlCommand(
                "ven.sp_venta_registrar",
                connection);

        command.CommandType =
            CommandType.StoredProcedure;

        command.Parameters.Add(
            new SqlParameter(
                "@id_empresa",
                SqlDbType.Int)
            {
                Value =
                    request.IdEmpresa
            });

        command.Parameters.Add(
            new SqlParameter(
                "@id_sucursal",
                SqlDbType.Int)
            {
                Value =
                    request.IdSucursal
            });

        command.Parameters.Add(
            new SqlParameter(
                "@id_cliente",
                SqlDbType.Int)
            {
                Value =
                    request.IdCliente
            });

        command.Parameters.Add(
            new SqlParameter(
                "@id_empleado",
                SqlDbType.Int)
            {
                Value =
                    request.IdEmpleado
            });

        command.Parameters.Add(
            new SqlParameter(
                "@id_almacen",
                SqlDbType.Int)
            {
                Value =
                    request.IdAlmacen
            });

        command.Parameters.Add(
            new SqlParameter(
                "@id_pedido",
                SqlDbType.BigInt)
            {
                Value =
                    request.IdPedido.HasValue
                        ? request.IdPedido.Value
                        : DBNull.Value
            });

        command.Parameters.Add(
            new SqlParameter(
                "@numero_venta",
                SqlDbType.NVarChar,
                30)
            {
                Value =
                    request.NumeroVenta
            });

        command.Parameters.Add(
            new SqlParameter(
                "@observacion",
                SqlDbType.NVarChar,
                500)
            {
                Value =
                    string.IsNullOrWhiteSpace(
                        request.Observacion)
                        ? DBNull.Value
                        : request.Observacion.Trim()
            });

        // =====================================================
        // TVP DETALLES
        // =====================================================

        var tablaDetalles =
            CrearTablaDetalles(
                request.Detalles);

        var parametroDetalles =
            command.Parameters.AddWithValue(
                "@detalles",
                tablaDetalles);

        parametroDetalles.SqlDbType =
            SqlDbType.Structured;

        parametroDetalles.TypeName =
            "ven.tt_detalle_venta";

        try
        {
            await using var reader =
                await command.ExecuteReaderAsync();

            VentaResponse? venta =
                await LeerVentaAsync(
                    reader);

            if (venta is null)
            {
                throw new InvalidOperationException(
                    "El procedimiento no devolvió la venta registrada.");
            }

            // Segundo result set:
            // detalle_venta
            if (await reader.NextResultAsync())
            {
                venta.Detalles =
                    await LeerDetallesAsync(
                        reader);
            }

            _logger.LogInformation(
                "Venta registrada correctamente. " +
                "IdVenta={IdVenta} | Numero={NumeroVenta}",
                venta.IdVenta,
                venta.NumeroVenta);

            return venta;
        }
        catch (SqlException ex)
        {
            _logger.LogError(
                ex,
                "Error SQL registrando venta. " +
                "Empresa={IdEmpresa} | Cliente={IdCliente}",
                request.IdEmpresa,
                request.IdCliente);

            throw;
        }
    }

    // =========================================================
    // OBTENER VENTA
    // =========================================================

    public async Task<VentaResponse?> ObtenerAsync(
        long idVenta)
    {
        if (idVenta <= 0)
        {
            return null;
        }

        await using var connection =
            new SqlConnection(
                _connectionString);

        await connection.OpenAsync();

        await using var command =
            new SqlCommand(
                "ven.sp_venta_obtener",
                connection);

        command.CommandType =
            CommandType.StoredProcedure;

        command.Parameters.Add(
            new SqlParameter(
                "@id_venta",
                SqlDbType.BigInt)
            {
                Value =
                    idVenta
            });

        try
        {
            await using var reader =
                await command.ExecuteReaderAsync();

            VentaResponse? venta =
                await LeerVentaAsync(
                    reader);

            if (venta is null)
            {
                return null;
            }

            if (await reader.NextResultAsync())
            {
                venta.Detalles =
                    await LeerDetallesAsync(
                        reader);
            }

            // sp_venta_obtener devuelve además:
            //
            // 3er result set = pagos
            // 4to result set = comprobantes
            //
            // Los leeremos posteriormente desde
            // PagoDbService y ComprobanteDbService.

            return venta;
        }
        catch (SqlException ex)
        {
            _logger.LogError(
                ex,
                "Error obteniendo venta {IdVenta}.",
                idVenta);

            throw;
        }
    }

    // =========================================================
    // ANULAR VENTA
    // =========================================================

    public async Task AnularAsync(
        VentaAnularRequest request)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        if (request.IdVenta <= 0)
        {
            throw new ArgumentException(
                "El IdVenta no es válido.",
                nameof(request));
        }

        if (request.IdEmpleado <= 0)
        {
            throw new ArgumentException(
                "El IdEmpleado no es válido.",
                nameof(request));
        }

        if (request.IdAlmacen <= 0)
        {
            throw new ArgumentException(
                "El IdAlmacen no es válido.",
                nameof(request));
        }

        if (string.IsNullOrWhiteSpace(
            request.Motivo))
        {
            throw new ArgumentException(
                "Debe indicar el motivo de la anulación.",
                nameof(request));
        }

        await using var connection =
            new SqlConnection(
                _connectionString);

        await connection.OpenAsync();

        await using var command =
            new SqlCommand(
                "ven.sp_venta_anular",
                connection);

        command.CommandType =
            CommandType.StoredProcedure;

        command.Parameters.Add(
            new SqlParameter(
                "@id_venta",
                SqlDbType.BigInt)
            {
                Value =
                    request.IdVenta
            });

        command.Parameters.Add(
            new SqlParameter(
                "@id_empleado",
                SqlDbType.Int)
            {
                Value =
                    request.IdEmpleado
            });

        command.Parameters.Add(
            new SqlParameter(
                "@id_almacen",
                SqlDbType.Int)
            {
                Value =
                    request.IdAlmacen
            });

        command.Parameters.Add(
            new SqlParameter(
                "@motivo",
                SqlDbType.NVarChar,
                500)
            {
                Value =
                    request.Motivo.Trim()
            });

        try
        {
            await command.ExecuteNonQueryAsync();

            _logger.LogInformation(
                "Venta anulada correctamente. " +
                "IdVenta={IdVenta}",
                request.IdVenta);
        }
        catch (SqlException ex)
        {
            _logger.LogError(
                ex,
                "Error anulando venta {IdVenta}.",
                request.IdVenta);

            throw;
        }
    }

    // =========================================================
    // TVP
    // =========================================================

    private static DataTable CrearTablaDetalles(
        IEnumerable<VentaDetalleCrearRequest> detalles)
    {
        var tabla =
            new DataTable();

        tabla.Columns.Add(
            "id_producto",
            typeof(int));

        tabla.Columns.Add(
            "cantidad",
            typeof(decimal));

        tabla.Columns.Add(
            "precio_unitario",
            typeof(decimal));

        tabla.Columns.Add(
            "descuento",
            typeof(decimal));

        tabla.Columns.Add(
            "impuesto",
            typeof(decimal));

        foreach (var detalle in detalles)
        {
            tabla.Rows.Add(
                detalle.IdProducto,
                detalle.Cantidad,
                detalle.PrecioUnitario,
                detalle.Descuento,
                detalle.Impuesto);
        }

        return tabla;
    }

    // =========================================================
    // LEER CABECERA
    // =========================================================

    private static async Task<VentaResponse?>
        LeerVentaAsync(
            SqlDataReader reader)
    {
        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new VentaResponse
        {
            IdVenta =
                ObtenerInt64(
                    reader,
                    "id_venta"),

            IdEmpresa =
                ObtenerInt32(
                    reader,
                    "id_empresa"),

            IdSucursal =
                ObtenerInt32(
                    reader,
                    "id_sucursal"),

            IdCliente =
                ObtenerInt32(
                    reader,
                    "id_cliente"),

            IdEmpleado =
                ObtenerInt32(
                    reader,
                    "id_empleado"),

            IdPedido =
                ObtenerInt64Nullable(
                    reader,
                    "id_pedido"),

            NumeroVenta =
                ObtenerString(
                    reader,
                    "numero_venta"),

            FechaVenta =
                ObtenerDateTime(
                    reader,
                    "fecha_venta"),

            Subtotal =
                ObtenerDecimal(
                    reader,
                    "subtotal"),

            Descuento =
                ObtenerDecimal(
                    reader,
                    "descuento"),

            Impuesto =
                ObtenerDecimal(
                    reader,
                    "impuesto"),

            Total =
                ObtenerDecimal(
                    reader,
                    "total"),

            Estado =
                ObtenerString(
                    reader,
                    "estado"),

            Observacion =
                ObtenerStringNullable(
                    reader,
                    "observacion"),

            Cliente =
                ObtenerString(
                    reader,
                    "cliente"),

            Vendedor =
                ObtenerString(
                    reader,
                    "vendedor"),

            Sucursal =
                ObtenerString(
                    reader,
                    "sucursal")
        };
    }

    // =========================================================
    // LEER DETALLE
    // =========================================================

    private static async Task<
        IReadOnlyList<VentaDetalleResponse>>
        LeerDetallesAsync(
            SqlDataReader reader)
    {
        var detalles =
            new List<VentaDetalleResponse>();

        while (await reader.ReadAsync())
        {
            detalles.Add(
                new VentaDetalleResponse
                {
                    IdDetalleVenta =
                        ObtenerInt64(
                            reader,
                            "id_detalle_venta"),

                    IdVenta =
                        ObtenerInt64(
                            reader,
                            "id_venta"),

                    IdProducto =
                        ObtenerInt32(
                            reader,
                            "id_producto"),

                    Cantidad =
                        ObtenerDecimal(
                            reader,
                            "cantidad"),

                    PrecioUnitario =
                        ObtenerDecimal(
                            reader,
                            "precio_unitario"),

                    Descuento =
                        ObtenerDecimal(
                            reader,
                            "descuento"),

                    Impuesto =
                        ObtenerDecimal(
                            reader,
                            "impuesto"),

                    Subtotal =
                        ObtenerDecimal(
                            reader,
                            "subtotal"),

                    Total =
                        ObtenerDecimal(
                            reader,
                            "total"),

                    Codigo =
                        ObtenerString(
                            reader,
                            "codigo"),

                    Producto =
                        ObtenerString(
                            reader,
                            "producto")
                });
        }

        return detalles;
    }

    // =========================================================
    // VALIDAR REQUEST
    // =========================================================

    private static void ValidarVenta(
        VentaCrearRequest request)
    {
        if (request.IdEmpresa <= 0)
        {
            throw new ArgumentException(
                "La empresa no es válida.");
        }

        if (request.IdSucursal <= 0)
        {
            throw new ArgumentException(
                "La sucursal no es válida.");
        }

        if (request.IdCliente <= 0)
        {
            throw new ArgumentException(
                "El cliente no es válido.");
        }

        if (request.IdEmpleado <= 0)
        {
            throw new ArgumentException(
                "El empleado no es válido.");
        }

        if (request.IdAlmacen <= 0)
        {
            throw new ArgumentException(
                "El almacén no es válido.");
        }

        if (string.IsNullOrWhiteSpace(
            request.NumeroVenta))
        {
            throw new ArgumentException(
                "Debe indicar el número de venta.");
        }

        if (request.Detalles is null ||
            request.Detalles.Count == 0)
        {
            throw new ArgumentException(
                "La venta debe contener productos.");
        }

        if (request.Detalles.Any(
            x => x.IdProducto <= 0))
        {
            throw new ArgumentException(
                "Existe un producto inválido.");
        }

        if (request.Detalles.Any(
            x => x.Cantidad <= 0))
        {
            throw new ArgumentException(
                "Las cantidades deben ser mayores que cero.");
        }

        if (request.Detalles.Any(
            x => x.PrecioUnitario < 0))
        {
            throw new ArgumentException(
                "Los precios no pueden ser negativos.");
        }

        if (request.Detalles.Any(
            x => x.Descuento < 0))
        {
            throw new ArgumentException(
                "Los descuentos no pueden ser negativos.");
        }

        if (request.Detalles.Any(
            x => x.Impuesto < 0))
        {
            throw new ArgumentException(
                "Los impuestos no pueden ser negativos.");
        }
    }

    // =========================================================
    // LECTORES SEGUROS
    // =========================================================

    private static int ObtenerOrdinal(
        SqlDataReader reader,
        string columna)
    {
        return reader.GetOrdinal(
            columna);
    }

    private static int ObtenerInt32(
        SqlDataReader reader,
        string columna)
    {
        int ordinal =
            ObtenerOrdinal(
                reader,
                columna);

        return reader.IsDBNull(
            ordinal)
                ? 0
                : Convert.ToInt32(
                    reader.GetValue(
                        ordinal));
    }

    private static long ObtenerInt64(
        SqlDataReader reader,
        string columna)
    {
        int ordinal =
            ObtenerOrdinal(
                reader,
                columna);

        return reader.IsDBNull(
            ordinal)
                ? 0L
                : Convert.ToInt64(
                    reader.GetValue(
                        ordinal));
    }

    private static long? ObtenerInt64Nullable(
        SqlDataReader reader,
        string columna)
    {
        int ordinal =
            ObtenerOrdinal(
                reader,
                columna);

        if (reader.IsDBNull(
            ordinal))
        {
            return null;
        }

        return Convert.ToInt64(
            reader.GetValue(
                ordinal));
    }

    private static decimal ObtenerDecimal(
        SqlDataReader reader,
        string columna)
    {
        int ordinal =
            ObtenerOrdinal(
                reader,
                columna);

        return reader.IsDBNull(
            ordinal)
                ? 0m
                : Convert.ToDecimal(
                    reader.GetValue(
                        ordinal));
    }

    private static DateTime ObtenerDateTime(
        SqlDataReader reader,
        string columna)
    {
        int ordinal =
            ObtenerOrdinal(
                reader,
                columna);

        return reader.IsDBNull(
            ordinal)
                ? DateTime.MinValue
                : Convert.ToDateTime(
                    reader.GetValue(
                        ordinal));
    }

    private static string ObtenerString(
        SqlDataReader reader,
        string columna)
    {
        int ordinal =
            ObtenerOrdinal(
                reader,
                columna);

        return reader.IsDBNull(
            ordinal)
                ? string.Empty
                : reader.GetValue(
                        ordinal)
                    ?.ToString()
                    ?? string.Empty;
    }

    private static string? ObtenerStringNullable(
        SqlDataReader reader,
        string columna)
    {
        int ordinal =
            ObtenerOrdinal(
                reader,
                columna);

        if (reader.IsDBNull(
            ordinal))
        {
            return null;
        }

        return reader.GetValue(
                ordinal)
            ?.ToString();
    }
}