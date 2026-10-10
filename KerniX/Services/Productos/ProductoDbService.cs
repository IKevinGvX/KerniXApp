using System.Data;

using KerniX.Models.Inventario.Producto;

using Microsoft.Data.SqlClient;

namespace KerniX.Services.Inventario;

public sealed class ProductoDbService
    : IProductoDbService
{
    private readonly string _connectionString;
    private readonly ILogger<ProductoDbService> _logger;


    public ProductoDbService(
        IConfiguration configuration,
        ILogger<ProductoDbService> logger)
    {
        _connectionString =
            configuration.GetConnectionString("KerniXDb")
            ?? throw new InvalidOperationException(
                "No existe ConnectionStrings:KerniXDb."
            );

        _logger = logger;
    }


    // =========================================================
    // LISTAR PRODUCTOS
    // =========================================================

    public async Task<ProductoListaResultado> ListarAsync(
        ProductoFiltro filtro,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filtro);

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
                    200
                );

        ProductoListaResultado resultado =
            new()
            {
                Pagina = filtro.Pagina,
                TamanoPagina = filtro.TamanoPagina
            };

        await using SqlConnection conexion =
            CrearConexion();

        await using SqlCommand command =
            new(
                "inv.sp_producto_listar",
                conexion
            );

        command.CommandType =
            CommandType.StoredProcedure;

        command.CommandTimeout = 60;


        Agregar(
            command,
            "@id_empresa",
            SqlDbType.Int,
            filtro.IdEmpresa
        );

        AgregarNullable(
            command,
            "@id_categoria_producto",
            SqlDbType.Int,
            filtro.IdCategoriaProducto
        );

        AgregarNullable(
            command,
            "@id_marca",
            SqlDbType.Int,
            filtro.IdMarca
        );

        AgregarNullable(
            command,
            "@id_unidad_medida",
            SqlDbType.Int,
            filtro.IdUnidadMedida
        );

        AgregarNullable(
            command,
            "@id_almacen",
            SqlDbType.Int,
            filtro.IdAlmacen
        );

        AgregarNullable(
            command,
            "@estado",
            SqlDbType.Bit,
            filtro.Estado
        );

        AgregarNullable(
            command,
            "@controla_stock",
            SqlDbType.Bit,
            filtro.ControlaStock
        );

        AgregarTextoNullable(
            command,
            "@estado_stock",
            filtro.EstadoStock,
            30
        );


        SqlParameter precioDesde =
            command.Parameters.Add(
                "@precio_desde",
                SqlDbType.Decimal
            );

        precioDesde.Precision = 18;
        precioDesde.Scale = 2;

        precioDesde.Value =
            filtro.PrecioDesde.HasValue
                ? filtro.PrecioDesde.Value
                : DBNull.Value;


        SqlParameter precioHasta =
            command.Parameters.Add(
                "@precio_hasta",
                SqlDbType.Decimal
            );

        precioHasta.Precision = 18;
        precioHasta.Scale = 2;

        precioHasta.Value =
            filtro.PrecioHasta.HasValue
                ? filtro.PrecioHasta.Value
                : DBNull.Value;


        AgregarTextoNullable(
            command,
            "@buscar",
            filtro.Buscar,
            200
        );

        Agregar(
            command,
            "@ordenar_por",
            SqlDbType.NVarChar,
            NormalizarOrden(
                filtro.OrdenarPor
            ),
            30
        );

        Agregar(
            command,
            "@direccion_orden",
            SqlDbType.NVarChar,
            NormalizarDireccion(
                filtro.DireccionOrden
            ),
            4
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


        while (
            await reader.ReadAsync(
                cancellationToken
            )
        )
        {
            resultado.Items.Add(
                MapearProductoLista(reader)
            );
        }


        // sp_producto_listar devuelve un segundo resultset
        // con el total de registros.

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
                ObtenerInt32(
                    reader,
                    0
                );
        }
        else
        {
            resultado.TotalRegistros =
                resultado.Items.Count;
        }


        return resultado;
    }


    // =========================================================
    // OBTENER PRODUCTO
    // =========================================================

    public async Task<ProductoDetalle?> ObtenerAsync(
        int idProducto,
        CancellationToken cancellationToken = default)
    {
        if (idProducto <= 0)
        {
            return null;
        }


        await using SqlConnection conexion =
            CrearConexion();

        await using SqlCommand command =
            new(
                "inv.sp_producto_obtener",
                conexion
            );

        command.CommandType =
            CommandType.StoredProcedure;

        command.Parameters.Add(
            "@id_producto",
            SqlDbType.Int
        ).Value =
            idProducto;


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


        return new ProductoDetalle
        {
            IdProducto =
                Int(
                    reader,
                    "IdProducto",
                    "id_producto"
                ),

            IdEmpresa =
                Int(
                    reader,
                    "IdEmpresa",
                    "id_empresa"
                ),

            IdCategoriaProducto =
                Int(
                    reader,
                    "IdCategoriaProducto",
                    "id_categoria_producto"
                ),

            IdMarca =
                NullableInt(
                    reader,
                    "IdMarca",
                    "id_marca"
                ),

            IdUnidadMedida =
                Int(
                    reader,
                    "IdUnidadMedida",
                    "id_unidad_medida"
                ),

            Codigo =
                Texto(
                    reader,
                    "Codigo",
                    "codigo"
                ),

            CodigoBarras =
                TextoNullable(
                    reader,
                    "CodigoBarras",
                    "codigo_barras"
                ),

            Nombre =
                Texto(
                    reader,
                    "Nombre",
                    "nombre"
                ),

            Descripcion =
                TextoNullable(
                    reader,
                    "Descripcion",
                    "descripcion"
                ),

            PrecioCompra =
                Decimal(
                    reader,
                    "PrecioCompra",
                    "precio_compra"
                ),

            PrecioVenta =
                Decimal(
                    reader,
                    "PrecioVenta",
                    "precio_venta"
                ),

            StockMinimo =
                Decimal(
                    reader,
                    "StockMinimo",
                    "stock_minimo"
                ),

            StockMaximo =
                NullableDecimal(
                    reader,
                    "StockMaximo",
                    "stock_maximo"
                ),

            ControlaStock =
                Bool(
                    reader,
                    "ControlaStock",
                    "controla_stock"
                ),

            Estado =
                Bool(
                    reader,
                    "Estado",
                    "estado"
                ),

            FechaRegistro =
                Fecha(
                    reader,
                    "FechaRegistro",
                    "fecha_registro"
                ),

            Categoria =
                Texto(
                    reader,
                    "Categoria",
                    "categoria"
                ),

            Marca =
                Texto(
                    reader,
                    "Marca",
                    "marca"
                ),

            UnidadMedida =
                Texto(
                    reader,
                    "UnidadMedida",
                    "unidad_medida"
                ),

            AbreviaturaUnidad =
                Texto(
                    reader,
                    "AbreviaturaUnidad",
                    "abreviatura_unidad"
                ),

            MargenMonto =
                Decimal(
                    reader,
                    "MargenMonto",
                    "margen_monto"
                ),

            MargenPorcentaje =
                Decimal(
                    reader,
                    "MargenPorcentaje",
                    "margen_porcentaje"
                )
        };
    }


    // =========================================================
    // CATÁLOGOS
    // =========================================================

    public async Task<ProductoCatalogos>
        ObtenerCatalogosAsync(
            int idEmpresa,
            CancellationToken cancellationToken = default)
    {
        if (idEmpresa <= 0)
        {
            throw new ArgumentException(
                "La empresa es obligatoria."
            );
        }


        ProductoCatalogos resultado =
            new();


        await using SqlConnection conexion =
            CrearConexion();

        await using SqlCommand command =
            new(
                "inv.sp_producto_catalogos",
                conexion
            );

        command.CommandType =
            CommandType.StoredProcedure;

        command.Parameters.Add(
            "@id_empresa",
            SqlDbType.Int
        ).Value =
            idEmpresa;


        await conexion.OpenAsync(
            cancellationToken
        );


        await using SqlDataReader reader =
            await command.ExecuteReaderAsync(
                cancellationToken
            );


        // =====================================================
        // RESULTSET 1 - CATEGORÍAS
        // =====================================================

        while (
            await reader.ReadAsync(
                cancellationToken
            )
        )
        {
            resultado.Categorias.Add(
                MapearCatalogo(reader)
            );
        }


        // =====================================================
        // RESULTSET 2 - MARCAS
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
                resultado.Marcas.Add(
                    MapearCatalogo(reader)
                );
            }
        }


        // =====================================================
        // RESULTSET 3 - UNIDADES
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
                resultado.UnidadesMedida.Add(
                    MapearCatalogo(reader)
                );
            }
        }


        // =====================================================
        // RESULTSET 4 - SUCURSALES
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
                resultado.Sucursales.Add(
                    MapearCatalogo(reader)
                );
            }
        }


        // =====================================================
        // RESULTSET 5 - ALMACENES
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
                resultado.Almacenes.Add(
                    new AlmacenCatalogoItem
                    {
                        Id =
                            Int(
                                reader,
                                "Id",
                                "id_almacen"
                            ),

                        Nombre =
                            Texto(
                                reader,
                                "Nombre",
                                "nombre"
                            ),

                        IdSucursal =
                            Int(
                                reader,
                                "IdSucursal",
                                "id_sucursal"
                            )
                    }
                );
            }
        }


        return resultado;
    }


    // =========================================================
    // CREAR
    // =========================================================

    public async Task<int> CrearAsync(
        ProductoGuardarRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidarGuardar(request);


        await using SqlConnection conexion =
            CrearConexion();

        await using SqlCommand command =
            new(
                "inv.sp_producto_crear",
                conexion
            );

        command.CommandType =
            CommandType.StoredProcedure;


        CargarParametrosProducto(
            command,
            request
        );


        command.Parameters.Add(
            "@id_usuario_registro",
            SqlDbType.Int
        ).Value =
            request.IdUsuario;


        SqlParameter output =
            command.Parameters.Add(
                "@id_producto",
                SqlDbType.Int
            );

        output.Direction =
            ParameterDirection.Output;


        await conexion.OpenAsync(
            cancellationToken
        );


        await command.ExecuteNonQueryAsync(
            cancellationToken
        );


        if (
            output.Value is null ||
            output.Value == DBNull.Value
        )
        {
            throw new InvalidOperationException(
                "El procedimiento no devolvió el ID del producto."
            );
        }


        int idProducto =
            Convert.ToInt32(
                output.Value
            );


        _logger.LogInformation(
            "Producto creado. IdProducto={IdProducto} Empresa={Empresa} Usuario={Usuario}",
            idProducto,
            request.IdEmpresa,
            request.IdUsuario
        );


        return idProducto;
    }


    // =========================================================
    // ACTUALIZAR
    // =========================================================

    public async Task ActualizarAsync(
        ProductoActualizarRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.IdProducto <= 0)
        {
            throw new ArgumentException(
                "El producto es obligatorio."
            );
        }


        ValidarGuardar(request);


        await using SqlConnection conexion =
            CrearConexion();

        await using SqlCommand command =
            new(
                "inv.sp_producto_actualizar",
                conexion
            );

        command.CommandType =
            CommandType.StoredProcedure;


        command.Parameters.Add(
            "@id_producto",
            SqlDbType.Int
        ).Value =
            request.IdProducto;


        CargarParametrosProducto(
            command,
            request
        );


        command.Parameters.Add(
            "@estado",
            SqlDbType.Bit
        ).Value =
            request.Estado;


        command.Parameters.Add(
            "@id_usuario_modifica",
            SqlDbType.Int
        ).Value =
            request.IdUsuario;


        await conexion.OpenAsync(
            cancellationToken
        );


        await command.ExecuteNonQueryAsync(
            cancellationToken
        );


        _logger.LogInformation(
            "Producto actualizado. IdProducto={IdProducto} Usuario={Usuario}",
            request.IdProducto,
            request.IdUsuario
        );
    }


    // =========================================================
    // CAMBIAR ESTADO
    // =========================================================

    public async Task CambiarEstadoAsync(
        int idProducto,
        bool estado,
        int idUsuario,
        CancellationToken cancellationToken = default)
    {
        if (idProducto <= 0)
        {
            throw new ArgumentException(
                "Producto inválido."
            );
        }

        if (idUsuario <= 0)
        {
            throw new ArgumentException(
                "Usuario inválido."
            );
        }


        await using SqlConnection conexion =
            CrearConexion();

        await using SqlCommand command =
            new(
                "inv.sp_producto_cambiar_estado",
                conexion
            );

        command.CommandType =
            CommandType.StoredProcedure;


        command.Parameters.Add(
            "@id_producto",
            SqlDbType.Int
        ).Value =
            idProducto;


        command.Parameters.Add(
            "@estado",
            SqlDbType.Bit
        ).Value =
            estado;


        command.Parameters.Add(
            "@id_usuario_modifica",
            SqlDbType.Int
        ).Value =
            idUsuario;


        await conexion.OpenAsync(
            cancellationToken
        );


        await command.ExecuteNonQueryAsync(
            cancellationToken
        );
    }


    // =========================================================
    // STOCK DEL PRODUCTO
    // =========================================================
    //
    // inv.sp_stock_producto devuelve EXACTAMENTE 3 resultsets:
    //
    // 1) Producto
    // 2) Stock por almacén
    // 3) Resumen
    //
    // Esta implementación respeta ese orden para evitar que el
    // detalle muestre 0 cuando el listado sí tiene existencias.
    // =========================================================

    public async Task<ProductoStockResultado>
        ObtenerStockAsync(
            int idProducto,
            int idEmpresa,
            CancellationToken cancellationToken = default)
    {
        if (idProducto <= 0)
        {
            throw new ArgumentException(
                "El producto es obligatorio.",
                nameof(idProducto)
            );
        }

        if (idEmpresa <= 0)
        {
            throw new ArgumentException(
                "La empresa es obligatoria.",
                nameof(idEmpresa)
            );
        }


        ProductoStockResultado resultado =
            new();


        await using SqlConnection conexion =
            CrearConexion();

        await using SqlCommand command =
            new(
                "inv.sp_stock_producto",
                conexion
            );

        command.CommandType =
            CommandType.StoredProcedure;

        command.CommandTimeout = 60;


        command.Parameters.Add(
            "@id_producto",
            SqlDbType.Int
        ).Value =
            idProducto;


        command.Parameters.Add(
            "@id_empresa",
            SqlDbType.Int
        ).Value =
            idEmpresa;


        await conexion.OpenAsync(
            cancellationToken
        );


        await using SqlDataReader reader =
            await command.ExecuteReaderAsync(
                cancellationToken
            );


        // =====================================================
        // RESULTSET 1 - PRODUCTO
        // =====================================================
        //
        // El procedimiento devuelve aquí:
        // IdProducto, IdEmpresa, Empresa, CodigoProducto,
        // CodigoBarras, Producto, Categoria, Marca,
        // UnidadMedida, AbreviaturaUnidad, ControlaStock, Estado.
        //
        // El detalle maestro del producto ya se obtiene mediante
        // inv.sp_producto_obtener, por eso aquí solo consumimos
        // este resultset y validamos que haya una fila.
        // =====================================================

        bool productoEncontrado =
            false;


        while (
            await reader.ReadAsync(
                cancellationToken
            )
        )
        {
            productoEncontrado =
                true;
        }


        if (!productoEncontrado)
        {
            _logger.LogWarning(
                "sp_stock_producto no devolvió el producto. Producto={Producto} Empresa={Empresa}",
                idProducto,
                idEmpresa
            );

            return resultado;
        }


        // =====================================================
        // RESULTSET 2 - STOCK POR ALMACÉN
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
                decimal cantidadActual =
                    Decimal(
                        reader,
                        "CantidadActual",
                        "cantidad_actual"
                    );


                decimal cantidadReservada =
                    Decimal(
                        reader,
                        "CantidadReservada",
                        "cantidad_reservada"
                    );


                // El SP real devuelve CantidadDisponible.
                // Si en el futuro cambia el alias, mantenemos
                // compatibilidad con StockDisponible.
                int indiceDisponible =
                    BuscarIndice(
                        reader,
                        "CantidadDisponible",
                        "cantidad_disponible",
                        "StockDisponible",
                        "stock_disponible",
                        "Disponible",
                        "disponible"
                    );


                decimal cantidadDisponible =
                    indiceDisponible >= 0 &&
                    !reader.IsDBNull(indiceDisponible)
                        ? Convert.ToDecimal(
                            reader.GetValue(
                                indiceDisponible
                            )
                        )
                        : cantidadActual -
                          cantidadReservada;


                ProductoStockItem item =
                    new()
                    {
                        IdStock =
                            NullableLong(
                                reader,
                                "IdStock",
                                "id_stock"
                            ),

                        IdAlmacen =
                            Int(
                                reader,
                                "IdAlmacen",
                                "id_almacen"
                            ),

                        Almacen =
                            Texto(
                                reader,
                                "Almacen",
                                "almacen"
                            ),

                        IdSucursal =
                            NullableInt(
                                reader,
                                "IdSucursal",
                                "id_sucursal"
                            ),

                        Sucursal =
                            Texto(
                                reader,
                                "Sucursal",
                                "sucursal"
                            ),

                        CantidadActual =
                            cantidadActual,

                        CantidadReservada =
                            cantidadReservada,

                        StockDisponible =
                            cantidadDisponible,

                        StockMinimo =
                            Decimal(
                                reader,
                                "StockMinimo",
                                "stock_minimo"
                            ),

                        StockMaximo =
                            NullableDecimal(
                                reader,
                                "StockMaximo",
                                "stock_maximo"
                            ),

                        EstadoStock =
                            NormalizarEstadoStock(
                                Texto(
                                    reader,
                                    "EstadoStock",
                                    "estado_stock"
                                )
                            ),

                        // El resultset 2 del SP no devuelve
                        // valorización por almacén. Los valores
                        // consolidados llegan en el resultset 3.
                        ValorInventarioCompra =
                            0M,

                        ValorInventarioVenta =
                            0M
                    };


                resultado.Almacenes.Add(
                    item
                );
            }
        }


        // =====================================================
        // TOTALES PROVISIONALES DESDE ALMACENES
        // =====================================================
        //
        // Si por alguna razón el resultset 3 no llega, el detalle
        // seguirá teniendo stock correcto gracias a estos valores.
        // =====================================================

        resultado.StockActualTotal =
            resultado.Almacenes.Sum(
                x => x.CantidadActual
            );


        resultado.StockReservadoTotal =
            resultado.Almacenes.Sum(
                x => x.CantidadReservada
            );


        resultado.StockDisponibleTotal =
            resultado.Almacenes.Sum(
                x => x.StockDisponible
            );


        // =====================================================
        // RESULTSET 3 - RESUMEN DEL PROCEDIMIENTO
        // =====================================================
        //
        // Columnas reales:
        // TotalAlmacenes
        // CantidadActual
        // CantidadReservada
        // CantidadDisponible
        // ValorCompra
        // ValorVenta
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
            resultado.StockActualTotal =
                Decimal(
                    reader,
                    "CantidadActual",
                    "cantidad_actual"
                );


            resultado.StockReservadoTotal =
                Decimal(
                    reader,
                    "CantidadReservada",
                    "cantidad_reservada"
                );


            resultado.StockDisponibleTotal =
                Decimal(
                    reader,
                    "CantidadDisponible",
                    "cantidad_disponible",
                    "StockDisponible",
                    "stock_disponible"
                );


            resultado.ValorCompraTotal =
                Decimal(
                    reader,
                    "ValorCompra",
                    "valor_compra",
                    "ValorInventarioCompra",
                    "valor_inventario_compra"
                );


            resultado.ValorVentaTotal =
                Decimal(
                    reader,
                    "ValorVenta",
                    "valor_venta",
                    "ValorInventarioVenta",
                    "valor_inventario_venta"
                );
        }


        // =====================================================
        // VALIDACIÓN DE CONSISTENCIA
        // =====================================================

        decimal totalActualAlmacenes =
            resultado.Almacenes.Sum(
                x => x.CantidadActual
            );


        decimal totalReservadoAlmacenes =
            resultado.Almacenes.Sum(
                x => x.CantidadReservada
            );


        decimal totalDisponibleAlmacenes =
            resultado.Almacenes.Sum(
                x => x.StockDisponible
            );


        if (
            resultado.StockActualTotal !=
            totalActualAlmacenes
            ||
            resultado.StockReservadoTotal !=
            totalReservadoAlmacenes
            ||
            resultado.StockDisponibleTotal !=
            totalDisponibleAlmacenes
        )
        {
            _logger.LogWarning(
                "Diferencia entre resumen y almacenes. Producto={Producto} ResumenActual={ResumenActual} AlmacenesActual={AlmacenesActual} ResumenDisponible={ResumenDisponible} AlmacenesDisponible={AlmacenesDisponible}",
                idProducto,
                resultado.StockActualTotal,
                totalActualAlmacenes,
                resultado.StockDisponibleTotal,
                totalDisponibleAlmacenes
            );
        }


        _logger.LogInformation(
            "Stock producto cargado. Producto={Producto} Empresa={Empresa} Almacenes={Almacenes} Actual={Actual} Reservado={Reservado} Disponible={Disponible} ValorCompra={ValorCompra} ValorVenta={ValorVenta}",
            idProducto,
            idEmpresa,
            resultado.Almacenes.Count,
            resultado.StockActualTotal,
            resultado.StockReservadoTotal,
            resultado.StockDisponibleTotal,
            resultado.ValorCompraTotal,
            resultado.ValorVentaTotal
        );


        return resultado;
    }


    // =========================================================
    // NORMALIZAR ESTADO DEL STOCK
    // =========================================================

    private static string NormalizarEstadoStock(
        string? estado)
    {
        string valor =
            (
                estado ??
                string.Empty
            )
            .Trim()
            .ToLowerInvariant();


        return valor switch
        {
            "sin_stock" =>
                "sin_stock",

            "stock_bajo" =>
                "bajo_stock",

            "bajo_stock" =>
                "bajo_stock",

            "bajo_minimo" =>
                "bajo_stock",

            "sobre_stock" =>
                "sobre_stock",

            "normal" =>
                "normal",

            _ =>
                valor
        };
    }


    // =========================================================
    // MAPEO LISTADO
    // =========================================================

    private static ProductoListaItem MapearProductoLista(
        SqlDataReader reader)
    {
        return new ProductoListaItem
        {
            IdProducto =
                Int(
                    reader,
                    "IdProducto",
                    "id_producto"
                ),

            IdEmpresa =
                Int(
                    reader,
                    "IdEmpresa",
                    "id_empresa"
                ),

            Empresa =
                Texto(
                    reader,
                    "Empresa",
                    "empresa"
                ),

            IdCategoriaProducto =
                Int(
                    reader,
                    "IdCategoriaProducto",
                    "id_categoria_producto"
                ),

            Categoria =
                Texto(
                    reader,
                    "Categoria",
                    "categoria"
                ),

            IdMarca =
                NullableInt(
                    reader,
                    "IdMarca",
                    "id_marca"
                ),

            Marca =
                Texto(
                    reader,
                    "Marca",
                    "marca"
                ),

            IdUnidadMedida =
                Int(
                    reader,
                    "IdUnidadMedida",
                    "id_unidad_medida"
                ),

            UnidadMedida =
                Texto(
                    reader,
                    "UnidadMedida",
                    "unidad_medida"
                ),

            AbreviaturaUnidad =
                Texto(
                    reader,
                    "AbreviaturaUnidad",
                    "abreviatura_unidad"
                ),

            Codigo =
                Texto(
                    reader,
                    "Codigo",
                    "codigo"
                ),

            CodigoBarras =
                TextoNullable(
                    reader,
                    "CodigoBarras",
                    "codigo_barras"
                ),

            Nombre =
                Texto(
                    reader,
                    "Nombre",
                    "nombre"
                ),

            Descripcion =
                TextoNullable(
                    reader,
                    "Descripcion",
                    "descripcion"
                ),

            PrecioCompra =
                Decimal(
                    reader,
                    "PrecioCompra",
                    "precio_compra"
                ),

            PrecioVenta =
                Decimal(
                    reader,
                    "PrecioVenta",
                    "precio_venta"
                ),

            MargenMonto =
                Decimal(
                    reader,
                    "MargenMonto",
                    "margen_monto"
                ),

            MargenPorcentaje =
                Decimal(
                    reader,
                    "MargenPorcentaje",
                    "margen_porcentaje"
                ),

            StockActual =
                Decimal(
                    reader,
                    "StockActual",
                    "stock_actual"
                ),

            StockReservado =
                Decimal(
                    reader,
                    "StockReservado",
                    "stock_reservado"
                ),

            StockDisponible =
                Decimal(
                    reader,
                    "StockDisponible",
                    "stock_disponible"
                ),

            StockMinimo =
                Decimal(
                    reader,
                    "StockMinimo",
                    "stock_minimo"
                ),

            StockMaximo =
                NullableDecimal(
                    reader,
                    "StockMaximo",
                    "stock_maximo"
                ),

            ControlaStock =
                Bool(
                    reader,
                    "ControlaStock",
                    "controla_stock"
                ),

            EstadoStock =
                Texto(
                    reader,
                    "EstadoStock",
                    "estado_stock"
                ),

            TotalAlmacenes =
                Int(
                    reader,
                    "TotalAlmacenes",
                    "total_almacenes"
                ),

            Estado =
                Bool(
                    reader,
                    "Estado",
                    "estado"
                ),

            FechaRegistro =
                Fecha(
                    reader,
                    "FechaRegistro",
                    "fecha_registro"
                )
        };
    }


    // =========================================================
    // PARAMETROS PRODUCTO
    // =========================================================
    // =========================================================
    // PARÁMETROS PRODUCTO
    // =========================================================

    private static void CargarParametrosProducto(
        SqlCommand command,
        ProductoGuardarRequest request)
    {
        command.Parameters.Add(
            "@id_empresa",
            SqlDbType.Int
        ).Value =
            request.IdEmpresa;


        command.Parameters.Add(
            "@id_categoria_producto",
            SqlDbType.Int
        ).Value =
            request.IdCategoriaProducto;


        SqlParameter marca =
            command.Parameters.Add(
                "@id_marca",
                SqlDbType.Int
            );

        marca.Value =
            request.IdMarca.HasValue &&
            request.IdMarca.Value > 0
                ? request.IdMarca.Value
                : DBNull.Value;


        command.Parameters.Add(
            "@id_unidad_medida",
            SqlDbType.Int
        ).Value =
            request.IdUnidadMedida;


        // =====================================================
        // CÓDIGO
        // inv.producto.codigo = NVARCHAR(100)
        // =====================================================

        SqlParameter codigo =
            command.Parameters.Add(
                "@codigo",
                SqlDbType.NVarChar,
                100
            );

        codigo.Value =
            request.Codigo.Trim();


        // =====================================================
        // CÓDIGO BARRAS
        // inv.producto.codigo_barras = NVARCHAR(200)
        // =====================================================

        SqlParameter codigoBarras =
            command.Parameters.Add(
                "@codigo_barras",
                SqlDbType.NVarChar,
                200
            );

        codigoBarras.Value =
            string.IsNullOrWhiteSpace(
                request.CodigoBarras
            )
                ? DBNull.Value
                : request.CodigoBarras.Trim();


        // =====================================================
        // NOMBRE
        // inv.producto.nombre = NVARCHAR(400)
        // =====================================================

        SqlParameter nombre =
            command.Parameters.Add(
                "@nombre",
                SqlDbType.NVarChar,
                400
            );

        nombre.Value =
            request.Nombre.Trim();


        // =====================================================
        // DESCRIPCIÓN
        // inv.producto.descripcion = NVARCHAR(1000)
        // =====================================================

        SqlParameter descripcion =
            command.Parameters.Add(
                "@descripcion",
                SqlDbType.NVarChar,
                1000
            );

        descripcion.Value =
            string.IsNullOrWhiteSpace(
                request.Descripcion
            )
                ? DBNull.Value
                : request.Descripcion.Trim();


        // =====================================================
        // PRECIO COMPRA
        // =====================================================

        SqlParameter precioCompra =
            command.Parameters.Add(
                "@precio_compra",
                SqlDbType.Decimal
            );

        precioCompra.Precision = 18;
        precioCompra.Scale = 2;

        precioCompra.Value =
            request.PrecioCompra;


        // =====================================================
        // PRECIO VENTA
        // =====================================================

        SqlParameter precioVenta =
            command.Parameters.Add(
                "@precio_venta",
                SqlDbType.Decimal
            );

        precioVenta.Precision = 18;
        precioVenta.Scale = 2;

        precioVenta.Value =
            request.PrecioVenta;


        // =====================================================
        // STOCK MÍNIMO
        // =====================================================

        SqlParameter stockMinimo =
            command.Parameters.Add(
                "@stock_minimo",
                SqlDbType.Decimal
            );

        stockMinimo.Precision = 18;
        stockMinimo.Scale = 4;

        stockMinimo.Value =
            request.StockMinimo;


        // =====================================================
        // STOCK MÁXIMO
        // =====================================================

        SqlParameter stockMaximo =
            command.Parameters.Add(
                "@stock_maximo",
                SqlDbType.Decimal
            );

        stockMaximo.Precision = 18;
        stockMaximo.Scale = 4;

        stockMaximo.Value =
            request.StockMaximo.HasValue
                ? request.StockMaximo.Value
                : DBNull.Value;


        // =====================================================
        // CONTROL STOCK
        // =====================================================

        command.Parameters.Add(
            "@controla_stock",
            SqlDbType.Bit
        ).Value =
            request.ControlaStock;
    }


    // =========================================================
    // VALIDACIONES
    // =========================================================

    private static void ValidarGuardar(
        ProductoGuardarRequest request)
    {
        ArgumentNullException.ThrowIfNull(
            request
        );


        if (request.IdEmpresa <= 0)
        {
            throw new InvalidOperationException(
                "La empresa es obligatoria."
            );
        }


        if (request.IdCategoriaProducto <= 0)
        {
            throw new InvalidOperationException(
                "La categoría es obligatoria."
            );
        }


        if (request.IdUnidadMedida <= 0)
        {
            throw new InvalidOperationException(
                "La unidad de medida es obligatoria."
            );
        }


        if (string.IsNullOrWhiteSpace(
            request.Codigo))
        {
            throw new InvalidOperationException(
                "El código del producto es obligatorio."
            );
        }


        if (string.IsNullOrWhiteSpace(
            request.Nombre))
        {
            throw new InvalidOperationException(
                "El nombre del producto es obligatorio."
            );
        }


        if (request.PrecioCompra < 0)
        {
            throw new InvalidOperationException(
                "El precio de compra no puede ser negativo."
            );
        }


        if (request.PrecioVenta < 0)
        {
            throw new InvalidOperationException(
                "El precio de venta no puede ser negativo."
            );
        }


        if (request.StockMinimo < 0)
        {
            throw new InvalidOperationException(
                "El stock mínimo no puede ser negativo."
            );
        }


        if (
            request.StockMaximo.HasValue &&
            request.StockMaximo.Value <
            request.StockMinimo
        )
        {
            throw new InvalidOperationException(
                "El stock máximo no puede ser menor al stock mínimo."
            );
        }


        if (request.IdUsuario <= 0)
        {
            throw new InvalidOperationException(
                "No se pudo identificar al usuario."
            );
        }
    }


    // =========================================================
    // CONEXION
    // =========================================================

    private SqlConnection CrearConexion()
    {
        return new SqlConnection(
            _connectionString
        );
    }


    // =========================================================
    // PARAMETROS
    // =========================================================

    private static void Agregar(
        SqlCommand command,
        string nombre,
        SqlDbType tipo,
        object valor,
        int? longitud = null)
    {
        SqlParameter parameter =
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

        parameter.Value =
            valor;
    }


    private static void AgregarNullable(
        SqlCommand command,
        string nombre,
        SqlDbType tipo,
        object? valor)
    {
        SqlParameter parameter =
            command.Parameters.Add(
                nombre,
                tipo
            );

        parameter.Value =
            valor ?? DBNull.Value;
    }


    private static void AgregarTextoNullable(
        SqlCommand command,
        string nombre,
        string? valor,
        int longitud)
    {
        SqlParameter parameter =
            command.Parameters.Add(
                nombre,
                SqlDbType.NVarChar,
                longitud
            );

        parameter.Value =
            string.IsNullOrWhiteSpace(valor)
                ? DBNull.Value
                : valor.Trim();
    }


    private static void AgregarDecimal(
        SqlCommand command,
        string nombre,
        decimal valor,
        byte precision,
        byte scale)
    {
        SqlParameter parameter =
            command.Parameters.Add(
                nombre,
                SqlDbType.Decimal
            );

        parameter.Precision =
            precision;

        parameter.Scale =
            scale;

        parameter.Value =
            valor;
    }


    // =========================================================
    // HELPERS READER
    // =========================================================

    private static int BuscarIndice(
        SqlDataReader reader,
        params string[] nombres)
    {
        foreach (
            string nombre in nombres
        )
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
        }

        return -1;
    }


    private static string Texto(
        SqlDataReader reader,
        params string[] nombres)
    {
        int i =
            BuscarIndice(
                reader,
                nombres
            );

        if (
            i < 0 ||
            reader.IsDBNull(i)
        )
        {
            return string.Empty;
        }

        return Convert.ToString(
            reader.GetValue(i)
        ) ?? string.Empty;
    }


    private static string? TextoNullable(
        SqlDataReader reader,
        params string[] nombres)
    {
        int i =
            BuscarIndice(
                reader,
                nombres
            );

        if (
            i < 0 ||
            reader.IsDBNull(i)
        )
        {
            return null;
        }

        return Convert.ToString(
            reader.GetValue(i)
        );
    }


    private static int Int(
        SqlDataReader reader,
        params string[] nombres)
    {
        int i =
            BuscarIndice(
                reader,
                nombres
            );

        if (
            i < 0 ||
            reader.IsDBNull(i)
        )
        {
            return 0;
        }

        return Convert.ToInt32(
            reader.GetValue(i)
        );
    }


    private static int? NullableInt(
        SqlDataReader reader,
        params string[] nombres)
    {
        int i =
            BuscarIndice(
                reader,
                nombres
            );

        if (
            i < 0 ||
            reader.IsDBNull(i)
        )
        {
            return null;
        }

        return Convert.ToInt32(
            reader.GetValue(i)
        );
    }


    private static long? NullableLong(
        SqlDataReader reader,
        params string[] nombres)
    {
        int i =
            BuscarIndice(
                reader,
                nombres
            );

        if (
            i < 0 ||
            reader.IsDBNull(i)
        )
        {
            return null;
        }

        return Convert.ToInt64(
            reader.GetValue(i)
        );
    }


    private static decimal Decimal(
        SqlDataReader reader,
        params string[] nombres)
    {
        int i =
            BuscarIndice(
                reader,
                nombres
            );

        if (
            i < 0 ||
            reader.IsDBNull(i)
        )
        {
            return 0M;
        }

        return Convert.ToDecimal(
            reader.GetValue(i)
        );
    }


    private static decimal? NullableDecimal(
        SqlDataReader reader,
        params string[] nombres)
    {
        int i =
            BuscarIndice(
                reader,
                nombres
            );

        if (
            i < 0 ||
            reader.IsDBNull(i)
        )
        {
            return null;
        }

        return Convert.ToDecimal(
            reader.GetValue(i)
        );
    }


    private static bool Bool(
        SqlDataReader reader,
        params string[] nombres)
    {
        int i =
            BuscarIndice(
                reader,
                nombres
            );

        if (
            i < 0 ||
            reader.IsDBNull(i)
        )
        {
            return false;
        }

        return Convert.ToBoolean(
            reader.GetValue(i)
        );
    }


    private static DateTime Fecha(
        SqlDataReader reader,
        params string[] nombres)
    {
        int i =
            BuscarIndice(
                reader,
                nombres
            );

        if (
            i < 0 ||
            reader.IsDBNull(i)
        )
        {
            return DateTime.MinValue;
        }

        return Convert.ToDateTime(
            reader.GetValue(i)
        );
    }


    private static int ObtenerInt32(
        SqlDataReader reader,
        int indice)
    {
        if (
            indice < 0 ||
            indice >= reader.FieldCount ||
            reader.IsDBNull(indice)
        )
        {
            return 0;
        }

        return Convert.ToInt32(
            reader.GetValue(indice)
        );
    }


    private static CatalogoItem MapearCatalogo(
        SqlDataReader reader)
    {
        return new CatalogoItem
        {
            Id =
                Int(
                    reader,
                    "Id",
                    "id"
                ),

            Nombre =
                Texto(
                    reader,
                    "Nombre",
                    "nombre"
                )
        };
    }


    private static string NormalizarOrden(
        string? valor)
    {
        string normalizado =
            valor?.Trim().ToLowerInvariant()
            ?? "nombre";

        return normalizado switch
        {
            "codigo" => "codigo",
            "precio_compra" => "precio_compra",
            "precio_venta" => "precio_venta",
            "stock" => "stock",
            "fecha_registro" => "fecha_registro",

            _ => "nombre"
        };
    }


    private static string NormalizarDireccion(
        string? valor)
    {
        return string.Equals(
            valor,
            "DESC",
            StringComparison.OrdinalIgnoreCase
        )
            ? "DESC"
            : "ASC";
    }
}