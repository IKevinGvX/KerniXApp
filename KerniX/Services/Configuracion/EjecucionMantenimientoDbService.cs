using System.Data;
using KerniX.Api.Models.Configuracion.Mantenimiento;
using Microsoft.Data.SqlClient;

namespace KerniX.Api.Services.Configuracion;

public sealed class EjecucionMantenimientoDbService
    : IEjecucionMantenimientoDbService
{
    private readonly string _connectionString;

    public EjecucionMantenimientoDbService(
        IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("KerniXDb")
            ?? throw new InvalidOperationException(
                "No existe KerniXDb.");
    }

    public Task<IReadOnlyList<EjecucionMantenimientoResponse>>
        ListarAsync()
    {
        return ListarInternoAsync(
            "cfg.sp_EjecucionMantenimiento_Listar",
            null);
    }

    public Task<EjecucionMantenimientoResponse?>
        ObtenerPorIdAsync(
            long idEjecucionMantenimiento)
    {
        return ObtenerAsync(
            "cfg.sp_EjecucionMantenimiento_ObtenerPorId",
            command =>
            {
                command.Parameters.Add(
                    "@IdEjecucionMantenimiento",
                    SqlDbType.BigInt)
                    .Value =
                    idEjecucionMantenimiento;
            });
    }

    public Task<IReadOnlyList<EjecucionMantenimientoResponse>>
        ListarPorPlanAsync(
            int idPlanMantenimiento)
    {
        return ListarInternoAsync(
            "cfg.sp_EjecucionMantenimiento_ListarPorPlan",
            command =>
            {
                command.Parameters.Add(
                    "@IdPlanMantenimiento",
                    SqlDbType.Int)
                    .Value =
                    idPlanMantenimiento;
            });
    }

    public Task<EjecucionMantenimientoResponse>
        IniciarAsync(
            EjecucionMantenimientoIniciarRequest request)
    {
        return EjecutarAsync(
            "cfg.sp_EjecucionMantenimiento_Iniciar",
            command =>
            {
                command.Parameters.Add(
                    "@IdPlanMantenimiento",
                    SqlDbType.Int)
                    .Value =
                    request.IdPlanMantenimiento;

                command.Parameters.Add(
                    "@OrigenEjecucion",
                    SqlDbType.NVarChar,
                    100)
                    .Value =
                    request.OrigenEjecucion;

                command.Parameters.Add(
                    "@IdEjecucionExterna",
                    SqlDbType.NVarChar,
                    400)
                    .Value =
                    (object?)request.IdEjecucionExterna
                    ?? DBNull.Value;
            });
    }

    public Task<EjecucionMantenimientoResponse>
        CompletarAsync(
            long idEjecucionMantenimiento,
            string? resultado)
    {
        return EjecutarAsync(
            "cfg.sp_EjecucionMantenimiento_Completar",
            command =>
            {
                command.Parameters.Add(
                    "@IdEjecucionMantenimiento",
                    SqlDbType.BigInt)
                    .Value =
                    idEjecucionMantenimiento;

                command.Parameters.Add(
                    "@Resultado",
                    SqlDbType.NVarChar,
                    -1)
                    .Value =
                    (object?)resultado
                    ?? DBNull.Value;
            });
    }

    public Task<EjecucionMantenimientoResponse>
        MarcarErrorAsync(
            long idEjecucionMantenimiento,
            string detalleError,
            string? resultado)
    {
        return EjecutarAsync(
            "cfg.sp_EjecucionMantenimiento_MarcarError",
            command =>
            {
                command.Parameters.Add(
                    "@IdEjecucionMantenimiento",
                    SqlDbType.BigInt)
                    .Value =
                    idEjecucionMantenimiento;

                command.Parameters.Add(
                    "@DetalleError",
                    SqlDbType.NVarChar,
                    -1)
                    .Value =
                    detalleError;

                command.Parameters.Add(
                    "@Resultado",
                    SqlDbType.NVarChar,
                    -1)
                    .Value =
                    (object?)resultado
                    ?? DBNull.Value;
            });
    }

    public Task<EjecucionMantenimientoResponse>
        CancelarAsync(
            long idEjecucionMantenimiento,
            string? resultado)
    {
        return EjecutarAsync(
            "cfg.sp_EjecucionMantenimiento_Cancelar",
            command =>
            {
                command.Parameters.Add(
                    "@IdEjecucionMantenimiento",
                    SqlDbType.BigInt)
                    .Value =
                    idEjecucionMantenimiento;

                command.Parameters.Add(
                    "@Resultado",
                    SqlDbType.NVarChar,
                    -1)
                    .Value =
                    (object?)resultado
                    ?? DBNull.Value;
            });
    }

    private async Task<IReadOnlyList<EjecucionMantenimientoResponse>>
        ListarInternoAsync(
            string procedimiento,
            Action<SqlCommand>? configurar)
    {
        var lista =
            new List<EjecucionMantenimientoResponse>();

        await using var connection =
            new SqlConnection(_connectionString);

        await using var command =
            new SqlCommand(
                procedimiento,
                connection);

        command.CommandType =
            CommandType.StoredProcedure;

        configurar?.Invoke(command);

        await connection.OpenAsync();

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            lista.Add(Mapear(reader));
        }

        return lista;
    }

    private async Task<EjecucionMantenimientoResponse?>
        ObtenerAsync(
            string procedimiento,
            Action<SqlCommand> configurar)
    {
        var lista =
            await ListarInternoAsync(
                procedimiento,
                configurar);

        return lista.FirstOrDefault();
    }

    private async Task<EjecucionMantenimientoResponse>
        EjecutarAsync(
            string procedimiento,
            Action<SqlCommand> configurar)
    {
        return await ObtenerAsync(
            procedimiento,
            configurar)
            ?? throw new InvalidOperationException(
                "El procedimiento no devolvió datos.");
    }

    private static EjecucionMantenimientoResponse Mapear(
        SqlDataReader reader)
    {
        return new EjecucionMantenimientoResponse
        {
            IdEjecucionMantenimiento =
                Convert.ToInt64(
                    reader["id_ejecucion_mantenimiento"]),

            IdPlanMantenimiento =
                Convert.ToInt32(
                    reader["id_plan_mantenimiento"]),

            CodigoPlan =
                LeerTexto(
                    reader,
                    "codigo_plan"),

            NombrePlan =
                LeerTexto(
                    reader,
                    "nombre_plan"),

            TipoTarea =
                LeerTexto(
                    reader,
                    "tipo_tarea"),

            OrigenEjecucion =
                reader["origen_ejecucion"]?.ToString()
                ?? string.Empty,

            IdEjecucionExterna =
                LeerTexto(
                    reader,
                    "id_ejecucion_externa"),

            FechaInicio =
                Convert.ToDateTime(
                    reader["fecha_inicio"]),

            FechaFin =
                reader["fecha_fin"] == DBNull.Value
                    ? null
                    : Convert.ToDateTime(
                        reader["fecha_fin"]),

            Estado =
                reader["estado"]?.ToString()
                ?? string.Empty,

            Resultado =
                reader["resultado"] == DBNull.Value
                    ? null
                    : reader["resultado"].ToString(),

            DetalleError =
                reader["detalle_error"] == DBNull.Value
                    ? null
                    : reader["detalle_error"].ToString(),

            DuracionSegundos =
                reader["duracion_segundos"] == DBNull.Value
                    ? null
                    : Convert.ToInt32(
                        reader["duracion_segundos"])
        };
    }

    private static string? LeerTexto(
        SqlDataReader reader,
        string nombre)
    {
        for (var i = 0; i < reader.FieldCount; i++)
        {
            if (!reader.GetName(i).Equals(
                    nombre,
                    StringComparison.OrdinalIgnoreCase))
                continue;

            return reader.IsDBNull(i)
                ? null
                : reader.GetValue(i).ToString();
        }

        return null;
    }
}