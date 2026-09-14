using System.Data;
using KerniX.Api.Models.Configuracion.Mantenimiento;
using Microsoft.Data.SqlClient;

namespace KerniX.Api.Services.Configuracion;

public sealed class RestauracionSistemaDbService
    : IRestauracionSistemaDbService
{
    private readonly string _connectionString;

    public RestauracionSistemaDbService(
        IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("KerniXDb")
            ?? throw new InvalidOperationException(
                "No existe KerniXDb.");
    }

    public async Task<IReadOnlyList<RestauracionSistemaResponse>>
        ListarAsync()
    {
        var lista =
            new List<RestauracionSistemaResponse>();

        await using var cn =
            new SqlConnection(_connectionString);

        await using var cmd =
            new SqlCommand(
                "cfg.sp_RestauracionSistema_Listar",
                cn);

        cmd.CommandType =
            CommandType.StoredProcedure;

        await cn.OpenAsync();

        await using var reader =
            await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
            lista.Add(Mapear(reader));

        return lista;
    }

    public Task<RestauracionSistemaResponse?>
        ObtenerPorIdAsync(long idRestauracion)
    {
        return ObtenerAsync(
            "cfg.sp_RestauracionSistema_ObtenerPorId",
            c =>
            {
                c.Parameters.Add(
                    "@IdRestauracion",
                    SqlDbType.BigInt)
                    .Value = idRestauracion;
            });
    }

    public Task<RestauracionSistemaResponse>
        IniciarAsync(
            RestauracionIniciarRequest request)
    {
        return EjecutarAsync(
            "cfg.sp_RestauracionSistema_Iniciar",
            c =>
            {
                c.Parameters.Add(
                    "@IdCopiaSeguridad",
                    SqlDbType.BigInt)
                    .Value =
                    request.IdCopiaSeguridad;

                c.Parameters.Add(
                    "@IdUsuario",
                    SqlDbType.Int)
                    .Value =
                    request.IdUsuario;

                c.Parameters.Add(
                    "@Observacion",
                    SqlDbType.NVarChar,
                    1000)
                    .Value =
                    (object?)request.Observacion
                    ?? DBNull.Value;
            });
    }

    public Task<RestauracionSistemaResponse>
        CompletarAsync(
            long idRestauracion,
            RestauracionFinalizarRequest request)
    {
        return FinalizarAsync(
            "cfg.sp_RestauracionSistema_Completar",
            idRestauracion,
            request);
    }

    public Task<RestauracionSistemaResponse>
        MarcarErrorAsync(
            long idRestauracion,
            RestauracionFinalizarRequest request)
    {
        return FinalizarAsync(
            "cfg.sp_RestauracionSistema_MarcarError",
            idRestauracion,
            request);
    }

    public Task<RestauracionSistemaResponse>
        CancelarAsync(
            long idRestauracion,
            string? observacion)
    {
        return EjecutarAsync(
            "cfg.sp_RestauracionSistema_Cancelar",
            c =>
            {
                c.Parameters.Add(
                    "@IdRestauracion",
                    SqlDbType.BigInt)
                    .Value = idRestauracion;

                c.Parameters.Add(
                    "@Observacion",
                    SqlDbType.NVarChar,
                    1000)
                    .Value =
                    (object?)observacion
                    ?? DBNull.Value;
            });
    }

    private Task<RestauracionSistemaResponse>
        FinalizarAsync(
            string sp,
            long id,
            RestauracionFinalizarRequest request)
    {
        return EjecutarAsync(
            sp,
            c =>
            {
                c.Parameters.Add(
                    "@IdRestauracion",
                    SqlDbType.BigInt)
                    .Value = id;

                c.Parameters.Add(
                    "@Resultado",
                    SqlDbType.NVarChar,
                    -1)
                    .Value =
                    (object?)request.Resultado
                    ?? DBNull.Value;

                c.Parameters.Add(
                    "@Observacion",
                    SqlDbType.NVarChar,
                    1000)
                    .Value =
                    (object?)request.Observacion
                    ?? DBNull.Value;
            });
    }

    private async Task<RestauracionSistemaResponse?>
        ObtenerAsync(
            string sp,
            Action<SqlCommand> configurar)
    {
        await using var cn =
            new SqlConnection(_connectionString);

        await using var cmd =
            new SqlCommand(sp, cn);

        cmd.CommandType =
            CommandType.StoredProcedure;

        configurar(cmd);

        await cn.OpenAsync();

        await using var reader =
            await cmd.ExecuteReaderAsync();

        return await reader.ReadAsync()
            ? Mapear(reader)
            : null;
    }

    private async Task<RestauracionSistemaResponse>
        EjecutarAsync(
            string sp,
            Action<SqlCommand> configurar)
    {
        return await ObtenerAsync(sp, configurar)
            ?? throw new InvalidOperationException(
                "El SP no devolvió datos.");
    }

    private static RestauracionSistemaResponse Mapear(
        SqlDataReader r)
    {
        return new RestauracionSistemaResponse
        {
            IdRestauracion =
                Convert.ToInt64(
                    r["id_restauracion"]),

            IdCopiaSeguridad =
                Convert.ToInt64(
                    r["id_copia_seguridad"]),

            IdUsuario =
                Convert.ToInt32(
                    r["id_usuario"]),

            FechaInicio =
                Convert.ToDateTime(
                    r["fecha_inicio"]),

            FechaFin =
                r["fecha_fin"] == DBNull.Value
                    ? null
                    : Convert.ToDateTime(
                        r["fecha_fin"]),

            Estado =
                r["estado"]?.ToString()
                ?? string.Empty,

            Resultado =
                r["resultado"] == DBNull.Value
                    ? null
                    : r["resultado"].ToString(),

            Observacion =
                r["observacion"] == DBNull.Value
                    ? null
                    : r["observacion"].ToString()
        };
    }
}