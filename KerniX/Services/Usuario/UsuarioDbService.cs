using System.Data;
using KerniX.Models.Usuarios;
using Microsoft.Data.SqlClient;

namespace KerniX.Services.Usuarios;

public sealed class UsuarioDbService
    : IUsuarioDbService
{
    private readonly IConfiguration
        _configuration;


    public UsuarioDbService(
        IConfiguration configuration)
    {
        _configuration =
            configuration;
    }
    public async Task ActualizarPerfilAsync(
        int idUsuario,
        string nombreUsuario,
        string correo)
    {
        string? connectionString =
            _configuration.GetConnectionString(
                "KerniXDb");

        if (string.IsNullOrWhiteSpace(
            connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:KerniXDb no está configurado.");
        }

        Console.WriteLine(
            "CONEXION PERFIL = " + connectionString);

        await using SqlConnection connection =
            new(connectionString);

        await using SqlCommand command =
            new(
                "dbo.usp_Usuario_ActualizarPerfil",
                connection);

        command.CommandType =
            CommandType.StoredProcedure;

        command.Parameters.Add(
            "@IdUsuario",
            SqlDbType.Int)
            .Value =
                idUsuario;

        command.Parameters.Add(
            "@NombreUsuario",
            SqlDbType.NVarChar,
            100)
            .Value =
                nombreUsuario;

        command.Parameters.Add(
            "@Correo",
            SqlDbType.NVarChar,
            200)
            .Value =
                correo;

        await connection.OpenAsync();

        Console.WriteLine(
            "BASE ACTUAL = " +
            connection.Database);

        int resultado =
            await command.ExecuteNonQueryAsync();

        Console.WriteLine(
            "UPDATE PERFIL EJECUTADO. RESULTADO = " +
            resultado);
    }

    public async Task<UsuarioPerfilResponse?>
        ObtenerPerfilAsync(
            int idUsuario)
    {
        string? connectionString =
            _configuration
                .GetConnectionString(
                    "KerniXDb");


        if (string.IsNullOrWhiteSpace(
            connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:KerniXDb no está configurado.");
        }


        await using SqlConnection connection =
            new(connectionString);


        await using SqlCommand command =
            new(
                "dbo.usp_Usuario_ObtenerPerfil",
                connection);


        command.CommandType =
            CommandType.StoredProcedure;


        command.Parameters.Add(
            "@IdUsuario",
            SqlDbType.Int)
            .Value =
                idUsuario;


        await connection.OpenAsync();


        await using SqlDataReader reader =
            await command.ExecuteReaderAsync();


        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new UsuarioPerfilResponse
        {
            IdUsuario =
                Convert.ToInt32(
                    reader["IdUsuario"]),

            IdEmpleado =
                reader["IdEmpleado"] == DBNull.Value
                    ? 0
                    : Convert.ToInt32(
                        reader["IdEmpleado"]),

            NombreUsuario =
                reader["NombreUsuario"]?.ToString()
                ?? string.Empty,

            Correo =
                reader["Correo"]?.ToString()
                ?? string.Empty,

            Empleado =
                reader["Empleado"]?.ToString()
                ?? string.Empty,

            Empresa =
                reader["Empresa"]?.ToString()
                ?? string.Empty,

            Sucursal =
                reader["Sucursal"]?.ToString()
                ?? string.Empty,

            Estado =
                Convert.ToBoolean(
                    reader["Estado"]),

            UltimoAcceso =
                reader["UltimoAcceso"] == DBNull.Value
                    ? null
                    : Convert.ToDateTime(
                        reader["UltimoAcceso"]),

            Roles =
                string.IsNullOrWhiteSpace(
                    reader["Roles"]?.ToString())
                    ? new List<string>()
                    : reader["Roles"]!
                        .ToString()!
                        .Split(
                            ',',
                            StringSplitOptions.RemoveEmptyEntries |
                            StringSplitOptions.TrimEntries)
                        .ToList()
        };  
    }
}