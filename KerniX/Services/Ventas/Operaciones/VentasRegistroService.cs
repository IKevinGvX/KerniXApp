using System;
using System.Data;
using System.Globalization;
using System.Security.Cryptography;
using KerniX.Models.Ventas.Operaciones;
using Microsoft.Data.SqlClient;

namespace KerniX.Services.Ventas.Operaciones;

/// <summary>
/// Escritura limitada a los SP de venta existentes. El IdEmpleado y el IdEmpresa
/// se obtienen en SQL desde el usuario de la sesión, nunca de campos HTML.
/// Por seguridad esta primera integración exige rol administrador activo.
/// </summary>
public sealed class VentasRegistroService : IVentasRegistroService
{
    private readonly string _connectionString;
    private readonly ILogger<VentasRegistroService> _logger;

    public VentasRegistroService(IConfiguration configuration, ILogger<VentasRegistroService> logger)
    {
        _connectionString = configuration.GetConnectionString("KerniXDb")
            ?? throw new InvalidOperationException("Falta ConnectionStrings:KerniXDb.");
        _logger = logger;
    }

    private sealed record Contexto(int IdEmpleado, int IdEmpresa);
    private sealed record VentaEncontrada(int IdCliente);

    private static readonly string[] MetodosPermitidos =
        { "EFECTIVO", "YAPE", "PLIN", "TARJETA", "TRANSFERENCIA" };
    private static readonly string[] TiposReclamoPermitidos =
        { "producto", "atencion", "comprobante" };

    private static async Task<Contexto?> ContextoUsuarioAsync(SqlConnection db, int idUsuario, CancellationToken ct)
    {
        const string sql = """
            SELECT TOP (1) e.id_empleado, e.id_empresa
            FROM seg.usuario AS u
            INNER JOIN org.empleado AS e ON e.id_empleado = u.id_empleado
            WHERE u.id_usuario = @usuario AND u.estado = 1 AND e.id_empresa > 0;
            """;
        await using var cmd = new SqlCommand(sql, db);
        cmd.Parameters.Add("@usuario", SqlDbType.Int).Value = idUsuario;
        await using var rd = await cmd.ExecuteReaderAsync(ct);
        if (!await rd.ReadAsync(ct)) return null;
        return new Contexto(Convert.ToInt32(rd.GetValue(0), CultureInfo.InvariantCulture),
            Convert.ToInt32(rd.GetValue(1), CultureInfo.InvariantCulture));
    }

    private static async Task<bool> EsAdministradorAsync(SqlConnection db, int idUsuario, int idEmpresa, CancellationToken ct)
    {
        const string sql = """
            SELECT TOP (1) 1
            FROM seg.usuario_rol ur
            INNER JOIN seg.rol r ON r.id_rol = ur.id_rol
            WHERE ur.id_usuario = @usuario AND r.es_administrador = 1
              AND r.estado = 1 AND (r.id_empresa = @empresa OR r.id_empresa IS NULL);
            """;
        await using var cmd = new SqlCommand(sql, db);
        cmd.Parameters.Add("@usuario", SqlDbType.Int).Value = idUsuario;
        cmd.Parameters.Add("@empresa", SqlDbType.Int).Value = idEmpresa;
        return await cmd.ExecuteScalarAsync(ct) is not null;
    }

    private static async Task<VentaEncontrada?> BuscarVentaAsync(SqlConnection db, long idVenta, int idEmpresa, CancellationToken ct)
    {
        const string sql = """
            SELECT TOP (1) id_cliente
            FROM ven.venta
            WHERE id_venta = @venta AND id_empresa = @empresa;
            """;
        await using var cmd = new SqlCommand(sql, db);
        cmd.Parameters.Add("@venta", SqlDbType.BigInt).Value = idVenta;
        cmd.Parameters.Add("@empresa", SqlDbType.Int).Value = idEmpresa;
        var value = await cmd.ExecuteScalarAsync(ct);
        return value is null || value is DBNull ? null
            : new VentaEncontrada(Convert.ToInt32(value, CultureInfo.InvariantCulture));
    }

    /// <summary>
    /// Se verifica la existencia de los parámetros antes de cualquier escritura.
    /// Si el SP cambió respecto al contrato previsto, no se intenta ejecutarlo.
    /// </summary>
    private static async Task<bool> TieneFirmaAsync(SqlConnection db, string procedimiento,
        string[] parametros, CancellationToken ct)
    {
        await using var cmd = new SqlCommand("""
            SELECT p.name
            FROM sys.parameters p
            WHERE p.object_id = OBJECT_ID(@sp, 'P');
            """, db);
        cmd.Parameters.Add("@sp", SqlDbType.NVarChar, 256).Value = procedimiento;
        var encontrados = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var rd = await cmd.ExecuteReaderAsync(ct);
        while (await rd.ReadAsync(ct)) encontrados.Add(rd.GetString(0));
        return parametros.All(encontrados.Contains);
    }

    public async Task<RegistroOperacionResultado> RegistrarPagoAsync(
        int idUsuario, PagoRegistroInput input, CancellationToken ct = default)
    {
        if (idUsuario <= 0) return new(false, "Tu sesión ha caducado. Inicia sesión nuevamente.");
        if (input.IdVenta <= 0 || input.Monto <= 0m)
            return new(false, "Debes indicar una venta y un monto mayor que cero.");

        var metodo = (input.MetodoPago ?? "").Trim().ToUpperInvariant();
        if (!MetodosPermitidos.Contains(metodo))
            return new(false, "Selecciona un método de pago válido.");
        if (!string.Equals(input.Moneda, "PEN", StringComparison.OrdinalIgnoreCase))
            return new(false, "Por ahora solo se admite la moneda PEN.");
        var operacion = input.NumeroOperacion?.Trim();
        if (operacion?.Length > 100)
            return new(false, "El número de operación no puede superar 100 caracteres.");
        if (metodo != "EFECTIVO" && string.IsNullOrWhiteSpace(operacion))
            return new(false, "Ingresa el número de operación del pago electrónico.");

        try
        {
            await using var db = new SqlConnection(_connectionString);
            await db.OpenAsync(ct);
            var contexto = await ContextoUsuarioAsync(db, idUsuario, ct);
            if (contexto is null) return new(false, "No se encontró la empresa y el empleado del usuario autenticado.");
            if (!await EsAdministradorAsync(db, idUsuario, contexto.IdEmpresa, ct))
                return new(false, "Solo un administrador autorizado puede registrar pagos en esta fase.");
            if (await BuscarVentaAsync(db, input.IdVenta, contexto.IdEmpresa, ct) is null)
                return new(false, "La venta no existe o no pertenece a tu empresa.");
            if (!await TieneFirmaAsync(db, "ven.sp_pago_registrar",
                new[] { "@id_venta", "@id_empleado", "@metodo_pago", "@moneda", "@monto", "@numero_operacion" }, ct))
                return new(false, "La firma actual de ven.sp_pago_registrar no coincide. Ejecuta SQL/VERIFICAR_OPERACIONES.sql.");

            await using var cmd = new SqlCommand("ven.sp_pago_registrar", db)
            { CommandType = CommandType.StoredProcedure, CommandTimeout = 45 };
            cmd.Parameters.Add("@id_venta", SqlDbType.BigInt).Value = input.IdVenta;
            cmd.Parameters.Add("@id_empleado", SqlDbType.Int).Value = contexto.IdEmpleado;
            cmd.Parameters.Add("@metodo_pago", SqlDbType.NVarChar, 30).Value = metodo;
            cmd.Parameters.Add("@moneda", SqlDbType.NVarChar, 20).Value = "PEN";
            var pMonto = cmd.Parameters.Add("@monto", SqlDbType.Decimal);
            pMonto.Precision = 18; pMonto.Scale = 4; pMonto.Value = input.Monto;
            cmd.Parameters.Add("@numero_operacion", SqlDbType.NVarChar, 100).Value =
                string.IsNullOrWhiteSpace(operacion) ? DBNull.Value : operacion;

            await cmd.ExecuteNonQueryAsync(ct);
            _logger.LogInformation("[VENTAS PAGOS] SP ejecutado. Venta={Venta} Empresa={Empresa} Usuario={Usuario}",
                input.IdVenta, contexto.IdEmpresa, idUsuario);
            return new(true, "Pago registrado correctamente. Verifica su aparición en el listado.");
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "[VENTAS PAGOS] Error SQL. Venta={Venta}", input.IdVenta);
            return new(false, $"SQL Server rechazó el pago (código {ex.Number}). Revisa el registro de Visual Studio.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "[VENTAS PAGOS] Error inesperado.");
            return new(false, "No se pudo completar el registro del pago. Revisa Salida en Visual Studio.");
        }
    }

    public async Task<RegistroOperacionResultado> RegistrarReclamoAsync(
        int idUsuario, ReclamoRegistroInput input, CancellationToken ct = default)
    {
        if (idUsuario <= 0) return new(false, "Tu sesión ha caducado. Inicia sesión nuevamente.");
        if (input.IdVenta <= 0) return new(false, "Debes indicar el ID de una venta válida.");
        var tipo = (input.TipoReclamo ?? "").Trim().ToLowerInvariant();
        if (!TiposReclamoPermitidos.Contains(tipo))
            return new(false, "Selecciona un tipo de reclamo válido.");
        var descripcion = input.Descripcion?.Trim() ?? "";
        if (descripcion.Length < 10 || descripcion.Length > 2000)
            return new(false, "Describe el reclamo en 10 a 2000 caracteres.");

        try
        {
            await using var db = new SqlConnection(_connectionString);
            await db.OpenAsync(ct);
            var contexto = await ContextoUsuarioAsync(db, idUsuario, ct);
            if (contexto is null) return new(false, "No se encontró la empresa y el empleado del usuario autenticado.");
            if (!await EsAdministradorAsync(db, idUsuario, contexto.IdEmpresa, ct))
                return new(false, "Solo un administrador autorizado puede registrar reclamos en esta fase.");
            var venta = await BuscarVentaAsync(db, input.IdVenta, contexto.IdEmpresa, ct);
            if (venta is null) return new(false, "La venta no existe o no pertenece a tu empresa.");
            if (!await TieneFirmaAsync(db, "ven.sp_reclamo_registrar",
                new[] { "@id_venta", "@id_cliente", "@id_empleado", "@numero_reclamo", "@tipo_reclamo", "@descripcion" }, ct))
                return new(false, "La firma actual de ven.sp_reclamo_registrar no coincide. Ejecuta SQL/VERIFICAR_OPERACIONES.sql.");

            // Referencia corta y no predecible. La BD debe imponer unicidad.
            var referencia = $"REC-{DateTime.UtcNow:yyMMddHHmmss}-{RandomNumberGenerator.GetInt32(100000, 999999)}";
            await using var cmd = new SqlCommand("ven.sp_reclamo_registrar", db)
            { CommandType = CommandType.StoredProcedure, CommandTimeout = 45 };
            cmd.Parameters.Add("@id_venta", SqlDbType.BigInt).Value = input.IdVenta;
            cmd.Parameters.Add("@id_cliente", SqlDbType.Int).Value = venta.IdCliente;
            cmd.Parameters.Add("@id_empleado", SqlDbType.Int).Value = contexto.IdEmpleado;
            cmd.Parameters.Add("@numero_reclamo", SqlDbType.NVarChar, 30).Value = referencia;
            cmd.Parameters.Add("@tipo_reclamo", SqlDbType.NVarChar, 50).Value = tipo;
            cmd.Parameters.Add("@descripcion", SqlDbType.NVarChar, -1).Value = descripcion;

            await cmd.ExecuteNonQueryAsync(ct);
            _logger.LogInformation("[VENTAS RECLAMOS] SP ejecutado. Venta={Venta} Empresa={Empresa} Usuario={Usuario}",
                input.IdVenta, contexto.IdEmpresa, idUsuario);
            return new(true, "Reclamo registrado correctamente.", referencia);
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "[VENTAS RECLAMOS] Error SQL. Venta={Venta}", input.IdVenta);
            return new(false, $"SQL Server rechazó el reclamo (código {ex.Number}). Revisa el registro de Visual Studio.");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "[VENTAS RECLAMOS] Error inesperado.");
            return new(false, "No se pudo registrar el reclamo. Revisa Salida en Visual Studio.");
        }
    }
}
