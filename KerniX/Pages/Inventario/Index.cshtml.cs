using KerniX.Models.Inventario.Producto;
using KerniX.Services.Inventario;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace KerniX.Pages.Inventario;

public sealed class IndexModel
    : InventarioEnterprisePageModel
{
    // =========================================================
    // SERVICIOS
    // =========================================================

    private readonly IProductoDbService _productoService;
    private readonly ILogger<IndexModel> _logger;


    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public IndexModel(
        IProductoDbService productoService,
        ILogger<IndexModel> logger)
    {
        _productoService =
            productoService;

        _logger =
            logger;
    }


    // =========================================================
    // KPI PRINCIPALES
    // =========================================================

    public int TotalProductos { get; private set; }

    public int ProductosActivos { get; private set; }

    public int ProductosInactivos { get; private set; }

    public decimal StockActual { get; private set; }

    public decimal StockReservado { get; private set; }

    public decimal StockDisponible { get; private set; }

    public int ProductosSinStock { get; private set; }

    public int ProductosBajoStock { get; private set; }

    public int ProductosNormales { get; private set; }

    public int ProductosSobreStock { get; private set; }

    public int AlertasStock =>
        ProductosSinStock
        +
        ProductosBajoStock;

    public int TotalAlmacenes { get; private set; }

    public decimal MargenPromedio { get; private set; }

    public decimal PrecioCompraPromedio { get; private set; }

    public decimal PrecioVentaPromedio { get; private set; }

    public decimal ValorInventarioCompra { get; private set; }

    public decimal ValorInventarioVenta { get; private set; }

    public decimal UtilidadPotencial =>
        ValorInventarioVenta
        -
        ValorInventarioCompra;


    // =========================================================
    // LISTADOS DEL DASHBOARD
    // =========================================================

    public List<ProductoListaItem> ProductosCriticos
    {
        get;
        private set;
    } = new();


    public List<ProductoListaItem> ProductosMayorStock
    {
        get;
        private set;
    } = new();


    public List<ProductoListaItem> ProductosMayorMargen
    {
        get;
        private set;
    } = new();


    public ProductoCatalogos Catalogos
    {
        get;
        private set;
    } = new();


    // =========================================================
    // ESTADÍSTICAS AUXILIARES
    // =========================================================

    public decimal PorcentajeDisponible =>
        StockActual > 0
            ? Math.Min(
                100M,
                StockDisponible
                /
                StockActual
                *
                100M
            )
            : 0M;


    public decimal PorcentajeReservado =>
        StockActual > 0
            ? Math.Min(
                100M,
                StockReservado
                /
                StockActual
                *
                100M
            )
            : 0M;


    public decimal PorcentajeSaludable
    {
        get
        {
            int total =
                ProductosNormales
                +
                ProductosBajoStock
                +
                ProductosSinStock
                +
                ProductosSobreStock;

            return total > 0
                ? ProductosNormales
                  /
                  (decimal)total
                  *
                  100M
                : 0M;
        }
    }


    // =========================================================
    // MENSAJES
    // =========================================================

    public string? MensajeError
    {
        get;
        private set;
    }


    // =========================================================
    // GET
    // =========================================================

    public async Task<IActionResult> OnGetAsync(
        CancellationToken cancellationToken)
    {
        // El dashboard no necesita empleado para ser consultado.
        if (
            !CargarSesion(
                requiereEmpleado: false
            )
        )
        {
            return IrLogin();
        }


        _logger.LogInformation(
            "[INVENTARIO] Dashboard iniciado. Usuario={Usuario} Empleado={Empleado} Empresa={Empresa} Sucursal={Sucursal}",
            IdUsuario,
            IdEmpleado,
            IdEmpresa,
            IdSucursal
        );


        try
        {
            // =================================================
            // CATÁLOGOS
            // =================================================

            Catalogos =
                await _productoService
                    .ObtenerCatalogosAsync(
                        IdEmpresa,
                        cancellationToken
                    );


            TotalAlmacenes =
                Catalogos
                    .Almacenes
                    .Count;


            // =================================================
            // CARGA COMPLETA DE PRODUCTOS
            // =================================================
            // El dashboard necesita todos los productos para que
            // stock, valorización y alertas no dependan solamente
            // de la primera página del listado.

            List<ProductoListaItem> productos =
                await CargarTodosLosProductosAsync(
                    cancellationToken
                );


            TotalProductos =
                productos.Count;


            // =================================================
            // ESTADOS MAESTROS
            // =================================================

            ProductosActivos =
                productos.Count(
                    x => x.Estado
                );


            ProductosInactivos =
                productos.Count(
                    x => !x.Estado
                );


            // =================================================
            // STOCK
            // =================================================

            StockActual =
                productos.Sum(
                    x => x.StockActual
                );


            StockReservado =
                productos.Sum(
                    x => x.StockReservado
                );


            StockDisponible =
                productos.Sum(
                    x => x.StockDisponible
                );


            // =================================================
            // CLASIFICACIÓN DE STOCK
            // =================================================

            ProductosSinStock =
                productos.Count(
                    x =>
                        EsEstadoStock(
                            x.EstadoStock,
                            "sin_stock"
                        )
                );


            ProductosBajoStock =
                productos.Count(
                    x =>
                        EsEstadoStock(
                            x.EstadoStock,
                            "bajo_stock"
                        )
                        ||
                        EsEstadoStock(
                            x.EstadoStock,
                            "bajo_minimo"
                        )
                );


            ProductosNormales =
                productos.Count(
                    x =>
                        EsEstadoStock(
                            x.EstadoStock,
                            "normal"
                        )
                );


            ProductosSobreStock =
                productos.Count(
                    x =>
                        EsEstadoStock(
                            x.EstadoStock,
                            "sobre_stock"
                        )
                );


            // =================================================
            // PRECIOS
            // =================================================

            List<ProductoListaItem> productosActivos =
                productos
                    .Where(
                        x => x.Estado
                    )
                    .ToList();


            PrecioCompraPromedio =
                productosActivos.Count > 0
                    ? productosActivos.Average(
                        x => x.PrecioCompra
                    )
                    : 0M;


            PrecioVentaPromedio =
                productosActivos.Count > 0
                    ? productosActivos.Average(
                        x => x.PrecioVenta
                    )
                    : 0M;


            MargenPromedio =
                productosActivos.Count > 0
                    ? productosActivos.Average(
                        x => x.MargenPorcentaje
                    )
                    : 0M;


            // =================================================
            // VALORIZACIÓN
            // =================================================

            ValorInventarioCompra =
                productos.Sum(
                    x =>
                        Math.Max(
                            0M,
                            x.StockDisponible
                        )
                        *
                        x.PrecioCompra
                );


            ValorInventarioVenta =
                productos.Sum(
                    x =>
                        Math.Max(
                            0M,
                            x.StockDisponible
                        )
                        *
                        x.PrecioVenta
                );


            // =================================================
            // PRODUCTOS CRÍTICOS
            // =================================================

            ProductosCriticos =
                productos
                    .Where(
                        x =>
                            x.Estado
                            &&
                            x.ControlaStock
                            &&
                            EsCritico(
                                x.EstadoStock
                            )
                    )
                    .OrderBy(
                        x => x.StockDisponible
                    )
                    .ThenBy(
                        x => x.Nombre
                    )
                    .Take(8)
                    .ToList();


            // =================================================
            // MAYOR DISPONIBILIDAD
            // =================================================

            ProductosMayorStock =
                productos
                    .Where(
                        x =>
                            x.Estado
                            &&
                            x.ControlaStock
                    )
                    .OrderByDescending(
                        x => x.StockDisponible
                    )
                    .ThenBy(
                        x => x.Nombre
                    )
                    .Take(8)
                    .ToList();


            // =================================================
            // MAYOR MARGEN
            // =================================================

            ProductosMayorMargen =
                productosActivos
                    .OrderByDescending(
                        x => x.MargenPorcentaje
                    )
                    .ThenBy(
                        x => x.Nombre
                    )
                    .Take(8)
                    .ToList();


            _logger.LogInformation(
                "[INVENTARIO] Dashboard cargado. Productos={Productos} Activos={Activos} Stock={Stock} Disponible={Disponible} Alertas={Alertas} Almacenes={Almacenes} ValorCosto={ValorCosto} ValorVenta={ValorVenta}",
                TotalProductos,
                ProductosActivos,
                StockActual,
                StockDisponible,
                AlertasStock,
                TotalAlmacenes,
                ValorInventarioCompra,
                ValorInventarioVenta
            );


            return Page();
        }
        catch (SqlException ex)
        {
            _logger.LogError(
                ex,
                "[INVENTARIO] Error SQL cargando dashboard. Empresa={Empresa} Sucursal={Sucursal}",
                IdEmpresa,
                IdSucursal
            );


            MensajeError =
                $"No fue posible cargar el inventario. SQL {ex.Number}: {ex.Message}";


            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[INVENTARIO] Error general cargando dashboard."
            );


            MensajeError =
                "No fue posible cargar el dashboard de inventario.";


            return Page();
        }
    }


    // =========================================================
    // CARGAR TODOS LOS PRODUCTOS
    // =========================================================

    private async Task<List<ProductoListaItem>>
        CargarTodosLosProductosAsync(
            CancellationToken cancellationToken)
    {
        const int tamanoPagina =
            100;

        const int maximoPaginasSeguridad =
            100;


        List<ProductoListaItem> acumulado =
            new();


        for (
            int pagina = 1;
            pagina <= maximoPaginasSeguridad;
            pagina++
        )
        {
            ProductoFiltro filtro =
                CrearFiltroDashboard(
                    pagina,
                    tamanoPagina
                );


            ProductoListaResultado resultado =
                await _productoService
                    .ListarAsync(
                        filtro,
                        cancellationToken
                    );


            if (
                resultado.Items == null
                ||
                resultado.Items.Count == 0
            )
            {
                break;
            }


            acumulado.AddRange(
                resultado.Items
            );


            if (
                resultado.TotalRegistros > 0
                &&
                acumulado.Count >=
                resultado.TotalRegistros
            )
            {
                break;
            }


            if (
                resultado.Items.Count
                <
                tamanoPagina
            )
            {
                break;
            }
        }


        // Protección adicional por si el SP devolviera
        // registros repetidos entre páginas.
        return acumulado
            .GroupBy(
                x => x.IdProducto
            )
            .Select(
                g => g.First()
            )
            .ToList();
    }


    // =========================================================
    // FILTRO DEL DASHBOARD
    // =========================================================

    private ProductoFiltro CrearFiltroDashboard(
        int pagina,
        int tamanoPagina)
    {
        return new ProductoFiltro
        {
            IdEmpresa =
                IdEmpresa,

            IdCategoriaProducto =
                null,

            IdMarca =
                null,

            IdUnidadMedida =
                null,

            IdAlmacen =
                null,

            Estado =
                null,

            ControlaStock =
                null,

            EstadoStock =
                null,

            PrecioDesde =
                null,

            PrecioHasta =
                null,

            Buscar =
                null,

            OrdenarPor =
                "nombre",

            DireccionOrden =
                "ASC",

            Pagina =
                pagina,

            TamanoPagina =
                tamanoPagina
        };
    }


    // =========================================================
    // ESTADO STOCK
    // =========================================================

    private static bool EsEstadoStock(
        string? actual,
        string esperado)
    {
        return string.Equals(
            actual?.Trim(),
            esperado,
            StringComparison.OrdinalIgnoreCase
        );
    }


    // =========================================================
    // ES CRÍTICO
    // =========================================================

    private static bool EsCritico(
        string? estado)
    {
        return
            EsEstadoStock(
                estado,
                "sin_stock"
            )
            ||
            EsEstadoStock(
                estado,
                "bajo_stock"
            )
            ||
            EsEstadoStock(
                estado,
                "bajo_minimo"
            );
    }


    // =========================================================
    // HELPERS PARA LA VISTA
    // =========================================================

    public string ObtenerClaseStock(
        string? estado)
    {
        string valor =
            estado
                ?.Trim()
                .ToLowerInvariant()
            ??
            string.Empty;


        return valor switch
        {
            "sin_stock" =>
                "inv-stock-danger",

            "bajo_stock" =>
                "inv-stock-warning",

            "bajo_minimo" =>
                "inv-stock-warning",

            "sobre_stock" =>
                "inv-stock-info",

            "normal" =>
                "inv-stock-success",

            _ =>
                "inv-stock-neutral"
        };
    }


    public string FormatearEstadoStock(
        string? estado)
    {
        string valor =
            estado
                ?.Trim()
                .ToLowerInvariant()
            ??
            string.Empty;


        return valor switch
        {
            "sin_stock" =>
                "Sin stock",

            "bajo_stock" =>
                "Stock bajo",

            "bajo_minimo" =>
                "Bajo mínimo",

            "sobre_stock" =>
                "Sobre stock",

            "normal" =>
                "Normal",

            _ =>
                "Sin clasificación"
        };
    }
}
