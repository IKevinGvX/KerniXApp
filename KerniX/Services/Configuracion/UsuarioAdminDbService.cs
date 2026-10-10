using System.Data;
using KerniX.Models.Configuracion.Usuarios;
using Microsoft.Data.SqlClient;

namespace KerniX.Services.Configuracion.Usuarios;

public sealed class UsuarioAdminDbService : IUsuarioAdminDbService
{
    private readonly string _connectionString;
    private readonly ILogger<UsuarioAdminDbService> _logger;

    public UsuarioAdminDbService(
        IConfiguration configuration,
        ILogger<UsuarioAdminDbService> logger)
    {
        _connectionString =
            configuration.GetConnectionString("KerniXDb")
            ?? throw new InvalidOperationException(
                "No se encontró ConnectionStrings:KerniXDb."
            );

        _logger = logger;
    }


    // =========================================================
    // LISTADO
    // =========================================================

    public async Task<UsuarioAdminResultado> ListarAsync(
        UsuarioAdminFiltro filtro,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filtro);

        filtro.Pagina =
            filtro.Pagina < 1
                ? 1
                : filtro.Pagina;

        filtro.TamanoPagina =
            filtro.TamanoPagina is < 1 or > 100
                ? 20
                : filtro.TamanoPagina;

        filtro.Buscar =
            string.IsNullOrWhiteSpace(filtro.Buscar)
                ? null
                : filtro.Buscar.Trim();

        UsuarioAdminResultado resultado =
            new()
            {
                Pagina = filtro.Pagina,
                TamanoPagina = filtro.TamanoPagina
            };

        await using SqlConnection connection =
            new(_connectionString);

        await connection.OpenAsync(
            cancellationToken
        );

        await using SqlCommand command =
            new(
                "seg.sp_usuario_listar",
                connection
            )
            {
                CommandType =
                    CommandType.StoredProcedure
            };

        command.Parameters.Add(
            "@id_empresa",
            SqlDbType.Int
        ).Value =
            filtro.IdEmpresa > 0
                ? filtro.IdEmpresa
                : DBNull.Value;

        command.Parameters.Add(
            "@id_sucursal",
            SqlDbType.Int
        ).Value =
            filtro.IdSucursal.HasValue &&
            filtro.IdSucursal.Value > 0
                ? filtro.IdSucursal.Value
                : DBNull.Value;

        command.Parameters.Add(
            "@estado",
            SqlDbType.Bit
        ).Value =
            filtro.Estado.HasValue
                ? filtro.Estado.Value
                : DBNull.Value;

        command.Parameters.Add(
            "@buscar",
            SqlDbType.NVarChar,
            150
        ).Value =
            string.IsNullOrWhiteSpace(
                filtro.Buscar
            )
                ? DBNull.Value
                : filtro.Buscar;

        command.Parameters.Add(
            "@id_rol",
            SqlDbType.Int
        ).Value =
            filtro.IdRol.HasValue &&
            filtro.IdRol.Value > 0
                ? filtro.IdRol.Value
                : DBNull.Value;

        command.Parameters.Add(
            "@pagina",
            SqlDbType.Int
        ).Value =
            filtro.Pagina;

        command.Parameters.Add(
            "@tamano_pagina",
            SqlDbType.Int
        ).Value =
            filtro.TamanoPagina;


        await using SqlDataReader reader =
            await command.ExecuteReaderAsync(
                cancellationToken
            );


        while (
            await reader.ReadAsync(
                cancellationToken
            )
        )
        {
            resultado.Items.Add(
                new UsuarioAdminListaItem
                {
                    IdUsuario =
                        Int(
                            reader,
                            "IdUsuario"
                        ),

                    IdEmpleado =
                        NullableInt(
                            reader,
                            "IdEmpleado"
                        ),

                    NombreUsuario =
                        Texto(
                            reader,
                            "NombreUsuario"
                        ),

                    Correo =
                        Texto(
                            reader,
                            "Correo"
                        ),

                    Empleado =
                        Texto(
                            reader,
                            "Empleado"
                        ),

                    Empresa =
                        Texto(
                            reader,
                            "Empresa"
                        ),

                    Sucursal =
                        Texto(
                            reader,
                            "Sucursal"
                        ),

                    Estado =
                        Bool(
                            reader,
                            "Estado"
                        ),

                    RequiereCambioClave =
                        Bool(
                            reader,
                            "RequiereCambioClave"
                        ),

                    IntentosFallidos =
                        Int(
                            reader,
                            "IntentosFallidos"
                        ),

                    BloqueadoHasta =
                        NullableDateTime(
                            reader,
                            "BloqueadoHasta"
                        ),

                    UltimoAcceso =
                        NullableDateTime(
                            reader,
                            "UltimoAcceso"
                        ),

                    Roles =
                        Texto(
                            reader,
                            "Roles"
                        )
                }
            );
        }


        if (
            await reader.NextResultAsync(
                cancellationToken
            ) &&
            await reader.ReadAsync(
                cancellationToken
            )
        )
        {
            resultado.TotalRegistros =
                Int(
                    reader,
                    "TotalRegistros"
                );
        }


        _logger.LogInformation(
            "[USUARIOS ADMIN] Listado. Empresa={Empresa} Sucursal={Sucursal} Rol={Rol} Estado={Estado} Buscar={Buscar} Registros={Registros} Total={Total}",
            filtro.IdEmpresa,
            filtro.IdSucursal,
            filtro.IdRol,
            filtro.Estado,
            filtro.Buscar,
            resultado.Items.Count,
            resultado.TotalRegistros
        );


        return resultado;
    }


    // =========================================================
    // RESUMEN
    // =========================================================

    public async Task<UsuarioAdminResumen> ObtenerResumenAsync(
        int idEmpresa,
        int? idSucursal,
        CancellationToken cancellationToken = default)
    {
        UsuarioAdminResumen resumen =
            new();

        const string sql =
            """
            SELECT
                COUNT_BIG(*) AS TotalUsuarios,

                SUM(
                    CASE
                        WHEN u.estado = 1
                        THEN 1 ELSE 0
                    END
                ) AS Activos,

                SUM(
                    CASE
                        WHEN u.estado = 0
                        THEN 1 ELSE 0
                    END
                ) AS Inactivos,

                SUM(
                    CASE
                        WHEN u.bloqueado_hasta IS NOT NULL
                             AND u.bloqueado_hasta > SYSDATETIME()
                        THEN 1 ELSE 0
                    END
                ) AS Bloqueados,

                SUM(
                    CASE
                        WHEN u.requiere_cambio_clave = 1
                        THEN 1 ELSE 0
                    END
                ) AS RequierenCambioClave

            FROM seg.usuario u

            LEFT JOIN org.empleado e
                ON e.id_empleado = u.id_empleado

            WHERE
                (@id_empresa IS NULL OR e.id_empresa = @id_empresa)
                AND
                (@id_sucursal IS NULL OR e.id_sucursal = @id_sucursal);
            """;

        await using SqlConnection connection =
            new(_connectionString);

        await connection.OpenAsync(
            cancellationToken
        );

        await using SqlCommand command =
            new(
                sql,
                connection
            );

        command.Parameters.Add(
            "@id_empresa",
            SqlDbType.Int
        ).Value =
            idEmpresa > 0
                ? idEmpresa
                : DBNull.Value;

        command.Parameters.Add(
            "@id_sucursal",
            SqlDbType.Int
        ).Value =
            idSucursal.HasValue &&
            idSucursal.Value > 0
                ? idSucursal.Value
                : DBNull.Value;


        await using SqlDataReader reader =
            await command.ExecuteReaderAsync(
                cancellationToken
            );


        if (
            await reader.ReadAsync(
                cancellationToken
            )
        )
        {
            resumen.TotalUsuarios =
                Convert.ToInt32(
                    reader["TotalUsuarios"]
                );

            resumen.Activos =
                reader["Activos"] == DBNull.Value
                    ? 0
                    : Convert.ToInt32(
                        reader["Activos"]
                    );

            resumen.Inactivos =
                reader["Inactivos"] == DBNull.Value
                    ? 0
                    : Convert.ToInt32(
                        reader["Inactivos"]
                    );

            resumen.Bloqueados =
                reader["Bloqueados"] == DBNull.Value
                    ? 0
                    : Convert.ToInt32(
                        reader["Bloqueados"]
                    );

            resumen.RequierenCambioClave =
                reader["RequierenCambioClave"] == DBNull.Value
                    ? 0
                    : Convert.ToInt32(
                        reader["RequierenCambioClave"]
                    );
        }


        return resumen;
    }


    // =========================================================
    // ROLES
    // =========================================================

    public async Task<IReadOnlyList<RolAdminItem>> ListarRolesAsync(
        int idEmpresa,
        CancellationToken cancellationToken = default)
    {
        List<RolAdminItem> roles =
            new();

        await using SqlConnection connection =
            new(_connectionString);

        await connection.OpenAsync(
            cancellationToken
        );

        await using SqlCommand command =
            new(
                "seg.sp_rol_listar",
                connection
            )
            {
                CommandType =
                    CommandType.StoredProcedure
            };

        command.Parameters.Add(
            "@id_empresa",
            SqlDbType.Int
        ).Value =
            idEmpresa > 0
                ? idEmpresa
                : DBNull.Value;

        command.Parameters.Add(
            "@estado",
            SqlDbType.Bit
        ).Value =
            true;

        command.Parameters.Add(
            "@buscar",
            SqlDbType.NVarChar,
            100
        ).Value =
            DBNull.Value;

        command.Parameters.Add(
            "@pagina",
            SqlDbType.Int
        ).Value =
            1;

        command.Parameters.Add(
            "@tamano_pagina",
            SqlDbType.Int
        ).Value =
            100;


        await using SqlDataReader reader =
            await command.ExecuteReaderAsync(
                cancellationToken
            );


        while (
            await reader.ReadAsync(
                cancellationToken
            )
        )
        {
            roles.Add(
                new RolAdminItem
                {
                    IdRol =
                        Int(
                            reader,
                            "IdRol"
                        ),

                    IdEmpresa =
                        NullableInt(
                            reader,
                            "IdEmpresa"
                        ),

                    Empresa =
                        Texto(
                            reader,
                            "Empresa"
                        ),

                    Nombre =
                        Texto(
                            reader,
                            "Nombre"
                        ),

                    Descripcion =
                        TextoNullable(
                            reader,
                            "Descripcion"
                        ),

                    EsAdministrador =
                        Bool(
                            reader,
                            "EsAdministrador"
                        ),

                    Estado =
                        Bool(
                            reader,
                            "Estado"
                        ),

                    TotalUsuarios =
                        Int(
                            reader,
                            "TotalUsuarios"
                        ),

                    TotalPermisos =
                        Int(
                            reader,
                            "TotalPermisos"
                        )
                }
            );
        }


        return roles;
    }



    // =========================================================
    // EMPLEADOS DISPONIBLES PARA CREAR USUARIO
    //
    // Alcance actual de KerniX:
    // - Gestión Ventas
    // - Gestión Inventario
    // =========================================================

    public async Task<IReadOnlyList<EmpleadoUsuarioDisponibleItem>>
        ListarEmpleadosDisponiblesAsync(
            int idEmpresa,
            int? idSucursal,
            CancellationToken cancellationToken = default)
    {
        List<EmpleadoUsuarioDisponibleItem> empleados =
            new();

        const string sql =
            """
            SELECT
                e.id_empleado AS IdEmpleado,
                e.id_empresa AS IdEmpresa,
                e.id_sucursal AS IdSucursal,

                CONCAT(
                    e.nombres,
                    N' ',
                    e.apellido_paterno,
                    CASE
                        WHEN e.apellido_materno IS NULL
                            THEN N''
                        ELSE CONCAT(
                            N' ',
                            e.apellido_materno
                        )
                    END
                ) AS Empleado,

                e.cargo AS Cargo,
                e.correo AS Correo,
                e.numero_documento AS NumeroDocumento,
                s.nombre AS Sucursal

            FROM org.empleado e

            INNER JOIN org.sucursal s
                ON s.id_sucursal =
                   e.id_sucursal

            LEFT JOIN seg.usuario u
                ON u.id_empleado =
                   e.id_empleado

            WHERE
                e.id_empresa =
                @id_empresa

                AND
                (
                    @id_sucursal IS NULL
                    OR
                    e.id_sucursal =
                    @id_sucursal
                )

                AND
                e.estado = 1

                AND
                u.id_usuario IS NULL

                AND
                e.cargo IN
                (
                    N'Jefe de Ventas',
                    N'Vendedor',
                    N'Atención al Cliente',
                    N'Jefe de Almacén',
                    N'Encargado de Inventario',
                    N'Almacenero'
                )

            ORDER BY
                CASE
                    WHEN e.cargo IN
                    (
                        N'Jefe de Ventas',
                        N'Vendedor',
                        N'Atención al Cliente'
                    )
                        THEN 1
                    ELSE 2
                END,
                e.cargo,
                e.nombres,
                e.apellido_paterno;
            """;


        await using SqlConnection connection =
            new(_connectionString);

        await connection.OpenAsync(
            cancellationToken
        );


        await using SqlCommand command =
            new(
                sql,
                connection
            );


        command.Parameters.Add(
            "@id_empresa",
            SqlDbType.Int
        ).Value =
            idEmpresa;


        command.Parameters.Add(
            "@id_sucursal",
            SqlDbType.Int
        ).Value =
            idSucursal.HasValue &&
            idSucursal.Value > 0
                ? idSucursal.Value
                : DBNull.Value;


        await using SqlDataReader reader =
            await command.ExecuteReaderAsync(
                cancellationToken
            );


        while (
            await reader.ReadAsync(
                cancellationToken
            )
        )
        {
            empleados.Add(
                new EmpleadoUsuarioDisponibleItem
                {
                    IdEmpleado =
                        Int(
                            reader,
                            "IdEmpleado"
                        ),

                    IdEmpresa =
                        Int(
                            reader,
                            "IdEmpresa"
                        ),

                    IdSucursal =
                        Int(
                            reader,
                            "IdSucursal"
                        ),

                    Empleado =
                        Texto(
                            reader,
                            "Empleado"
                        ),

                    Cargo =
                        Texto(
                            reader,
                            "Cargo"
                        ),

                    Correo =
                        Texto(
                            reader,
                            "Correo"
                        ),

                    NumeroDocumento =
                        Texto(
                            reader,
                            "NumeroDocumento"
                        ),

                    Sucursal =
                        Texto(
                            reader,
                            "Sucursal"
                        )
                }
            );
        }


        _logger.LogInformation(
            "[USUARIOS ADMIN] Empleados disponibles. Empresa={Empresa} Sucursal={Sucursal} Total={Total}",
            idEmpresa,
            idSucursal,
            empleados.Count
        );


        return empleados;
    }


    // =========================================================
    // CREAR USUARIO + ASIGNAR CATEGORÍA/ROL
    //
    // La API de KerniX valida claves BCrypt.
    // Por eso se genera un BCrypt con work factor 11 antes de
    // ejecutar seg.sp_usuario_crear.
    // =========================================================

    public async Task<int> CrearUsuarioAsync(
        UsuarioCrearAdminRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            request
        );


        if (request.IdEmpleado <= 0)
        {
            throw new ArgumentException(
                "El empleado es obligatorio.",
                nameof(request)
            );
        }


        if (request.IdRol <= 0)
        {
            throw new ArgumentException(
                "La categoría o rol es obligatoria.",
                nameof(request)
            );
        }


        if (
            string.IsNullOrWhiteSpace(
                request.NombreUsuario
            )
        )
        {
            throw new ArgumentException(
                "El nombre de usuario es obligatorio.",
                nameof(request)
            );
        }


        if (
            string.IsNullOrWhiteSpace(
                request.Correo
            )
        )
        {
            throw new ArgumentException(
                "El correo es obligatorio.",
                nameof(request)
            );
        }


        if (
            string.IsNullOrWhiteSpace(
                request.PasswordTemporal
            )
        )
        {
            throw new ArgumentException(
                "La contraseña temporal es obligatoria.",
                nameof(request)
            );
        }


        if (request.IdUsuarioRegistro <= 0)
        {
            throw new ArgumentException(
                "No se pudo identificar al usuario administrador.",
                nameof(request)
            );
        }


        string nombreUsuario =
            request.NombreUsuario
                .Trim()
                .ToLowerInvariant();


        string correo =
            request.Correo
                .Trim()
                .ToLowerInvariant();


        // IMPORTANTE:
        // Requiere paquete NuGet BCrypt.Net-Next en el FRONTEND.
        string claveHash =
            BCrypt.Net.BCrypt.HashPassword(
                request.PasswordTemporal,
                workFactor: 11
            );


        await using SqlConnection connection =
            new(_connectionString);

        await connection.OpenAsync(
            cancellationToken
        );


        using SqlTransaction transaction =
            connection.BeginTransaction();


        try
        {
            int idUsuario;


            // -------------------------------------------------
            // 1. CREAR USUARIO
            // -------------------------------------------------

            await using (
                SqlCommand command =
                    new(
                        "seg.sp_usuario_crear",
                        connection,
                        transaction
                    )
                    {
                        CommandType =
                            CommandType.StoredProcedure
                    }
            )
            {
                command.Parameters.Add(
                    "@id_empleado",
                    SqlDbType.Int
                ).Value =
                    request.IdEmpleado;


                command.Parameters.Add(
                    "@nombre_usuario",
                    SqlDbType.NVarChar,
                    100
                ).Value =
                    nombreUsuario;


                command.Parameters.Add(
                    "@correo",
                    SqlDbType.NVarChar,
                    200
                ).Value =
                    correo;


                command.Parameters.Add(
                    "@clave_hash",
                    SqlDbType.NVarChar,
                    500
                ).Value =
                    claveHash;


                command.Parameters.Add(
                    "@requiere_cambio_clave",
                    SqlDbType.Bit
                ).Value =
                    request.RequiereCambioClave;


                command.Parameters.Add(
                    "@id_usuario_registro",
                    SqlDbType.Int
                ).Value =
                    request.IdUsuarioRegistro;


                SqlParameter idUsuarioOutput =
                    command.Parameters.Add(
                        "@id_usuario",
                        SqlDbType.Int
                    );

                idUsuarioOutput.Direction =
                    ParameterDirection.Output;


                await command.ExecuteNonQueryAsync(
                    cancellationToken
                );


                idUsuario =
                    Convert.ToInt32(
                        idUsuarioOutput.Value
                    );
            }


            if (idUsuario <= 0)
            {
                throw new InvalidOperationException(
                    "El procedimiento no devolvió un id_usuario válido."
                );
            }


            // -------------------------------------------------
            // 2. ASIGNAR CATEGORÍA / ROL
            // -------------------------------------------------

            await using (
                SqlCommand command =
                    new(
                        "seg.sp_usuario_rol_asignar",
                        connection,
                        transaction
                    )
                    {
                        CommandType =
                            CommandType.StoredProcedure
                    }
            )
            {
                command.Parameters.Add(
                    "@id_usuario",
                    SqlDbType.Int
                ).Value =
                    idUsuario;


                command.Parameters.Add(
                    "@id_rol",
                    SqlDbType.Int
                ).Value =
                    request.IdRol;


                command.Parameters.Add(
                    "@id_usuario_registro",
                    SqlDbType.Int
                ).Value =
                    request.IdUsuarioRegistro;


                await command.ExecuteNonQueryAsync(
                    cancellationToken
                );
            }


            // -------------------------------------------------
            // 3. ESTADO INICIAL
            // -------------------------------------------------

            if (!request.Estado)
            {
                await using SqlCommand command =
                    new(
                        "seg.sp_usuario_cambiar_estado",
                        connection,
                        transaction
                    )
                    {
                        CommandType =
                            CommandType.StoredProcedure
                    };


                command.Parameters.Add(
                    "@id_usuario",
                    SqlDbType.Int
                ).Value =
                    idUsuario;


                command.Parameters.Add(
                    "@estado",
                    SqlDbType.Bit
                ).Value =
                    false;


                command.Parameters.Add(
                    "@id_usuario_modifica",
                    SqlDbType.Int
                ).Value =
                    request.IdUsuarioRegistro;


                await command.ExecuteNonQueryAsync(
                    cancellationToken
                );
            }


            transaction.Commit();


            _logger.LogInformation(
                "[USUARIOS ADMIN] Usuario creado. IdUsuario={IdUsuario} Empleado={Empleado} Rol={Rol} Administrador={Administrador}",
                idUsuario,
                request.IdEmpleado,
                request.IdRol,
                request.IdUsuarioRegistro
            );


            return idUsuario;
        }
        catch
        {
            try
            {
                transaction.Rollback();
            }
            catch
            {
                // No ocultar la excepción original.
            }

            throw;
        }
    }



    // =========================================================
    // OBTENER USUARIO PARA EDITAR
    // =========================================================

    public async Task<UsuarioAdminEditarDetalle?> ObtenerParaEditarAsync(
        int idUsuario,
        int idEmpresa,
        CancellationToken cancellationToken = default)
    {
        if (idUsuario <= 0)
        {
            return null;
        }


        const string sql =
            """
            SELECT TOP (1)
                u.id_usuario AS IdUsuario,
                u.id_empleado AS IdEmpleado,

                e.id_empresa AS IdEmpresa,
                e.id_sucursal AS IdSucursal,

                u.nombre_usuario AS NombreUsuario,
                u.correo AS Correo,

                CONCAT(
                    e.nombres,
                    N' ',
                    e.apellido_paterno,
                    CASE
                        WHEN e.apellido_materno IS NULL
                            THEN N''
                        ELSE CONCAT(
                            N' ',
                            e.apellido_materno
                        )
                    END
                ) AS Empleado,

                emp.razon_social AS Empresa,
                s.nombre AS Sucursal,
                e.cargo AS Cargo,

                u.estado AS Estado,
                u.requiere_cambio_clave AS RequiereCambioClave,
                u.intentos_fallidos AS IntentosFallidos,
                u.bloqueado_hasta AS BloqueadoHasta,
                u.ultimo_acceso AS UltimoAcceso,

                rol.IdRol,
                rol.Rol

            FROM seg.usuario u

            INNER JOIN org.empleado e
                ON e.id_empleado =
                   u.id_empleado

            INNER JOIN org.empresa emp
                ON emp.id_empresa =
                   e.id_empresa

            INNER JOIN org.sucursal s
                ON s.id_sucursal =
                   e.id_sucursal

            OUTER APPLY
            (
                SELECT TOP (1)
                    r.id_rol AS IdRol,
                    r.nombre AS Rol
                FROM seg.usuario_rol ur
                INNER JOIN seg.rol r
                    ON r.id_rol =
                       ur.id_rol
                WHERE
                    ur.id_usuario =
                    u.id_usuario
                    AND
                    ur.estado = 1
                    AND
                    r.estado = 1
                ORDER BY
                    r.es_administrador DESC,
                    r.id_rol
            ) rol

            WHERE
                u.id_usuario =
                @id_usuario

                AND
                e.id_empresa =
                @id_empresa;
            """;


        await using SqlConnection connection =
            new(_connectionString);

        await connection.OpenAsync(
            cancellationToken
        );


        await using SqlCommand command =
            new(
                sql,
                connection
            );


        command.Parameters.Add(
            "@id_usuario",
            SqlDbType.Int
        ).Value =
            idUsuario;


        command.Parameters.Add(
            "@id_empresa",
            SqlDbType.Int
        ).Value =
            idEmpresa;


        await using SqlDataReader reader =
            await command.ExecuteReaderAsync(
                cancellationToken
            );


        if (
            !await reader.ReadAsync(
                cancellationToken
            )
        )
        {
            _logger.LogWarning(
                "[USUARIOS ADMIN] ObtenerParaEditar sin resultado. Usuario={Usuario} Empresa={Empresa}",
                idUsuario,
                idEmpresa
            );

            return null;
        }


        UsuarioAdminEditarDetalle detalle =
            new()
            {
                IdUsuario =
                    Int(
                        reader,
                        "IdUsuario"
                    ),

                IdEmpleado =
                    Int(
                        reader,
                        "IdEmpleado"
                    ),

                IdEmpresa =
                    Int(
                        reader,
                        "IdEmpresa"
                    ),

                IdSucursal =
                    Int(
                        reader,
                        "IdSucursal"
                    ),

                NombreUsuario =
                    Texto(
                        reader,
                        "NombreUsuario"
                    ),

                Correo =
                    Texto(
                        reader,
                        "Correo"
                    ),

                Empleado =
                    Texto(
                        reader,
                        "Empleado"
                    ),

                Empresa =
                    Texto(
                        reader,
                        "Empresa"
                    ),

                Sucursal =
                    Texto(
                        reader,
                        "Sucursal"
                    ),

                Cargo =
                    Texto(
                        reader,
                        "Cargo"
                    ),

                Estado =
                    Bool(
                        reader,
                        "Estado"
                    ),

                RequiereCambioClave =
                    Bool(
                        reader,
                        "RequiereCambioClave"
                    ),

                IntentosFallidos =
                    Int(
                        reader,
                        "IntentosFallidos"
                    ),

                BloqueadoHasta =
                    NullableDateTime(
                        reader,
                        "BloqueadoHasta"
                    ),

                UltimoAcceso =
                    NullableDateTime(
                        reader,
                        "UltimoAcceso"
                    ),

                IdRol =
                    NullableInt(
                        reader,
                        "IdRol"
                    ),

                Rol =
                    Texto(
                        reader,
                        "Rol"
                    )
            };


        _logger.LogInformation(
            "[USUARIOS ADMIN] ObtenerParaEditar OK. Usuario={Usuario} Empleado={Empleado} Empresa={Empresa} Rol={Rol}",
            detalle.IdUsuario,
            detalle.IdEmpleado,
            detalle.IdEmpresa,
            detalle.IdRol
        );


        return detalle;
    }


    // =========================================================
    // ACTUALIZAR USUARIO + CATEGORÍA
    // =========================================================

    public async Task ActualizarUsuarioAsync(
        UsuarioAdminActualizarRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            request
        );


        if (request.IdUsuario <= 0)
        {
            throw new ArgumentException(
                "Usuario inválido.",
                nameof(request)
            );
        }


        if (request.IdEmpleado <= 0)
        {
            throw new ArgumentException(
                "Empleado inválido.",
                nameof(request)
            );
        }


        if (request.IdRol <= 0)
        {
            throw new ArgumentException(
                "Selecciona una categoría.",
                nameof(request)
            );
        }


        if (
            string.IsNullOrWhiteSpace(
                request.NombreUsuario
            )
        )
        {
            throw new ArgumentException(
                "El nombre de usuario es obligatorio.",
                nameof(request)
            );
        }


        if (
            string.IsNullOrWhiteSpace(
                request.Correo
            )
        )
        {
            throw new ArgumentException(
                "El correo es obligatorio.",
                nameof(request)
            );
        }


        await using SqlConnection connection =
            new(_connectionString);

        await connection.OpenAsync(
            cancellationToken
        );


        using SqlTransaction transaction =
            connection.BeginTransaction();


        try
        {
            // -------------------------------------------------
            // 1. DATOS DEL USUARIO
            // -------------------------------------------------

            await using (
                SqlCommand command =
                    new(
                        "seg.sp_usuario_actualizar",
                        connection,
                        transaction
                    )
                    {
                        CommandType =
                            CommandType.StoredProcedure
                    }
            )
            {
                command.Parameters.Add(
                    "@id_usuario",
                    SqlDbType.Int
                ).Value =
                    request.IdUsuario;


                command.Parameters.Add(
                    "@id_empleado",
                    SqlDbType.Int
                ).Value =
                    request.IdEmpleado;


                command.Parameters.Add(
                    "@nombre_usuario",
                    SqlDbType.NVarChar,
                    100
                ).Value =
                    request.NombreUsuario
                        .Trim()
                        .ToLowerInvariant();


                command.Parameters.Add(
                    "@correo",
                    SqlDbType.NVarChar,
                    200
                ).Value =
                    request.Correo
                        .Trim()
                        .ToLowerInvariant();


                command.Parameters.Add(
                    "@estado",
                    SqlDbType.Bit
                ).Value =
                    request.Estado;


                command.Parameters.Add(
                    "@requiere_cambio_clave",
                    SqlDbType.Bit
                ).Value =
                    request.RequiereCambioClave;


                command.Parameters.Add(
                    "@id_usuario_modifica",
                    SqlDbType.Int
                ).Value =
                    request.IdUsuarioModifica;


                await command.ExecuteNonQueryAsync(
                    cancellationToken
                );
            }


            // -------------------------------------------------
            // 2. LIMPIAR CATEGORÍAS ANTERIORES
            // -------------------------------------------------

            await using (
                SqlCommand command =
                    new(
                        "seg.sp_usuario_roles_limpiar",
                        connection,
                        transaction
                    )
                    {
                        CommandType =
                            CommandType.StoredProcedure
                    }
            )
            {
                command.Parameters.Add(
                    "@id_usuario",
                    SqlDbType.Int
                ).Value =
                    request.IdUsuario;


                await command.ExecuteNonQueryAsync(
                    cancellationToken
                );
            }


            // -------------------------------------------------
            // 3. ASIGNAR NUEVA CATEGORÍA
            // -------------------------------------------------

            await using (
                SqlCommand command =
                    new(
                        "seg.sp_usuario_rol_asignar",
                        connection,
                        transaction
                    )
                    {
                        CommandType =
                            CommandType.StoredProcedure
                    }
            )
            {
                command.Parameters.Add(
                    "@id_usuario",
                    SqlDbType.Int
                ).Value =
                    request.IdUsuario;


                command.Parameters.Add(
                    "@id_rol",
                    SqlDbType.Int
                ).Value =
                    request.IdRol;


                command.Parameters.Add(
                    "@id_usuario_registro",
                    SqlDbType.Int
                ).Value =
                    request.IdUsuarioModifica;


                await command.ExecuteNonQueryAsync(
                    cancellationToken
                );
            }


            transaction.Commit();


            _logger.LogInformation(
                "[USUARIOS ADMIN] Usuario actualizado. Usuario={Usuario} Empleado={Empleado} Rol={Rol} Modifica={Modifica}",
                request.IdUsuario,
                request.IdEmpleado,
                request.IdRol,
                request.IdUsuarioModifica
            );
        }
        catch
        {
            try
            {
                transaction.Rollback();
            }
            catch
            {
                // Mantener excepción original.
            }

            throw;
        }
    }


    // =========================================================
    // RESTABLECER CONTRASEÑA
    // =========================================================

    public async Task RestablecerClaveAsync(
        int idUsuario,
        string passwordTemporal,
        bool requiereCambioClave,
        CancellationToken cancellationToken = default)
    {
        if (idUsuario <= 0)
        {
            throw new ArgumentException(
                "Usuario inválido.",
                nameof(idUsuario)
            );
        }


        if (
            string.IsNullOrWhiteSpace(
                passwordTemporal
            )
        )
        {
            throw new ArgumentException(
                "La contraseña temporal es obligatoria.",
                nameof(passwordTemporal)
            );
        }


        string claveHash =
            BCrypt.Net.BCrypt.HashPassword(
                passwordTemporal,
                workFactor: 11
            );


        await using SqlConnection connection =
            new(_connectionString);

        await connection.OpenAsync(
            cancellationToken
        );


        await using SqlCommand command =
            new(
                "seg.sp_usuario_restablecer_clave",
                connection
            )
            {
                CommandType =
                    CommandType.StoredProcedure
            };


        command.Parameters.Add(
            "@id_usuario",
            SqlDbType.Int
        ).Value =
            idUsuario;


        command.Parameters.Add(
            "@clave_hash",
            SqlDbType.NVarChar,
            500
        ).Value =
            claveHash;


        command.Parameters.Add(
            "@requiere_cambio_clave",
            SqlDbType.Bit
        ).Value =
            requiereCambioClave;


        await command.ExecuteNonQueryAsync(
            cancellationToken
        );


        _logger.LogInformation(
            "[USUARIOS ADMIN] Contraseña restablecida. Usuario={Usuario} RequiereCambio={RequiereCambio}",
            idUsuario,
            requiereCambioClave
        );
    }



    // =========================================================
    // MONITOREO POR USUARIO
    //
    // Incluye:
    // - datos de la cuenta
    // - sesiones
    // - historial de acceso
    // - auditoría ejecutada por o sobre el usuario
    // =========================================================

    public async Task<UsuarioMonitoreoResultado> ObtenerMonitoreoAsync(
        int idUsuario,
        int idEmpresa,
        CancellationToken cancellationToken = default)
    {
        UsuarioMonitoreoResultado resultado =
            new();


        if (
            idUsuario <= 0 ||
            idEmpresa <= 0
        )
        {
            return resultado;
        }


        await using SqlConnection connection =
            new(_connectionString);

        await connection.OpenAsync(
            cancellationToken
        );


        // =====================================================
        // 1. USUARIO
        // =====================================================

        const string sqlUsuario =
            """
            SELECT TOP (1)
                u.id_usuario AS IdUsuario,
                u.id_empleado AS IdEmpleado,

                e.id_empresa AS IdEmpresa,
                e.id_sucursal AS IdSucursal,

                u.nombre_usuario AS NombreUsuario,
                u.correo AS Correo,

                CONCAT(
                    e.nombres,
                    N' ',
                    e.apellido_paterno,
                    CASE
                        WHEN e.apellido_materno IS NULL
                            THEN N''
                        ELSE CONCAT(
                            N' ',
                            e.apellido_materno
                        )
                    END
                ) AS Empleado,

                emp.razon_social AS Empresa,
                s.nombre AS Sucursal,
                e.cargo AS Cargo,

                u.estado AS Estado,
                u.requiere_cambio_clave AS RequiereCambioClave,
                u.intentos_fallidos AS IntentosFallidos,
                u.bloqueado_hasta AS BloqueadoHasta,
                u.ultimo_acceso AS UltimoAcceso,

                rol.IdRol,
                rol.Rol

            FROM seg.usuario u

            LEFT JOIN org.empleado e
                ON e.id_empleado =
                   u.id_empleado

            LEFT JOIN org.empresa emp
                ON emp.id_empresa =
                   e.id_empresa

            LEFT JOIN org.sucursal s
                ON s.id_sucursal =
                   e.id_sucursal

            OUTER APPLY
            (
                SELECT TOP (1)
                    r.id_rol AS IdRol,
                    r.nombre AS Rol

                FROM seg.usuario_rol ur

                INNER JOIN seg.rol r
                    ON r.id_rol =
                       ur.id_rol

                WHERE
                    ur.id_usuario =
                    u.id_usuario

                    AND
                    ur.estado = 1

                    AND
                    r.estado = 1

                ORDER BY
                    r.es_administrador DESC,
                    r.id_rol
            ) rol

            WHERE
                u.id_usuario =
                @id_usuario

                AND
                (
                    e.id_empresa =
                    @id_empresa
                    OR
                    e.id_empresa IS NULL
                );
            """;


        await using (
            SqlCommand command =
                new(
                    sqlUsuario,
                    connection
                )
        )
        {
            command.Parameters.Add(
                "@id_usuario",
                SqlDbType.Int
            ).Value =
                idUsuario;


            command.Parameters.Add(
                "@id_empresa",
                SqlDbType.Int
            ).Value =
                idEmpresa;


            await using SqlDataReader reader =
                await command.ExecuteReaderAsync(
                    cancellationToken
                );


            if (
                await reader.ReadAsync(
                    cancellationToken
                )
            )
            {
                resultado.Usuario =
                    new UsuarioAdminEditarDetalle
                    {
                        IdUsuario =
                            Int(
                                reader,
                                "IdUsuario"
                            ),

                        IdEmpleado =
                            Int(
                                reader,
                                "IdEmpleado"
                            ),

                        IdEmpresa =
                            Int(
                                reader,
                                "IdEmpresa"
                            ),

                        IdSucursal =
                            Int(
                                reader,
                                "IdSucursal"
                            ),

                        NombreUsuario =
                            Texto(
                                reader,
                                "NombreUsuario"
                            ),

                        Correo =
                            Texto(
                                reader,
                                "Correo"
                            ),

                        Empleado =
                            Texto(
                                reader,
                                "Empleado"
                            ),

                        Empresa =
                            Texto(
                                reader,
                                "Empresa"
                            ),

                        Sucursal =
                            Texto(
                                reader,
                                "Sucursal"
                            ),

                        Cargo =
                            Texto(
                                reader,
                                "Cargo"
                            ),

                        Estado =
                            Bool(
                                reader,
                                "Estado"
                            ),

                        RequiereCambioClave =
                            Bool(
                                reader,
                                "RequiereCambioClave"
                            ),

                        IntentosFallidos =
                            Int(
                                reader,
                                "IntentosFallidos"
                            ),

                        BloqueadoHasta =
                            NullableDateTime(
                                reader,
                                "BloqueadoHasta"
                            ),

                        UltimoAcceso =
                            NullableDateTime(
                                reader,
                                "UltimoAcceso"
                            ),

                        IdRol =
                            NullableInt(
                                reader,
                                "IdRol"
                            ),

                        Rol =
                            Texto(
                                reader,
                                "Rol"
                            )
                    };
            }
        }


        if (resultado.Usuario is null)
        {
            _logger.LogWarning(
                "[USUARIOS MONITOREO] Usuario no encontrado. Usuario={Usuario} Empresa={Empresa}",
                idUsuario,
                idEmpresa
            );

            return resultado;
        }



        // =====================================================
        // 2. EQUIPO ASIGNADO AL EMPLEADO
        //
        // La relación real observada en la BD es:
        // org.empleado -> mon.asignacion_maquina -> mon.maquina
        // =====================================================

        int idMaquina =
            0;


        const string sqlMaquinaAsignada =
            """
            SELECT TOP (1)
                am.id_maquina
            FROM mon.asignacion_maquina am
            INNER JOIN mon.maquina m
                ON m.id_maquina =
                   am.id_maquina
            WHERE
                am.id_empleado =
                @id_empleado
                AND
                m.id_empresa =
                @id_empresa
            ORDER BY
                am.id_asignacion_maquina DESC;
            """;


        await using (
            SqlCommand command =
                new(
                    sqlMaquinaAsignada,
                    connection
                )
        )
        {
            command.Parameters.Add(
                "@id_empleado",
                SqlDbType.Int
            ).Value =
                resultado.Usuario.IdEmpleado;


            command.Parameters.Add(
                "@id_empresa",
                SqlDbType.Int
            ).Value =
                idEmpresa;


            object? valor =
                await command.ExecuteScalarAsync(
                    cancellationToken
                );


            if (
                valor is not null &&
                valor != DBNull.Value
            )
            {
                idMaquina =
                    Convert.ToInt32(
                        valor
                    );
            }
        }


        if (idMaquina > 0)
        {
            // =================================================
            // 2.1 DATOS DE LA MÁQUINA
            // =================================================

            const string sqlMaquina =
                """
                SELECT TOP (1)
                    *
                FROM mon.maquina
                WHERE
                    id_maquina =
                    @id_maquina;
                """;


            await using (
                SqlCommand command =
                    new(
                        sqlMaquina,
                        connection
                    )
            )
            {
                command.Parameters.Add(
                    "@id_maquina",
                    SqlDbType.Int
                ).Value =
                    idMaquina;


                await using SqlDataReader reader =
                    await command.ExecuteReaderAsync(
                        cancellationToken
                    );


                if (
                    await reader.ReadAsync(
                        cancellationToken
                    )
                )
                {
                    resultado.Equipo =
                        new UsuarioEquipoMonitoreoItem
                        {
                            IdMaquina =
                                Int(
                                    reader,
                                    "id_maquina"
                                ),

                            CodigoEquipo =
                                TextoCualquiera(
                                    reader,
                                    "codigo_equipo",
                                    "CodigoEquipo",
                                    "codigo",
                                    "Codigo"
                                ),

                            NombreEquipo =
                                TextoCualquiera(
                                    reader,
                                    "nombre_equipo",
                                    "NombreEquipo",
                                    "nombre",
                                    "Nombre"
                                ),

                            NumeroSerie =
                                TextoCualquiera(
                                    reader,
                                    "numero_serie",
                                    "NumeroSerie",
                                    "serie",
                                    "Serie"
                                ),

                            DireccionMac =
                                TextoCualquiera(
                                    reader,
                                    "direccion_mac",
                                    "DireccionMac",
                                    "mac",
                                    "Mac"
                                ),

                            DireccionIpActual =
                                TextoCualquiera(
                                    reader,
                                    "direccion_ip_actual",
                                    "DireccionIpActual",
                                    "direccion_ip",
                                    "DireccionIp",
                                    "ip",
                                    "Ip"
                                ),

                            SistemaOperativo =
                                TextoCualquiera(
                                    reader,
                                    "sistema_operativo",
                                    "SistemaOperativo",
                                    "so",
                                    "SO"
                                ),

                            VersionSistemaOperativo =
                                TextoCualquiera(
                                    reader,
                                    "version_sistema_operativo",
                                    "VersionSistemaOperativo",
                                    "version_so",
                                    "VersionSo"
                                ),

                            Arquitectura =
                                TextoCualquiera(
                                    reader,
                                    "arquitectura",
                                    "Arquitectura"
                                ),

                            Fabricante =
                                TextoCualquiera(
                                    reader,
                                    "fabricante",
                                    "Fabricante"
                                ),

                            Modelo =
                                TextoCualquiera(
                                    reader,
                                    "modelo",
                                    "Modelo"
                                ),

                            Estado =
                                TextoCualquiera(
                                    reader,
                                    "estado",
                                    "Estado"
                                ),

                            FechaRegistro =
                                NullableDateTimeCualquiera(
                                    reader,
                                    "fecha_registro",
                                    "FechaRegistro"
                                ),

                            UltimaComunicacion =
                                NullableDateTimeCualquiera(
                                    reader,
                                    "ultima_comunicacion",
                                    "UltimaComunicacion",
                                    "fecha_ultima_comunicacion",
                                    "FechaUltimaComunicacion"
                                )
                        };
                }
            }


            // =================================================
            // 2.2 AGENTE DE MONITOREO
            // =================================================

            const string sqlAgente =
                """
                SELECT TOP (1)
                    *
                FROM mon.agente_monitoreo
                WHERE
                    id_maquina =
                    @id_maquina
                ORDER BY
                    id_agente DESC;
                """;


            await using (
                SqlCommand command =
                    new(
                        sqlAgente,
                        connection
                    )
            )
            {
                command.Parameters.Add(
                    "@id_maquina",
                    SqlDbType.Int
                ).Value =
                    idMaquina;


                await using SqlDataReader reader =
                    await command.ExecuteReaderAsync(
                        cancellationToken
                    );


                if (
                    await reader.ReadAsync(
                        cancellationToken
                    )
                )
                {
                    resultado.Agente =
                        new UsuarioAgenteMonitoreoItem
                        {
                            IdAgente =
                                Int(
                                    reader,
                                    "id_agente"
                                ),

                            IdMaquina =
                                Int(
                                    reader,
                                    "id_maquina"
                                ),

                            CodigoAgente =
                                TextoCualquiera(
                                    reader,
                                    "codigo_agente",
                                    "CodigoAgente"
                                ),

                            VersionAgente =
                                TextoCualquiera(
                                    reader,
                                    "version_agente",
                                    "VersionAgente"
                                ),

                            IntervaloEnvioSegundos =
                                NullableIntCualquiera(
                                    reader,
                                    "intervalo_envio_segundos",
                                    "IntervaloEnvioSegundos"
                                ),

                            FechaInstalacion =
                                NullableDateTimeCualquiera(
                                    reader,
                                    "fecha_instalacion",
                                    "FechaInstalacion"
                                ),

                            UltimaComunicacion =
                                NullableDateTimeCualquiera(
                                    reader,
                                    "ultima_comunicacion",
                                    "UltimaComunicacion"
                                ),

                            Estado =
                                TextoCualquiera(
                                    reader,
                                    "estado",
                                    "Estado"
                                )
                        };
                }
            }


            // =================================================
            // 2.3 ÚLTIMA MÉTRICA
            //
            // Procedimiento existente:
            // mon.sp_metrica_ultima @id_maquina
            // =================================================

            await using (
                SqlCommand command =
                    new(
                        "mon.sp_metrica_ultima",
                        connection
                    )
                    {
                        CommandType =
                            CommandType.StoredProcedure
                    }
            )
            {
                command.Parameters.Add(
                    "@id_maquina",
                    SqlDbType.Int
                ).Value =
                    idMaquina;


                await using SqlDataReader reader =
                    await command.ExecuteReaderAsync(
                        cancellationToken
                    );


                if (
                    await reader.ReadAsync(
                        cancellationToken
                    )
                )
                {
                    resultado.MetricaActual =
                        new UsuarioMetricaMonitoreoItem
                        {
                            IdMetrica =
                                NullableLongCualquiera(
                                    reader,
                                    "id_metrica",
                                    "IdMetrica"
                                ),

                            IdMaquina =
                                Int(
                                    reader,
                                    "id_maquina"
                                ),

                            PorcentajeCpu =
                                NullableDecimalCualquiera(
                                    reader,
                                    "porcentaje_cpu",
                                    "PorcentajeCpu"
                                ),

                            PorcentajeRam =
                                NullableDecimalCualquiera(
                                    reader,
                                    "porcentaje_ram",
                                    "PorcentajeRam"
                                ),

                            PorcentajeDisco =
                                NullableDecimalCualquiera(
                                    reader,
                                    "porcentaje_disco",
                                    "PorcentajeDisco"
                                ),

                            EspacioLibreGb =
                                NullableDecimalCualquiera(
                                    reader,
                                    "espacio_libre_gb",
                                    "EspacioLibreGb"
                                ),

                            TemperaturaCpu =
                                NullableDecimalCualquiera(
                                    reader,
                                    "temperatura_cpu",
                                    "TemperaturaCpu"
                                ),

                            LatenciaMs =
                                NullableDecimalCualquiera(
                                    reader,
                                    "latencia_ms",
                                    "LatenciaMs"
                                ),

                            EstadoRed =
                                TextoCualquiera(
                                    reader,
                                    "estado_red",
                                    "EstadoRed"
                                ),

                            FechaHora =
                                NullableDateTimeCualquiera(
                                    reader,
                                    "fecha_hora",
                                    "FechaHora",
                                    "fecha_registro",
                                    "FechaRegistro"
                                )
                        };
                }
            }
        }


        _logger.LogInformation(
            "[USUARIOS MONITOREO] Equipo. Usuario={Usuario} Empleado={Empleado} Maquina={Maquina} TieneEquipo={TieneEquipo} TieneMetrica={TieneMetrica}",
            idUsuario,
            resultado.Usuario.IdEmpleado,
            idMaquina,
            resultado.Equipo is not null,
            resultado.MetricaActual is not null
        );


        // =====================================================
        // 3. SESIONES
        // =====================================================

        await using (
            SqlCommand command =
                new(
                    "seg.sp_sesion_listar_usuario",
                    connection
                )
                {
                    CommandType =
                        CommandType.StoredProcedure
                }
        )
        {
            command.Parameters.Add(
                "@id_usuario",
                SqlDbType.Int
            ).Value =
                idUsuario;


            command.Parameters.Add(
                "@estado",
                SqlDbType.NVarChar,
                20
            ).Value =
                DBNull.Value;


            await using SqlDataReader reader =
                await command.ExecuteReaderAsync(
                    cancellationToken
                );


            while (
                await reader.ReadAsync(
                    cancellationToken
                )
            )
            {
                resultado.Sesiones.Add(
                    new UsuarioSesionMonitoreoItem
                    {
                        IdSesion =
                            NullableLongCualquiera(
                                reader,
                                "id_sesion_usuario",
                                "IdSesionUsuario",
                                "id_sesion",
                                "IdSesion"
                            ),

                        Estado =
                            TextoCualquiera(
                                reader,
                                "estado",
                                "Estado"
                            ),

                        FechaInicio =
                            NullableDateTimeCualquiera(
                                reader,
                                "fecha_inicio",
                                "FechaInicio",
                                "fecha_registro",
                                "FechaRegistro"
                            ),

                        FechaCierre =
                            NullableDateTimeCualquiera(
                                reader,
                                "fecha_cierre",
                                "FechaCierre"
                            ),

                        FechaExpiracion =
                            NullableDateTimeCualquiera(
                                reader,
                                "fecha_expiracion",
                                "FechaExpiracion",
                                "expira_en",
                                "ExpiraEn"
                            ),

                        Ip =
                            TextoCualquiera(
                                reader,
                                "ip",
                                "Ip",
                                "direccion_ip",
                                "DireccionIp",
                                "ip_address",
                                "IpAddress"
                            ),

                        Dispositivo =
                            TextoCualquiera(
                                reader,
                                "dispositivo",
                                "Dispositivo",
                                "nombre_dispositivo",
                                "NombreDispositivo",
                                "equipo",
                                "Equipo"
                            ),

                        UserAgent =
                            TextoCualquiera(
                                reader,
                                "user_agent",
                                "UserAgent",
                                "agente_usuario",
                                "AgenteUsuario",
                                "navegador",
                                "Navegador"
                            )
                    }
                );
            }
        }


        // =====================================================
        // 4. HISTORIAL DE ACCESO
        //
        // Esta tabla ya existe en el esquema seg.
        // Se usa un mapper tolerante a distintos aliases.
        // =====================================================

        const string sqlHistorial =
            """
            SELECT TOP (100)
                *
            FROM seg.historial_acceso
            WHERE id_usuario =
                  @id_usuario;
            """;


        await using (
            SqlCommand command =
                new(
                    sqlHistorial,
                    connection
                )
        )
        {
            command.Parameters.Add(
                "@id_usuario",
                SqlDbType.Int
            ).Value =
                idUsuario;


            await using SqlDataReader reader =
                await command.ExecuteReaderAsync(
                    cancellationToken
                );


            while (
                await reader.ReadAsync(
                    cancellationToken
                )
            )
            {
                resultado.HistorialAcceso.Add(
                    new UsuarioHistorialAccesoItem
                    {
                        IdHistorialAcceso =
                            NullableLongCualquiera(
                                reader,
                                "id_historial_acceso",
                                "IdHistorialAcceso",
                                "id_historial",
                                "IdHistorial"
                            ),

                        Fecha =
                            NullableDateTimeCualquiera(
                                reader,
                                "fecha_acceso",
                                "FechaAcceso",
                                "fecha_registro",
                                "FechaRegistro",
                                "fecha",
                                "Fecha"
                            ),

                        Exitoso =
                            NullableBoolCualquiera(
                                reader,
                                "exitoso",
                                "Exitoso",
                                "es_exitoso",
                                "EsExitoso",
                                "resultado",
                                "Resultado"
                            ),

                        Ip =
                            TextoCualquiera(
                                reader,
                                "ip",
                                "Ip",
                                "direccion_ip",
                                "DireccionIp",
                                "ip_address",
                                "IpAddress"
                            ),

                        UserAgent =
                            TextoCualquiera(
                                reader,
                                "user_agent",
                                "UserAgent",
                                "agente_usuario",
                                "AgenteUsuario",
                                "navegador",
                                "Navegador"
                            ),

                        Detalle =
                            TextoCualquiera(
                                reader,
                                "detalle",
                                "Detalle",
                                "mensaje",
                                "Mensaje",
                                "motivo",
                                "Motivo",
                                "observacion",
                                "Observacion"
                            )
                    }
                );
            }
        }


        // =====================================================
        // 5. AUDITORÍA
        //
        // Incluye acciones hechas POR el usuario y acciones
        // administrativas SOBRE la cuenta del usuario.
        // =====================================================

        const string sqlAuditoria =
            """
            SELECT TOP (100)
                a.id_auditoria AS IdAuditoria,
                a.id_usuario AS IdUsuarioActor,

                ISNULL(
                    u.nombre_usuario,
                    N'Sistema'
                ) AS UsuarioActor,

                a.modulo AS Modulo,
                a.entidad AS Entidad,
                a.id_registro AS IdRegistro,
                a.accion AS Accion,
                a.valor_anterior AS ValorAnterior,
                a.valor_nuevo AS ValorNuevo,
                a.fecha_registro AS FechaRegistro

            FROM seg.auditoria_sistema a

            LEFT JOIN seg.usuario u
                ON u.id_usuario =
                   a.id_usuario

            WHERE
                a.id_usuario =
                @id_usuario

                OR
                (
                    a.entidad IN
                    (
                        N'usuario',
                        N'usuario_rol'
                    )

                    AND
                    a.id_registro =
                    CONVERT(
                        NVARCHAR(100),
                        @id_usuario
                    )
                )

            ORDER BY
                a.fecha_registro DESC;
            """;


        await using (
            SqlCommand command =
                new(
                    sqlAuditoria,
                    connection
                )
        )
        {
            command.Parameters.Add(
                "@id_usuario",
                SqlDbType.Int
            ).Value =
                idUsuario;


            await using SqlDataReader reader =
                await command.ExecuteReaderAsync(
                    cancellationToken
                );


            while (
                await reader.ReadAsync(
                    cancellationToken
                )
            )
            {
                resultado.Auditoria.Add(
                    new UsuarioAuditoriaMonitoreoItem
                    {
                        IdAuditoria =
                            NullableLongCualquiera(
                                reader,
                                "IdAuditoria",
                                "id_auditoria"
                            ),

                        IdUsuarioActor =
                            Int(
                                reader,
                                "IdUsuarioActor"
                            ),

                        UsuarioActor =
                            Texto(
                                reader,
                                "UsuarioActor"
                            ),

                        Modulo =
                            Texto(
                                reader,
                                "Modulo"
                            ),

                        Entidad =
                            Texto(
                                reader,
                                "Entidad"
                            ),

                        IdRegistro =
                            Texto(
                                reader,
                                "IdRegistro"
                            ),

                        Accion =
                            Texto(
                                reader,
                                "Accion"
                            ),

                        ValorAnterior =
                            Texto(
                                reader,
                                "ValorAnterior"
                            ),

                        ValorNuevo =
                            Texto(
                                reader,
                                "ValorNuevo"
                            ),

                        FechaRegistro =
                            NullableDateTime(
                                reader,
                                "FechaRegistro"
                            )
                    }
                );
            }
        }


        _logger.LogInformation(
            "[USUARIOS MONITOREO] Cargado. Usuario={Usuario} Sesiones={Sesiones} Historial={Historial} Auditoria={Auditoria}",
            idUsuario,
            resultado.Sesiones.Count,
            resultado.HistorialAcceso.Count,
            resultado.Auditoria.Count
        );


        return resultado;
    }


    // =========================================================
    // CAMBIAR ESTADO
    // =========================================================

    public Task CambiarEstadoAsync(
        int idUsuario,
        bool estado,
        int idUsuarioModifica,
        CancellationToken cancellationToken = default)
    {
        return EjecutarAsync(
            "seg.sp_usuario_cambiar_estado",
            command =>
            {
                command.Parameters.Add(
                    "@id_usuario",
                    SqlDbType.Int
                ).Value =
                    idUsuario;

                command.Parameters.Add(
                    "@estado",
                    SqlDbType.Bit
                ).Value =
                    estado;

                command.Parameters.Add(
                    "@id_usuario_modifica",
                    SqlDbType.Int
                ).Value =
                    idUsuarioModifica;
            },
            cancellationToken
        );
    }


    // =========================================================
    // BLOQUEO
    // =========================================================

    public Task BloquearAsync(
        int idUsuario,
        int minutos,
        CancellationToken cancellationToken = default)
    {
        if (minutos <= 0)
        {
            minutos = 30;
        }

        return EjecutarAsync(
            "seg.sp_usuario_bloquear",
            command =>
            {
                command.Parameters.Add(
                    "@id_usuario",
                    SqlDbType.Int
                ).Value =
                    idUsuario;

                command.Parameters.Add(
                    "@minutos",
                    SqlDbType.Int
                ).Value =
                    minutos;
            },
            cancellationToken
        );
    }


    public Task DesbloquearAsync(
        int idUsuario,
        CancellationToken cancellationToken = default)
    {
        return EjecutarAsync(
            "seg.sp_usuario_desbloquear",
            command =>
            {
                command.Parameters.Add(
                    "@id_usuario",
                    SqlDbType.Int
                ).Value =
                    idUsuario;
            },
            cancellationToken
        );
    }


    // =========================================================
    // SESIONES
    // =========================================================

    public Task RevocarSesionesAsync(
        int idUsuario,
        CancellationToken cancellationToken = default)
    {
        return EjecutarAsync(
            "seg.sp_sesion_revocar_usuario",
            command =>
            {
                command.Parameters.Add(
                    "@id_usuario",
                    SqlDbType.Int
                ).Value =
                    idUsuario;
            },
            cancellationToken
        );
    }


    // =========================================================
    // ROLES USUARIO
    // =========================================================

    public async Task AsignarRolAsync(
        int idUsuario,
        int idRol,
        int idUsuarioRegistro,
        CancellationToken cancellationToken = default)
    {
        await using SqlConnection connection =
            new(_connectionString);

        await connection.OpenAsync(
            cancellationToken
        );

        using SqlTransaction transaction =
            connection.BeginTransaction();

        try
        {
            await using (
                SqlCommand limpiar =
                    new(
                        "seg.sp_usuario_roles_limpiar",
                        connection,
                        transaction
                    )
                    {
                        CommandType =
                            CommandType.StoredProcedure
                    }
            )
            {
                limpiar.Parameters.Add(
                    "@id_usuario",
                    SqlDbType.Int
                ).Value =
                    idUsuario;

                await limpiar.ExecuteNonQueryAsync(
                    cancellationToken
                );
            }

            await using (
                SqlCommand asignar =
                    new(
                        "seg.sp_usuario_rol_asignar",
                        connection,
                        transaction
                    )
                    {
                        CommandType =
                            CommandType.StoredProcedure
                    }
            )
            {
                asignar.Parameters.Add(
                    "@id_usuario",
                    SqlDbType.Int
                ).Value =
                    idUsuario;

                asignar.Parameters.Add(
                    "@id_rol",
                    SqlDbType.Int
                ).Value =
                    idRol;

                asignar.Parameters.Add(
                    "@id_usuario_registro",
                    SqlDbType.Int
                ).Value =
                    idUsuarioRegistro;

                await asignar.ExecuteNonQueryAsync(
                    cancellationToken
                );
            }

            transaction.Commit();

            _logger.LogInformation(
                "[USUARIOS ADMIN] Categoría reemplazada. Usuario={Usuario} Rol={Rol} Admin={Admin}",
                idUsuario,
                idRol,
                idUsuarioRegistro
            );
        }
        catch
        {
            try
            {
                transaction.Rollback();
            }
            catch
            {
            }

            throw;
        }
    }


    public Task QuitarRolAsync(
        int idUsuario,
        int idRol,
        CancellationToken cancellationToken = default)
    {
        return EjecutarAsync(
            "seg.sp_usuario_quitar_rol",
            command =>
            {
                command.Parameters.Add(
                    "@id_usuario",
                    SqlDbType.Int
                ).Value =
                    idUsuario;

                command.Parameters.Add(
                    "@id_rol",
                    SqlDbType.Int
                ).Value =
                    idRol;
            },
            cancellationToken
        );
    }


    // =========================================================
    // EJECUCIÓN GENÉRICA
    // =========================================================

    private async Task EjecutarAsync(
        string procedimiento,
        Action<SqlCommand> configurar,
        CancellationToken cancellationToken)
    {
        await using SqlConnection connection =
            new(_connectionString);

        await connection.OpenAsync(
            cancellationToken
        );

        await using SqlCommand command =
            new(
                procedimiento,
                connection
            )
            {
                CommandType =
                    CommandType.StoredProcedure
            };

        configurar(
            command
        );

        await command.ExecuteNonQueryAsync(
            cancellationToken
        );
    }


    // =========================================================
    // HELPERS
    // =========================================================

    private static int Ordinal(
        SqlDataReader reader,
        string columna)
    {
        for (
            int i = 0;
            i < reader.FieldCount;
            i++
        )
        {
            if (
                string.Equals(
                    reader.GetName(i),
                    columna,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return i;
            }
        }

        return -1;
    }


    private static string Texto(
        SqlDataReader reader,
        string columna)
    {
        int i =
            Ordinal(
                reader,
                columna
            );

        return
            i < 0 ||
            reader.IsDBNull(i)
                ? string.Empty
                : Convert.ToString(
                    reader.GetValue(i)
                ) ?? string.Empty;
    }


    private static string? TextoNullable(
        SqlDataReader reader,
        string columna)
    {
        string valor =
            Texto(
                reader,
                columna
            );

        return string.IsNullOrWhiteSpace(
            valor
        )
            ? null
            : valor;
    }


    private static int Int(
        SqlDataReader reader,
        string columna)
    {
        int i =
            Ordinal(
                reader,
                columna
            );

        return
            i < 0 ||
            reader.IsDBNull(i)
                ? 0
                : Convert.ToInt32(
                    reader.GetValue(i)
                );
    }


    private static int? NullableInt(
        SqlDataReader reader,
        string columna)
    {
        int i =
            Ordinal(
                reader,
                columna
            );

        return
            i < 0 ||
            reader.IsDBNull(i)
                ? null
                : Convert.ToInt32(
                    reader.GetValue(i)
                );
    }


    private static bool Bool(
        SqlDataReader reader,
        string columna)
    {
        int i =
            Ordinal(
                reader,
                columna
            );

        return
            i >= 0 &&
            !reader.IsDBNull(i) &&
            Convert.ToBoolean(
                reader.GetValue(i)
            );
    }




    // =========================================================
    // HELPERS FLEXIBLES PARA MONITOREO
    // =========================================================

    private static string TextoCualquiera(
        SqlDataReader reader,
        params string[] columnas)
    {
        foreach (string columna in columnas)
        {
            int i =
                Ordinal(
                    reader,
                    columna
                );

            if (
                i >= 0 &&
                !reader.IsDBNull(i)
            )
            {
                return
                    Convert.ToString(
                        reader.GetValue(i)
                    )
                    ?? string.Empty;
            }
        }

        return string.Empty;
    }

    // =========================================================
    // HELPERS FLEXIBLES ADICIONALES PARA MONITOREO
    // =========================================================

    private static int? NullableIntCualquiera(
        SqlDataReader reader,
        params string[] columnas)
    {
        foreach (string columna in columnas)
        {
            int i =
                Ordinal(
                    reader,
                    columna
                );

            if (
                i >= 0 &&
                !reader.IsDBNull(i)
            )
            {
                return
                    Convert.ToInt32(
                        reader.GetValue(i)
                    );
            }
        }

        return null;
    }


    private static decimal? NullableDecimalCualquiera(
        SqlDataReader reader,
        params string[] columnas)
    {
        foreach (string columna in columnas)
        {
            int i =
                Ordinal(
                    reader,
                    columna
                );

            if (
                i >= 0 &&
                !reader.IsDBNull(i)
            )
            {
                return
                    Convert.ToDecimal(
                        reader.GetValue(i)
                    );
            }
        }

        return null;
    }





    private static long? NullableLongCualquiera(
        SqlDataReader reader,
        params string[] columnas)
    {
        foreach (string columna in columnas)
        {
            int i =
                Ordinal(
                    reader,
                    columna
                );

            if (
                i >= 0 &&
                !reader.IsDBNull(i)
            )
            {
                return
                    Convert.ToInt64(
                        reader.GetValue(i)
                    );
            }
        }

        return null;
    }


    private static DateTime? NullableDateTimeCualquiera(
        SqlDataReader reader,
        params string[] columnas)
    {
        foreach (string columna in columnas)
        {
            int i =
                Ordinal(
                    reader,
                    columna
                );

            if (
                i >= 0 &&
                !reader.IsDBNull(i)
            )
            {
                return
                    Convert.ToDateTime(
                        reader.GetValue(i)
                    );
            }
        }

        return null;
    }


    private static bool? NullableBoolCualquiera(
        SqlDataReader reader,
        params string[] columnas)
    {
        foreach (string columna in columnas)
        {
            int i =
                Ordinal(
                    reader,
                    columna
                );

            if (
                i < 0 ||
                reader.IsDBNull(i)
            )
            {
                continue;
            }


            object valor =
                reader.GetValue(i);


            if (valor is bool valorBool)
            {
                return valorBool;
            }


            if (
                valor is byte ||
                valor is short ||
                valor is int ||
                valor is long ||
                valor is decimal
            )
            {
                try
                {
                    return
                        Convert.ToInt32(
                            valor
                        ) != 0;
                }
                catch
                {
                    // continúa con interpretación por texto
                }
            }


            string texto =
                Convert.ToString(
                    valor
                )
                ?.Trim()
                .ToLowerInvariant()
                ?? string.Empty;


            if (
                texto is
                    "1" or
                    "true" or
                    "si" or
                    "sí" or
                    "exitoso" or
                    "correcto" or
                    "success"
            )
            {
                return true;
            }


            if (
                texto is
                    "0" or
                    "false" or
                    "no" or
                    "fallido" or
                    "error" or
                    "failed"
            )
            {
                return false;
            }
        }

        return null;
    }

    private static DateTime? NullableDateTime(
        SqlDataReader reader,
        string columna)
    {
        int i =
            Ordinal(
                reader,
                columna
            );

        return
            i < 0 ||
            reader.IsDBNull(i)
                ? null
                : Convert.ToDateTime(
                    reader.GetValue(i)
                );
    }
}
