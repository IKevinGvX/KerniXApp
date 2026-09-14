using System.Data;
using KerniX.Models.Ventas.Pago;
using Microsoft.Data.SqlClient;

namespace KerniX.Services.Ventas;

public sealed class PagoDbService
    : IPagoDbService
{
    private readonly string _connectionString;
    private readonly ILogger<PagoDbService> _logger;

    public PagoDbService(
        IConfiguration configuration,
        ILogger<PagoDbService> logger)
    {
        _connectionString =
            configuration.GetConnectionString(
                "KerniXDb")
            ?? throw new InvalidOperationException(
                "No se encontró la conexión KerniXDb.");

        _logger = logger;
    }

    // =========================================================
    // REGISTRAR PAGO
    // =========================================================

    public async Task<PagoResponse> RegistrarAsync(
        PagoCrearRequest request)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        ValidarRequest(
            request);

        await using var connection =
            new SqlConnection(
                _connectionString);

        await connection.OpenAsync();

        await using var command =
            new SqlCommand(
                "ven.sp_pago_registrar",
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
                "@metodo_pago",
                SqlDbType.NVarChar,
                30)
            {
                Value =
                    request.MetodoPago
                        .Trim()
            });

        command.Parameters.Add(
            new SqlParameter(
                "@moneda",
                SqlDbType.NVarChar,
                20)
            {
                Value =
                    request.Moneda
                        .Trim()
            });

        command.Parameters.Add(
            new SqlParameter(
                "@monto",
                SqlDbType.Decimal)
            {
                Precision = 18,
                Scale = 4,
                Value =
                    request.Monto
            });

        command.Parameters.Add(
            new SqlParameter(
                "@numero_operacion",
                SqlDbType.NVarChar,
                100)
            {
                Value =
                    string.IsNullOrWhiteSpace(
                        request.NumeroOperacion)
                        ? DBNull.Value
                        : request.NumeroOperacion.Trim()
            });

        try
        {
            await using var reader =
                await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                throw new InvalidOperationException(
                    "El procedimiento no devolvió el pago registrado.");
            }

            var pago =
                LeerPago(
                    reader);

            _logger.LogInformation(
                "Pago registrado. " +
                "IdPago={IdPago} | " +
                "IdVenta={IdVenta} | " +
                "Monto={Monto}",
                pago.IdPago,
                pago.IdVenta,
                pago.Monto);

            return pago;
        }
        catch (SqlException ex)
        {
            _logger.LogError(
                ex,
                "Error SQL registrando pago. " +
                "IdVenta={IdVenta}",
                request.IdVenta);

            throw;
        }
    }

    // =========================================================
    // LISTAR PAGOS DE UNA VENTA
    // =========================================================

    public async Task<IReadOnlyList<PagoResponse>>
        ListarPorVentaAsync(
            long idVenta)
    {
        if (idVenta <= 0)
        {
            return Array.Empty<PagoResponse>();
        }

        const string sql = """
            SELECT
                p.id_pago,
                p.id_venta,
                p.id_empleado,
                p.metodo_pago,
                p.moneda,
                p.monto,
                p.numero_operacion,
                p.fecha_pago,
                p.estado

            FROM ven.pago p

            WHERE p.id_venta =
                @id_venta

            ORDER BY
                p.fecha_pago DESC,
                p.id_pago DESC;
            """;

        var pagos =
            new List<PagoResponse>();

        await using var connection =
            new SqlConnection(
                _connectionString);

        await connection.OpenAsync();

        await using var command =
            new SqlCommand(
                sql,
                connection);

        command.Parameters.Add(
            new SqlParameter(
                "@id_venta",
                SqlDbType.BigInt)
            {
                Value =
                    idVenta
            });

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            pagos.Add(
                new PagoResponse
                {
                    IdPago =
                        Convert.ToInt64(
                            reader["id_pago"]),

                    IdVenta =
                        Convert.ToInt64(
                            reader["id_venta"]),

                    IdEmpleado =
                        Convert.ToInt32(
                            reader["id_empleado"]),

                    MetodoPago =
                        reader["metodo_pago"]
                            ?.ToString()
                        ?? string.Empty,

                    Moneda =
                        reader["moneda"]
                            ?.ToString()
                        ?? string.Empty,

                    Monto =
                        Convert.ToDecimal(
                            reader["monto"]),

                    NumeroOperacion =
                        reader["numero_operacion"] is DBNull
                            ? null
                            : reader["numero_operacion"]
                                ?.ToString(),

                    FechaPago =
                        Convert.ToDateTime(
                            reader["fecha_pago"]),

                    Estado =
                        reader["estado"]
                            ?.ToString()
                        ?? string.Empty
                });
        }

        return pagos;
    }

    // =========================================================
    // RESUMEN DE PAGOS
    // =========================================================

    public async Task<PagoResumenResponse>
        ObtenerResumenAsync(
            long idVenta)
    {
        if (idVenta <= 0)
        {
            throw new ArgumentException(
                "La venta no es válida.",
                nameof(idVenta));
        }

        const string sql = """
            SELECT
                v.id_venta,

                v.total AS total_venta,

                ISNULL(
                    SUM(
                        CASE
                            WHEN p.estado <> 'anulado'
                                THEN p.monto
                            ELSE 0
                        END
                    ),
                    0
                ) AS total_pagado,

                v.total -
                ISNULL(
                    SUM(
                        CASE
                            WHEN p.estado <> 'anulado'
                                THEN p.monto
                            ELSE 0
                        END
                    ),
                    0
                ) AS saldo_pendiente

            FROM ven.venta v

            LEFT JOIN ven.pago p
                ON p.id_venta =
                   v.id_venta

            WHERE
                v.id_venta =
                    @id_venta

            GROUP BY
                v.id_venta,
                v.total;
            """;

        await using var connection =
            new SqlConnection(
                _connectionString);

        await connection.OpenAsync();

        await using var command =
            new SqlCommand(
                sql,
                connection);

        command.Parameters.Add(
            new SqlParameter(
                "@id_venta",
                SqlDbType.BigInt)
            {
                Value =
                    idVenta
            });

        await using var reader =
            await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            throw new InvalidOperationException(
                "No se encontró la venta.");
        }

        return new PagoResumenResponse
        {
            IdVenta =
                Convert.ToInt64(
                    reader["id_venta"]),

            TotalVenta =
                Convert.ToDecimal(
                    reader["total_venta"]),

            TotalPagado =
                Convert.ToDecimal(
                    reader["total_pagado"]),

            SaldoPendiente =
                Convert.ToDecimal(
                    reader["saldo_pendiente"])
        };
    }

    // =========================================================
    // LEER PAGO
    // =========================================================

    private static PagoResponse LeerPago(
        SqlDataReader reader)
    {
        return new PagoResponse
        {
            IdPago =
                Convert.ToInt64(
                    reader["id_pago"]),

            IdVenta =
                Convert.ToInt64(
                    reader["id_venta"]),

            IdEmpleado =
                Convert.ToInt32(
                    reader["id_empleado"]),

            MetodoPago =
                reader["metodo_pago"]
                    ?.ToString()
                ?? string.Empty,

            Moneda =
                reader["moneda"]
                    ?.ToString()
                ?? string.Empty,

            Monto =
                Convert.ToDecimal(
                    reader["monto"]),

            NumeroOperacion =
                reader["numero_operacion"] is DBNull
                    ? null
                    : reader["numero_operacion"]
                        ?.ToString(),

            FechaPago =
                Convert.ToDateTime(
                    reader["fecha_pago"]),

            Estado =
                reader["estado"]
                    ?.ToString()
                ?? string.Empty,

            TotalVenta =
                Convert.ToDecimal(
                    reader["total_venta"]),

            TotalPagado =
                Convert.ToDecimal(
                    reader["total_pagado"]),

            SaldoPendiente =
                Convert.ToDecimal(
                    reader["saldo_pendiente"])
        };
    }

    // =========================================================
    // VALIDACIONES
    // =========================================================

    private static void ValidarRequest(
        PagoCrearRequest request)
    {
        if (request.IdVenta <= 0)
        {
            throw new ArgumentException(
                "La venta no es válida.");
        }

        if (request.IdEmpleado <= 0)
        {
            throw new ArgumentException(
                "El empleado no es válido.");
        }

        if (string.IsNullOrWhiteSpace(
            request.MetodoPago))
        {
            throw new ArgumentException(
                "Debe indicar el método de pago.");
        }

        if (string.IsNullOrWhiteSpace(
            request.Moneda))
        {
            throw new ArgumentException(
                "Debe indicar la moneda.");
        }

        if (request.Monto <= 0)
        {
            throw new ArgumentException(
                "El monto debe ser mayor que cero.");
        }
    }
}