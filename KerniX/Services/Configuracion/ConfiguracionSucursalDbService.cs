using System.Data;
using KerniX.Api.Models.Configuracion.ConfiguracionSucursal;
using Microsoft.Data.SqlClient;

namespace KerniX.Api.Services.Configuracion;

public sealed class ConfiguracionSucursalDbService
    : IConfiguracionSucursalDbService
{
    private readonly string _connectionString;

    public ConfiguracionSucursalDbService(
        IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("KerniXDb")
            ?? throw new InvalidOperationException(
                "No existe la conexión KerniXDb.");
    }

    public async Task<IReadOnlyList<ConfiguracionSucursalResponse>>
        ListarAsync(int idSucursal)
    {
        return await ListarInternoAsync(
            "cfg.sp_ConfiguracionSucursal_Listar",
            idSucursal);
    }

    public async Task<IReadOnlyList<ConfiguracionSucursalResponse>>
        ListarActivasAsync(int idSucursal)
    {
        return await ListarInternoAsync(
            "cfg.sp_ConfiguracionSucursal_ListarActivas",
            idSucursal);
    }

    private async Task<IReadOnlyList<ConfiguracionSucursalResponse>>
        ListarInternoAsync(
            string procedimiento,
            int idSucursal)
    {
        var lista =
            new List<ConfiguracionSucursalResponse>();

        await using var connection =
            new SqlConnection(_connectionString);

        await using var command =
            new SqlCommand(
                procedimiento,
                connection);

        command.CommandType =
            CommandType.StoredProcedure;

        command.Parameters.Add(
            "@IdSucursal",
            SqlDbType.Int)
            .Value = idSucursal;

        await connection.OpenAsync();

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            lista.Add(Mapear(reader));
        }

        return lista;
    }

    public async Task<ConfiguracionSucursalResponse?>
        ObtenerPorIdAsync(
            int idConfiguracionSucursal)
    {
        await using var connection =
            new SqlConnection(_connectionString);

        await using var command =
            new SqlCommand(
                "cfg.sp_ConfiguracionSucursal_ObtenerPorId",
                connection);

        command.CommandType =
            CommandType.StoredProcedure;

        command.Parameters.Add(
            "@IdConfiguracionSucursal",
            SqlDbType.Int)
            .Value = idConfiguracionSucursal;

        await connection.OpenAsync();

        await using var reader =
            await command.ExecuteReaderAsync();

        return await reader.ReadAsync()
            ? Mapear(reader)
            : null;
    }

    public async Task<ConfiguracionSucursalResponse?>
        ObtenerPorCodigoAsync(
            int idSucursal,
            string codigo)
    {
        await using var connection =
            new SqlConnection(_connectionString);

        await using var command =
            new SqlCommand(
                "cfg.sp_ConfiguracionSucursal_ObtenerPorCodigo",
                connection);

        command.CommandType =
            CommandType.StoredProcedure;

        command.Parameters.Add(
            "@IdSucursal",
            SqlDbType.Int)
            .Value = idSucursal;

        command.Parameters.Add(
            "@Codigo",
            SqlDbType.NVarChar,
            200)
            .Value = codigo;

        await connection.OpenAsync();

        await using var reader =
            await command.ExecuteReaderAsync();

        return await reader.ReadAsync()
            ? Mapear(reader)
            : null;
    }

    public async Task<ConfiguracionSucursalResponse>
        CrearAsync(
            ConfiguracionSucursalCrearRequest request)
    {
        return await EjecutarAsync(
            "cfg.sp_ConfiguracionSucursal_Crear",
            command =>
            {
                command.Parameters.Add(
                    "@IdSucursal",
                    SqlDbType.Int)
                    .Value = request.IdSucursal;

                command.Parameters.Add(
                    "@Codigo",
                    SqlDbType.NVarChar,
                    200)
                    .Value = request.Codigo;

                command.Parameters.Add(
                    "@Valor",
                    SqlDbType.NVarChar,
                    -1)
                    .Value =
                    (object?)request.Valor
                    ?? DBNull.Value;

                command.Parameters.Add(
                    "@Descripcion",
                    SqlDbType.NVarChar,
                    600)
                    .Value =
                    (object?)request.Descripcion
                    ?? DBNull.Value;

                command.Parameters.Add(
                    "@Estado",
                    SqlDbType.Bit)
                    .Value = request.Estado;
            });
    }

    public async Task<ConfiguracionSucursalResponse>
        ActualizarAsync(
            int idConfiguracionSucursal,
            ConfiguracionSucursalActualizarRequest request)
    {
        return await EjecutarAsync(
            "cfg.sp_ConfiguracionSucursal_Actualizar",
            command =>
            {
                command.Parameters.Add(
                    "@IdConfiguracionSucursal",
                    SqlDbType.Int)
                    .Value =
                    idConfiguracionSucursal;

                command.Parameters.Add(
                    "@Valor",
                    SqlDbType.NVarChar,
                    -1)
                    .Value =
                    (object?)request.Valor
                    ?? DBNull.Value;

                command.Parameters.Add(
                    "@Descripcion",
                    SqlDbType.NVarChar,
                    600)
                    .Value =
                    (object?)request.Descripcion
                    ?? DBNull.Value;

                command.Parameters.Add(
                    "@Estado",
                    SqlDbType.Bit)
                    .Value = request.Estado;
            });
    }

    public async Task<ConfiguracionSucursalResponse>
        ActualizarValorAsync(
            int idSucursal,
            string codigo,
            string? valor)
    {
        return await EjecutarAsync(
            "cfg.sp_ConfiguracionSucursal_ActualizarValor",
            command =>
            {
                command.Parameters.Add(
                    "@IdSucursal",
                    SqlDbType.Int)
                    .Value = idSucursal;

                command.Parameters.Add(
                    "@Codigo",
                    SqlDbType.NVarChar,
                    200)
                    .Value = codigo;

                command.Parameters.Add(
                    "@Valor",
                    SqlDbType.NVarChar,
                    -1)
                    .Value =
                    (object?)valor
                    ?? DBNull.Value;
            });
    }

    public async Task<ConfiguracionSucursalResponse>
        CambiarEstadoAsync(
            int idConfiguracionSucursal,
            bool estado)
    {
        return await EjecutarAsync(
            "cfg.sp_ConfiguracionSucursal_CambiarEstado",
            command =>
            {
                command.Parameters.Add(
                    "@IdConfiguracionSucursal",
                    SqlDbType.Int)
                    .Value =
                    idConfiguracionSucursal;

                command.Parameters.Add(
                    "@Estado",
                    SqlDbType.Bit)
                    .Value = estado;
            });
    }

    public async Task<ConfiguracionSucursalResponse>
        GuardarAsync(
            ConfiguracionSucursalGuardarRequest request)
    {
        return await EjecutarAsync(
            "cfg.sp_ConfiguracionSucursal_Guardar",
            command =>
            {
                command.Parameters.Add(
                    "@IdSucursal",
                    SqlDbType.Int)
                    .Value =
                    request.IdSucursal;

                command.Parameters.Add(
                    "@Codigo",
                    SqlDbType.NVarChar,
                    200)
                    .Value =
                    request.Codigo;

                command.Parameters.Add(
                    "@Valor",
                    SqlDbType.NVarChar,
                    -1)
                    .Value =
                    (object?)request.Valor
                    ?? DBNull.Value;

                command.Parameters.Add(
                    "@Descripcion",
                    SqlDbType.NVarChar,
                    600)
                    .Value =
                    (object?)request.Descripcion
                    ?? DBNull.Value;
            });
    }

    private async Task<ConfiguracionSucursalResponse>
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

        configurar(command);

        await connection.OpenAsync();

        await using var reader =
            await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            throw new InvalidOperationException(
                "El procedimiento no devolvió datos.");
        }

        return Mapear(reader);
    }

    private static ConfiguracionSucursalResponse Mapear(
        SqlDataReader reader)
    {
        return new ConfiguracionSucursalResponse
        {
            IdConfiguracionSucursal =
                Convert.ToInt32(
                    reader["id_configuracion_sucursal"]),

            IdSucursal =
                Convert.ToInt32(
                    reader["id_sucursal"]),

            Codigo =
                reader["codigo"]?.ToString()
                ?? string.Empty,

            Valor =
                reader["valor"] == DBNull.Value
                    ? null
                    : reader["valor"].ToString(),

            Descripcion =
                reader["descripcion"] == DBNull.Value
                    ? null
                    : reader["descripcion"].ToString(),

            Estado =
                Convert.ToBoolean(
                    reader["estado"])
        };
    }
}