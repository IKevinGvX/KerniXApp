using System.Data;

using KerniX.Models.Inventario.EntradaStock;

using Microsoft.Data.SqlClient;

namespace KerniX.Services.Inventario;

public sealed class EntradaStockDbService
    : IEntradaStockDbService
{
    private readonly string _connectionString;

    private readonly ILogger<EntradaStockDbService>
        _logger;


    public EntradaStockDbService(
        IConfiguration configuration,
        ILogger<EntradaStockDbService> logger)
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
    // CATÁLOGOS
    // =========================================================

    public async Task<EntradaStockCatalogos>
        ObtenerCatalogosAsync(
            int idEmpresa,
            int? idSucursal = null,
            CancellationToken cancellationToken = default)
    {
        if (idEmpresa <= 0)
        {
            throw new ArgumentException(
                "La empresa es obligatoria."
            );
        }


        EntradaStockCatalogos resultado =
            new();


        await using SqlConnection conexion =
            CrearConexion();

        await using SqlCommand command =
            new(
                "inv.sp_entrada_stock_catalogos",
                conexion
            );

        command.CommandType =
            CommandType.StoredProcedure;

        command.CommandTimeout =
            60;


        Agregar(
            command,
            "@id_empresa",
            SqlDbType.Int,
            idEmpresa
        );

        AgregarNullable(
            command,
            "@id_sucursal",
            SqlDbType.Int,
            idSucursal
        );


        await conexion.OpenAsync(
            cancellationToken
        );

        await using SqlDataReader reader =
            await command.ExecuteReaderAsync(
                cancellationToken
            );


        // =====================================================
        // RESULTSET 1 - ALMACENES
        // =====================================================

        while (
            await reader.ReadAsync(
                cancellationToken
            )
        )
        {
            resultado.Almacenes.Add(
                new EntradaStockAlmacenItem
                {
                    IdAlmacen =
                        Int(
                            reader,
                            "IdAlmacen"
                        ),

                    IdSucursal =
                        Int(
                            reader,
                            "IdSucursal"
                        ),

                    IdEmpresa =
                        Int(
                            reader,
                            "IdEmpresa"
                        ),

                    Sucursal =
                        Texto(
                            reader,
                            "Sucursal"
                        ),

                    Codigo =
                        Texto(
                            reader,
                            "Codigo"
                        ),

                    Nombre =
                        Texto(
                            reader,
                            "Nombre"
                        ),

                    Ubicacion =
                        TextoNullable(
                            reader,
                            "Ubicacion"
                        )
                }
            );
        }


        // =====================================================
        // RESULTSET 2 - PRODUCTOS
        // =====================================================

        if (
            await reader.NextResultAsync(
                cancellationToken
            )
        )
        {
            while (
                await reader.ReadAsync(
                    cancellationToken
                )
            )
            {
                resultado.Productos.Add(
                    new EntradaStockProductoItem
                    {
                        IdProducto =
                            Int(
                                reader,
                                "IdProducto"
                            ),

                        Codigo =
                            Texto(
                                reader,
                                "Codigo"
                            ),

                        Nombre =
                            Texto(
                                reader,
                                "Nombre"
                            ),

                        IdCategoriaProducto =
                            Int(
                                reader,
                                "IdCategoriaProducto"
                            ),

                        IdMarca =
                            NullableInt(
                                reader,
                                "IdMarca"
                            ),

                        IdUnidadMedida =
                            Int(
                                reader,
                                "IdUnidadMedida"
                            ),

                        PrecioCompra =
                            Decimal(
                                reader,
                                "PrecioCompra"
                            ),

                        PrecioVenta =
                            Decimal(
                                reader,
                                "PrecioVenta"
                            ),

                        StockMinimo =
                            Decimal(
                                reader,
                                "StockMinimo"
                            ),

                        StockMaximo =
                            NullableDecimal(
                                reader,
                                "StockMaximo"
                            ),

                        ControlaStock =
                            Bool(
                                reader,
                                "ControlaStock"
                            )
                    }
                );
            }
        }


        // =====================================================
        // RESULTSET 3 - TIPOS DE MOVIMIENTO
        // =====================================================

        if (
            await reader.NextResultAsync(
                cancellationToken
            )
        )
        {
            while (
                await reader.ReadAsync(
                    cancellationToken
                )
            )
            {
                resultado.TiposMovimiento.Add(
                    new EntradaStockTipoMovimientoItem
                    {
                        IdTipoMovimiento =
                            Int(
                                reader,
                                "IdTipoMovimiento"
                            ),

                        Codigo =
                            Texto(
                                reader,
                                "Codigo"
                            ),

                        Nombre =
                            Texto(
                                reader,
                                "Nombre"
                            ),

                        Naturaleza =
                            Texto(
                                reader,
                                "Naturaleza"
                            ),

                        Descripcion =
                            TextoNullable(
                                reader,
                                "Descripcion"
                            )
                    }
                );
            }
        }


        _logger.LogInformation(
            "[ENTRADA STOCK] Catálogos cargados. Empresa={Empresa} Sucursal={Sucursal} Almacenes={Almacenes} Productos={Productos} Tipos={Tipos}",
            idEmpresa,
            idSucursal,
            resultado.Almacenes.Count,
            resultado.Productos.Count,
            resultado.TiposMovimiento.Count
        );


        return resultado;
    }


    // =========================================================
    // REGISTRAR
    // =========================================================

    public async Task<EntradaStockResultado>
        RegistrarAsync(
            EntradaStockGuardarRequest request,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            request
        );

        ValidarRegistro(
            request
        );


        await using SqlConnection conexion =
            CrearConexion();

        await using SqlCommand command =
            new(
                "inv.sp_entrada_stock_registrar",
                conexion
            );

        command.CommandType =
            CommandType.StoredProcedure;

        command.CommandTimeout =
            60;


        Agregar(
            command,
            "@id_empresa",
            SqlDbType.Int,
            request.IdEmpresa
        );

        Agregar(
            command,
            "@id_sucursal",
            SqlDbType.Int,
            request.IdSucursal
        );

        Agregar(
            command,
            "@id_almacen",
            SqlDbType.Int,
            request.IdAlmacen
        );

        Agregar(
            command,
            "@id_producto",
            SqlDbType.Int,
            request.IdProducto
        );

        Agregar(
            command,
            "@id_empleado",
            SqlDbType.Int,
            request.IdEmpleado
        );


        SqlParameter cantidad =
            command.Parameters.Add(
                "@cantidad",
                SqlDbType.Decimal
            );

        cantidad.Precision =
            18;

        cantidad.Scale =
            4;

        cantidad.Value =
            request.Cantidad;


        AgregarTextoNullable(
            command,
            "@referencia",
            request.Referencia,
            100
        );

        AgregarTextoNullable(
            command,
            "@motivo",
            request.Motivo,
            300
        );

        Agregar(
            command,
            "@codigo_movimiento",
            SqlDbType.NVarChar,
            NormalizarCodigoMovimiento(
                request.CodigoMovimiento
            ),
            50
        );


        await conexion.OpenAsync(
            cancellationToken
        );

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
            throw new InvalidOperationException(
                "SQL Server no devolvió el resultado de la entrada de stock."
            );
        }


        EntradaStockResultado resultado =
            MapearResultado(
                reader
            );


        if (
            resultado.IdMovimientoInventario <= 0
        )
        {
            throw new InvalidOperationException(
                "La entrada fue ejecutada pero no devolvió un movimiento válido."
            );
        }


        _logger.LogInformation(
            "[ENTRADA STOCK] Registrada. Movimiento={Movimiento} Numero={Numero} Empresa={Empresa} Sucursal={Sucursal} Almacen={Almacen} Producto={Producto} Cantidad={Cantidad} StockAnterior={Anterior} StockNuevo={Nuevo} Empleado={Empleado}",
            resultado.IdMovimientoInventario,
            resultado.NumeroEntrada,
            resultado.IdEmpresa,
            resultado.IdSucursal,
            resultado.IdAlmacen,
            resultado.IdProducto,
            resultado.Cantidad,
            resultado.StockAnterior,
            resultado.StockNuevo,
            resultado.IdEmpleado
        );


        return resultado;
    }


    // =========================================================
    // LISTAR
    // =========================================================

    public async Task<EntradaStockListaResultado>
        ListarAsync(
            EntradaStockFiltro filtro,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(
            filtro
        );

        if (filtro.IdEmpresa <= 0)
        {
            throw new ArgumentException(
                "La empresa es obligatoria."
            );
        }


        filtro.Pagina =
            filtro.Pagina <= 0
                ? 1
                : filtro.Pagina;

        filtro.TamanoPagina =
            filtro.TamanoPagina <= 0
                ? 20
                : Math.Min(
                    filtro.TamanoPagina,
                    100
                );


        EntradaStockListaResultado resultado =
            new()
            {
                Pagina =
                    filtro.Pagina,

                TamanoPagina =
                    filtro.TamanoPagina
            };


        await using SqlConnection conexion =
            CrearConexion();

        await using SqlCommand command =
            new(
                "inv.sp_entrada_stock_listar",
                conexion
            );

        command.CommandType =
            CommandType.StoredProcedure;

        command.CommandTimeout =
            60;


        Agregar(
            command,
            "@id_empresa",
            SqlDbType.Int,
            filtro.IdEmpresa
        );

        AgregarNullable(
            command,
            "@id_sucursal",
            SqlDbType.Int,
            filtro.IdSucursal
        );

        AgregarNullable(
            command,
            "@id_almacen",
            SqlDbType.Int,
            filtro.IdAlmacen
        );

        AgregarNullable(
            command,
            "@id_producto",
            SqlDbType.Int,
            filtro.IdProducto
        );

        AgregarFechaNullable(
            command,
            "@fecha_desde",
            filtro.FechaDesde
        );

        AgregarFechaNullable(
            command,
            "@fecha_hasta",
            filtro.FechaHasta
        );

        AgregarTextoNullable(
            command,
            "@buscar",
            filtro.Buscar,
            200
        );

        Agregar(
            command,
            "@pagina",
            SqlDbType.Int,
            filtro.Pagina
        );

        Agregar(
            command,
            "@tamano_pagina",
            SqlDbType.Int,
            filtro.TamanoPagina
        );


        await conexion.OpenAsync(
            cancellationToken
        );

        await using SqlDataReader reader =
            await command.ExecuteReaderAsync(
                cancellationToken
            );


        // =====================================================
        // RESULTSET 1 - ITEMS
        // =====================================================

        while (
            await reader.ReadAsync(
                cancellationToken
            )
        )
        {
            resultado.Items.Add(
                new EntradaStockListaItem
                {
                    IdMovimientoInventario =
                        Long(
                            reader,
                            "IdMovimientoInventario"
                        ),

                    NumeroEntrada =
                        Texto(
                            reader,
                            "NumeroEntrada"
                        ),

                    FechaMovimiento =
                        Fecha(
                            reader,
                            "FechaMovimiento"
                        ),

                    IdSucursal =
                        Int(
                            reader,
                            "IdSucursal"
                        ),

                    Sucursal =
                        Texto(
                            reader,
                            "Sucursal"
                        ),

                    IdAlmacen =
                        Int(
                            reader,
                            "IdAlmacen"
                        ),

                    Almacen =
                        Texto(
                            reader,
                            "Almacen"
                        ),

                    IdProducto =
                        Int(
                            reader,
                            "IdProducto"
                        ),

                    CodigoProducto =
                        Texto(
                            reader,
                            "CodigoProducto"
                        ),

                    Producto =
                        Texto(
                            reader,
                            "Producto"
                        ),

                    CodigoMovimiento =
                        Texto(
                            reader,
                            "CodigoMovimiento"
                        ),

                    TipoMovimiento =
                        Texto(
                            reader,
                            "TipoMovimiento"
                        ),

                    Cantidad =
                        Decimal(
                            reader,
                            "Cantidad"
                        ),

                    StockAnterior =
                        Decimal(
                            reader,
                            "StockAnterior"
                        ),

                    StockNuevo =
                        Decimal(
                            reader,
                            "StockNuevo"
                        ),

                    Referencia =
                        TextoNullable(
                            reader,
                            "Referencia"
                        ),

                    Motivo =
                        TextoNullable(
                            reader,
                            "Motivo"
                        ),

                    IdEmpleado =
                        Int(
                            reader,
                            "IdEmpleado"
                        )
                }
            );
        }


        // =====================================================
        // RESULTSET 2 - TOTAL
        // =====================================================

        if (
            await reader.NextResultAsync(
                cancellationToken
            )
            &&
            await reader.ReadAsync(
                cancellationToken
            )
        )
        {
            resultado.TotalRegistros =
                Long(
                    reader,
                    "TotalRegistros"
                );
        }


        return resultado;
    }


    // =========================================================
    // OBTENER DETALLE
    // =========================================================

    public async Task<EntradaStockResultado?>
        ObtenerAsync(
            long idMovimientoInventario,
            int idEmpresa,
            CancellationToken cancellationToken = default)
    {
        if (idMovimientoInventario <= 0)
        {
            throw new ArgumentException(
                "La entrada es obligatoria."
            );
        }

        if (idEmpresa <= 0)
        {
            throw new ArgumentException(
                "La empresa es obligatoria."
            );
        }


        await using SqlConnection conexion =
            CrearConexion();

        await using SqlCommand command =
            new(
                "inv.sp_entrada_stock_obtener",
                conexion
            );

        command.CommandType =
            CommandType.StoredProcedure;

        command.CommandTimeout =
            60;


        Agregar(
            command,
            "@id_movimiento_inventario",
            SqlDbType.BigInt,
            idMovimientoInventario
        );

        Agregar(
            command,
            "@id_empresa",
            SqlDbType.Int,
            idEmpresa
        );


        await conexion.OpenAsync(
            cancellationToken
        );

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
            return null;
        }


        return MapearResultado(
            reader
        );
    }


    // =========================================================
    // RESUMEN
    // =========================================================

    public async Task<EntradaStockResumen>
        ObtenerResumenAsync(
            int idEmpresa,
            int? idSucursal = null,
            int? idAlmacen = null,
            CancellationToken cancellationToken = default)
    {
        if (idEmpresa <= 0)
        {
            throw new ArgumentException(
                "La empresa es obligatoria."
            );
        }


        await using SqlConnection conexion =
            CrearConexion();

        await using SqlCommand command =
            new(
                "inv.sp_entrada_stock_resumen",
                conexion
            );

        command.CommandType =
            CommandType.StoredProcedure;

        command.CommandTimeout =
            60;


        Agregar(
            command,
            "@id_empresa",
            SqlDbType.Int,
            idEmpresa
        );

        AgregarNullable(
            command,
            "@id_sucursal",
            SqlDbType.Int,
            idSucursal
        );

        AgregarNullable(
            command,
            "@id_almacen",
            SqlDbType.Int,
            idAlmacen
        );


        await conexion.OpenAsync(
            cancellationToken
        );

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
            return new EntradaStockResumen();
        }


        return new EntradaStockResumen
        {
            TotalEntradas =
                Long(
                    reader,
                    "TotalEntradas"
                ),

            CantidadIngresada =
                Decimal(
                    reader,
                    "CantidadIngresada"
                ),

            EntradasHoy =
                Int(
                    reader,
                    "EntradasHoy"
                ),

            CantidadHoy =
                Decimal(
                    reader,
                    "CantidadHoy"
                )
        };
    }


    // =========================================================
    // MAPEO RESULTADO
    // =========================================================

    private static EntradaStockResultado
        MapearResultado(
            SqlDataReader reader)
    {
        return new EntradaStockResultado
        {
            IdMovimientoInventario =
                Long(
                    reader,
                    "IdMovimientoInventario"
                ),

            NumeroEntrada =
                Texto(
                    reader,
                    "NumeroEntrada"
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

            Sucursal =
                TextoNullable(
                    reader,
                    "Sucursal"
                ),

            IdAlmacen =
                Int(
                    reader,
                    "IdAlmacen"
                ),

            CodigoAlmacen =
                Texto(
                    reader,
                    "CodigoAlmacen"
                ),

            Almacen =
                Texto(
                    reader,
                    "Almacen"
                ),

            IdProducto =
                Int(
                    reader,
                    "IdProducto"
                ),

            CodigoProducto =
                Texto(
                    reader,
                    "CodigoProducto"
                ),

            Producto =
                Texto(
                    reader,
                    "Producto"
                ),

            IdUnidadMedida =
                NullableInt(
                    reader,
                    "IdUnidadMedida"
                ),

            IdTipoMovimiento =
                Int(
                    reader,
                    "IdTipoMovimiento"
                ),

            CodigoMovimiento =
                Texto(
                    reader,
                    "CodigoMovimiento"
                ),

            TipoMovimiento =
                Texto(
                    reader,
                    "TipoMovimiento"
                ),

            Naturaleza =
                TextoNullable(
                    reader,
                    "Naturaleza"
                ),

            IdEmpleado =
                Int(
                    reader,
                    "IdEmpleado"
                ),

            Cantidad =
                Decimal(
                    reader,
                    "Cantidad"
                ),

            StockAnterior =
                Decimal(
                    reader,
                    "StockAnterior"
                ),

            StockNuevo =
                Decimal(
                    reader,
                    "StockNuevo"
                ),

            Referencia =
                TextoNullable(
                    reader,
                    "Referencia"
                ),

            Motivo =
                TextoNullable(
                    reader,
                    "Motivo"
                ),

            FechaMovimiento =
                Fecha(
                    reader,
                    "FechaMovimiento"
                ),

            CantidadActual =
                Decimal(
                    reader,
                    "CantidadActual"
                ),

            CantidadReservada =
                Decimal(
                    reader,
                    "CantidadReservada"
                ),

            CantidadDisponible =
                Decimal(
                    reader,
                    "CantidadDisponible"
                )
        };
    }


    // =========================================================
    // VALIDACIONES
    // =========================================================

    private static void ValidarRegistro(
        EntradaStockGuardarRequest request)
    {
        if (request.IdEmpresa <= 0)
        {
            throw new ArgumentException(
                "La empresa es obligatoria."
            );
        }

        if (request.IdSucursal <= 0)
        {
            throw new ArgumentException(
                "La sucursal es obligatoria."
            );
        }

        if (request.IdAlmacen <= 0)
        {
            throw new ArgumentException(
                "El almacén es obligatorio."
            );
        }

        if (request.IdProducto <= 0)
        {
            throw new ArgumentException(
                "El producto es obligatorio."
            );
        }

        if (request.IdEmpleado <= 0)
        {
            throw new ArgumentException(
                "No se pudo identificar al empleado."
            );
        }

        if (request.Cantidad <= 0)
        {
            throw new ArgumentException(
                "La cantidad debe ser mayor que cero."
            );
        }

        if (
            request.Referencia?.Length > 100
        )
        {
            throw new ArgumentException(
                "La referencia no puede superar los 100 caracteres."
            );
        }

        if (
            request.Motivo?.Length > 300
        )
        {
            throw new ArgumentException(
                "El motivo no puede superar los 300 caracteres."
            );
        }
    }


    // =========================================================
    // CONEXIÓN
    // =========================================================

    private SqlConnection CrearConexion()
    {
        return new SqlConnection(
            _connectionString
        );
    }


    // =========================================================
    // PARÁMETROS
    // =========================================================

    private static void Agregar(
        SqlCommand command,
        string nombre,
        SqlDbType tipo,
        object valor,
        int? longitud = null)
    {
        SqlParameter parametro =
            longitud.HasValue
                ? command.Parameters.Add(
                    nombre,
                    tipo,
                    longitud.Value
                )
                : command.Parameters.Add(
                    nombre,
                    tipo
                );

        parametro.Value =
            valor;
    }


    private static void AgregarNullable(
        SqlCommand command,
        string nombre,
        SqlDbType tipo,
        object? valor)
    {
        SqlParameter parametro =
            command.Parameters.Add(
                nombre,
                tipo
            );

        parametro.Value =
            valor
            ?? DBNull.Value;
    }


    private static void AgregarTextoNullable(
        SqlCommand command,
        string nombre,
        string? valor,
        int longitud)
    {
        SqlParameter parametro =
            command.Parameters.Add(
                nombre,
                SqlDbType.NVarChar,
                longitud
            );

        parametro.Value =
            string.IsNullOrWhiteSpace(
                valor
            )
                ? DBNull.Value
                : valor.Trim();
    }


    private static void AgregarFechaNullable(
        SqlCommand command,
        string nombre,
        DateTime? valor)
    {
        SqlParameter parametro =
            command.Parameters.Add(
                nombre,
                SqlDbType.Date
            );

        parametro.Value =
            valor.HasValue
                ? valor.Value.Date
                : DBNull.Value;
    }


    private static string NormalizarCodigoMovimiento(
        string? valor)
    {
        return string.IsNullOrWhiteSpace(
            valor
        )
            ? "entrada"
            : valor.Trim().ToLowerInvariant();
    }


    // =========================================================
    // LECTURA SEGURA
    // =========================================================

    private static int Int(
        SqlDataReader reader,
        string nombre)
    {
        int indice =
            BuscarIndice(
                reader,
                nombre
            );

        return indice < 0 ||
               reader.IsDBNull(indice)
            ? 0
            : Convert.ToInt32(
                reader.GetValue(
                    indice
                )
            );
    }


    private static int? NullableInt(
        SqlDataReader reader,
        string nombre)
    {
        int indice =
            BuscarIndice(
                reader,
                nombre
            );

        return indice < 0 ||
               reader.IsDBNull(indice)
            ? null
            : Convert.ToInt32(
                reader.GetValue(
                    indice
                )
            );
    }


    private static long Long(
        SqlDataReader reader,
        string nombre)
    {
        int indice =
            BuscarIndice(
                reader,
                nombre
            );

        return indice < 0 ||
               reader.IsDBNull(indice)
            ? 0L
            : Convert.ToInt64(
                reader.GetValue(
                    indice
                )
            );
    }


    private static decimal Decimal(
        SqlDataReader reader,
        string nombre)
    {
        int indice =
            BuscarIndice(
                reader,
                nombre
            );

        return indice < 0 ||
               reader.IsDBNull(indice)
            ? 0M
            : Convert.ToDecimal(
                reader.GetValue(
                    indice
                )
            );
    }


    private static decimal? NullableDecimal(
        SqlDataReader reader,
        string nombre)
    {
        int indice =
            BuscarIndice(
                reader,
                nombre
            );

        return indice < 0 ||
               reader.IsDBNull(indice)
            ? null
            : Convert.ToDecimal(
                reader.GetValue(
                    indice
                )
            );
    }


    private static bool Bool(
        SqlDataReader reader,
        string nombre)
    {
        int indice =
            BuscarIndice(
                reader,
                nombre
            );

        return indice >= 0 &&
               !reader.IsDBNull(indice) &&
               Convert.ToBoolean(
                   reader.GetValue(
                       indice
                   )
               );
    }


    private static string Texto(
        SqlDataReader reader,
        string nombre)
    {
        return TextoNullable(
            reader,
            nombre
        )
        ?? string.Empty;
    }


    private static string? TextoNullable(
        SqlDataReader reader,
        string nombre)
    {
        int indice =
            BuscarIndice(
                reader,
                nombre
            );

        return indice < 0 ||
               reader.IsDBNull(indice)
            ? null
            : Convert.ToString(
                reader.GetValue(
                    indice
                )
            );
    }


    private static DateTime Fecha(
        SqlDataReader reader,
        string nombre)
    {
        int indice =
            BuscarIndice(
                reader,
                nombre
            );

        return indice < 0 ||
               reader.IsDBNull(indice)
            ? DateTime.MinValue
            : Convert.ToDateTime(
                reader.GetValue(
                    indice
                )
            );
    }


    private static int BuscarIndice(
        SqlDataReader reader,
        string nombre)
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
                    nombre,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return i;
            }
        }

        return -1;
    }
}
