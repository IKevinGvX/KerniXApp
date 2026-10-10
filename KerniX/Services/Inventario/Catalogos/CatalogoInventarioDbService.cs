using System.Data;

using KerniX.Models.Inventario.Catalogos;

using Microsoft.Data.SqlClient;

namespace KerniX.Services.Inventario.Catalogos;

public sealed class CatalogoInventarioDbService
    : ICatalogoInventarioDbService
{
    private readonly string _connectionString;
    private readonly ILogger<CatalogoInventarioDbService> _logger;


    public CatalogoInventarioDbService(
        IConfiguration configuration,
        ILogger<CatalogoInventarioDbService> logger)
    {
        _connectionString =
            configuration.GetConnectionString(
                "KerniXDb"
            )
            ?? throw new InvalidOperationException(
                "No existe ConnectionStrings:KerniXDb."
            );

        _logger =
            logger;
    }


    // =========================================================
    // CATEGORÍAS
    // =========================================================

    public async Task<IReadOnlyList<CategoriaProductoAdminItem>>
        ListarCategoriasAsync(
            int idEmpresa,
            string? buscar,
            bool? estado,
            CancellationToken cancellationToken = default)
    {
        List<CategoriaProductoAdminItem> items =
            new();


        const string sql =
            """
            SELECT
                c.id_categoria_producto AS IdCategoriaProducto,
                c.id_empresa AS IdEmpresa,
                c.nombre AS Nombre,
                c.descripcion AS Descripcion,
                c.estado AS Estado,
                c.fecha_registro AS FechaRegistro,
                COUNT(p.id_producto) AS TotalProductos

            FROM inv.categoria_producto c

            LEFT JOIN inv.producto p
                ON p.id_categoria_producto =
                   c.id_categoria_producto
               AND p.id_empresa =
                   c.id_empresa

            WHERE
                c.id_empresa =
                @id_empresa

                AND
                (
                    @buscar IS NULL
                    OR
                    c.nombre LIKE
                        N'%' + @buscar + N'%'
                    OR
                    c.descripcion LIKE
                        N'%' + @buscar + N'%'
                )

                AND
                (
                    @estado IS NULL
                    OR
                    c.estado =
                    @estado
                )

            GROUP BY
                c.id_categoria_producto,
                c.id_empresa,
                c.nombre,
                c.descripcion,
                c.estado,
                c.fecha_registro

            ORDER BY
                c.estado DESC,
                c.nombre;
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
            "@buscar",
            SqlDbType.NVarChar,
            300
        ).Value =
            string.IsNullOrWhiteSpace(
                buscar
            )
                ? DBNull.Value
                : buscar.Trim();


        command.Parameters.Add(
            "@estado",
            SqlDbType.Bit
        ).Value =
            estado.HasValue
                ? estado.Value
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
            items.Add(
                new CategoriaProductoAdminItem
                {
                    IdCategoriaProducto =
                        reader.GetInt32(
                            reader.GetOrdinal(
                                "IdCategoriaProducto"
                            )
                        ),

                    IdEmpresa =
                        reader.GetInt32(
                            reader.GetOrdinal(
                                "IdEmpresa"
                            )
                        ),

                    Nombre =
                        reader.GetString(
                            reader.GetOrdinal(
                                "Nombre"
                            )
                        ),

                    Descripcion =
                        reader.IsDBNull(
                            reader.GetOrdinal(
                                "Descripcion"
                            )
                        )
                            ? null
                            : reader.GetString(
                                reader.GetOrdinal(
                                    "Descripcion"
                                )
                            ),

                    Estado =
                        reader.GetBoolean(
                            reader.GetOrdinal(
                                "Estado"
                            )
                        ),

                    FechaRegistro =
                        reader.GetDateTime(
                            reader.GetOrdinal(
                                "FechaRegistro"
                            )
                        ),

                    TotalProductos =
                        Convert.ToInt32(
                            reader[
                                "TotalProductos"
                            ]
                        )
                }
            );
        }


        return items;
    }


    public async Task<int> CrearCategoriaAsync(
        CategoriaProductoGuardarRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            request
        );


        string nombre =
            NormalizarTexto(
                request.Nombre
            );


        if (request.IdEmpresa <= 0)
        {
            throw new ArgumentException(
                "La empresa es obligatoria."
            );
        }


        if (
            string.IsNullOrWhiteSpace(
                nombre
            )
        )
        {
            throw new ArgumentException(
                "El nombre de la categoría es obligatorio."
            );
        }


        if (nombre.Length > 150)
        {
            throw new ArgumentException(
                "El nombre de la categoría no puede superar 150 caracteres."
            );
        }


        const string sql =
            """
            IF EXISTS
            (
                SELECT 1
                FROM inv.categoria_producto
                WHERE
                    id_empresa =
                    @id_empresa
                    AND
                    LOWER(LTRIM(RTRIM(nombre))) =
                    LOWER(LTRIM(RTRIM(@nombre)))
            )
            BEGIN
                THROW 65001,
                    'Ya existe una categoría con ese nombre para la empresa.',
                    1;
            END;


            INSERT INTO inv.categoria_producto
            (
                id_empresa,
                nombre,
                descripcion,
                estado,
                fecha_registro
            )
            OUTPUT
                INSERTED.id_categoria_producto
            VALUES
            (
                @id_empresa,
                @nombre,
                @descripcion,
                @estado,
                SYSDATETIME()
            );
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
            request.IdEmpresa;


        command.Parameters.Add(
            "@nombre",
            SqlDbType.NVarChar,
            300
        ).Value =
            nombre;


        command.Parameters.Add(
            "@descripcion",
            SqlDbType.NVarChar,
            600
        ).Value =
            string.IsNullOrWhiteSpace(
                request.Descripcion
            )
                ? DBNull.Value
                : request.Descripcion.Trim();


        command.Parameters.Add(
            "@estado",
            SqlDbType.Bit
        ).Value =
            request.Estado;


        object? id =
            await command.ExecuteScalarAsync(
                cancellationToken
            );


        int idCategoria =
            Convert.ToInt32(
                id
            );


        _logger.LogInformation(
            "[CATALOGOS] Categoría creada. Empresa={Empresa} Categoria={Categoria} Nombre={Nombre}",
            request.IdEmpresa,
            idCategoria,
            nombre
        );


        return idCategoria;
    }


    public async Task ActualizarCategoriaAsync(
        CategoriaProductoActualizarRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            request
        );


        string nombre =
            NormalizarTexto(
                request.Nombre
            );


        if (
            request.IdCategoriaProducto <= 0 ||
            request.IdEmpresa <= 0
        )
        {
            throw new ArgumentException(
                "Categoría inválida."
            );
        }


        if (
            string.IsNullOrWhiteSpace(
                nombre
            )
        )
        {
            throw new ArgumentException(
                "El nombre de la categoría es obligatorio."
            );
        }


        const string sql =
            """
            IF NOT EXISTS
            (
                SELECT 1
                FROM inv.categoria_producto
                WHERE
                    id_categoria_producto =
                    @id_categoria_producto
                    AND
                    id_empresa =
                    @id_empresa
            )
            BEGIN
                THROW 65002,
                    'La categoría no existe o no pertenece a la empresa.',
                    1;
            END;


            IF EXISTS
            (
                SELECT 1
                FROM inv.categoria_producto
                WHERE
                    id_empresa =
                    @id_empresa
                    AND
                    id_categoria_producto <>
                    @id_categoria_producto
                    AND
                    LOWER(LTRIM(RTRIM(nombre))) =
                    LOWER(LTRIM(RTRIM(@nombre)))
            )
            BEGIN
                THROW 65003,
                    'Ya existe otra categoría con ese nombre.',
                    1;
            END;


            UPDATE inv.categoria_producto
            SET
                nombre =
                    @nombre,
                descripcion =
                    @descripcion,
                estado =
                    @estado
            WHERE
                id_categoria_producto =
                @id_categoria_producto
                AND
                id_empresa =
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
            "@id_categoria_producto",
            SqlDbType.Int
        ).Value =
            request.IdCategoriaProducto;


        command.Parameters.Add(
            "@id_empresa",
            SqlDbType.Int
        ).Value =
            request.IdEmpresa;


        command.Parameters.Add(
            "@nombre",
            SqlDbType.NVarChar,
            300
        ).Value =
            nombre;


        command.Parameters.Add(
            "@descripcion",
            SqlDbType.NVarChar,
            600
        ).Value =
            string.IsNullOrWhiteSpace(
                request.Descripcion
            )
                ? DBNull.Value
                : request.Descripcion.Trim();


        command.Parameters.Add(
            "@estado",
            SqlDbType.Bit
        ).Value =
            request.Estado;


        await command.ExecuteNonQueryAsync(
            cancellationToken
        );


        _logger.LogInformation(
            "[CATALOGOS] Categoría actualizada. Categoria={Categoria}",
            request.IdCategoriaProducto
        );
    }


    public async Task CambiarEstadoCategoriaAsync(
        int idCategoriaProducto,
        int idEmpresa,
        bool estado,
        CancellationToken cancellationToken = default)
    {
        const string sql =
            """
            UPDATE inv.categoria_producto
            SET
                estado =
                @estado
            WHERE
                id_categoria_producto =
                @id_categoria_producto
                AND
                id_empresa =
                @id_empresa;


            IF @@ROWCOUNT = 0
            BEGIN
                THROW 65004,
                    'La categoría no existe o no pertenece a la empresa.',
                    1;
            END;
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
            "@id_categoria_producto",
            SqlDbType.Int
        ).Value =
            idCategoriaProducto;


        command.Parameters.Add(
            "@id_empresa",
            SqlDbType.Int
        ).Value =
            idEmpresa;


        command.Parameters.Add(
            "@estado",
            SqlDbType.Bit
        ).Value =
            estado;


        await command.ExecuteNonQueryAsync(
            cancellationToken
        );
    }


    // =========================================================
    // UNIDADES DE MEDIDA
    // =========================================================

    public async Task<IReadOnlyList<UnidadMedidaAdminItem>>
        ListarUnidadesAsync(
            string? buscar,
            bool? estado,
            CancellationToken cancellationToken = default)
    {
        List<UnidadMedidaAdminItem> items =
            new();


        const string sql =
            """
            SELECT
                u.id_unidad_medida AS IdUnidadMedida,
                u.codigo AS Codigo,
                u.nombre AS Nombre,
                u.abreviatura AS Abreviatura,
                u.estado AS Estado,
                COUNT(p.id_producto) AS TotalProductos

            FROM inv.unidad_medida u

            LEFT JOIN inv.producto p
                ON p.id_unidad_medida =
                   u.id_unidad_medida

            WHERE
                (
                    @buscar IS NULL
                    OR
                    u.codigo LIKE
                        N'%' + @buscar + N'%'
                    OR
                    u.nombre LIKE
                        N'%' + @buscar + N'%'
                    OR
                    u.abreviatura LIKE
                        N'%' + @buscar + N'%'
                )

                AND
                (
                    @estado IS NULL
                    OR
                    u.estado =
                    @estado
                )

            GROUP BY
                u.id_unidad_medida,
                u.codigo,
                u.nombre,
                u.abreviatura,
                u.estado

            ORDER BY
                u.estado DESC,
                u.nombre;
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
            "@buscar",
            SqlDbType.NVarChar,
            200
        ).Value =
            string.IsNullOrWhiteSpace(
                buscar
            )
                ? DBNull.Value
                : buscar.Trim();


        command.Parameters.Add(
            "@estado",
            SqlDbType.Bit
        ).Value =
            estado.HasValue
                ? estado.Value
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
            items.Add(
                new UnidadMedidaAdminItem
                {
                    IdUnidadMedida =
                        reader.GetInt32(
                            reader.GetOrdinal(
                                "IdUnidadMedida"
                            )
                        ),

                    Codigo =
                        reader.GetString(
                            reader.GetOrdinal(
                                "Codigo"
                            )
                        ),

                    Nombre =
                        reader.GetString(
                            reader.GetOrdinal(
                                "Nombre"
                            )
                        ),

                    Abreviatura =
                        reader.GetString(
                            reader.GetOrdinal(
                                "Abreviatura"
                            )
                        ),

                    Estado =
                        reader.GetBoolean(
                            reader.GetOrdinal(
                                "Estado"
                            )
                        ),

                    TotalProductos =
                        Convert.ToInt32(
                            reader[
                                "TotalProductos"
                            ]
                        )
                }
            );
        }


        return items;
    }


    public async Task<int> CrearUnidadAsync(
        UnidadMedidaGuardarRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            request
        );


        string codigo =
            NormalizarCodigo(
                request.Codigo
            );

        string nombre =
            NormalizarTexto(
                request.Nombre
            );

        string abreviatura =
            NormalizarTexto(
                request.Abreviatura
            );


        ValidarUnidad(
            codigo,
            nombre,
            abreviatura
        );


        const string sql =
            """
            IF EXISTS
            (
                SELECT 1
                FROM inv.unidad_medida
                WHERE
                    LOWER(LTRIM(RTRIM(codigo))) =
                    LOWER(LTRIM(RTRIM(@codigo)))
            )
            BEGIN
                THROW 65101,
                    'Ya existe una unidad de medida con ese código.',
                    1;
            END;


            INSERT INTO inv.unidad_medida
            (
                codigo,
                nombre,
                abreviatura,
                estado
            )
            OUTPUT
                INSERTED.id_unidad_medida
            VALUES
            (
                @codigo,
                @nombre,
                @abreviatura,
                @estado
            );
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
            "@codigo",
            SqlDbType.NVarChar,
            40
        ).Value =
            codigo;


        command.Parameters.Add(
            "@nombre",
            SqlDbType.NVarChar,
            200
        ).Value =
            nombre;


        command.Parameters.Add(
            "@abreviatura",
            SqlDbType.NVarChar,
            40
        ).Value =
            abreviatura;


        command.Parameters.Add(
            "@estado",
            SqlDbType.Bit
        ).Value =
            request.Estado;


        object? id =
            await command.ExecuteScalarAsync(
                cancellationToken
            );


        int idUnidad =
            Convert.ToInt32(
                id
            );


        _logger.LogInformation(
            "[CATALOGOS] Unidad creada. Unidad={Unidad} Codigo={Codigo}",
            idUnidad,
            codigo
        );


        return idUnidad;
    }


    public async Task ActualizarUnidadAsync(
        UnidadMedidaActualizarRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            request
        );


        string codigo =
            NormalizarCodigo(
                request.Codigo
            );

        string nombre =
            NormalizarTexto(
                request.Nombre
            );

        string abreviatura =
            NormalizarTexto(
                request.Abreviatura
            );


        ValidarUnidad(
            codigo,
            nombre,
            abreviatura
        );


        const string sql =
            """
            IF NOT EXISTS
            (
                SELECT 1
                FROM inv.unidad_medida
                WHERE
                    id_unidad_medida =
                    @id_unidad_medida
            )
            BEGIN
                THROW 65102,
                    'La unidad de medida no existe.',
                    1;
            END;


            IF EXISTS
            (
                SELECT 1
                FROM inv.unidad_medida
                WHERE
                    id_unidad_medida <>
                    @id_unidad_medida
                    AND
                    LOWER(LTRIM(RTRIM(codigo))) =
                    LOWER(LTRIM(RTRIM(@codigo)))
            )
            BEGIN
                THROW 65103,
                    'Ya existe otra unidad de medida con ese código.',
                    1;
            END;


            UPDATE inv.unidad_medida
            SET
                codigo =
                    @codigo,
                nombre =
                    @nombre,
                abreviatura =
                    @abreviatura,
                estado =
                    @estado
            WHERE
                id_unidad_medida =
                @id_unidad_medida;
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
            "@id_unidad_medida",
            SqlDbType.Int
        ).Value =
            request.IdUnidadMedida;


        command.Parameters.Add(
            "@codigo",
            SqlDbType.NVarChar,
            40
        ).Value =
            codigo;


        command.Parameters.Add(
            "@nombre",
            SqlDbType.NVarChar,
            200
        ).Value =
            nombre;


        command.Parameters.Add(
            "@abreviatura",
            SqlDbType.NVarChar,
            40
        ).Value =
            abreviatura;


        command.Parameters.Add(
            "@estado",
            SqlDbType.Bit
        ).Value =
            request.Estado;


        await command.ExecuteNonQueryAsync(
            cancellationToken
        );
    }


    public async Task CambiarEstadoUnidadAsync(
        int idUnidadMedida,
        bool estado,
        CancellationToken cancellationToken = default)
    {
        const string sql =
            """
            UPDATE inv.unidad_medida
            SET
                estado =
                @estado
            WHERE
                id_unidad_medida =
                @id_unidad_medida;


            IF @@ROWCOUNT = 0
            BEGIN
                THROW 65104,
                    'La unidad de medida no existe.',
                    1;
            END;
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
            "@id_unidad_medida",
            SqlDbType.Int
        ).Value =
            idUnidadMedida;


        command.Parameters.Add(
            "@estado",
            SqlDbType.Bit
        ).Value =
            estado;


        await command.ExecuteNonQueryAsync(
            cancellationToken
        );
    }



    // =========================================================
    // MARCAS
    // =========================================================

    public async Task<IReadOnlyList<MarcaAdminItem>>
        ListarMarcasAsync(
            int idEmpresa,
            string? buscar,
            bool? estado,
            CancellationToken cancellationToken = default)
    {
        List<MarcaAdminItem> items =
            new();


        const string sql =
            """
            SELECT
                m.id_marca AS IdMarca,
                m.id_empresa AS IdEmpresa,
                m.nombre AS Nombre,
                m.descripcion AS Descripcion,
                m.estado AS Estado,
                m.fecha_registro AS FechaRegistro,
                COUNT(p.id_producto) AS TotalProductos

            FROM inv.marca m

            LEFT JOIN inv.producto p
                ON p.id_marca =
                   m.id_marca
               AND p.id_empresa =
                   m.id_empresa

            WHERE
                m.id_empresa =
                @id_empresa

                AND
                (
                    @buscar IS NULL
                    OR
                    m.nombre LIKE
                        N'%' + @buscar + N'%'
                    OR
                    m.descripcion LIKE
                        N'%' + @buscar + N'%'
                )

                AND
                (
                    @estado IS NULL
                    OR
                    m.estado =
                    @estado
                )

            GROUP BY
                m.id_marca,
                m.id_empresa,
                m.nombre,
                m.descripcion,
                m.estado,
                m.fecha_registro

            ORDER BY
                m.estado DESC,
                m.nombre;
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
            "@buscar",
            SqlDbType.NVarChar,
            300
        ).Value =
            string.IsNullOrWhiteSpace(
                buscar
            )
                ? DBNull.Value
                : buscar.Trim();


        command.Parameters.Add(
            "@estado",
            SqlDbType.Bit
        ).Value =
            estado.HasValue
                ? estado.Value
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
            items.Add(
                new MarcaAdminItem
                {
                    IdMarca =
                        reader.GetInt32(
                            reader.GetOrdinal(
                                "IdMarca"
                            )
                        ),

                    IdEmpresa =
                        reader.GetInt32(
                            reader.GetOrdinal(
                                "IdEmpresa"
                            )
                        ),

                    Nombre =
                        reader.GetString(
                            reader.GetOrdinal(
                                "Nombre"
                            )
                        ),

                    Descripcion =
                        reader.IsDBNull(
                            reader.GetOrdinal(
                                "Descripcion"
                            )
                        )
                            ? null
                            : reader.GetString(
                                reader.GetOrdinal(
                                    "Descripcion"
                                )
                            ),

                    Estado =
                        reader.GetBoolean(
                            reader.GetOrdinal(
                                "Estado"
                            )
                        ),

                    FechaRegistro =
                        reader.GetDateTime(
                            reader.GetOrdinal(
                                "FechaRegistro"
                            )
                        ),

                    TotalProductos =
                        Convert.ToInt32(
                            reader[
                                "TotalProductos"
                            ]
                        )
                }
            );
        }


        return items;
    }


    public async Task<int> CrearMarcaAsync(
        MarcaGuardarRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            request
        );


        string nombre =
            NormalizarTexto(
                request.Nombre
            );


        if (request.IdEmpresa <= 0)
        {
            throw new ArgumentException(
                "La empresa es obligatoria."
            );
        }


        if (
            string.IsNullOrWhiteSpace(
                nombre
            )
        )
        {
            throw new ArgumentException(
                "El nombre de la marca es obligatorio."
            );
        }


        if (nombre.Length > 150)
        {
            throw new ArgumentException(
                "El nombre de la marca no puede superar 150 caracteres."
            );
        }


        const string sql =
            """
            IF EXISTS
            (
                SELECT 1
                FROM inv.marca
                WHERE
                    id_empresa =
                    @id_empresa
                    AND
                    LOWER(LTRIM(RTRIM(nombre))) =
                    LOWER(LTRIM(RTRIM(@nombre)))
            )
            BEGIN
                THROW 65201,
                    'Ya existe una marca con ese nombre para la empresa.',
                    1;
            END;


            INSERT INTO inv.marca
            (
                id_empresa,
                nombre,
                descripcion,
                estado,
                fecha_registro
            )
            OUTPUT
                INSERTED.id_marca
            VALUES
            (
                @id_empresa,
                @nombre,
                @descripcion,
                @estado,
                SYSDATETIME()
            );
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
            request.IdEmpresa;


        command.Parameters.Add(
            "@nombre",
            SqlDbType.NVarChar,
            300
        ).Value =
            nombre;


        command.Parameters.Add(
            "@descripcion",
            SqlDbType.NVarChar,
            600
        ).Value =
            string.IsNullOrWhiteSpace(
                request.Descripcion
            )
                ? DBNull.Value
                : request.Descripcion.Trim();


        command.Parameters.Add(
            "@estado",
            SqlDbType.Bit
        ).Value =
            request.Estado;


        object? id =
            await command.ExecuteScalarAsync(
                cancellationToken
            );


        int idMarca =
            Convert.ToInt32(
                id
            );


        _logger.LogInformation(
            "[CATALOGOS] Marca creada. Empresa={Empresa} Marca={Marca} Nombre={Nombre}",
            request.IdEmpresa,
            idMarca,
            nombre
        );


        return idMarca;
    }


    public async Task ActualizarMarcaAsync(
        MarcaActualizarRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            request
        );


        string nombre =
            NormalizarTexto(
                request.Nombre
            );


        if (
            request.IdMarca <= 0 ||
            request.IdEmpresa <= 0
        )
        {
            throw new ArgumentException(
                "Marca inválida."
            );
        }


        if (
            string.IsNullOrWhiteSpace(
                nombre
            )
        )
        {
            throw new ArgumentException(
                "El nombre de la marca es obligatorio."
            );
        }


        if (nombre.Length > 150)
        {
            throw new ArgumentException(
                "El nombre de la marca no puede superar 150 caracteres."
            );
        }


        const string sql =
            """
            IF NOT EXISTS
            (
                SELECT 1
                FROM inv.marca
                WHERE
                    id_marca =
                    @id_marca
                    AND
                    id_empresa =
                    @id_empresa
            )
            BEGIN
                THROW 65202,
                    'La marca no existe o no pertenece a la empresa.',
                    1;
            END;


            IF EXISTS
            (
                SELECT 1
                FROM inv.marca
                WHERE
                    id_empresa =
                    @id_empresa
                    AND
                    id_marca <>
                    @id_marca
                    AND
                    LOWER(LTRIM(RTRIM(nombre))) =
                    LOWER(LTRIM(RTRIM(@nombre)))
            )
            BEGIN
                THROW 65203,
                    'Ya existe otra marca con ese nombre.',
                    1;
            END;


            UPDATE inv.marca
            SET
                nombre =
                    @nombre,
                descripcion =
                    @descripcion,
                estado =
                    @estado
            WHERE
                id_marca =
                @id_marca
                AND
                id_empresa =
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
            "@id_marca",
            SqlDbType.Int
        ).Value =
            request.IdMarca;


        command.Parameters.Add(
            "@id_empresa",
            SqlDbType.Int
        ).Value =
            request.IdEmpresa;


        command.Parameters.Add(
            "@nombre",
            SqlDbType.NVarChar,
            300
        ).Value =
            nombre;


        command.Parameters.Add(
            "@descripcion",
            SqlDbType.NVarChar,
            600
        ).Value =
            string.IsNullOrWhiteSpace(
                request.Descripcion
            )
                ? DBNull.Value
                : request.Descripcion.Trim();


        command.Parameters.Add(
            "@estado",
            SqlDbType.Bit
        ).Value =
            request.Estado;


        await command.ExecuteNonQueryAsync(
            cancellationToken
        );


        _logger.LogInformation(
            "[CATALOGOS] Marca actualizada. Marca={Marca} Empresa={Empresa}",
            request.IdMarca,
            request.IdEmpresa
        );
    }


    public async Task CambiarEstadoMarcaAsync(
        int idMarca,
        int idEmpresa,
        bool estado,
        CancellationToken cancellationToken = default)
    {
        if (
            idMarca <= 0 ||
            idEmpresa <= 0
        )
        {
            throw new ArgumentException(
                "Marca inválida."
            );
        }


        const string sql =
            """
            UPDATE inv.marca
            SET
                estado =
                @estado
            WHERE
                id_marca =
                @id_marca
                AND
                id_empresa =
                @id_empresa;


            IF @@ROWCOUNT = 0
            BEGIN
                THROW 65204,
                    'La marca no existe o no pertenece a la empresa.',
                    1;
            END;
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
            "@id_marca",
            SqlDbType.Int
        ).Value =
            idMarca;


        command.Parameters.Add(
            "@id_empresa",
            SqlDbType.Int
        ).Value =
            idEmpresa;


        command.Parameters.Add(
            "@estado",
            SqlDbType.Bit
        ).Value =
            estado;


        await command.ExecuteNonQueryAsync(
            cancellationToken
        );


        _logger.LogInformation(
            "[CATALOGOS] Estado de marca actualizado. Marca={Marca} Empresa={Empresa} Estado={Estado}",
            idMarca,
            idEmpresa,
            estado
        );
    }


    // =========================================================
    // HELPERS
    // =========================================================

    private static string NormalizarTexto(
        string? valor)
    {
        return
            (valor ?? string.Empty)
            .Trim();
    }


    private static string NormalizarCodigo(
        string? valor)
    {
        return
            (valor ?? string.Empty)
            .Trim()
            .ToLowerInvariant();
    }


    private static void ValidarUnidad(
        string codigo,
        string nombre,
        string abreviatura)
    {
        if (
            string.IsNullOrWhiteSpace(
                codigo
            )
        )
        {
            throw new ArgumentException(
                "El código es obligatorio."
            );
        }


        if (
            string.IsNullOrWhiteSpace(
                nombre
            )
        )
        {
            throw new ArgumentException(
                "El nombre es obligatorio."
            );
        }


        if (
            string.IsNullOrWhiteSpace(
                abreviatura
            )
        )
        {
            throw new ArgumentException(
                "La abreviatura es obligatoria."
            );
        }


        if (codigo.Length > 40)
        {
            throw new ArgumentException(
                "El código no puede superar 40 caracteres."
            );
        }


        if (nombre.Length > 200)
        {
            throw new ArgumentException(
                "El nombre no puede superar 200 caracteres."
            );
        }


        if (abreviatura.Length > 40)
        {
            throw new ArgumentException(
                "La abreviatura no puede superar 40 caracteres."
            );
        }
    }
}
