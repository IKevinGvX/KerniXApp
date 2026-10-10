using KerniX.Models.Sunat;
using Microsoft.Data.SqlClient;
using System.Data;

namespace KerniX.Services.Sunat;

public sealed class SunatConfiguracionDbService
    : ISunatConfiguracionDbService
{
    private readonly string
        _connectionString;


    public SunatConfiguracionDbService(
        IConfiguration configuration
    )
    {
        _connectionString =
            configuration.GetConnectionString(
                "KerniXDb"
            )
            ??
            throw new InvalidOperationException(
                "No existe ConnectionStrings:KerniXDb."
            );
    }


    public async Task<SunatConfiguracion?>
        ObtenerAsync(
            int idEmpresa,
            CancellationToken cancellationToken = default
        )
    {
        const string sql = """
        SELECT
            id_configuracion_sunat,
            id_empresa,
            ambiente,
            ruc,
            usuario_sol,
            clave_sol_cifrada,
            nombre_certificado,
            certificado_cifrado,
            password_certificado_cifrado,
            enviar_automaticamente,
            estado,
            fecha_registro,
            fecha_actualizacion
        FROM cfg.configuracion_sunat
        WHERE id_empresa = @id_empresa;
        """;


        await using var connection =
            new SqlConnection(
                _connectionString
            );

        await connection.OpenAsync(
            cancellationToken
        );


        await using var command =
            new SqlCommand(
                sql,
                connection
            );


        command.Parameters.Add(
            "@id_empresa",
            SqlDbType.Int
        ).Value = idEmpresa;


        await using var reader =
            await command.ExecuteReaderAsync(
                cancellationToken
            );


        if (
            !await reader.ReadAsync(
                cancellationToken
            )
        )
        {
            return null;
        }


        return new SunatConfiguracion
        {
            IdConfiguracionSunat =
                reader.GetInt32(
                    reader.GetOrdinal(
                        "id_configuracion_sunat"
                    )
                ),

            IdEmpresa =
                reader.GetInt32(
                    reader.GetOrdinal(
                        "id_empresa"
                    )
                ),

            Ambiente =
                reader.GetString(
                    reader.GetOrdinal(
                        "ambiente"
                    )
                ),

            Ruc =
                reader.GetString(
                    reader.GetOrdinal(
                        "ruc"
                    )
                ),

            UsuarioSol =
                reader.GetString(
                    reader.GetOrdinal(
                        "usuario_sol"
                    )
                ),

            ClaveSolCifrada =
                reader[
                    "clave_sol_cifrada"
                ] as string,

            NombreCertificado =
                reader[
                    "nombre_certificado"
                ] as string,

            CertificadoCifrado =
                reader[
                    "certificado_cifrado"
                ] == DBNull.Value
                    ? null
                    : (byte[])reader[
                        "certificado_cifrado"
                    ],

            PasswordCertificadoCifrado =
                reader[
                    "password_certificado_cifrado"
                ] as string,

            EnviarAutomaticamente =
                Convert.ToBoolean(
                    reader[
                        "enviar_automaticamente"
                    ]
                ),

            Estado =
                Convert.ToBoolean(
                    reader["estado"]
                ),

            FechaRegistro =
                Convert.ToDateTime(
                    reader[
                        "fecha_registro"
                    ]
                ),

            FechaActualizacion =
                reader[
                    "fecha_actualizacion"
                ] == DBNull.Value
                    ? null
                    : Convert.ToDateTime(
                        reader[
                            "fecha_actualizacion"
                        ]
                    )
        };
    }


    public async Task GuardarAsync(
        SunatConfiguracion configuracion,
        CancellationToken cancellationToken = default
    )
    {
        const string sql = """
        IF EXISTS
        (
            SELECT 1
            FROM cfg.configuracion_sunat
            WHERE id_empresa = @id_empresa
        )
        BEGIN

            UPDATE cfg.configuracion_sunat
            SET
                ambiente = @ambiente,
                ruc = @ruc,
                usuario_sol = @usuario_sol,

                clave_sol_cifrada =
                    COALESCE(
                        @clave_sol_cifrada,
                        clave_sol_cifrada
                    ),

                nombre_certificado =
                    COALESCE(
                        @nombre_certificado,
                        nombre_certificado
                    ),

                certificado_cifrado =
                    COALESCE(
                        @certificado_cifrado,
                        certificado_cifrado
                    ),

                password_certificado_cifrado =
                    COALESCE(
                        @password_certificado_cifrado,
                        password_certificado_cifrado
                    ),

                enviar_automaticamente =
                    @enviar_automaticamente,

                estado =
                    @estado,

                fecha_actualizacion =
                    SYSDATETIME()

            WHERE id_empresa =
                  @id_empresa;

        END
        ELSE
        BEGIN

            INSERT INTO cfg.configuracion_sunat
            (
                id_empresa,
                ambiente,
                ruc,
                usuario_sol,
                clave_sol_cifrada,
                nombre_certificado,
                certificado_cifrado,
                password_certificado_cifrado,
                enviar_automaticamente,
                estado
            )
            VALUES
            (
                @id_empresa,
                @ambiente,
                @ruc,
                @usuario_sol,
                @clave_sol_cifrada,
                @nombre_certificado,
                @certificado_cifrado,
                @password_certificado_cifrado,
                @enviar_automaticamente,
                @estado
            );

        END;
        """;


        await using var connection =
            new SqlConnection(
                _connectionString
            );

        await connection.OpenAsync(
            cancellationToken
        );


        await using var command =
            new SqlCommand(
                sql,
                connection
            );


        command.Parameters.AddWithValue(
            "@id_empresa",
            configuracion.IdEmpresa
        );

        command.Parameters.AddWithValue(
            "@ambiente",
            configuracion.Ambiente
        );

        command.Parameters.AddWithValue(
            "@ruc",
            configuracion.Ruc
        );

        command.Parameters.AddWithValue(
            "@usuario_sol",
            configuracion.UsuarioSol
        );


        command.Parameters.Add(
            "@clave_sol_cifrada",
            SqlDbType.NVarChar,
            -1
        ).Value =
            (object?)
            configuracion.ClaveSolCifrada
            ??
            DBNull.Value;


        command.Parameters.Add(
            "@nombre_certificado",
            SqlDbType.NVarChar,
            255
        ).Value =
            (object?)
            configuracion.NombreCertificado
            ??
            DBNull.Value;


        command.Parameters.Add(
            "@certificado_cifrado",
            SqlDbType.VarBinary,
            -1
        ).Value =
            (object?)
            configuracion.CertificadoCifrado
            ??
            DBNull.Value;


        command.Parameters.Add(
            "@password_certificado_cifrado",
            SqlDbType.NVarChar,
            -1
        ).Value =
            (object?)
            configuracion
                .PasswordCertificadoCifrado
            ??
            DBNull.Value;


        command.Parameters.AddWithValue(
            "@enviar_automaticamente",
            configuracion
                .EnviarAutomaticamente
        );

        command.Parameters.AddWithValue(
            "@estado",
            configuracion.Estado
        );


        await command.ExecuteNonQueryAsync(
            cancellationToken
        );
    }
}