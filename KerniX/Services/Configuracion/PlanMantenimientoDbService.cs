using KerniX.Api.Models.Configuracion.Mantenimiento;
using KerniX.Models.Configuracion.Mantenimiento;
using Microsoft.Data.SqlClient;
using System.Data;

namespace KerniX.Api.Services.Configuracion;

public sealed class PlanMantenimientoDbService
    : IPlanMantenimientoDbService
{
    private readonly string _connectionString;

    public PlanMantenimientoDbService(
        IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("KerniXDb")
            ?? throw new InvalidOperationException(
                "No existe la cadena de conexión KerniXDb.");
    }

    public Task<IReadOnlyList<PlanMantenimientoResponse>>
        ListarAsync()
    {
        return ListarInternoAsync(
            "cfg.sp_PlanMantenimiento_Listar");
    }

    public Task<IReadOnlyList<PlanMantenimientoResponse>>
        ListarActivosAsync()
    {
        return ListarInternoAsync(
            "cfg.sp_PlanMantenimiento_ListarActivos");
    }

    public Task<IReadOnlyList<PlanMantenimientoResponse>>
        ListarManualesAsync()
    {
        return ListarInternoAsync(
            "cfg.sp_PlanMantenimiento_ListarManuales");
    }

    public Task<PlanMantenimientoResponse?>
        ObtenerPorIdAsync(
            int idPlanMantenimiento)
    {
        return ObtenerAsync(
            "cfg.sp_PlanMantenimiento_ObtenerPorId",
            command =>
            {
                command.Parameters.Add(
                    "@IdPlanMantenimiento",
                    SqlDbType.Int)
                    .Value =
                    idPlanMantenimiento;
            });
    }

    public Task<PlanMantenimientoResponse?>
        ObtenerPorCodigoAsync(
            string codigo)
    {
        return ObtenerAsync(
            "cfg.sp_PlanMantenimiento_ObtenerPorCodigo",
            command =>
            {
                command.Parameters.Add(
                    "@Codigo",
                    SqlDbType.NVarChar,
                    200)
                    .Value =
                    codigo;
            });
    }

    public Task<PlanMantenimientoResponse>
        CrearAsync(
            PlanMantenimientoCrearRequest request)
    {
        return EjecutarAsync(
            "cfg.sp_PlanMantenimiento_Crear",
            command =>
            {
                AgregarDatos(
                    command,
                    request.Nombre,
                    request.Descripcion,
                    request.TipoTarea,
                    request.ProcedimientoEjecutar,
                    request.Parametros,
                    request.Frecuencia,
                    request.DiaSemana,
                    request.DiaMes,
                    request.HoraEjecucion,
                    request.PermiteEjecucionManual,
                    request.Estado);

                command.Parameters.Add(
                    "@Codigo",
                    SqlDbType.NVarChar,
                    200)
                    .Value =
                    request.Codigo;
            });
    }

    public Task<PlanMantenimientoResponse>
        ActualizarAsync(
            int idPlanMantenimiento,
            PlanMantenimientoActualizarRequest request)
    {
        return EjecutarAsync(
            "cfg.sp_PlanMantenimiento_Actualizar",
            command =>
            {
                command.Parameters.Add(
                    "@IdPlanMantenimiento",
                    SqlDbType.Int)
                    .Value =
                    idPlanMantenimiento;

                AgregarDatos(
                    command,
                    request.Nombre,
                    request.Descripcion,
                    request.TipoTarea,
                    request.ProcedimientoEjecutar,
                    request.Parametros,
                    request.Frecuencia,
                    request.DiaSemana,
                    request.DiaMes,
                    request.HoraEjecucion,
                    request.PermiteEjecucionManual,
                    request.Estado);
            });
    }

    public Task<PlanMantenimientoResponse>
        CambiarEstadoAsync(
            int idPlanMantenimiento,
            bool estado)
    {
        return EjecutarAsync(
            "cfg.sp_PlanMantenimiento_CambiarEstado",
            command =>
            {
                command.Parameters.Add(
                    "@IdPlanMantenimiento",
                    SqlDbType.Int)
                    .Value =
                    idPlanMantenimiento;

                command.Parameters.Add(
                    "@Estado",
                    SqlDbType.Bit)
                    .Value =
                    estado;
            });
    }

    private async Task<IReadOnlyList<PlanMantenimientoResponse>>
        ListarInternoAsync(
            string procedimiento)
    {
        var lista =
            new List<PlanMantenimientoResponse>();

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

    private async Task<PlanMantenimientoResponse?>
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

    private async Task<PlanMantenimientoResponse>
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

    private static void AgregarDatos(
        SqlCommand command,
        string nombre,
        string? descripcion,
        string tipoTarea,
        string procedimientoEjecutar,
        string? parametros,
        string frecuencia,
        byte? diaSemana,
        byte? diaMes,
        TimeSpan? horaEjecucion,
        bool permiteManual,
        bool estado)
    {
        command.Parameters.Add(
            "@Nombre",
            SqlDbType.NVarChar,
            300)
            .Value = nombre;

        command.Parameters.Add(
            "@Descripcion",
            SqlDbType.NVarChar,
            1000)
            .Value =
            (object?)descripcion
            ?? DBNull.Value;

        command.Parameters.Add(
            "@TipoTarea",
            SqlDbType.NVarChar,
            100)
            .Value = tipoTarea;

        command.Parameters.Add(
            "@ProcedimientoEjecutar",
            SqlDbType.NVarChar,
            600)
            .Value = procedimientoEjecutar;

        command.Parameters.Add(
            "@Parametros",
            SqlDbType.NVarChar,
            -1)
            .Value =
            (object?)parametros
            ?? DBNull.Value;

        command.Parameters.Add(
            "@Frecuencia",
            SqlDbType.NVarChar,
            60)
            .Value = frecuencia;

        command.Parameters.Add(
            "@DiaSemana",
            SqlDbType.TinyInt)
            .Value =
            (object?)diaSemana
            ?? DBNull.Value;

        command.Parameters.Add(
            "@DiaMes",
            SqlDbType.TinyInt)
            .Value =
            (object?)diaMes
            ?? DBNull.Value;

        command.Parameters.Add(
            "@HoraEjecucion",
            SqlDbType.Time)
            .Value =
            (object?)horaEjecucion
            ?? DBNull.Value;

        command.Parameters.Add(
            "@PermiteEjecucionManual",
            SqlDbType.Bit)
            .Value =
            permiteManual;

        command.Parameters.Add(
            "@Estado",
            SqlDbType.Bit)
            .Value =
            estado;
    }

    private static PlanMantenimientoResponse Mapear(
        SqlDataReader reader)
    {
        return new PlanMantenimientoResponse
        {
            IdPlanMantenimiento =
                Convert.ToInt32(
                    reader["id_plan_mantenimiento"]),

            Codigo =
                reader["codigo"]?.ToString()
                ?? string.Empty,

            Nombre =
                reader["nombre"]?.ToString()
                ?? string.Empty,

            Descripcion =
                reader["descripcion"] == DBNull.Value
                    ? null
                    : reader["descripcion"].ToString(),

            TipoTarea =
                reader["tipo_tarea"]?.ToString()
                ?? string.Empty,

            ProcedimientoEjecutar =
                reader["procedimiento_ejecutar"]?.ToString()
                ?? string.Empty,

            Parametros =
                reader["parametros"] == DBNull.Value
                    ? null
                    : reader["parametros"].ToString(),

            Frecuencia =
                reader["frecuencia"]?.ToString()
                ?? string.Empty,

            DiaSemana =
                reader["dia_semana"] == DBNull.Value
                    ? null
                    : Convert.ToByte(
                        reader["dia_semana"]),

            DiaMes =
                reader["dia_mes"] == DBNull.Value
                    ? null
                    : Convert.ToByte(
                        reader["dia_mes"]),

            HoraEjecucion =
                reader["hora_ejecucion"] == DBNull.Value
                    ? null
                    : (TimeSpan)reader["hora_ejecucion"],

            PermiteEjecucionManual =
                Convert.ToBoolean(
                    reader["permite_ejecucion_manual"]),

            Estado =
                Convert.ToBoolean(
                    reader["estado"]),

            FechaCreacion =
                Convert.ToDateTime(
                    reader["fecha_creacion"]),

            FechaActualizacion =
                reader["fecha_actualizacion"] == DBNull.Value
                    ? null
                    : Convert.ToDateTime(
                        reader["fecha_actualizacion"])
        };
    }
}