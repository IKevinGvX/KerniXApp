using System.Data;
using KerniX.Api.Services.Configuracion;
using KerniX.Models.Ventas.Comprobante;
using Microsoft.Data.SqlClient;

namespace KerniX.Services.Ventas;

public sealed class ComprobanteDbService
    : IComprobanteDbService
{
    private readonly string _connectionString;
    private readonly ISerieComprobanteDbService _serieService;
    private readonly ILogger<ComprobanteDbService> _logger;

    public ComprobanteDbService(
        IConfiguration configuration,
        ISerieComprobanteDbService serieService,
        ILogger<ComprobanteDbService> logger)
    {
        _connectionString =
            configuration.GetConnectionString(
                "KerniXDb")
            ?? throw new InvalidOperationException(
                "No se encontró la conexión KerniXDb.");

        _serieService =
            serieService;

        _logger =
            logger;
    }

    // =========================================================
    // EMITIR COMPROBANTE
    // =========================================================

    public async Task<ComprobanteResponse> EmitirAsync(
        ComprobanteCrearRequest request)
    {
        ArgumentNullException.ThrowIfNull(
            request);

        ValidarRequest(
            request);

        string tipoComprobante =
            request.TipoComprobante
                .Trim()
                .ToUpperInvariant();

        // =====================================================
        // OBTENER SERIE ACTIVA
        // =====================================================

        var series =
            await _serieService
                .ListarActivasAsync(
                    request.IdEmpresa,
                    request.IdSucursal);

        var serie =
            series.FirstOrDefault(
                x =>
                    string.Equals(
                        x.TipoComprobante,
                        tipoComprobante,
                        StringComparison.OrdinalIgnoreCase));

        if (serie is null)
        {
            throw new InvalidOperationException(
                $"No existe una serie activa para {tipoComprobante}.");
        }

        // =====================================================
        // GENERAR SIGUIENTE NUMERO
        // =====================================================
        var siguiente =
            await _serieService
                .GenerarNumeroAsync(
                    request.IdEmpresa,
                    request.IdSucursal,
                    tipoComprobante,
                    serie.Serie);

        string serieComprobante =
            siguiente.Serie;

        string numero =
            siguiente.Correlativo
                .ToString("D8");
        // =====================================================
        // INSERTAR COMPROBANTE
        // =====================================================

        await using var connection =
            new SqlConnection(
                _connectionString);

        await connection.OpenAsync();

        await using var command =
            new SqlCommand(
                "ven.sp_comprobante_emitir",
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
                "@tipo_comprobante",
                SqlDbType.NVarChar,
                20)
            {
                Value =
                    tipoComprobante
            });

        command.Parameters.Add(
            new SqlParameter(
                "@serie",
                SqlDbType.NVarChar,
                10)
            {
                Value =
                    serieComprobante
            });

        command.Parameters.Add(
            new SqlParameter(
                "@numero",
                SqlDbType.NVarChar,
                20)
            {
                Value =
                    numero
            });

        command.Parameters.Add(
            new SqlParameter(
                "@estado_sunat",
                SqlDbType.NVarChar,
                30)
            {
                Value =
                    string.IsNullOrWhiteSpace(
                        request.EstadoSunat)
                        ? "pendiente"
                        : request.EstadoSunat.Trim()
            });

        command.Parameters.Add(
            new SqlParameter(
                "@codigo_hash",
                SqlDbType.NVarChar,
                500)
            {
                Value =
                    string.IsNullOrWhiteSpace(
                        request.CodigoHash)
                        ? DBNull.Value
                        : request.CodigoHash.Trim()
            });

        command.Parameters.Add(
            new SqlParameter(
                "@ruta_xml",
                SqlDbType.NVarChar,
                500)
            {
                Value =
                    string.IsNullOrWhiteSpace(
                        request.RutaXml)
                        ? DBNull.Value
                        : request.RutaXml.Trim()
            });

        command.Parameters.Add(
            new SqlParameter(
                "@ruta_pdf",
                SqlDbType.NVarChar,
                500)
            {
                Value =
                    string.IsNullOrWhiteSpace(
                        request.RutaPdf)
                        ? DBNull.Value
                        : request.RutaPdf.Trim()
            });

        try
        {
            await using var reader =
                await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                throw new InvalidOperationException(
                    "El procedimiento no devolvió el comprobante emitido.");
            }

            var comprobante =
                LeerComprobante(
                    reader);

            _logger.LogInformation(
                "Comprobante emitido. " +
                "IdComprobante={IdComprobante} | " +
                "Venta={IdVenta} | " +
                "Numero={Numero}",
                comprobante.IdComprobante,
                comprobante.IdVenta,
                comprobante.NumeroCompleto);

            return comprobante;
        }
        catch (SqlException ex)
        {
            _logger.LogError(
                ex,
                "Error SQL emitiendo comprobante. " +
                "IdVenta={IdVenta}",
                request.IdVenta);

            throw;
        }
    }

    // =========================================================
    // LISTAR POR VENTA
    // =========================================================

    public async Task<IReadOnlyList<ComprobanteResponse>>
        ListarPorVentaAsync(
            long idVenta)
    {
        if (idVenta <= 0)
        {
            return Array.Empty<ComprobanteResponse>();
        }

        const string sql = """
            SELECT
                id_comprobante,
                id_venta,
                tipo_comprobante,
                serie,
                numero,
                fecha_emision,
                subtotal,
                impuesto,
                total,
                estado_sunat,
                codigo_hash,
                ruta_xml,
                ruta_pdf

            FROM ven.comprobante

            WHERE id_venta =
                @id_venta

            ORDER BY
                fecha_emision DESC,
                id_comprobante DESC;
            """;

        var lista =
            new List<ComprobanteResponse>();

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
            lista.Add(
                LeerComprobante(
                    reader));
        }

        return lista;
    }

    // =========================================================
    // OBTENER POR ID
    // =========================================================

    public async Task<ComprobanteResponse?>
        ObtenerPorIdAsync(
            long idComprobante)
    {
        if (idComprobante <= 0)
        {
            return null;
        }

        const string sql = """
            SELECT TOP (1)
                id_comprobante,
                id_venta,
                tipo_comprobante,
                serie,
                numero,
                fecha_emision,
                subtotal,
                impuesto,
                total,
                estado_sunat,
                codigo_hash,
                ruta_xml,
                ruta_pdf

            FROM ven.comprobante

            WHERE id_comprobante =
                @id_comprobante;
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
                "@id_comprobante",
                SqlDbType.BigInt)
            {
                Value =
                    idComprobante
            });

        await using var reader =
            await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        return LeerComprobante(
            reader);
    }

    // =========================================================
    // LEER COMPROBANTE
    // =========================================================

    private static ComprobanteResponse
        LeerComprobante(
            SqlDataReader reader)
    {
        return new ComprobanteResponse
        {
            IdComprobante =
                Convert.ToInt64(
                    reader["id_comprobante"]),

            IdVenta =
                Convert.ToInt64(
                    reader["id_venta"]),

            TipoComprobante =
                reader["tipo_comprobante"]
                    ?.ToString()
                ?? string.Empty,

            Serie =
                reader["serie"]
                    ?.ToString()
                ?? string.Empty,

            Numero =
                reader["numero"]
                    ?.ToString()
                ?? string.Empty,

            FechaEmision =
                Convert.ToDateTime(
                    reader["fecha_emision"]),

            Subtotal =
                Convert.ToDecimal(
                    reader["subtotal"]),

            Impuesto =
                Convert.ToDecimal(
                    reader["impuesto"]),

            Total =
                Convert.ToDecimal(
                    reader["total"]),

            EstadoSunat =
                reader["estado_sunat"] is DBNull
                    ? null
                    : reader["estado_sunat"]
                        ?.ToString(),

            CodigoHash =
                reader["codigo_hash"] is DBNull
                    ? null
                    : reader["codigo_hash"]
                        ?.ToString(),

            RutaXml =
                reader["ruta_xml"] is DBNull
                    ? null
                    : reader["ruta_xml"]
                        ?.ToString(),

            RutaPdf =
                reader["ruta_pdf"] is DBNull
                    ? null
                    : reader["ruta_pdf"]
                        ?.ToString()
        };
    }

    // =========================================================
    // VALIDAR REQUEST
    // =========================================================

    private static void ValidarRequest(
        ComprobanteCrearRequest request)
    {
        if (request.IdVenta <= 0)
        {
            throw new ArgumentException(
                "La venta no es válida.");
        }

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

        if (string.IsNullOrWhiteSpace(
            request.TipoComprobante))
        {
            throw new ArgumentException(
                "Debe indicar el tipo de comprobante.");
        }
    }
}