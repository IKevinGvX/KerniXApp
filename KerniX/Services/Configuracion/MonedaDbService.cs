using System.Data;
using KerniX.Api.Models.Configuracion.Moneda;
using Microsoft.Data.SqlClient;

namespace KerniX.Api.Services.Configuracion;

public sealed class MonedaDbService
    : IMonedaDbService
{
    private readonly string _connectionString;

    public MonedaDbService(
        IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("KerniXDb")
            ?? throw new InvalidOperationException(
                "No existe KerniXDb.");
    }

    public Task<IReadOnlyList<MonedaResponse>>
        ListarAsync()
    {
        return ListarInternoAsync(
            "cfg.sp_Moneda_Listar");
    }

    public Task<IReadOnlyList<MonedaResponse>>
        ListarActivasAsync()
    {
        return ListarInternoAsync(
            "cfg.sp_Moneda_ListarActivas");
    }

    private async Task<IReadOnlyList<MonedaResponse>>
        ListarInternoAsync(
            string procedimiento)
    {
        var lista =
            new List<MonedaResponse>();

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

    public Task<MonedaResponse?>
        ObtenerPorIdAsync(int idMoneda)
    {
        return ObtenerAsync(
            "cfg.sp_Moneda_ObtenerPorId",
            command =>
            {
                command.Parameters.Add(
                    "@IdMoneda",
                    SqlDbType.Int)
                    .Value = idMoneda;
            });
    }

    public Task<MonedaResponse?>
        ObtenerPorCodigoAsync(string codigo)
    {
        return ObtenerAsync(
            "cfg.sp_Moneda_ObtenerPorCodigo",
            command =>
            {
                command.Parameters.Add(
                    "@Codigo",
                    SqlDbType.NVarChar,
                    20)
                    .Value = codigo;
            });
    }

    public Task<MonedaResponse>
        CrearAsync(
            MonedaCrearRequest request)
    {
        return EjecutarAsync(
            "cfg.sp_Moneda_Crear",
            command =>
            {
                command.Parameters.Add(
                    "@Codigo",
                    SqlDbType.NVarChar,
                    20)
                    .Value = request.Codigo;

                command.Parameters.Add(
                    "@Nombre",
                    SqlDbType.NVarChar,
                    200)
                    .Value = request.Nombre;

                command.Parameters.Add(
                    "@Simbolo",
                    SqlDbType.NVarChar,
                    20)
                    .Value =
                    (object?)request.Simbolo
                    ?? DBNull.Value;

                command.Parameters.Add(
                    "@CantidadDecimales",
                    SqlDbType.Int)
                    .Value =
                    request.CantidadDecimales;

                command.Parameters.Add(
                    "@Estado",
                    SqlDbType.Bit)
                    .Value = request.Estado;
            });
    }

    public Task<MonedaResponse>
        ActualizarAsync(
            int idMoneda,
            MonedaActualizarRequest request)
    {
        return EjecutarAsync(
            "cfg.sp_Moneda_Actualizar",
            command =>
            {
                command.Parameters.Add(
                    "@IdMoneda",
                    SqlDbType.Int)
                    .Value = idMoneda;

                command.Parameters.Add(
                    "@Codigo",
                    SqlDbType.NVarChar,
                    20)
                    .Value = request.Codigo;

                command.Parameters.Add(
                    "@Nombre",
                    SqlDbType.NVarChar,
                    200)
                    .Value = request.Nombre;

                command.Parameters.Add(
                    "@Simbolo",
                    SqlDbType.NVarChar,
                    20)
                    .Value =
                    (object?)request.Simbolo
                    ?? DBNull.Value;

                command.Parameters.Add(
                    "@CantidadDecimales",
                    SqlDbType.Int)
                    .Value =
                    request.CantidadDecimales;

                command.Parameters.Add(
                    "@Estado",
                    SqlDbType.Bit)
                    .Value =
                    request.Estado;
            });
    }

    public Task<MonedaResponse>
        CambiarEstadoAsync(
            int idMoneda,
            bool estado)
    {
        return EjecutarAsync(
            "cfg.sp_Moneda_CambiarEstado",
            command =>
            {
                command.Parameters.Add(
                    "@IdMoneda",
                    SqlDbType.Int)
                    .Value = idMoneda;

                command.Parameters.Add(
                    "@Estado",
                    SqlDbType.Bit)
                    .Value = estado;
            });
    }

    public async Task<bool>
        TieneDependenciasAsync(
            int idMoneda)
    {
        await using var connection =
            new SqlConnection(_connectionString);

        await using var command =
            new SqlCommand(
                "cfg.sp_Moneda_TieneDependencias",
                connection);

        command.CommandType =
            CommandType.StoredProcedure;

        command.Parameters.Add(
            "@IdMoneda",
            SqlDbType.Int)
            .Value = idMoneda;

        await connection.OpenAsync();

        var result =
            await command.ExecuteScalarAsync();

        return result != null
            && result != DBNull.Value
            && Convert.ToBoolean(result);
    }

    private async Task<MonedaResponse?>
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

    private async Task<MonedaResponse>
        EjecutarAsync(
            string procedimiento,
            Action<SqlCommand> configurar)
    {
        var result =
            await ObtenerAsync(
                procedimiento,
                configurar);

        return result
            ?? throw new InvalidOperationException(
                "El procedimiento no devolvió datos.");
    }

    private static MonedaResponse Mapear(
        SqlDataReader reader)
    {
        return new MonedaResponse
        {
            IdMoneda =
                Convert.ToInt32(
                    reader["id_moneda"]),

            Codigo =
                reader["codigo"]?.ToString()
                ?? string.Empty,

            Nombre =
                reader["nombre"]?.ToString()
                ?? string.Empty,

            Simbolo =
                reader["simbolo"] == DBNull.Value
                    ? null
                    : reader["simbolo"].ToString(),

            CantidadDecimales =
                Convert.ToInt32(
                    reader["cantidad_decimales"]),

            Estado =
                Convert.ToBoolean(
                    reader["estado"])
        };
    }
}