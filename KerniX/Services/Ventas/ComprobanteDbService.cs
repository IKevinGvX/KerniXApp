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
            configuration.GetConnectionString("KerniXDb")
            ?? throw new InvalidOperationException(
                "No se encontró la conexión KerniXDb.");

        _serieService = serieService;
        _logger = logger;
    }

    // =========================================================
    // EMITIR COMPROBANTE
    // =========================================================

    public async Task<ComprobanteResponse> EmitirAsync(
        ComprobanteCrearRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        ValidarRequest(request);

        string tipoComprobante =
            request.TipoComprobante
                .Trim()
                .ToUpperInvariant();

        var series = await _serieService
            .ListarActivasAsync(
                request.IdEmpresa,
                request.IdSucursal);

        var serie = series.FirstOrDefault(
            x => string.Equals(
                x.TipoComprobante,
                tipoComprobante,
                StringComparison.OrdinalIgnoreCase));

        if (serie is null)
        {
            throw new InvalidOperationException(
                $"No existe una serie activa para {tipoComprobante}.");
        }

        await using var connection =
            new SqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var command =
            new SqlCommand(
                "ven.sp_comprobante_emitir",
                connection)
            {
                CommandType = CommandType.StoredProcedure,
                CommandTimeout = 60
            };

        command.Parameters.Add(
            "@id_venta",
            SqlDbType.BigInt
        ).Value = request.IdVenta;

        command.Parameters.Add(
            "@id_empresa",
            SqlDbType.Int
        ).Value = request.IdEmpresa;

        command.Parameters.Add(
            "@id_sucursal",
            SqlDbType.Int
        ).Value = request.IdSucursal;

        command.Parameters.Add(
            "@tipo_comprobante",
            SqlDbType.NVarChar,
            20
        ).Value = tipoComprobante;

        command.Parameters.Add(
            "@serie",
            SqlDbType.NVarChar,
            10
        ).Value = serie.Serie;

        // La numeración se genera exclusivamente en SQL Server.
        command.Parameters.Add(
            "@numero",
            SqlDbType.NVarChar,
            20
        ).Value = DBNull.Value;

        command.Parameters.Add(
            "@estado_sunat",
            SqlDbType.NVarChar,
            30
        ).Value =
            string.IsNullOrWhiteSpace(request.EstadoSunat)
                ? "pendiente"
                : request.EstadoSunat.Trim();

        command.Parameters.Add(
            "@codigo_hash",
            SqlDbType.NVarChar,
            500
        ).Value =
            string.IsNullOrWhiteSpace(request.CodigoHash)
                ? DBNull.Value
                : request.CodigoHash.Trim();

        command.Parameters.Add(
            "@ruta_xml",
            SqlDbType.NVarChar,
            500
        ).Value =
            string.IsNullOrWhiteSpace(request.RutaXml)
                ? DBNull.Value
                : request.RutaXml.Trim();

        command.Parameters.Add(
            "@ruta_pdf",
            SqlDbType.NVarChar,
            500
        ).Value =
            string.IsNullOrWhiteSpace(request.RutaPdf)
                ? DBNull.Value
                : request.RutaPdf.Trim();

        try
        {
            await using var reader =
                await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                throw new InvalidOperationException(
                    "SQL Server no devolvió el comprobante.");
            }

            var comprobante = LeerComprobante(reader);

            _logger.LogInformation(
                "Comprobante registrado. Id={Id} Venta={Venta} Numero={Numero}",
                comprobante.IdComprobante,
                comprobante.IdVenta,
                comprobante.NumeroCompleto);

            return comprobante;
        }
        catch (SqlException ex)
        {
            _logger.LogError(
                ex,
                "Error emitiendo comprobante para venta {IdVenta}",
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
            WHERE id_venta = @id_venta
            ORDER BY
                fecha_emision DESC,
                id_comprobante DESC;
            """;

        var lista =
            new List<ComprobanteResponse>();

        await using var connection =
            new SqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var command =
            new SqlCommand(sql, connection);

        command.Parameters.Add(
            new SqlParameter(
                "@id_venta",
                SqlDbType.BigInt)
            {
                Value = idVenta
            });

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            lista.Add(
                LeerComprobante(reader));
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
            WHERE id_comprobante = @id_comprobante;
            """;

        await using var connection =
            new SqlConnection(_connectionString);

        await connection.OpenAsync();

        await using var command =
            new SqlCommand(sql, connection);

        command.Parameters.Add(
            new SqlParameter(
                "@id_comprobante",
                SqlDbType.BigInt)
            {
                Value = idComprobante
            });

        await using var reader =
            await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        return LeerComprobante(reader);
    }

    // =========================================================
    // OBTENER DETALLE COMPLETO PARA BOLETA / FACTURA
    // =========================================================

    public async Task<ComprobanteDetalleModel?>
        ObtenerDetalleAsync(
            long idVenta,
            CancellationToken cancellationToken = default)
    {
        if (idVenta <= 0)
        {
            return null;
        }

        await using var connection =
            new SqlConnection(_connectionString);

        await connection.OpenAsync(
            cancellationToken);

        await using var command =
            new SqlCommand(
                "ven.sp_Comprobante_ObtenerDetalle",
                connection)
            {
                CommandType =
                    CommandType.StoredProcedure,

                CommandTimeout =
                    60
            };

        command.Parameters.Add(
            new SqlParameter(
                "@id_venta",
                SqlDbType.BigInt)
            {
                Value = idVenta
            });

        try
        {
            await using var reader =
                await command.ExecuteReaderAsync(
                    cancellationToken);

            var resultado =
                new ComprobanteDetalleModel();

            // 1) CABECERA
            if (!await reader.ReadAsync(
                cancellationToken))
            {
                _logger.LogWarning(
                    "No se encontró información del comprobante. Venta={IdVenta}",
                    idVenta);

                return null;
            }

            resultado.Cabecera =
                LeerCabeceraComprobante(
                    reader);

            // 2) PRODUCTOS
            bool existeDetalle =
                await reader.NextResultAsync(
                    cancellationToken);

            if (existeDetalle)
            {
                while (await reader.ReadAsync(
                    cancellationToken))
                {
                    resultado.Productos.Add(
                        LeerProductoComprobante(
                            reader));
                }
            }

            _logger.LogInformation(
                "Detalle de comprobante cargado. Venta={IdVenta} | Comprobante={NumeroComprobante} | Productos={Productos}",
                resultado.Cabecera.IdVenta,
                resultado.Cabecera.NumeroComprobante,
                resultado.Productos.Count);

            return resultado;
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                "Consulta de comprobante cancelada. Venta={IdVenta}",
                idVenta);

            throw;
        }
        catch (SqlException ex)
        {
            _logger.LogError(
                ex,
                "Error SQL obteniendo detalle del comprobante. Venta={IdVenta}",
                idVenta);

            throw;
        }
    }

    // =========================================================
    // LEER CABECERA COMPLETA
    // =========================================================

    private static ComprobanteCabeceraModel
        LeerCabeceraComprobante(
            SqlDataReader reader)
    {
        return new ComprobanteCabeceraModel
        {
            IdVenta =
                LeerInt64(reader, "id_venta"),

            NumeroVenta =
                LeerString(reader, "numero_venta"),

            FechaVenta =
                LeerDateTime(reader, "fecha_venta"),

            VentaSubtotal =
                LeerDecimal(reader, "venta_subtotal"),

            VentaDescuento =
                LeerDecimal(reader, "venta_descuento"),

            VentaImpuesto =
                LeerDecimal(reader, "venta_impuesto"),

            VentaTotal =
                LeerDecimal(reader, "venta_total"),

            VentaEstado =
                LeerString(reader, "venta_estado"),

            Observacion =
                LeerStringNullable(reader, "observacion"),

            IdComprobante =
                LeerInt64Nullable(reader, "id_comprobante"),

            TipoComprobante =
                LeerString(reader, "tipo_comprobante"),

            Serie =
                LeerString(reader, "serie"),

            Numero =
                LeerString(reader, "numero"),

            NumeroComprobante =
                LeerString(reader, "numero_comprobante"),

            FechaEmision =
                LeerDateTimeNullable(reader, "fecha_emision"),

            ComprobanteSubtotal =
                LeerDecimal(reader, "comprobante_subtotal"),

            ComprobanteImpuesto =
                LeerDecimal(reader, "comprobante_impuesto"),

            ComprobanteTotal =
                LeerDecimal(reader, "comprobante_total"),

            EstadoSunat =
                LeerStringNullable(reader, "estado_sunat"),

            CodigoHash =
                LeerStringNullable(reader, "codigo_hash"),

            RutaXml =
                LeerStringNullable(reader, "ruta_xml"),

            RutaPdf =
                LeerStringNullable(reader, "ruta_pdf"),

            CodigoRespuestaSunat =
                LeerStringNullable(reader, "codigo_respuesta_sunat"),

            MensajeRespuestaSunat =
                LeerStringNullable(reader, "mensaje_respuesta_sunat"),

            FechaEnvioSunat =
                LeerDateTimeNullable(reader, "fecha_envio_sunat"),

            FechaRespuestaSunat =
                LeerDateTimeNullable(reader, "fecha_respuesta_sunat"),

            RutaCdr =
                LeerStringNullable(reader, "ruta_cdr"),

            IntentosSunat =
                LeerInt32(reader, "intentos_sunat"),

            TicketSunat =
                LeerStringNullable(reader, "ticket_sunat"),

            IdCliente =
                LeerInt32(reader, "id_cliente"),

            TipoCliente =
                LeerString(reader, "tipo_cliente"),

            ClienteTipoDocumento =
                LeerString(reader, "cliente_tipo_documento"),

            ClienteNumeroDocumento =
                LeerString(reader, "cliente_numero_documento"),

            ClienteNombre =
                LeerString(reader, "cliente_nombre"),

            ClienteRazonSocial =
                LeerStringNullable(reader, "cliente_razon_social"),

            ClienteNombres =
                LeerStringNullable(reader, "cliente_nombres"),

            ClienteApellidos =
                LeerStringNullable(reader, "cliente_apellidos"),

            ClienteTelefono =
                LeerStringNullable(reader, "cliente_telefono"),

            ClienteCorreo =
                LeerStringNullable(reader, "cliente_correo"),

            IdEmpresa =
                LeerInt32(reader, "id_empresa"),

            EmpresaRazonSocial =
                LeerString(reader, "empresa_razon_social"),

            EmpresaNombreComercial =
                LeerStringNullable(reader, "empresa_nombre_comercial"),

            EmpresaRuc =
                LeerString(reader, "empresa_ruc"),

            EmpresaDireccionFiscal =
                LeerStringNullable(reader, "empresa_direccion_fiscal"),

            EmpresaTelefono =
                LeerStringNullable(reader, "empresa_telefono"),

            EmpresaCorreo =
                LeerStringNullable(reader, "empresa_correo"),

            EmpresaSitioWeb =
                LeerStringNullable(reader, "empresa_sitio_web"),

            EmpresaLogoUrl =
                LeerStringNullable(reader, "empresa_logo_url"),

            IdSucursal =
                LeerInt32(reader, "id_sucursal"),

            SucursalCodigo =
                LeerString(reader, "sucursal_codigo"),

            SucursalNombre =
                LeerString(reader, "sucursal_nombre"),

            SucursalDireccion =
                LeerStringNullable(reader, "sucursal_direccion"),

            SucursalTelefono =
                LeerStringNullable(reader, "sucursal_telefono"),

            SucursalCorreo =
                LeerStringNullable(reader, "sucursal_correo"),

            SucursalUbigeo =
                LeerStringNullable(reader, "sucursal_ubigeo"),

            IdPago =
                LeerInt64Nullable(reader, "id_pago"),

            MetodoPago =
                LeerStringNullable(reader, "metodo_pago"),

            Moneda =
                LeerStringNullable(reader, "moneda"),

            PagoMonto =
                LeerDecimal(reader, "pago_monto"),

            NumeroOperacion =
                LeerStringNullable(reader, "numero_operacion"),

            FechaPago =
                LeerDateTimeNullable(reader, "fecha_pago"),

            PagoEstado =
                LeerStringNullable(reader, "pago_estado")
        };
    }

    // =========================================================
    // LEER PRODUCTO DEL COMPROBANTE
    // =========================================================

    private static ComprobanteProductoModel
        LeerProductoComprobante(
            SqlDataReader reader)
    {
        return new ComprobanteProductoModel
        {
            IdDetalleVenta =
                LeerInt64(reader, "id_detalle_venta"),

            IdVenta =
                LeerInt64(reader, "id_venta"),

            IdProducto =
                LeerInt32(reader, "id_producto"),

            CodigoProducto =
                LeerString(reader, "codigo_producto"),

            ProductoNombre =
                LeerString(reader, "producto_nombre"),

            ProductoDescripcion =
                LeerStringNullable(reader, "producto_descripcion"),

            Categoria =
                LeerStringNullable(reader, "categoria"),

            Marca =
                LeerStringNullable(reader, "marca"),

            UnidadCodigo =
                LeerStringNullable(reader, "unidad_codigo"),

            UnidadNombre =
                LeerStringNullable(reader, "unidad_nombre"),

            UnidadAbreviatura =
                LeerStringNullable(reader, "unidad_abreviatura"),

            Cantidad =
                LeerDecimal(reader, "cantidad"),

            PrecioUnitario =
                LeerDecimal(reader, "precio_unitario"),

            Descuento =
                LeerDecimal(reader, "descuento"),

            Subtotal =
                LeerDecimal(reader, "subtotal"),

            Impuesto =
                LeerDecimal(reader, "impuesto"),

            Total =
                LeerDecimal(reader, "total")
        };
    }

    // =========================================================
    // LEER COMPROBANTE SIMPLE
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
    // HELPERS SQL
    // =========================================================

    private static string LeerString(
        SqlDataReader reader,
        string columna)
    {
        int ordinal =
            reader.GetOrdinal(columna);

        if (reader.IsDBNull(ordinal))
        {
            return string.Empty;
        }

        return Convert
            .ToString(reader.GetValue(ordinal))
            ?.Trim()
            ?? string.Empty;
    }

    private static string?
        LeerStringNullable(
            SqlDataReader reader,
            string columna)
    {
        int ordinal =
            reader.GetOrdinal(columna);

        if (reader.IsDBNull(ordinal))
        {
            return null;
        }

        string? valor =
            Convert
                .ToString(reader.GetValue(ordinal))
                ?.Trim();

        return string.IsNullOrWhiteSpace(valor)
            ? null
            : valor;
    }

    private static int LeerInt32(
        SqlDataReader reader,
        string columna)
    {
        int ordinal =
            reader.GetOrdinal(columna);

        if (reader.IsDBNull(ordinal))
        {
            return 0;
        }

        return Convert.ToInt32(
            reader.GetValue(ordinal));
    }

    private static long LeerInt64(
        SqlDataReader reader,
        string columna)
    {
        int ordinal =
            reader.GetOrdinal(columna);

        if (reader.IsDBNull(ordinal))
        {
            return 0L;
        }

        return Convert.ToInt64(
            reader.GetValue(ordinal));
    }

    private static long?
        LeerInt64Nullable(
            SqlDataReader reader,
            string columna)
    {
        int ordinal =
            reader.GetOrdinal(columna);

        if (reader.IsDBNull(ordinal))
        {
            return null;
        }

        return Convert.ToInt64(
            reader.GetValue(ordinal));
    }

    private static decimal LeerDecimal(
        SqlDataReader reader,
        string columna)
    {
        int ordinal =
            reader.GetOrdinal(columna);

        if (reader.IsDBNull(ordinal))
        {
            return 0m;
        }

        return Convert.ToDecimal(
            reader.GetValue(ordinal));
    }

    private static DateTime LeerDateTime(
        SqlDataReader reader,
        string columna)
    {
        int ordinal =
            reader.GetOrdinal(columna);

        if (reader.IsDBNull(ordinal))
        {
            return DateTime.MinValue;
        }

        return Convert.ToDateTime(
            reader.GetValue(ordinal));
    }

    private static DateTime?
        LeerDateTimeNullable(
            SqlDataReader reader,
            string columna)
    {
        int ordinal =
            reader.GetOrdinal(columna);

        if (reader.IsDBNull(ordinal))
        {
            return null;
        }

        return Convert.ToDateTime(
            reader.GetValue(ordinal));
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
