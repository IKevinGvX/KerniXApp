using System.Data;
using KerniX.Models.Auth;
using Microsoft.Data.SqlClient;

namespace KerniX.Services.Empleados;

public sealed class EmpleadoDbService
    : IEmpleadoDbService
{
    private readonly string _connectionString;
    private readonly ILogger<EmpleadoDbService> _logger;

    public EmpleadoDbService(
        IConfiguration configuration,
        ILogger<EmpleadoDbService> logger)
    {
        _connectionString =
            configuration.GetConnectionString("KerniXDb")
            ?? throw new InvalidOperationException(
                "No existe ConnectionStrings:KerniXDb en appsettings.json.");

        _logger = logger;
    }

    public async Task<UsuarioEmpleadoDb?> ObtenerPorUsuarioAsync(
        string usuario,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(usuario))
        {
            return null;
        }

        try
        {
            await using var connection =
                new SqlConnection(_connectionString);

            await connection.OpenAsync(
                cancellationToken);

            await using var command =
                new SqlCommand(
                    "dbo.sp_Auth_ObtenerUsuarioEmpleado",
                    connection);

            command.CommandType =
                CommandType.StoredProcedure;

            command.Parameters.Add(
                new SqlParameter(
                    "@nombre_usuario",
                    SqlDbType.NVarChar,
                    100)
                {
                    Value = usuario.Trim()
                });

            _logger.LogInformation(
                "Ejecutando sp_Auth_ObtenerUsuarioEmpleado para {Usuario}.",
                usuario);

            await using var reader =
                await command.ExecuteReaderAsync(
                    CommandBehavior.SingleRow,
                    cancellationToken);

            if (!await reader.ReadAsync(
                    cancellationToken))
            {
                _logger.LogWarning(
                    "No se encontró Usuario/Empleado para {Usuario}.",
                    usuario);

                return null;
            }

            var resultado =
                new UsuarioEmpleadoDb
                {
                    IdUsuario =
                        GetInt32(
                            reader,
                            "IdUsuario"),

                    IdEmpleado =
                        GetNullableInt32(
                            reader,
                            "IdEmpleado"),

                    Usuario =
                        GetString(
                            reader,
                            "Usuario")
                        ?? usuario,

                    Nombre =
                        GetString(
                            reader,
                            "Nombre"),

                    ApellidoPaterno =
                        GetString(
                            reader,
                            "ApellidoPaterno"),

                    ApellidoMaterno =
                        GetString(
                            reader,
                            "ApellidoMaterno"),

                    Correo =
                        GetString(
                            reader,
                            "Correo"),

                    Telefono =
                        GetString(
                            reader,
                            "Telefono"),

                    Cargo =
                        GetString(
                            reader,
                            "Cargo"),

                    TipoDocumento =
                        GetString(
                            reader,
                            "TipoDocumento"),

                    NumeroDocumento =
                        GetString(
                            reader,
                            "NumeroDocumento"),

                    IdEmpresa =
                        GetNullableInt32(
                            reader,
                            "IdEmpresa"),

                    IdSucursal =
                        GetNullableInt32(
                            reader,
                            "IdSucursal"),

                    FechaIngreso =
                        GetNullableDateTime(
                            reader,
                            "FechaIngreso")
                };

            _logger.LogInformation(
                "Usuario/Empleado encontrado. IdUsuario {IdUsuario}, IdEmpleado {IdEmpleado}, Nombre {NombreCompleto}.",
                resultado.IdUsuario,
                resultado.IdEmpleado,
                resultado.NombreCompleto);

            return resultado;
        }
        catch (SqlException ex)
        {
            _logger.LogError(
                ex,
                "Error SQL consultando Usuario/Empleado. SQL {Numero}: {Mensaje}",
                ex.Number,
                ex.Message);

            throw;
        }
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private static string? GetString(
        SqlDataReader reader,
        string column)
    {
        var ordinal =
            reader.GetOrdinal(column);

        if (reader.IsDBNull(ordinal))
        {
            return null;
        }

        return reader.GetString(ordinal);
    }

    private static int GetInt32(
        SqlDataReader reader,
        string column)
    {
        var ordinal =
            reader.GetOrdinal(column);

        return reader.GetInt32(ordinal);
    }

    private static int? GetNullableInt32(
        SqlDataReader reader,
        string column)
    {
        var ordinal =
            reader.GetOrdinal(column);

        if (reader.IsDBNull(ordinal))
        {
            return null;
        }

        return reader.GetInt32(ordinal);
    }

    private static DateTime? GetNullableDateTime(
        SqlDataReader reader,
        string column)
    {
        var ordinal =
            reader.GetOrdinal(column);

        if (reader.IsDBNull(ordinal))
        {
            return null;
        }

        return reader.GetDateTime(ordinal);
    }
}