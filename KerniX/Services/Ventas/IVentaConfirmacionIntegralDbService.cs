using System.Data;
using KerniX.Models.Ventas.Venta;
using Microsoft.Data.SqlClient;
using static KerniX.Models.Ventas.Venta.VentaConfirmarRequest;

namespace KerniX.Services.Ventas;

public interface IVentaConfirmacionIntegralDbService
{
    Task<VentaConfirmacionIntegralResponse> ConfirmarAsync(
        VentaConfirmacionIntegralRequest request,
        CancellationToken cancellationToken = default);
}

public sealed class VentaConfirmacionIntegralRequest
{
    public int IdEmpresa { get; set; }
    public int IdSucursal { get; set; }
    public int IdCliente { get; set; }
    public int IdEmpleado { get; set; }
    public int IdAlmacen { get; set; }
    public long? IdPedido { get; set; }

    public string NumeroVenta { get; set; } = string.Empty;
    public string? Observacion { get; set; }

    public string MetodoPago { get; set; } = string.Empty;
    public string? NumeroOperacion { get; set; }
    public decimal MontoRecibido { get; set; }

    public string TipoComprobante { get; set; } = "SIN_COMPROBANTE";

    // Aquí Descuento llega como porcentaje.
    public List<VentaConfirmarDetalleRequest> Detalles { get; set; } = new();
}

public sealed class VentaConfirmacionIntegralResponse
{
    public long IdVenta { get; set; }
    public string NumeroVenta { get; set; } = string.Empty;

    public decimal VentaSubtotal { get; set; }
    public decimal VentaDescuento { get; set; }
    public decimal VentaImpuesto { get; set; }
    public decimal VentaTotal { get; set; }
    public string VentaEstado { get; set; } = string.Empty;

    public long IdPago { get; set; }
    public decimal PagoMonto { get; set; }
    public string MetodoPago { get; set; } = string.Empty;
    public string PagoEstado { get; set; } = string.Empty;

    public long? IdComprobante { get; set; }
    public string? TipoComprobante { get; set; }
    public string? Serie { get; set; }
    public string? Numero { get; set; }

    public decimal? ComprobanteSubtotal { get; set; }
    public decimal? ComprobanteImpuesto { get; set; }
    public decimal? ComprobanteTotal { get; set; }

    public string? EstadoSunat { get; set; }

    public string? NumeroCompleto =>
        string.IsNullOrWhiteSpace(Serie) ||
        string.IsNullOrWhiteSpace(Numero)
            ? null
            : $"{Serie}-{Numero}";
}

public sealed class VentaConfirmacionIntegralDbService
    : IVentaConfirmacionIntegralDbService
{
    private readonly string _connectionString;
    private readonly ILogger<VentaConfirmacionIntegralDbService> _logger;

    public VentaConfirmacionIntegralDbService(
        IConfiguration configuration,
        ILogger<VentaConfirmacionIntegralDbService> logger)
    {
        _connectionString =
            configuration.GetConnectionString("KerniXDb")
            ?? throw new InvalidOperationException(
                "No se encontró ConnectionStrings:KerniXDb.");

        _logger = logger;
    }

    public async Task<VentaConfirmacionIntegralResponse> ConfirmarAsync(
        VentaConfirmacionIntegralRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        Validar(request);

        await using var connection =
            new SqlConnection(_connectionString);

        await connection.OpenAsync(cancellationToken);

        await using var command =
            new SqlCommand(
                "ven.sp_venta_confirmar_integral",
                connection)
            {
                CommandType =
                    CommandType.StoredProcedure,

                CommandTimeout =
                    90
            };

        command.Parameters.Add(
            "@id_empresa",
            SqlDbType.Int
        ).Value =
            request.IdEmpresa;

        command.Parameters.Add(
            "@id_sucursal",
            SqlDbType.Int
        ).Value =
            request.IdSucursal;

        command.Parameters.Add(
            "@id_cliente",
            SqlDbType.Int
        ).Value =
            request.IdCliente;

        command.Parameters.Add(
            "@id_empleado",
            SqlDbType.Int
        ).Value =
            request.IdEmpleado;

        command.Parameters.Add(
            "@id_almacen",
            SqlDbType.Int
        ).Value =
            request.IdAlmacen;

        command.Parameters.Add(
            "@id_pedido",
            SqlDbType.BigInt
        ).Value =
            request.IdPedido.HasValue
                ? request.IdPedido.Value
                : DBNull.Value;

        command.Parameters.Add(
            "@numero_venta",
            SqlDbType.NVarChar,
            30
        ).Value =
            request.NumeroVenta.Trim();

        command.Parameters.Add(
            "@observacion",
            SqlDbType.NVarChar,
            500
        ).Value =
            string.IsNullOrWhiteSpace(
                request.Observacion)
                ? DBNull.Value
                : request.Observacion.Trim();

        DataTable detalles =
            CrearDetalles(
                request.Detalles);

        SqlParameter parametroDetalles =
            command.Parameters.Add(
                "@detalles",
                SqlDbType.Structured);

        parametroDetalles.TypeName =
            "ven.tt_detalle_venta";

        parametroDetalles.Value =
            detalles;

        command.Parameters.Add(
            "@metodo_pago",
            SqlDbType.NVarChar,
            30
        ).Value =
            request.MetodoPago.Trim();

        command.Parameters.Add(
            "@numero_operacion",
            SqlDbType.NVarChar,
            100
        ).Value =
            string.IsNullOrWhiteSpace(
                request.NumeroOperacion)
                ? DBNull.Value
                : request.NumeroOperacion.Trim();

        SqlParameter montoRecibido =
            command.Parameters.Add(
                "@monto_recibido",
                SqlDbType.Decimal);

        montoRecibido.Precision =
            18;

        montoRecibido.Scale =
            4;

        montoRecibido.Value =
            request.MontoRecibido;

        command.Parameters.Add(
            "@tipo_comprobante",
            SqlDbType.NVarChar,
            20
        ).Value =
            request.TipoComprobante.Trim();

        try
        {
            await using SqlDataReader reader =
                await command.ExecuteReaderAsync(
                    cancellationToken);

            // Los SP internos de pago/comprobante pueden devolver
            // resultsets informativos. Avanzamos hasta encontrar
            // el resultset FINAL del procedimiento integral.
            while (true)
            {
                if (TieneColumna(
                        reader,
                        "kx_confirmacion"))
                {
                    if (!await reader.ReadAsync(
                        cancellationToken))
                    {
                        throw new InvalidOperationException(
                            "El resultado final de la confirmación está vacío.");
                    }

                    VentaConfirmacionIntegralResponse result =
                        LeerResultado(reader);

                    _logger.LogInformation(
                        "Venta integral confirmada. " +
                        "Venta={IdVenta} | " +
                        "Pago={IdPago} | " +
                        "Comprobante={IdComprobante} | " +
                        "Total={Total}",
                        result.IdVenta,
                        result.IdPago,
                        result.IdComprobante,
                        result.VentaTotal);

                    return result;
                }

                if (!await reader.NextResultAsync(
                    cancellationToken))
                {
                    throw new InvalidOperationException(
                        "SQL Server no devolvió el resultset final " +
                        "de ven.sp_venta_confirmar_integral.");
                }
            }
        }
        catch (SqlException ex)
        {
            _logger.LogError(
                ex,
                "Error SQL en confirmación integral. " +
                "Pedido={IdPedido} | Cliente={IdCliente}",
                request.IdPedido,
                request.IdCliente);

            throw;
        }
    }

    private static VentaConfirmacionIntegralResponse
        LeerResultado(
            SqlDataReader reader)
    {
        return new VentaConfirmacionIntegralResponse
        {
            IdVenta =
                Convert.ToInt64(
                    reader["id_venta"]),

            NumeroVenta =
                reader["numero_venta"]
                    ?.ToString()
                ?? string.Empty,

            VentaSubtotal =
                Convert.ToDecimal(
                    reader["venta_subtotal"]),

            VentaDescuento =
                Convert.ToDecimal(
                    reader["venta_descuento"]),

            VentaImpuesto =
                Convert.ToDecimal(
                    reader["venta_impuesto"]),

            VentaTotal =
                Convert.ToDecimal(
                    reader["venta_total"]),

            VentaEstado =
                reader["venta_estado"]
                    ?.ToString()
                ?? string.Empty,

            IdPago =
                Convert.ToInt64(
                    reader["id_pago"]),

            PagoMonto =
                Convert.ToDecimal(
                    reader["pago_monto"]),

            MetodoPago =
                reader["metodo_pago"]
                    ?.ToString()
                ?? string.Empty,

            PagoEstado =
                reader["pago_estado"]
                    ?.ToString()
                ?? string.Empty,

            IdComprobante =
                reader["id_comprobante"] is DBNull
                    ? null
                    : Convert.ToInt64(
                        reader["id_comprobante"]),

            TipoComprobante =
                reader["tipo_comprobante"] is DBNull
                    ? null
                    : reader["tipo_comprobante"]
                        ?.ToString(),

            Serie =
                reader["serie"] is DBNull
                    ? null
                    : reader["serie"]
                        ?.ToString(),

            Numero =
                reader["numero"] is DBNull
                    ? null
                    : reader["numero"]
                        ?.ToString(),

            ComprobanteSubtotal =
                reader["comprobante_subtotal"] is DBNull
                    ? null
                    : Convert.ToDecimal(
                        reader["comprobante_subtotal"]),

            ComprobanteImpuesto =
                reader["comprobante_impuesto"] is DBNull
                    ? null
                    : Convert.ToDecimal(
                        reader["comprobante_impuesto"]),

            ComprobanteTotal =
                reader["comprobante_total"] is DBNull
                    ? null
                    : Convert.ToDecimal(
                        reader["comprobante_total"]),

            EstadoSunat =
                reader["estado_sunat"] is DBNull
                    ? null
                    : reader["estado_sunat"]
                        ?.ToString()
        };
    }

    private static DataTable CrearDetalles(
        IReadOnlyCollection<VentaConfirmarDetalleRequest> detalles)
    {
        DataTable table =
            new();

        table.Columns.Add(
            "id_producto",
            typeof(int));

        table.Columns.Add(
            "cantidad",
            typeof(decimal));

        table.Columns.Add(
            "precio_unitario",
            typeof(decimal));

        table.Columns.Add(
            "descuento",
            typeof(decimal));

        table.Columns.Add(
            "impuesto",
            typeof(decimal));

        foreach (
            VentaConfirmarDetalleRequest detalle
            in detalles)
        {
            decimal baseLinea =
                checked(
                    detalle.Cantidad
                    *
                    detalle.PrecioUnitario);

            decimal descuentoMonto =
                Math.Round(
                    baseLinea
                    *
                    detalle.Descuento
                    /
                    100m,
                    4,
                    MidpointRounding.AwayFromZero);

            table.Rows.Add(
                detalle.IdProducto,
                detalle.Cantidad,
                detalle.PrecioUnitario,
                descuentoMonto,
                0m);
        }

        return table;
    }

    private static bool TieneColumna(
        SqlDataReader reader,
        string nombre)
    {
        for (
            int i = 0;
            i < reader.FieldCount;
            i++)
        {
            if (string.Equals(
                reader.GetName(i),
                nombre,
                StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static void Validar(
        VentaConfirmacionIntegralRequest request)
    {
        if (request.IdEmpresa <= 0)
            throw new ArgumentException(
                "Empresa inválida.");

        if (request.IdSucursal <= 0)
            throw new ArgumentException(
                "Sucursal inválida.");

        if (request.IdCliente <= 0)
            throw new ArgumentException(
                "Cliente inválido.");

        if (request.IdEmpleado <= 0)
            throw new ArgumentException(
                "Empleado inválido.");

        if (request.IdAlmacen <= 0)
            throw new ArgumentException(
                "Almacén inválido.");

        if (request.IdPedido is <= 0)
            throw new ArgumentException(
                "Pedido inválido.");

        if (string.IsNullOrWhiteSpace(
            request.NumeroVenta))
        {
            throw new ArgumentException(
                "Número de venta obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(
            request.MetodoPago))
        {
            throw new ArgumentException(
                "Método de pago obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(
            request.TipoComprobante))
        {
            throw new ArgumentException(
                "Tipo de comprobante obligatorio.");
        }

        if (request.Detalles is null ||
            request.Detalles.Count == 0)
        {
            throw new ArgumentException(
                "La venta debe contener productos.");
        }

        if (string.Equals(
                request.MetodoPago,
                "EFECTIVO",
                StringComparison.OrdinalIgnoreCase)
            &&
            request.MontoRecibido <= 0)
        {
            throw new ArgumentException(
                "El monto recibido debe ser mayor que cero.");
        }

        foreach (
            VentaConfirmarDetalleRequest detalle
            in request.Detalles)
        {
            if (detalle.IdProducto <= 0)
                throw new ArgumentException(
                    "Existe un producto inválido.");

            if (detalle.Cantidad <= 0)
                throw new ArgumentException(
                    "La cantidad debe ser mayor que cero.");

            if (detalle.PrecioUnitario <= 0)
                throw new ArgumentException(
                    "El precio debe ser mayor que cero.");

            if (detalle.Descuento < 0 ||
                detalle.Descuento > 100)
            {
                throw new ArgumentException(
                    "El descuento debe estar entre 0 y 100.");
            }
        }
    }
}
