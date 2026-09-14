using System.Data;
using KerniX.Api.Models.Configuracion.Mantenimiento;
using Microsoft.Data.SqlClient;

namespace KerniX.Api.Services.Configuracion;

public sealed class AlertaMantenimientoDbService
    : IAlertaMantenimientoDbService
{
    private readonly string _connectionString;

    public AlertaMantenimientoDbService(
        IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("KerniXDb")
            ?? throw new InvalidOperationException(
                "No existe KerniXDb.");
    }

    public Task<IReadOnlyList<AlertaMantenimientoResponse>>
        ListarAsync()
    {
        return ListarInternoAsync(
            "cfg.sp_AlertaMantenimiento_Listar");
    }

    public Task<IReadOnlyList<AlertaMantenimientoResponse>>
        ListarPendientesAsync()
    {
        return ListarInternoAsync(
            "cfg.sp_AlertaMantenimiento_ListarPendientes");
    }

    public Task<AlertaMantenimientoResponse?>
        ObtenerPorIdAsync(
            long idAlertaMantenimiento)
    {
        return ObtenerAsync(
            "cfg.sp_AlertaMantenimiento_ObtenerPorId",
            command =>
            {
                command.Parameters.Add(
                    "@IdAlertaMantenimiento",
                    SqlDbType.BigInt)
                    .Value =
                    idAlertaMantenimiento;
            });
    }

    public Task<AlertaMantenimientoResponse>
        CrearAsync(
            AlertaMantenimientoCrearRequest request)
    {
        return EjecutarAsync(
            "cfg.sp_AlertaMantenimiento_Crear",
            command =>
            {
                command.Parameters.Add(
                    "@IdEjecucionMantenimiento",
                    SqlDbType.BigInt)
                    .Value =
                    (object?)request.IdEjecucionMantenimiento
                    ?? DBNull.Value;

                command.Parameters.Add(
                    "@TipoAlerta",
                    SqlDbType.NVarChar,
                    100)
                    .Value =
                    request.TipoAlerta;

                command.Parameters.Add(
                    "@Severidad",
                    SqlDbType.NVarChar,
                    60)
                    .Value =
                    request.Severidad;

                command.Parameters.Add(
                    "@Titulo",
                    SqlDbType.NVarChar,
                    400)
                    .Value =
                    request.Titulo;

                command.Parameters.Add(
                    "@Descripcion",
                    SqlDbType.NVarChar,
                    -1)
                    .Value =
                    (object?)request.Descripcion
                    ?? DBNull.Value;
            });
    }

    public Task<AlertaMantenimientoResponse>
        AtenderAsync(
            long idAlertaMantenimiento)
    {
        return EjecutarAsync(
            "cfg.sp_AlertaMantenimiento_Atender",
            command =>
            {
                command.Parameters.Add(
                    "@IdAlertaMantenimiento",
                    SqlDbType.BigInt)
                    .Value =
                    idAlertaMantenimiento;
            });
    }

    public Task<AlertaMantenimientoResponse>
        DescartarAsync(
            long idAlertaMantenimiento)
    {
        return EjecutarAsync(
            "cfg.sp_AlertaMantenimiento_Descartar",
            command =>
            {
                command.Parameters.Add(
                    "@IdAlertaMantenimiento",
                    SqlDbType.BigInt)
                    .Value =
                    idAlertaMantenimiento;
            });
    }

    public Task<AlertaMantenimientoResponse>
        CrearDesdeErrorAsync(
            long idEjecucionMantenimiento)
    {
        return EjecutarAsync(
            "cfg.sp_AlertaMantenimiento_CrearDesdeError",
            command =>
            {
                command.Parameters.Add(
                    "@IdEjecucionMantenimiento",
                    SqlDbType.BigInt)
                    .Value =
                    idEjecucionMantenimiento;
            });
    }

    private async Task<IReadOnlyList<AlertaMantenimientoResponse>>
        ListarInternoAsync(
            string procedimiento)
    {
        var lista =
            new List<AlertaMantenimientoResponse>();

        await using var connection =
            new SqlConnection(_connectionString);

        await using var command =
            new SqlCommand(
                procedimiento,
                connection);

        command.CommandType =
            CommandType.StoredProcedure;

        await connection.OpenAsync();

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            lista.Add(Mapear(reader));
        }

        return lista;
    }

    private async Task<AlertaMantenimientoResponse?>
        ObtenerAsync(
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

        configurar(command);

        await connection.OpenAsync();

        await using var reader =
            await command.ExecuteReaderAsync();

        return await reader.ReadAsync()
            ? Mapear(reader)
            : null;
    }

    private async Task<AlertaMantenimientoResponse>
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

    private static AlertaMantenimientoResponse Mapear(
        SqlDataReader reader)
    {
        return new AlertaMantenimientoResponse
        {
            IdAlertaMantenimiento =
                Convert.ToInt64(
                    reader["id_alerta_mantenimiento"]),

            IdEjecucionMantenimiento =
                reader["id_ejecucion_mantenimiento"] == DBNull.Value
                    ? null
                    : Convert.ToInt64(
                        reader["id_ejecucion_mantenimiento"]),

            TipoAlerta =
                reader["tipo_alerta"]?.ToString()
                ?? string.Empty,

            Severidad =
                reader["severidad"]?.ToString()
                ?? string.Empty,

            Titulo =
                reader["titulo"]?.ToString()
                ?? string.Empty,

            Descripcion =
                reader["descripcion"] == DBNull.Value
                    ? null
                    : reader["descripcion"].ToString(),

            Estado =
                reader["estado"]?.ToString()
                ?? string.Empty,

            FechaGeneracion =
                Convert.ToDateTime(
                    reader["fecha_generacion"]),

            FechaAtencion =
                reader["fecha_atencion"] == DBNull.Value
                    ? null
                    : Convert.ToDateTime(
                        reader["fecha_atencion"])
        };
    }
}