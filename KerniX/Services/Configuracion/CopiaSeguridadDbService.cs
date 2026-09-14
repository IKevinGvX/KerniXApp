using System.Data;
using KerniX.Api.Models.Configuracion.Mantenimiento;
using Microsoft.Data.SqlClient;

namespace KerniX.Api.Services.Configuracion;

public sealed class CopiaSeguridadDbService
    : ICopiaSeguridadDbService
{
    private readonly string _connectionString;

    public CopiaSeguridadDbService(
        IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("KerniXDb")
            ?? throw new InvalidOperationException(
                "No existe KerniXDb.");
    }

    public async Task<IReadOnlyList<CopiaSeguridadResponse>>
        ListarAsync(int idEmpresa)
    {
        var lista = new List<CopiaSeguridadResponse>();

        await using var cn =
            new SqlConnection(_connectionString);

        await using var cmd =
            new SqlCommand(
                "cfg.sp_CopiaSeguridad_Listar",
                cn);

        cmd.CommandType =
            CommandType.StoredProcedure;

        cmd.Parameters.Add(
            "@IdEmpresa",
            SqlDbType.Int)
            .Value = idEmpresa;

        await cn.OpenAsync();

        await using var reader =
            await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
            lista.Add(Mapear(reader));

        return lista;
    }

    public Task<CopiaSeguridadResponse?>
        ObtenerPorIdAsync(
            long idCopiaSeguridad)
    {
        return ObtenerAsync(
            "cfg.sp_CopiaSeguridad_ObtenerPorId",
            c =>
            {
                c.Parameters.Add(
                    "@IdCopiaSeguridad",
                    SqlDbType.BigInt)
                    .Value = idCopiaSeguridad;
            });
    }

    public Task<CopiaSeguridadResponse?>
        ObtenerUltimaAsync(
            int idEmpresa)
    {
        return ObtenerAsync(
            "cfg.sp_CopiaSeguridad_ObtenerUltima",
            c =>
            {
                c.Parameters.Add(
                    "@IdEmpresa",
                    SqlDbType.Int)
                    .Value = idEmpresa;
            });
    }

    public Task<CopiaSeguridadResponse>
        IniciarAsync(
            CopiaSeguridadIniciarRequest request)
    {
        return EjecutarAsync(
            "cfg.sp_CopiaSeguridad_Iniciar",
            c =>
            {
                c.Parameters.Add(
                    "@IdEmpresa",
                    SqlDbType.Int)
                    .Value = request.IdEmpresa;

                c.Parameters.Add(
                    "@IdUsuario",
                    SqlDbType.Int)
                    .Value = request.IdUsuario;

                c.Parameters.Add(
                    "@TipoCopia",
                    SqlDbType.NVarChar,
                    60)
                    .Value = request.TipoCopia;

                c.Parameters.Add(
                    "@NombreArchivo",
                    SqlDbType.NVarChar,
                    510)
                    .Value = request.NombreArchivo;

                c.Parameters.Add(
                    "@RutaArchivo",
                    SqlDbType.NVarChar,
                    2000)
                    .Value = request.RutaArchivo;

                c.Parameters.Add(
                    "@Observacion",
                    SqlDbType.NVarChar,
                    1000)
                    .Value =
                    (object?)request.Observacion
                    ?? DBNull.Value;
            });
    }

    public Task<CopiaSeguridadResponse>
        CompletarAsync(
            long idCopiaSeguridad,
            CopiaSeguridadCompletarRequest request)
    {
        return EjecutarAsync(
            "cfg.sp_CopiaSeguridad_Completar",
            c =>
            {
                c.Parameters.Add(
                    "@IdCopiaSeguridad",
                    SqlDbType.BigInt)
                    .Value = idCopiaSeguridad;

                c.Parameters.Add(
                    "@TamanoBytes",
                    SqlDbType.BigInt)
                    .Value = request.TamanoBytes;

                c.Parameters.Add(
                    "@HashArchivo",
                    SqlDbType.NVarChar,
                    1000)
                    .Value =
                    (object?)request.HashArchivo
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

    public Task<CopiaSeguridadResponse>
        MarcarErrorAsync(
            long idCopiaSeguridad,
            string observacion)
    {
        return EjecutarAsync(
            "cfg.sp_CopiaSeguridad_MarcarError",
            c =>
            {
                c.Parameters.Add(
                    "@IdCopiaSeguridad",
                    SqlDbType.BigInt)
                    .Value = idCopiaSeguridad;

                c.Parameters.Add(
                    "@Observacion",
                    SqlDbType.NVarChar,
                    1000)
                    .Value = observacion;
            });
    }

    public Task<CopiaSeguridadResponse>
        CancelarAsync(
            long idCopiaSeguridad,
            string? observacion)
    {
        return EjecutarAsync(
            "cfg.sp_CopiaSeguridad_Cancelar",
            c =>
            {
                c.Parameters.Add(
                    "@IdCopiaSeguridad",
                    SqlDbType.BigInt)
                    .Value = idCopiaSeguridad;

                c.Parameters.Add(
                    "@Observacion",
                    SqlDbType.NVarChar,
                    1000)
                    .Value =
                    (object?)observacion
                    ?? DBNull.Value;
            });
    }

    private async Task<CopiaSeguridadResponse?>
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

    private async Task<CopiaSeguridadResponse>
        EjecutarAsync(
            string sp,
            Action<SqlCommand> configurar)
    {
        return await ObtenerAsync(
            sp,
            configurar)
            ?? throw new InvalidOperationException(
                "El SP no devolvió datos.");
    }

    private static CopiaSeguridadResponse Mapear(
        SqlDataReader r)
    {
        return new CopiaSeguridadResponse
        {
            IdCopiaSeguridad =
                Convert.ToInt64(
                    r["id_copia_seguridad"]),

            IdEmpresa =
                Convert.ToInt32(
                    r["id_empresa"]),

            IdUsuario =
                Convert.ToInt32(
                    r["id_usuario"]),

            TipoCopia =
                r["tipo_copia"]?.ToString()
                ?? string.Empty,

            NombreArchivo =
                r["nombre_archivo"]?.ToString()
                ?? string.Empty,

            RutaArchivo =
                r["ruta_archivo"]?.ToString()
                ?? string.Empty,

            TamanoBytes =
                r["tamano_bytes"] == DBNull.Value
                    ? null
                    : Convert.ToInt64(
                        r["tamano_bytes"]),

            HashArchivo =
                r["hash_archivo"] == DBNull.Value
                    ? null
                    : r["hash_archivo"].ToString(),

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

            Observacion =
                r["observacion"] == DBNull.Value
                    ? null
                    : r["observacion"].ToString()
        };
    }
}