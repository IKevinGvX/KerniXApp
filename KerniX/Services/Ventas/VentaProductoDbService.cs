using System.Data;
using KerniX.Models.Ventas.Producto;
using Microsoft.Data.SqlClient;

namespace KerniX.Services.Ventas;

public sealed class VentaProductoDbService
    : IVentaProductoDbService
{
    private readonly string _connectionString;
    private readonly ILogger<VentaProductoDbService> _logger;

    public VentaProductoDbService(
        IConfiguration configuration,
        ILogger<VentaProductoDbService> logger)
    {
        _connectionString =
            configuration.GetConnectionString(
                "KerniXDb")
            ?? throw new InvalidOperationException(
                "No se encontró la conexión KerniXDb.");

        _logger = logger;
    }

    // =========================================================
    // BUSCAR PRODUCTOS PARA VENTA
    // =========================================================

    public async Task<
        IReadOnlyList<VentaProductoBuscarResponse>>
        BuscarAsync(
            int idEmpresa,
            int idAlmacen,
            string? termino,
            int limite = 20)
    {
        if (idEmpresa <= 0)
        {
            throw new ArgumentException(
                "La empresa no es válida.",
                nameof(idEmpresa));
        }

        if (idAlmacen <= 0)
        {
            throw new ArgumentException(
                "El almacén no es válido.",
                nameof(idAlmacen));
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

        var productos =
            new List<VentaProductoBuscarResponse>();

        const string sql = """
            SELECT TOP (@limite)

                p.id_producto,
                p.codigo,
                p.codigo_barras,
                p.nombre,
                p.precio_venta,
                p.controla_stock,

                ISNULL(
                    s.cantidad_actual,
                    0
                )
                -
                ISNULL(
                    s.cantidad_reservada,
                    0
                ) AS stock_disponible

            FROM inv.producto p

            LEFT JOIN inv.stock s
                ON s.id_producto =
                   p.id_producto

               AND s.id_almacen =
                   @id_almacen

            WHERE
                p.id_empresa = @id_empresa

                AND p.estado = 1

                AND
                (
                    @termino IS NULL
                    OR @termino = ''

                    OR p.codigo
                        LIKE '%' + @termino + '%'

                    OR p.codigo_barras
                        LIKE '%' + @termino + '%'

                    OR p.nombre
                        LIKE '%' + @termino + '%'

                    OR p.descripcion
                        LIKE '%' + @termino + '%'
                )

            ORDER BY
                p.nombre,
                p.codigo;
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
                "@id_almacen",
                SqlDbType.Int)
            {
                Value = idAlmacen
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
                productos.Add(
                    new VentaProductoBuscarResponse
                    {
                        IdProducto =
                            Convert.ToInt32(
                                reader["id_producto"]),

                        Codigo =
                            reader["codigo"]
                                ?.ToString()
                            ?? string.Empty,

                        CodigoBarras =
                            reader["codigo_barras"] is DBNull
                                ? null
                                : reader["codigo_barras"]
                                    ?.ToString(),

                        Nombre =
                            reader["nombre"]
                                ?.ToString()
                            ?? string.Empty,

                        PrecioVenta =
                            Convert.ToDecimal(
                                reader["precio_venta"]),

                        ControlaStock =
                            Convert.ToBoolean(
                                reader["controla_stock"]),

                        StockDisponible =
                            Convert.ToDecimal(
                                reader["stock_disponible"])
                    });
            }

            return productos;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error buscando productos para venta. " +
                "Empresa={IdEmpresa} | " +
                "Almacen={IdAlmacen} | " +
                "Termino={Termino}",
                idEmpresa,
                idAlmacen,
                termino);

            throw;
        }
    }

    // =========================================================
    // OBTENER PRODUCTO POR ID
    // =========================================================

    public async Task<VentaProductoResponse?>
        ObtenerPorIdAsync(
            int idEmpresa,
            int idProducto)
    {
        if (idEmpresa <= 0 ||
            idProducto <= 0)
        {
            return null;
        }

        const string sql = """
            SELECT TOP (1)

                p.id_producto,
                p.id_empresa,
                p.codigo,
                p.codigo_barras,
                p.nombre,
                p.descripcion,
                p.precio_venta,
                p.precio_compra,
                p.controla_stock,
                p.estado

            FROM inv.producto p

            WHERE
                p.id_empresa =
                    @id_empresa

                AND p.id_producto =
                    @id_producto;
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
                "@id_producto",
                SqlDbType.Int)
            {
                Value = idProducto
            });

        try
        {
            await using var reader =
                await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                return null;
            }

            return new VentaProductoResponse
            {
                IdProducto =
                    Convert.ToInt32(
                        reader["id_producto"]),

                IdEmpresa =
                    Convert.ToInt32(
                        reader["id_empresa"]),

                Codigo =
                    reader["codigo"]
                        ?.ToString()
                    ?? string.Empty,

                CodigoBarras =
                    reader["codigo_barras"] is DBNull
                        ? null
                        : reader["codigo_barras"]
                            ?.ToString(),

                Nombre =
                    reader["nombre"]
                        ?.ToString()
                    ?? string.Empty,

                Descripcion =
                    reader["descripcion"] is DBNull
                        ? null
                        : reader["descripcion"]
                            ?.ToString(),

                PrecioVenta =
                    Convert.ToDecimal(
                        reader["precio_venta"]),

                PrecioCompra =
                    Convert.ToDecimal(
                        reader["precio_compra"]),

                ControlaStock =
                    Convert.ToBoolean(
                        reader["controla_stock"]),

                Estado =
                    Convert.ToBoolean(
                        reader["estado"])
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error obteniendo producto. " +
                "Empresa={IdEmpresa} | Producto={IdProducto}",
                idEmpresa,
                idProducto);

            throw;
        }
    }

    // =========================================================
    // OBTENER STOCK
    // =========================================================

    public async Task<VentaProductoStockResponse?>
        ObtenerStockAsync(
            int idAlmacen,
            int idProducto)
    {
        if (idAlmacen <= 0 ||
            idProducto <= 0)
        {
            return null;
        }

        const string sql = """
            SELECT TOP (1)

                s.id_stock,
                s.id_almacen,
                s.id_producto,

                a.nombre AS almacen,

                p.codigo AS codigo_producto,
                p.nombre AS producto,

                s.cantidad_actual,
                s.cantidad_reservada,
                s.stock_minimo,
                s.stock_maximo,
                s.fecha_actualizacion

            FROM inv.stock s

            INNER JOIN inv.almacen a
                ON a.id_almacen =
                   s.id_almacen

            INNER JOIN inv.producto p
                ON p.id_producto =
                   s.id_producto

            WHERE
                s.id_almacen =
                    @id_almacen

                AND s.id_producto =
                    @id_producto;
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
                "@id_almacen",
                SqlDbType.Int)
            {
                Value = idAlmacen
            });

        command.Parameters.Add(
            new SqlParameter(
                "@id_producto",
                SqlDbType.Int)
            {
                Value = idProducto
            });

        try
        {
            await using var reader =
                await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                return null;
            }

            return new VentaProductoStockResponse
            {
                IdStock =
                    Convert.ToInt64(
                        reader["id_stock"]),

                IdAlmacen =
                    Convert.ToInt32(
                        reader["id_almacen"]),

                IdProducto =
                    Convert.ToInt32(
                        reader["id_producto"]),

                Almacen =
                    reader["almacen"]
                        ?.ToString()
                    ?? string.Empty,

                CodigoProducto =
                    reader["codigo_producto"]
                        ?.ToString()
                    ?? string.Empty,

                Producto =
                    reader["producto"]
                        ?.ToString()
                    ?? string.Empty,

                CantidadActual =
                    Convert.ToDecimal(
                        reader["cantidad_actual"]),

                CantidadReservada =
                    Convert.ToDecimal(
                        reader["cantidad_reservada"]),

                StockMinimo =
                    Convert.ToDecimal(
                        reader["stock_minimo"]),

                StockMaximo =
                    reader["stock_maximo"] is DBNull
                        ? null
                        : Convert.ToDecimal(
                            reader["stock_maximo"]),

                FechaActualizacion =
                    Convert.ToDateTime(
                        reader["fecha_actualizacion"])
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error consultando stock. " +
                "Almacen={IdAlmacen} | Producto={IdProducto}",
                idAlmacen,
                idProducto);

            throw;
        }
    }
}