using System.Data;
using KerniX.Api.Models.Configuracion.Mantenimiento;
using Microsoft.Data.SqlClient;

namespace KerniX.Api.Services.Configuracion;

public sealed class MantenimientoDbService
    : IMantenimientoDbService
{
    private readonly string _connectionString;

    public MantenimientoDbService(
        IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("KerniXDb")
            ?? throw new InvalidOperationException(
                "No existe KerniXDb.");
    }

    public Task<MantenimientoResultadoResponse>
        EjecutarPlanAsync(
            int idPlanMantenimiento,
            MantenimientoEjecutarRequest request)
    {
        return EjecutarAsync(
            "cfg.sp_Mantenimiento_EjecutarPlan",
            command =>
            {
                command.Parameters.Add(
                    "@IdPlanMantenimiento",
                    SqlDbType.Int)
                    .Value =
                    idPlanMantenimiento;

                AgregarParametrosEjecucion(
                    command,
                    request);
            });
    }

    public Task<MantenimientoResultadoResponse>
        EjecutarPorCodigoAsync(
            string codigo,
            MantenimientoEjecutarRequest request)
    {
        return EjecutarAsync(
            "cfg.sp_Mantenimiento_EjecutarPorCodigo",
            command =>
            {
                command.Parameters.Add(
                    "@Codigo",
                    SqlDbType.NVarChar,
                    200)
                    .Value =
                    codigo;

                AgregarParametrosEjecucion(
                    command,
                    request);
            });
    }

    public async Task<EjecucionMantenimientoResponse?>
        ObtenerResultadoAsync(
            long idEjecucionMantenimiento)
    {
        await using var connection =
            new SqlConnection(_connectionString);

        await using var command =
            new SqlCommand(
                "cfg.sp_Mantenimiento_ObtenerResultadoEjecucion",
                connection);

        command.CommandType =
            CommandType.StoredProcedure;

        command.Parameters.Add(
            "@IdEjecucionMantenimiento",
            SqlDbType.BigInt)
            .Value =
            idEjecucionMantenimiento;

        await connection.OpenAsync();

        await using var reader =
            await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
            return null;

        return new EjecucionMantenimientoResponse
        {
            IdEjecucionMantenimiento =
                Convert.ToInt64(
                    reader["id_ejecucion_mantenimiento"]),

            IdPlanMantenimiento =
                Convert.ToInt32(
                    reader["id_plan_mantenimiento"]),

            CodigoPlan =
                reader["codigo_plan"]?.ToString(),

            NombrePlan =
                reader["nombre_plan"]?.ToString(),

            TipoTarea =
                reader["tipo_tarea"]?.ToString(),

            OrigenEjecucion =
                reader["origen_ejecucion"]?.ToString()
                ?? string.Empty,

            IdEjecucionExterna =
                reader["id_ejecucion_externa"] == DBNull.Value
                    ? null
                    : reader["id_ejecucion_externa"].ToString(),

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

    private async Task<MantenimientoResultadoResponse>
        EjecutarAsync(
            string procedimiento,
            Action<SqlCommand> configurar)
    {
        await using var connection =
            new SqlConnection(_connectionString);

        await using var command =
            new SqlCommand(
                procedimiento,
                connection);

        command.CommandType =
            CommandType.StoredProcedure;

        // Mantenimiento puede tardar bastante.
        command.CommandTimeout = 300;

        configurar(command);

        await connection.OpenAsync();

        await using var reader =
            await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            throw new InvalidOperationException(
                "El mantenimiento no devolvió resultado.");
        }

        return new MantenimientoResultadoResponse
        {
            IdEjecucionMantenimiento =
                Convert.ToInt64(
                    reader["id_ejecucion_mantenimiento"]),

            IdPlanMantenimiento =
                Convert.ToInt32(
                    reader["id_plan_mantenimiento"]),

            CodigoPlan =
                reader["codigo_plan"]?.ToString()
                ?? string.Empty,

            NombrePlan =
                reader["nombre_plan"]?.ToString()
                ?? string.Empty,

            Estado =
                reader["estado"]?.ToString()
                ?? string.Empty,

            Resultado =
                reader["resultado"] == DBNull.Value
                    ? null
                    : reader["resultado"].ToString()
        };
    }

    private static void AgregarParametrosEjecucion(
        SqlCommand command,
        MantenimientoEjecutarRequest request)
    {
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

        command.Parameters.Add(
            "@RutaBackup",
            SqlDbType.NVarChar,
            2000)
            .Value =
            (object?)request.RutaBackup
            ?? DBNull.Value;
    }
}