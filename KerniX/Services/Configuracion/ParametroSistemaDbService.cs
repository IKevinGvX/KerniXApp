using System.Data;
using KerniX.Api.Models.Configuracion.ParametroSistema;
using Microsoft.Data.SqlClient;

namespace KerniX.Api.Services.Configuracion;

public sealed class ParametroSistemaDbService
    : IParametroSistemaDbService
{
    private readonly string _connectionString;

    public ParametroSistemaDbService(
        IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("KerniXDb")
            ?? throw new InvalidOperationException(
                "No se encontró la cadena de conexión 'KerniXDb'.");
    }

    public async Task<IReadOnlyList<ParametroSistemaResponse>> ListarAsync(
        int idEmpresa)
    {
        var resultado =
            new List<ParametroSistemaResponse>();

        await using var connection =
            new SqlConnection(_connectionString);

        await using var command =
            new SqlCommand(
                "cfg.sp_ParametroSistema_Listar",
                connection);

        command.CommandType =
            CommandType.StoredProcedure;

        command.Parameters.Add(
            new SqlParameter(
                "@IdEmpresa",
                SqlDbType.Int)
            {
                Value = idEmpresa
            });

        await connection.OpenAsync();

        await using var reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            resultado.Add(
                Mapear(reader));
        }

        return resultado;
    }

    public async Task<ParametroSistemaResponse?> ObtenerPorIdAsync(
        int idParametro)
    {
        await using var connection =
            new SqlConnection(_connectionString);

        await using var command =
            new SqlCommand(
                "cfg.sp_ParametroSistema_ObtenerPorId",
                connection);

        command.CommandType =
            CommandType.StoredProcedure;

        command.Parameters.Add(
            new SqlParameter(
                "@IdParametro",
                SqlDbType.Int)
            {
                Value = idParametro
            });

        await connection.OpenAsync();

        await using var reader =
            await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        return Mapear(reader);
    }

    public async Task<ParametroSistemaResponse?> ObtenerPorCodigoAsync(
        int idEmpresa,
        string codigo)
    {
        await using var connection =
            new SqlConnection(_connectionString);

        await using var command =
            new SqlCommand(
                "cfg.sp_ParametroSistema_ObtenerPorCodigo",
                connection);

        command.CommandType =
            CommandType.StoredProcedure;

        command.Parameters.Add(
            new SqlParameter(
                "@IdEmpresa",
                SqlDbType.Int)
            {
                Value = idEmpresa
            });

        command.Parameters.Add(
            new SqlParameter(
                "@Codigo",
                SqlDbType.NVarChar,
                200)
            {
                Value = codigo
            });

        await connection.OpenAsync();

        await using var reader =
            await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        return Mapear(reader);
    }

    public async Task<ParametroSistemaResponse> CrearAsync(
        ParametroSistemaCrearRequest request)
    {
        await using var connection =
            new SqlConnection(_connectionString);

        await using var command =
            new SqlCommand(
                "cfg.sp_ParametroSistema_Crear",
                connection);

        command.CommandType =
            CommandType.StoredProcedure;

        command.Parameters.Add(
            new SqlParameter(
                "@IdEmpresa",
                SqlDbType.Int)
            {
                Value = request.IdEmpresa
            });

        command.Parameters.Add(
            new SqlParameter(
                "@Codigo",
                SqlDbType.NVarChar,
                200)
            {
                Value = request.Codigo
            });

        command.Parameters.Add(
            new SqlParameter(
                "@Nombre",
                SqlDbType.NVarChar,
                300)
            {
                Value = request.Nombre
            });

        command.Parameters.Add(
            new SqlParameter(
                "@Valor",
                SqlDbType.NVarChar,
                -1)
            {
                Value = (object?)request.Valor
                    ?? DBNull.Value
            });

        command.Parameters.Add(
            new SqlParameter(
                "@TipoDato",
                SqlDbType.NVarChar,
                60)
            {
                Value = request.TipoDato
            });

        command.Parameters.Add(
            new SqlParameter(
                "@Categoria",
                SqlDbType.NVarChar,
                200)
            {
                Value = (object?)request.Categoria
                    ?? DBNull.Value
            });

        command.Parameters.Add(
            new SqlParameter(
                "@Descripcion",
                SqlDbType.NVarChar,
                600)
            {
                Value = (object?)request.Descripcion
                    ?? DBNull.Value
            });

        command.Parameters.Add(
            new SqlParameter(
                "@Editable",
                SqlDbType.Bit)
            {
                Value = request.Editable
            });

        command.Parameters.Add(
            new SqlParameter(
                "@Estado",
                SqlDbType.Bit)
            {
                Value = request.Estado
            });

        await connection.OpenAsync();

        await using var reader =
            await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            throw new InvalidOperationException(
                "El procedimiento no devolvió el parámetro creado.");
        }

        return Mapear(reader);
    }

    public async Task<ParametroSistemaResponse> ActualizarAsync(
        int idParametro,
        ParametroSistemaActualizarRequest request)
    {
        await using var connection =
            new SqlConnection(_connectionString);

        await using var command =
            new SqlCommand(
                "cfg.sp_ParametroSistema_Actualizar",
                connection);

        command.CommandType =
            CommandType.StoredProcedure;

        command.Parameters.Add(
            new SqlParameter(
                "@IdParametro",
                SqlDbType.Int)
            {
                Value = idParametro
            });

        command.Parameters.Add(
            new SqlParameter(
                "@Nombre",
                SqlDbType.NVarChar,
                300)
            {
                Value = request.Nombre
            });

        command.Parameters.Add(
            new SqlParameter(
                "@Valor",
                SqlDbType.NVarChar,
                -1)
            {
                Value = (object?)request.Valor
                    ?? DBNull.Value
            });

        command.Parameters.Add(
            new SqlParameter(
                "@TipoDato",
                SqlDbType.NVarChar,
                60)
            {
                Value = request.TipoDato
            });

        command.Parameters.Add(
            new SqlParameter(
                "@Categoria",
                SqlDbType.NVarChar,
                200)
            {
                Value = (object?)request.Categoria
                    ?? DBNull.Value
            });

        command.Parameters.Add(
            new SqlParameter(
                "@Descripcion",
                SqlDbType.NVarChar,
                600)
            {
                Value = (object?)request.Descripcion
                    ?? DBNull.Value
            });

        command.Parameters.Add(
            new SqlParameter(
                "@Editable",
                SqlDbType.Bit)
            {
                Value = request.Editable
            });

        command.Parameters.Add(
            new SqlParameter(
                "@Estado",
                SqlDbType.Bit)
            {
                Value = request.Estado
            });

        await connection.OpenAsync();

        await using var reader =
            await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            throw new InvalidOperationException(
                "El procedimiento no devolvió el parámetro actualizado.");
        }

        return Mapear(reader);
    }

    public async Task<ParametroSistemaResponse> ActualizarValorAsync(
        ParametroSistemaActualizarValorRequest request)
    {
        await using var connection =
            new SqlConnection(_connectionString);

        await using var command =
            new SqlCommand(
                "cfg.sp_ParametroSistema_ActualizarValor",
                connection);

        command.CommandType =
            CommandType.StoredProcedure;

        command.Parameters.Add(
            new SqlParameter(
                "@IdEmpresa",
                SqlDbType.Int)
            {
                Value = request.IdEmpresa
            });

        command.Parameters.Add(
            new SqlParameter(
                "@Codigo",
                SqlDbType.NVarChar,
                200)
            {
                Value = request.Codigo
            });

        command.Parameters.Add(
            new SqlParameter(
                "@Valor",
                SqlDbType.NVarChar,
                -1)
            {
                Value = (object?)request.Valor
                    ?? DBNull.Value
            });

        await connection.OpenAsync();

        await using var reader =
            await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            throw new InvalidOperationException(
                "El procedimiento no devolvió el parámetro actualizado.");
        }

        return Mapear(reader);
    }

    public async Task<ParametroSistemaResponse> CambiarEstadoAsync(
        int idParametro,
        bool estado)
    {
        await using var connection =
            new SqlConnection(_connectionString);

        await using var command =
            new SqlCommand(
                "cfg.sp_ParametroSistema_CambiarEstado",
                connection);

        command.CommandType =
            CommandType.StoredProcedure;

        command.Parameters.Add(
            new SqlParameter(
                "@IdParametro",
                SqlDbType.Int)
            {
                Value = idParametro
            });

        command.Parameters.Add(
            new SqlParameter(
                "@Estado",
                SqlDbType.Bit)
            {
                Value = estado
            });

        await connection.OpenAsync();

        await using var reader =
            await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            throw new InvalidOperationException(
                "El procedimiento no devolvió el parámetro actualizado.");
        }

        return Mapear(reader);
    }

    private static ParametroSistemaResponse Mapear(
        SqlDataReader reader)
    {
        return new ParametroSistemaResponse
        {
            IdParametro =
                Convert.ToInt32(
                    reader["id_parametro"]),

            IdEmpresa =
                Convert.ToInt32(
                    reader["id_empresa"]),

            Codigo =
                reader["codigo"]?.ToString()
                ?? string.Empty,

            Nombre =
                reader["nombre"]?.ToString()
                ?? string.Empty,

            Valor =
                reader["valor"] == DBNull.Value
                    ? null
                    : reader["valor"].ToString(),

            TipoDato =
                reader["tipo_dato"]?.ToString()
                ?? string.Empty,

            Categoria =
                reader["categoria"] == DBNull.Value
                    ? null
                    : reader["categoria"].ToString(),

            Descripcion =
                reader["descripcion"] == DBNull.Value
                    ? null
                    : reader["descripcion"].ToString(),

            Editable =
                Convert.ToBoolean(
                    reader["editable"]),

            Estado =
                Convert.ToBoolean(
                    reader["estado"])
        };
    }
}