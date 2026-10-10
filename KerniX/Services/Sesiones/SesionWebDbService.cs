using System.Data;
using KerniX.Services.Sesiones;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;

namespace KerniX.Services.Sesiones;

public sealed class SesionWebDbService : ISesionWebDbService
{
    private readonly string _connectionString;

    // Consistente con Program.cs: AddSession(...IdleTimeout = 30 minutos).
    // Cambiar ambos valores si se decide un tiempo diferente.
    private const int InactividadMinutos = 30;

    public SesionWebDbService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("KerniXDb")
            ?? throw new InvalidOperationException(
                "Falta ConnectionStrings:KerniXDb.");
    }

    public async Task RegistrarAsync(Guid idSesion, int idUsuario,
        HttpContext context, CancellationToken cancellationToken)
    {
        if (idSesion == Guid.Empty || idUsuario <= 0)
            throw new ArgumentException("Sesión o usuario no válido.");

        string agente = context.Request.Headers["User-Agent"].ToString();
        string navegador = DetectarNavegador(agente);
        string dispositivo = DetectarDispositivo(agente);
        string? ip = context.Connection.RemoteIpAddress?.ToString();
        if (ip?.Length > 45) ip = ip[..45];

        const string sql = @"
INSERT INTO seg.sesion_web
(id_sesion, id_usuario, fecha_inicio_utc, ultimo_acceso_utc,
 ip, navegador, dispositivo)
VALUES (@id, @usuario, SYSUTCDATETIME(), SYSUTCDATETIME(),
 @ip, @navegador, @dispositivo);";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@id", SqlDbType.UniqueIdentifier).Value = idSesion;
        command.Parameters.Add("@usuario", SqlDbType.Int).Value = idUsuario;
        command.Parameters.Add("@ip", SqlDbType.NVarChar, 45).Value =
            (object?)ip ?? DBNull.Value;
        command.Parameters.Add("@navegador", SqlDbType.NVarChar, 60).Value = navegador;
        command.Parameters.Add("@dispositivo", SqlDbType.NVarChar, 60).Value = dispositivo;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<bool> ValidarYActualizarAsync(Guid idSesion, int idUsuario,
        CancellationToken cancellationToken)
    {
        if (idSesion == Guid.Empty || idUsuario <= 0) return false;

        // Validación en cada request. Solo actualiza último acceso cada 60s.
        // Rechaza sesiones revocadas o inactivas durante 30 minutos.
        const string sql = @"
UPDATE seg.sesion_web
SET ultimo_acceso_utc = CASE
 WHEN DATEDIFF(SECOND, ultimo_acceso_utc, SYSUTCDATETIME()) >= 60
 THEN SYSUTCDATETIME() ELSE ultimo_acceso_utc END
WHERE id_sesion = @id AND id_usuario = @usuario
 AND fecha_cierre_utc IS NULL
 AND ultimo_acceso_utc > DATEADD(MINUTE, -30, SYSUTCDATETIME());
SELECT @@ROWCOUNT;";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@id", SqlDbType.UniqueIdentifier).Value = idSesion;
        command.Parameters.Add("@usuario", SqlDbType.Int).Value = idUsuario;
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken)) == 1;
    }

    public async Task<IReadOnlyList<SesionWebResumen>> ListarActivasAsync(
        int idUsuario, CancellationToken cancellationToken)
    {
        if (idUsuario <= 0) return Array.Empty<SesionWebResumen>();
        const string sql = @"
SELECT id_sesion, fecha_inicio_utc, ultimo_acceso_utc,
       dispositivo, navegador, ip
FROM seg.sesion_web
WHERE id_usuario = @usuario
  AND fecha_cierre_utc IS NULL
  AND ultimo_acceso_utc > DATEADD(MINUTE, -30, SYSUTCDATETIME())
ORDER BY ultimo_acceso_utc DESC;";

        var list = new List<SesionWebResumen>();
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@usuario", SqlDbType.Int).Value = idUsuario;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            list.Add(new SesionWebResumen
            {
                IdSesion = reader.GetGuid(0),
                FechaInicioUtc = reader.GetDateTime(1),
                UltimoAccesoUtc = reader.GetDateTime(2),
                Dispositivo = reader.GetString(3),
                Navegador = reader.GetString(4),
                Ip = reader.IsDBNull(5) ? null : reader.GetString(5)
            });
        }
        return list;
    }

    public async Task<bool> CerrarAsync(Guid idSesion, int idUsuario,
        string motivo, CancellationToken cancellationToken)
    {
        if (idSesion == Guid.Empty || idUsuario <= 0) return false;
        const string sql = @"
UPDATE seg.sesion_web
SET fecha_cierre_utc = SYSUTCDATETIME(), motivo_cierre = @motivo
WHERE id_sesion = @id AND id_usuario = @usuario
  AND fecha_cierre_utc IS NULL;";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@id", SqlDbType.UniqueIdentifier).Value = idSesion;
        command.Parameters.Add("@usuario", SqlDbType.Int).Value = idUsuario;
        command.Parameters.Add("@motivo", SqlDbType.NVarChar, 30).Value =
            motivo.Length > 30 ? motivo[..30] : motivo;
        return await command.ExecuteNonQueryAsync(cancellationToken) == 1;
    }

    public async Task<int> CerrarOtrasAsync(Guid idSesionActual, int idUsuario,
        CancellationToken cancellationToken)
    {
        if (idSesionActual == Guid.Empty || idUsuario <= 0) return 0;
        const string sql = @"
UPDATE seg.sesion_web
SET fecha_cierre_utc = SYSUTCDATETIME(), motivo_cierre = N'cierre_remoto'
WHERE id_usuario = @usuario AND id_sesion <> @actual
  AND fecha_cierre_utc IS NULL;";

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@usuario", SqlDbType.Int).Value = idUsuario;
        command.Parameters.Add("@actual", SqlDbType.UniqueIdentifier).Value = idSesionActual;
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string DetectarNavegador(string agente)
    {
        if (agente.Contains("Edg/", StringComparison.OrdinalIgnoreCase)) return "Microsoft Edge";
        if (agente.Contains("OPR/", StringComparison.OrdinalIgnoreCase)) return "Opera";
        if (agente.Contains("Chrome/", StringComparison.OrdinalIgnoreCase)) return "Google Chrome";
        if (agente.Contains("Firefox/", StringComparison.OrdinalIgnoreCase)) return "Mozilla Firefox";
        if (agente.Contains("Safari/", StringComparison.OrdinalIgnoreCase)) return "Safari";
        return "Navegador no identificado";
    }

    private static string DetectarDispositivo(string agente)
    {
        if (agente.Contains("Android", StringComparison.OrdinalIgnoreCase)) return "Android";
        if (agente.Contains("iPhone", StringComparison.OrdinalIgnoreCase) ||
            agente.Contains("iPad", StringComparison.OrdinalIgnoreCase)) return "iPhone / iPad";
        if (agente.Contains("Windows", StringComparison.OrdinalIgnoreCase)) return "Windows";
        if (agente.Contains("Macintosh", StringComparison.OrdinalIgnoreCase)) return "macOS";
        if (agente.Contains("Linux", StringComparison.OrdinalIgnoreCase)) return "Linux";
        return "Dispositivo no identificado";
    }
}
