using System.Data;
using KerniX.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;

namespace KerniX.Services.Proveedores;

public sealed record ProveedorContexto(int IdUsuario, int IdEmpresa,
    int IdSucursal, bool PuedeGestionar);

// Siempre verifica la vinculación usuario/empresa en SQL Server.
// Los permisos de escritura requieren un rol administrador ACTIVO de la empresa.
public sealed class ProveedorContextService
{
    private readonly IHttpContextAccessor _http;
    private readonly string _connectionString;

    public ProveedorContextService(IHttpContextAccessor http,
        IConfiguration configuration)
    {
        _http = http;
        _connectionString = configuration.GetConnectionString("KerniXDb")
            ?? throw new InvalidOperationException("Falta ConnectionStrings:KerniXDb.");
    }

    public async Task<ProveedorContexto?> ObtenerAsync(CancellationToken cancellationToken)
    {
        var ctx = _http.HttpContext;
        if (ctx is null || string.IsNullOrWhiteSpace(
                ctx.Session.GetString(SessionKeys.AccessToken)))
            return null;

        int idUsuario = ctx.Session.GetInt32(SessionKeys.IdUsuario) ?? 0;
        if (idUsuario <= 0) return null;

        const string sql = """
            SELECT TOP (1)
                e.id_empresa,
                e.id_sucursal,
                CAST(CASE WHEN EXISTS (
                    SELECT 1
                    FROM seg.usuario_rol ur
                    INNER JOIN seg.rol r ON r.id_rol = ur.id_rol
                    WHERE ur.id_usuario = u.id_usuario
                      AND ur.estado = 1
                      AND r.estado = 1
                      AND r.es_administrador = 1
                      AND (r.id_empresa IS NULL OR r.id_empresa = e.id_empresa)
                ) THEN 1 ELSE 0 END AS bit) AS PuedeGestionar
            FROM seg.usuario u
            INNER JOIN org.empleado e ON e.id_empleado = u.id_empleado
            WHERE u.id_usuario = @IdUsuario
              AND u.estado = 1 AND e.estado = 1
              AND e.id_empresa IS NOT NULL;
            """;

        await using var cn = new SqlConnection(_connectionString);
        await cn.OpenAsync(cancellationToken);
        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.Add("@IdUsuario", SqlDbType.Int).Value = idUsuario;
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        int idEmpresa = reader.IsDBNull(0) ? 0 : Convert.ToInt32(reader.GetValue(0));
        int idSucursal = reader.IsDBNull(1) ? 0 : Convert.ToInt32(reader.GetValue(1));
        if (idEmpresa <= 0) return null;
        bool puedeGestionar = !reader.IsDBNull(2) && reader.GetBoolean(2);

        ctx.Session.SetInt32("IdEmpresa", idEmpresa);
        if (idSucursal > 0) ctx.Session.SetInt32("IdSucursal", idSucursal);
        else ctx.Session.Remove("IdSucursal");

        return new ProveedorContexto(idUsuario, idEmpresa, idSucursal, puedeGestionar);
    }
}
