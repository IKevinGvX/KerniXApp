using KerniX.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Data;

namespace KerniX.Pages.Configuracion;

public sealed class BackupsModel : PageModel
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<BackupsModel> _logger;

    public BackupsModel(
        IConfiguration configuration,
        ILogger<BackupsModel> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public int IdEmpresa { get; private set; }

    [TempData]
    public string? MensajeExito { get; set; }

    [TempData]
    public string? MensajeError { get; set; }

    public string? UltimoArchivo { get; set; }

    public DateTime? UltimaFecha { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        if (!ObtenerContexto())
        {
            return RedirectToPage(
                "/Account/Login");
        }

        await CargarUltimoBackupAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostEjecutarBackupAsync()
    {
        if (!ObtenerContexto())
        {
            return RedirectToPage(
                "/Account/Login");
        }

        try
        {
            string? connectionString =
                _configuration.GetConnectionString(
                    "KerniXDb");

            if (string.IsNullOrWhiteSpace(
                connectionString))
            {
                throw new InvalidOperationException(
                    "No se encontró la conexión KerniXDb.");
            }

            await using var connection =
                new SqlConnection(
                    connectionString);

            await connection.OpenAsync();

            await using var command =
                new SqlCommand(
                    "dbo.sp_Backup_KerniXERP",
                    connection);

            command.CommandType =
                CommandType.StoredProcedure;

            command.CommandTimeout = 300;

            await command.ExecuteNonQueryAsync();

            MensajeExito =
                "Copia de seguridad generada correctamente.";

            _logger.LogInformation(
                "Backup manual ejecutado correctamente. Empresa: {IdEmpresa}",
                IdEmpresa);

            return RedirectToPage(
                "/Configuracion/Backups");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error ejecutando backup manual.");

            MensajeError =
                $"No se pudo generar el backup: {ex.Message}";

            return RedirectToPage(
                "/Configuracion/Backups");
        }
    }

    private async Task CargarUltimoBackupAsync()
    {
        try
        {
            string? connectionString =
                _configuration.GetConnectionString(
                    "KerniXDb");

            if (string.IsNullOrWhiteSpace(
                connectionString))
            {
                return;
            }

            await using var connection =
                new SqlConnection(
                    connectionString);

            await connection.OpenAsync();

            const string sql = """
                SELECT TOP (1)
                    physical_device_name,
                    backup_finish_date
                FROM msdb.dbo.backupset AS bs
                INNER JOIN msdb.dbo.backupmediafamily AS bmf
                    ON bs.media_set_id = bmf.media_set_id
                WHERE bs.database_name = DB_NAME()
                  AND bs.type = 'D'
                ORDER BY bs.backup_finish_date DESC;
                """;

            await using var command =
                new SqlCommand(
                    sql,
                    connection);

            await using var reader =
                await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                UltimoArchivo =
                    reader["physical_device_name"]
                        ?.ToString();

                if (reader["backup_finish_date"]
                    != DBNull.Value)
                {
                    UltimaFecha =
                        Convert.ToDateTime(
                            reader["backup_finish_date"]);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "No se pudo obtener el último backup desde msdb.");
        }
    }

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
}