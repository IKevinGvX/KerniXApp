using System.Data;
using KerniX.Models.Ventas.Cliente;
using Microsoft.Data.SqlClient;

namespace KerniX.Services.Ventas;

public sealed class VentaClienteDbService
    : IVentaClienteDbService
{
    private readonly string _connectionString;
    private readonly ILogger<VentaClienteDbService> _logger;

    public VentaClienteDbService(
        IConfiguration configuration,
        ILogger<VentaClienteDbService> logger)
    {
        _connectionString =
            configuration.GetConnectionString(
                "KerniXDb")
            ?? throw new InvalidOperationException(
                "No se encontró la conexión KerniXDb.");

        _logger = logger;
    }

    // =========================================================
    // BUSCAR CLIENTES
    // =========================================================

    public async Task<
        IReadOnlyList<VentaClienteBuscarResponse>>
        BuscarAsync(
            int idEmpresa,
            string? termino,
            int limite = 20)
    {
        if (idEmpresa <= 0)
        {
            throw new ArgumentException(
                "La empresa no es válida.",
                nameof(idEmpresa));
        }

        if (limite <= 0)
        {
            limite = 20;
        }

        if (limite > 100)
        {
            limite = 100;
        }

        termino =
            termino?.Trim();

        var clientes =
            new List<VentaClienteBuscarResponse>();

        const string sql = """
            SELECT TOP (@limite)
                c.id_cliente,
                c.numero_documento,

                COALESCE(
                    NULLIF(
                        LTRIM(RTRIM(c.razon_social)),
                        ''
                    ),
                    NULLIF(
                        LTRIM(
                            RTRIM(
                                CONCAT(
                                    ISNULL(c.nombres, ''),
                                    ' ',
                                    ISNULL(c.apellidos, '')
                                )
                            )
                        ),
                        ''
                    ),
                    c.numero_documento
                ) AS nombre,

                c.telefono,
                c.correo,
                c.estado

            FROM ven.cliente c

            WHERE
                c.id_empresa = @id_empresa

                AND c.estado = 1

                AND
                (
                    @termino IS NULL

                    OR @termino = ''

                    OR c.numero_documento
                        LIKE '%' + @termino + '%'

                    OR c.razon_social
                        LIKE '%' + @termino + '%'

                    OR c.nombres
                        LIKE '%' + @termino + '%'

                    OR c.apellidos
                        LIKE '%' + @termino + '%'

                    OR c.telefono
                        LIKE '%' + @termino + '%'

                    OR c.correo
                        LIKE '%' + @termino + '%'
                )

            ORDER BY
                nombre,
                c.numero_documento;
            """;

        await using var connection =
            new SqlConnection(
                _connectionString);

        await connection.OpenAsync();

        await using var command =
            new SqlCommand(
                sql,
                connection);

        command.CommandType =
            CommandType.Text;

        command.Parameters.Add(
            new SqlParameter(
                "@limite",
                SqlDbType.Int)
            {
                Value = limite
            });

        command.Parameters.Add(
            new SqlParameter(
                "@id_empresa",
                SqlDbType.Int)
            {
                Value = idEmpresa
            });

        command.Parameters.Add(
            new SqlParameter(
                "@termino",
                SqlDbType.NVarChar,
                200)
            {
                Value =
                    string.IsNullOrWhiteSpace(
                        termino)
                        ? DBNull.Value
                        : termino
            });

        try
        {
            await using var reader =
                await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                clientes.Add(
                    new VentaClienteBuscarResponse
                    {
                        IdCliente =
                            Convert.ToInt32(
                                reader["id_cliente"]),

                        NumeroDocumento =
                            reader["numero_documento"]
                                ?.ToString()
                            ?? string.Empty,

                        Nombre =
                            reader["nombre"]
                                ?.ToString()
                            ?? string.Empty,

                        Telefono =
                            reader["telefono"] is DBNull
                                ? null
                                : reader["telefono"]
                                    ?.ToString(),

                        Correo =
                            reader["correo"] is DBNull
                                ? null
                                : reader["correo"]
                                    ?.ToString(),

                        Estado =
                            Convert.ToBoolean(
                                reader["estado"])
                    });
            }

            return clientes;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error buscando clientes de ventas. " +
                "IdEmpresa={IdEmpresa} | Termino={Termino}",
                idEmpresa,
                termino);

            throw;
        }
    }

    // =========================================================
    // OBTENER CLIENTE
    // =========================================================

    public async Task<VentaClienteResponse?>
        ObtenerPorIdAsync(
            int idEmpresa,
            int idCliente)
    {
        if (idEmpresa <= 0 ||
            idCliente <= 0)
        {
            return null;
        }

        const string sql = """
            SELECT TOP (1)
                c.id_cliente,
                c.id_empresa,
                c.tipo_cliente,
                c.tipo_documento,
                c.numero_documento,
                c.razon_social,
                c.nombres,
                c.apellidos,
                c.telefono,
                c.correo,
                c.estado

            FROM ven.cliente c

            WHERE
                c.id_empresa = @id_empresa
                AND c.id_cliente = @id_cliente;
            """;

        await using var connection =
            new SqlConnection(
                _connectionString);

        await connection.OpenAsync();

        await using var command =
            new SqlCommand(
                sql,
                connection);

        command.CommandType =
            CommandType.Text;

        command.Parameters.Add(
            new SqlParameter(
                "@id_empresa",
                SqlDbType.Int)
            {
                Value = idEmpresa
            });

        command.Parameters.Add(
            new SqlParameter(
                "@id_cliente",
                SqlDbType.Int)
            {
                Value = idCliente
            });

        try
        {
            await using var reader =
                await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                return null;
            }

            return new VentaClienteResponse
            {
                IdCliente =
                    Convert.ToInt32(
                        reader["id_cliente"]),

                IdEmpresa =
                    Convert.ToInt32(
                        reader["id_empresa"]),

                TipoCliente =
                    reader["tipo_cliente"]
                        ?.ToString()
                    ?? string.Empty,

                TipoDocumento =
                    reader["tipo_documento"]
                        ?.ToString()
                    ?? string.Empty,

                NumeroDocumento =
                    reader["numero_documento"]
                        ?.ToString()
                    ?? string.Empty,

                RazonSocial =
                    reader["razon_social"] is DBNull
                        ? null
                        : reader["razon_social"]
                            ?.ToString(),

                Nombres =
                    reader["nombres"] is DBNull
                        ? null
                        : reader["nombres"]
                            ?.ToString(),

                Apellidos =
                    reader["apellidos"] is DBNull
                        ? null
                        : reader["apellidos"]
                            ?.ToString(),

                Telefono =
                    reader["telefono"] is DBNull
                        ? null
                        : reader["telefono"]
                            ?.ToString(),

                Correo =
                    reader["correo"] is DBNull
                        ? null
                        : reader["correo"]
                            ?.ToString(),

                Estado =
                    Convert.ToBoolean(
                        reader["estado"])
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error obteniendo cliente. " +
                "IdEmpresa={IdEmpresa} | IdCliente={IdCliente}",
                idEmpresa,
                idCliente);

            throw;
        }
    }
}