using KerniX.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Diagnostics;

namespace KerniX.Pages.Configuracion;

public sealed class MantenimientoModel : PageModel
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<MantenimientoModel> _logger;

    public MantenimientoModel(
        IConfiguration configuration,
        ILogger<MantenimientoModel> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public int IdEmpresa { get; private set; }

    public string BaseDatos { get; private set; }
        = "kernixerp";

    [TempData]
    public string? MensajeExito { get; set; }

    [TempData]
    public string? MensajeError { get; set; }

    [TempData]
    public string? UltimaOperacion { get; set; }

    [TempData]
    public string? UltimaDuracion { get; set; }

    // =========================================================
    // GET
    // =========================================================

    public async Task<IActionResult> OnGetAsync()
    {
        if (!ObtenerContexto())
        {
            return RedirectToPage(
                "/Account/Login");
        }

        try
        {
            BaseDatos =
                await ObtenerNombreBaseDatosAsync();
        }
        catch
        {
            BaseDatos = "kernixerp";
        }

        return Page();
    }

    // =========================================================
    // VERIFICAR INTEGRIDAD
    // =========================================================

    public async Task<IActionResult>
        OnPostVerificarIntegridadAsync()
    {
        if (!ObtenerContexto())
        {
            return RedirectToPage(
                "/Account/Login");
        }

        var reloj =
            Stopwatch.StartNew();

        try
        {
            const string sql = """
                DBCC CHECKDB
                WITH
                    NO_INFOMSGS,
                    ALL_ERRORMSGS;
                """;

            await EjecutarComandoAsync(
                sql,
                600);

            reloj.Stop();

            UltimaOperacion =
                "Verificación de integridad";

            UltimaDuracion =
                FormatearDuracion(
                    reloj.Elapsed);

            MensajeExito =
                "Verificación de integridad completada. " +
                "SQL Server no reportó errores.";

            _logger.LogInformation(
                "DBCC CHECKDB completado. Duración: {Duracion}.",
                reloj.Elapsed);

            return RedirectToPage(
                "/Configuracion/Mantenimiento");
        }
        catch (Exception ex)
        {
            reloj.Stop();

            _logger.LogError(
                ex,
                "Error ejecutando DBCC CHECKDB.");

            MensajeError =
                $"Error verificando la integridad: {ex.Message}";

            return RedirectToPage(
                "/Configuracion/Mantenimiento");
        }
    }

    // =========================================================
    // ESTADÍSTICAS
    // =========================================================

    public async Task<IActionResult>
        OnPostActualizarEstadisticasAsync()
    {
        if (!ObtenerContexto())
        {
            return RedirectToPage(
                "/Account/Login");
        }

        var reloj =
            Stopwatch.StartNew();

        try
        {
            const string sql = """
                EXEC sys.sp_updatestats;
                """;

            await EjecutarComandoAsync(
                sql,
                600);

            reloj.Stop();

            UltimaOperacion =
                "Actualización de estadísticas";

            UltimaDuracion =
                FormatearDuracion(
                    reloj.Elapsed);

            MensajeExito =
                "Las estadísticas de la base de datos " +
                "se actualizaron correctamente.";

            _logger.LogInformation(
                "sp_updatestats completado. Duración: {Duracion}.",
                reloj.Elapsed);

            return RedirectToPage(
                "/Configuracion/Mantenimiento");
        }
        catch (Exception ex)
        {
            reloj.Stop();

            _logger.LogError(
                ex,
                "Error actualizando estadísticas.");

            MensajeError =
                $"No se pudieron actualizar las estadísticas: {ex.Message}";

            return RedirectToPage(
                "/Configuracion/Mantenimiento");
        }
    }

    // =========================================================
    // ÍNDICES
    // =========================================================

    public async Task<IActionResult>
        OnPostMantenerIndicesAsync()
    {
        if (!ObtenerContexto())
        {
            return RedirectToPage(
                "/Account/Login");
        }

        var reloj =
            Stopwatch.StartNew();

        try
        {
            const string sql = """
                SET NOCOUNT ON;

                DECLARE
                    @SchemaName SYSNAME,
                    @TableName SYSNAME,
                    @IndexName SYSNAME,
                    @Fragmentation FLOAT,
                    @SqlCommand NVARCHAR(MAX);

                DECLARE indice_cursor CURSOR LOCAL FAST_FORWARD
                FOR
                SELECT
                    s.name,
                    t.name,
                    i.name,
                    ips.avg_fragmentation_in_percent
                FROM sys.dm_db_index_physical_stats
                (
                    DB_ID(),
                    NULL,
                    NULL,
                    NULL,
                    'LIMITED'
                ) AS ips
                INNER JOIN sys.tables AS t
                    ON ips.object_id = t.object_id
                INNER JOIN sys.schemas AS s
                    ON t.schema_id = s.schema_id
                INNER JOIN sys.indexes AS i
                    ON ips.object_id = i.object_id
                    AND ips.index_id = i.index_id
                WHERE
                    i.index_id > 0
                    AND i.name IS NOT NULL
                    AND ips.page_count >= 10
                    AND ips.avg_fragmentation_in_percent >= 10;

                OPEN indice_cursor;

                FETCH NEXT FROM indice_cursor
                INTO
                    @SchemaName,
                    @TableName,
                    @IndexName,
                    @Fragmentation;

                WHILE @@FETCH_STATUS = 0
                BEGIN

                    IF @Fragmentation >= 30
                    BEGIN
                        SET @SqlCommand =
                            N'ALTER INDEX ' +
                            QUOTENAME(@IndexName) +
                            N' ON ' +
                            QUOTENAME(@SchemaName) +
                            N'.' +
                            QUOTENAME(@TableName) +
                            N' REBUILD;';
                    END
                    ELSE
                    BEGIN
                        SET @SqlCommand =
                            N'ALTER INDEX ' +
                            QUOTENAME(@IndexName) +
                            N' ON ' +
                            QUOTENAME(@SchemaName) +
                            N'.' +
                            QUOTENAME(@TableName) +
                            N' REORGANIZE;';
                    END;

                    EXEC sys.sp_executesql
                        @SqlCommand;

                    FETCH NEXT FROM indice_cursor
                    INTO
                        @SchemaName,
                        @TableName,
                        @IndexName,
                        @Fragmentation;
                END;

                CLOSE indice_cursor;
                DEALLOCATE indice_cursor;
                """;

            await EjecutarComandoAsync(
                sql,
                900);

            reloj.Stop();

            UltimaOperacion =
                "Mantenimiento de índices";

            UltimaDuracion =
                FormatearDuracion(
                    reloj.Elapsed);

            MensajeExito =
                "Mantenimiento de índices completado correctamente.";

            _logger.LogInformation(
                "Mantenimiento de índices completado. Duración: {Duracion}.",
                reloj.Elapsed);

            return RedirectToPage(
                "/Configuracion/Mantenimiento");
        }
        catch (Exception ex)
        {
            reloj.Stop();

            _logger.LogError(
                ex,
                "Error realizando mantenimiento de índices.");

            MensajeError =
                $"No se pudo realizar el mantenimiento de índices: {ex.Message}";

            return RedirectToPage(
                "/Configuracion/Mantenimiento");
        }
    }

    // =========================================================
    // HELPERS SQL
    // =========================================================

    private async Task EjecutarComandoAsync(
        string sql,
        int timeout)
    {
        string connectionString =
            ObtenerConnectionString();

        await using var connection =
            new SqlConnection(
                connectionString);

        await connection.OpenAsync();

        await using var command =
            new SqlCommand(
                sql,
                connection);

        command.CommandType =
            CommandType.Text;

        command.CommandTimeout =
            timeout;

        await command.ExecuteNonQueryAsync();
    }

    private async Task<string>
        ObtenerNombreBaseDatosAsync()
    {
        string connectionString =
            ObtenerConnectionString();

        await using var connection =
            new SqlConnection(
                connectionString);

        await connection.OpenAsync();

        await using var command =
            new SqlCommand(
                "SELECT DB_NAME();",
                connection);

        object? resultado =
            await command.ExecuteScalarAsync();

        return resultado?.ToString()
            ?? "kernixerp";
    }

    private string ObtenerConnectionString()
    {
        string? connectionString =
            _configuration.GetConnectionString(
                "KerniXDb");

        if (string.IsNullOrWhiteSpace(
            connectionString))
        {
            throw new InvalidOperationException(
                "La cadena de conexión KerniXDb no está configurada.");
        }

        return connectionString;
    }

    // =========================================================
    // SESIÓN
    // =========================================================

    private bool ObtenerContexto()
    {
        int? idUsuario =
            HttpContext.Session.GetInt32(
                SessionKeys.IdUsuario);

        if (!idUsuario.HasValue ||
            idUsuario.Value <= 0)
        {
            return false;
        }

        IdEmpresa =
            HttpContext.Session.GetInt32(
                "IdEmpresa") ?? 0;

        if (IdEmpresa <= 0)
        {
            IdEmpresa = 1;

            HttpContext.Session.SetInt32(
                "IdEmpresa",
                IdEmpresa);
        }

        return true;
    }

    private static string FormatearDuracion(
        TimeSpan duracion)
    {
        if (duracion.TotalMinutes >= 1)
        {
            return
                $"{(int)duracion.TotalMinutes} min " +
                $"{duracion.Seconds} s";
        }

        return
            $"{duracion.TotalSeconds:0.00} s";
    }
}