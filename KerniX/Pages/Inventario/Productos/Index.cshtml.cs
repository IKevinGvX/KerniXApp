using System.Globalization;

using KerniX.Models.Inventario.Producto;
using KerniX.Security;
using KerniX.Services.Inventario;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Data.SqlClient;

namespace KerniX.Pages.Inventario.Productos;

public sealed class IndexModel : PageModel
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
    public List<int> GraficaProductos { get; private set; } =
    new();

    public List<int> GraficaStock { get; private set; } =
        new();

    public List<int> GraficaCriticos { get; private set; } =
        new();

    public List<int> GraficaMargen { get; private set; } =
        new();

    // =========================================================
    // CONTEXTO
    // =========================================================

    public int IdUsuario { get; private set; }

    public int IdEmpresa { get; private set; }

    public int IdSucursal { get; private set; }


    // =========================================================
    // DATOS
    // =========================================================

    [BindProperty(SupportsGet = true)]
    public ProductoFiltro Filtro { get; set; } =
        new();

    // =========================================================
    // FILTROS GET DIRECTOS
    //
    // Estos campos evitan depender del binding de un objeto
    // anidado (Filtro.Buscar, Filtro.IdMarca, etc.).
    // =========================================================

    [BindProperty(SupportsGet = true)]
    public string? Buscar { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? IdCategoriaProducto { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? IdMarca { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? IdAlmacen { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? EstadoStock { get; set; }

    public ProductoListaResultado Resultado { get; private set; } =
        new();

    public ProductoCatalogos Catalogos { get; private set; } =
        new();

    public ProductoResumenDashboard Resumen { get; private set; } =
        new();


    // =========================================================
    // GRÁFICOS
    // =========================================================

    public string GraficaProductosPuntos { get; private set; } =
        "0,60 300,60";

    public string GraficaStockPuntos { get; private set; } =
        "0,60 300,60";

    public string GraficaCriticosPuntos { get; private set; } =
        "0,60 300,60";

    public string GraficaMargenPuntos { get; private set; } =
        "0,60 300,60";


    // =========================================================
    // MENSAJES
    // =========================================================

    public string? MensajeError { get; private set; }

    [TempData]
    public string? MensajeExito { get; set; }


    // =========================================================
    // GET
    // =========================================================

    public async Task<IActionResult> OnGetAsync(
        CancellationToken cancellationToken)
    {
        if (!CargarContextoSesion())
        {
            return RedirectToPage(
                "/Account/Login"
            );
        }

        // =====================================================
        // COPIAR FILTROS GET DIRECTOS AL DTO QUE CONSUME SQL
        // =====================================================

        Filtro ??= new ProductoFiltro();

        // =====================================================
        // RESPALDO DIRECTO DE QUERY STRING
        // =====================================================

        if (
            string.IsNullOrWhiteSpace(Buscar) &&
            Request.Query.TryGetValue(
                "Buscar",
                out var buscarQuery
            )
        )
        {
            Buscar =
                buscarQuery.ToString();
        }

        if (
            !IdCategoriaProducto.HasValue &&
            Request.Query.TryGetValue(
                "IdCategoriaProducto",
                out var categoriaQuery
            ) &&
            int.TryParse(
                categoriaQuery.ToString(),
                out int categoriaValor
            )
        )
        {
            IdCategoriaProducto =
                categoriaValor;
        }

        if (
            !IdMarca.HasValue &&
            Request.Query.TryGetValue(
                "IdMarca",
                out var marcaQuery
            ) &&
            int.TryParse(
                marcaQuery.ToString(),
                out int marcaValor
            )
        )
        {
            IdMarca =
                marcaValor;
        }

        if (
            !IdAlmacen.HasValue &&
            Request.Query.TryGetValue(
                "IdAlmacen",
                out var almacenQuery
            ) &&
            int.TryParse(
                almacenQuery.ToString(),
                out int almacenValor
            )
        )
        {
            IdAlmacen =
                almacenValor;
        }

        if (
            string.IsNullOrWhiteSpace(EstadoStock) &&
            Request.Query.TryGetValue(
                "EstadoStock",
                out var estadoStockQuery
            )
        )
        {
            EstadoStock =
                estadoStockQuery.ToString();
        }


        Filtro.Buscar =
            Limpiar(
                Buscar
            );

        Filtro.IdCategoriaProducto =
            IdCategoriaProducto;

        Filtro.IdMarca =
            IdMarca;

        Filtro.IdAlmacen =
            IdAlmacen;

        Filtro.EstadoStock =
            Limpiar(
                EstadoStock
            );

        // Una nueva búsqueda siempre empieza en página 1.
        Filtro.Pagina = 1;

        NormalizarFiltro();

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


            // =================================================
            // LISTADO FILTRADO
            // =================================================

            Filtro.IdEmpresa =
                IdEmpresa;

            Resultado =
                await _productoService
                    .ListarAsync(
                        Filtro,
                        cancellationToken
                    );


            // Mantener los valores visibles después de aplicar el filtro.
            Buscar = Filtro.Buscar;
            IdCategoriaProducto = Filtro.IdCategoriaProducto;
            IdMarca = Filtro.IdMarca;
            IdAlmacen = Filtro.IdAlmacen;
            EstadoStock = Filtro.EstadoStock;


            // =================================================
            // TODOS LOS PRODUCTOS PARA KPI
            // =================================================

            List<ProductoListaItem> todos =
                await CargarTodosLosProductosAsync(
                    IdEmpresa,
                    cancellationToken
                );


            ConstruirResumen(
                todos
            );

            ConstruirGraficos(
                todos
            );


            _logger.LogInformation(
                "[PRODUCTOS] Página cargada. Query={Query} Usuario={Usuario} Empresa={Empresa} Buscar={Buscar} Categoria={Categoria} Marca={Marca} Almacen={Almacen} EstadoStock={EstadoStock} Registros={Registros} Total={Total}",
                Request.QueryString.Value,
                IdUsuario,
                IdEmpresa,
                Filtro.Buscar,
                Filtro.IdCategoriaProducto,
                Filtro.IdMarca,
                Filtro.IdAlmacen,
                Filtro.EstadoStock,
                Resultado.Items.Count,
                Resultado.TotalRegistros
            );


            return Page();
        }
        catch (SqlException ex)
        {
            _logger.LogError(
                ex,
                "[PRODUCTOS] Error SQL."
            );

            MensajeError =
                $"No fue posible cargar los productos. {ex.Message}";

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[PRODUCTOS] Error general."
            );

            MensajeError =
                "No fue posible cargar el módulo de productos.";

            return Page();
        }
    }


    // =========================================================
    // CAMBIAR ESTADO
    // =========================================================

    public async Task<IActionResult>
        OnPostCambiarEstadoAsync(
            int idProducto,
            bool estado,
            CancellationToken cancellationToken)
    {
        if (!CargarContextoSesion())
        {
            return RedirectToPage(
                "/Account/Login"
            );
        }


        if (idProducto <= 0)
        {
            TempData["ProductoError"] =
                "El producto indicado no es válido.";

            return RedirectToPage();
        }


        try
        {
            await _productoService
                .CambiarEstadoAsync(
                    idProducto,
                    estado,
                    IdUsuario,
                    cancellationToken
                );


            MensajeExito =
                estado
                    ? "Producto activado correctamente."
                    : "Producto desactivado correctamente.";
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[PRODUCTOS] Error modificando estado. Producto={Producto}",
                idProducto
            );

            TempData["ProductoError"] =
                ex.Message;
        }


        return RedirectToPage();
    }


    // =========================================================
    // CARGAR TODOS PARA DASHBOARD
    // =========================================================

    private async Task<List<ProductoListaItem>>
        CargarTodosLosProductosAsync(
            int idEmpresa,
            CancellationToken cancellationToken)
    {
        List<ProductoListaItem> resultado =
            new();

        int pagina = 1;
        const int tamano = 200;


        while (true)
        {
            ProductoFiltro filtro =
                new()
                {
                    IdEmpresa =
                        idEmpresa,

                    Pagina =
                        pagina,

                    TamanoPagina =
                        tamano,

                    OrdenarPor =
                        "nombre",

                    DireccionOrden =
                        "ASC"
                };


            ProductoListaResultado bloque =
                await _productoService
                    .ListarAsync(
                        filtro,
                        cancellationToken
                    );


            resultado.AddRange(
                bloque.Items
            );


            if (
                resultado.Count >= bloque.TotalRegistros ||
                bloque.Items.Count == 0
            )
            {
                break;
            }


            pagina++;
        }


        return resultado;
    }


    // =========================================================
    // RESUMEN
    // =========================================================

    private void ConstruirResumen(
        List<ProductoListaItem> productos)
    {
        int total =
            productos.Count;


        Resumen =
            new ProductoResumenDashboard
            {
                TotalProductos =
                    total,

                ProductosActivos =
                    productos.Count(
                        x => x.Estado
                    ),

                ProductosInactivos =
                    productos.Count(
                        x => !x.Estado
                    ),

                ProductosControlStock =
                    productos.Count(
                        x => x.ControlaStock
                    ),

                ProductosSinStock =
                    productos.Count(
                        x => EsEstado(
                            x.EstadoStock,
                            "sin_stock"
                        )
                    ),

                ProductosBajoStock =
                    productos.Count(
                        x => EsEstado(
                            x.EstadoStock,
                            "bajo_stock"
                        )
                    ),

                ProductosNormal =
                    productos.Count(
                        x => EsEstado(
                            x.EstadoStock,
                            "normal"
                        )
                    ),

                ProductosSobreStock =
                    productos.Count(
                        x => EsEstado(
                            x.EstadoStock,
                            "sobre_stock"
                        )
                    ),

                StockActualTotal =
                    productos.Sum(
                        x => x.StockActual
                    ),

                StockReservadoTotal =
                    productos.Sum(
                        x => x.StockReservado
                    ),

                StockDisponibleTotal =
                    productos.Sum(
                        x => x.StockDisponible
                    ),

                MargenPromedio =
                    total == 0
                        ? 0M
                        : productos.Average(
                            x => x.MargenPorcentaje
                        ),

                PrecioCompraPromedio =
                    total == 0
                        ? 0M
                        : productos.Average(
                            x => x.PrecioCompra
                        ),

                PrecioVentaPromedio =
                    total == 0
                        ? 0M
                        : productos.Average(
                            x => x.PrecioVenta
                        ),

                ValorVentaDisponible =
                    productos.Sum(
                        x =>
                            x.StockDisponible *
                            x.PrecioVenta
                    ),

                ValorCompraDisponible =
                    productos.Sum(
                        x =>
                            x.StockDisponible *
                            x.PrecioCompra
                    )
            };


        Resumen.ProductosCriticos =
            Resumen.ProductosSinStock +
            Resumen.ProductosBajoStock;


        ProductoListaItem? mayor =
            productos
                .OrderByDescending(
                    x => x.StockDisponible
                )
                .FirstOrDefault();


        if (mayor != null)
        {
            Resumen.ProductoMayorStock =
                mayor.Nombre;

            Resumen.MayorStockDisponible =
                mayor.StockDisponible;
        }
    }


    // =========================================================
    // GRÁFICOS REALES
    // =========================================================

    private void ConstruirGraficos(
        List<ProductoListaItem> productos)
    {
        List<decimal> categorias =
            productos
                .GroupBy(
                    x =>
                        string.IsNullOrWhiteSpace(
                            x.Categoria
                        )
                            ? "Sin categoría"
                            : x.Categoria
                )
                .Select(
                    g => (decimal)g.Count()
                )
                .OrderByDescending(
                    x => x
                )
                .Take(8)
                .ToList();


        List<decimal> stocks =
            productos
                .OrderByDescending(
                    x => x.StockDisponible
                )
                .Take(8)
                .Select(
                    x => x.StockDisponible
                )
                .ToList();


        List<decimal> criticos =
            new()
            {
                Resumen.ProductosNormal,
                Resumen.ProductosSobreStock,
                Resumen.ProductosBajoStock,
                Resumen.ProductosSinStock
            };


        List<decimal> margenes =
            productos
                .OrderBy(
                    x => x.Nombre
                )
                .Take(8)
                .Select(
                    x => x.MargenPorcentaje
                )
                .ToList();


        GraficaProductosPuntos =
            CrearPuntosGrafica(
                categorias
            );

        GraficaStockPuntos =
            CrearPuntosGrafica(
                stocks
            );

        GraficaCriticosPuntos =
            CrearPuntosGrafica(
                criticos
            );

        GraficaMargenPuntos =
            CrearPuntosGrafica(
                margenes
            );
    }


    private static string CrearPuntosGrafica(
        IReadOnlyList<decimal> valores)
    {
        const decimal ancho = 300M;
        const decimal alto = 70M;
        const decimal padding = 5M;


        if (valores.Count == 0)
        {
            return "0,60 300,60";
        }


        decimal maximo =
            valores.Max();

        decimal minimo =
            valores.Min();


        if (maximo == minimo)
        {
            maximo =
                minimo + 1M;
        }


        List<string> puntos =
            new();


        for (
            int i = 0;
            i < valores.Count;
            i++
        )
        {
            decimal x =
                valores.Count == 1
                    ? ancho / 2M
                    : (
                        i *
                        ancho /
                        (valores.Count - 1M)
                    );


            decimal normalizado =
                (
                    valores[i] -
                    minimo
                )
                /
                (
                    maximo -
                    minimo
                );


            decimal y =
                alto -
                padding -
                (
                    normalizado *
                    (
                        alto -
                        padding * 2M
                    )
                );


            puntos.Add(
                string.Create(
                    CultureInfo.InvariantCulture,
                    $"{x:0.##},{y:0.##}"
                )
            );
        }


        return string.Join(
            " ",
            puntos
        );
    }


    // =========================================================
    // SESIÓN
    // =========================================================

    private bool CargarContextoSesion()
    {
        IdUsuario =
            ObtenerEnteroSesion(
                SessionKeys.IdUsuario
            );


        IdEmpresa =
            ObtenerEnteroSesion(
                "IdEmpresa"
            );


        IdSucursal =
            ObtenerEnteroSesion(
                "IdSucursal"
            );


        if (IdUsuario <= 0)
        {
            return false;
        }


        if (IdEmpresa <= 0)
        {
            IdEmpresa = 1;
        }


        if (IdSucursal <= 0)
        {
            IdSucursal = 1;
        }


        return true;
    }


    private int ObtenerEnteroSesion(
        string clave)
    {
        int? entero =
            HttpContext.Session.GetInt32(
                clave
            );


        if (entero.HasValue)
        {
            return entero.Value;
        }


        string? texto =
            HttpContext.Session.GetString(
                clave
            );


        return int.TryParse(
            texto,
            out int numero
        )
            ? numero
            : 0;
    }


    // =========================================================
    // FILTROS
    // =========================================================

    private void NormalizarFiltro()
    {
        Filtro.Pagina =
            Math.Max(
                Filtro.Pagina,
                1
            );


        Filtro.TamanoPagina =
            Filtro.TamanoPagina switch
            {
                10 => 10,
                20 => 20,
                50 => 50,
                100 => 100,

                _ => 20
            };


        Filtro.Buscar =
            Limpiar(
                Filtro.Buscar
            );


        Filtro.EstadoStock =
            Limpiar(
                Filtro.EstadoStock
            );


        Filtro.OrdenarPor =
            string.IsNullOrWhiteSpace(
                Filtro.OrdenarPor
            )
                ? "nombre"
                : Filtro.OrdenarPor;


        Filtro.DireccionOrden =
            string.Equals(
                Filtro.DireccionOrden,
                "DESC",
                StringComparison.OrdinalIgnoreCase
            )
                ? "DESC"
                : "ASC";
    }


    private static string? Limpiar(
        string? texto)
    {
        return string.IsNullOrWhiteSpace(
            texto
        )
            ? null
            : texto.Trim();
    }


    // =========================================================
    // HELPERS VISTA
    // =========================================================

    public string ObtenerClaseStock(
        string? estado)
    {
        return (
            estado ??
            string.Empty
        )
        .Trim()
        .ToLowerInvariant()
        switch
        {
            "sin_stock" =>
                "kx-stock-danger",

            "bajo_stock" =>
                "kx-stock-warning",

            "sobre_stock" =>
                "kx-stock-over",

            "normal" =>
                "kx-stock-normal",

            _ =>
                "kx-stock-neutral"
        };
    }


    public string FormatearEstadoStock(
        string? estado)
    {
        return (
            estado ??
            string.Empty
        )
        .Trim()
        .ToLowerInvariant()
        switch
        {
            "sin_stock" =>
                "Sin stock",

            "bajo_stock" =>
                "Stock bajo",

            "sobre_stock" =>
                "Sobre stock",

            "normal" =>
                "Normal",

            _ =>
                "No aplica"
        };
    }


    public string CrearUrlPagina(
        int pagina)
    {
        Dictionary<string, string?> parametros =
            new()
            {
                ["Filtro.Pagina"] =
                    pagina.ToString(),

                ["Filtro.TamanoPagina"] =
                    Filtro.TamanoPagina.ToString(),

                ["Buscar"] =
                    Filtro.Buscar,

                ["IdCategoriaProducto"] =
                    Filtro.IdCategoriaProducto?.ToString(),

                ["IdMarca"] =
                    Filtro.IdMarca?.ToString(),

                ["Filtro.IdUnidadMedida"] =
                    Filtro.IdUnidadMedida?.ToString(),

                ["IdAlmacen"] =
                    Filtro.IdAlmacen?.ToString(),

                ["EstadoStock"] =
                    Filtro.EstadoStock
            };


        return QueryHelpers.AddQueryString(
            "/Inventario/Productos",
            parametros
        );
    }


    private static bool EsEstado(
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
    // DTO DASHBOARD
    // =========================================================

    public sealed class ProductoResumenDashboard
    {
        public int TotalProductos { get; set; }

        public int ProductosActivos { get; set; }

        public int ProductosInactivos { get; set; }

        public int ProductosControlStock { get; set; }

        public int ProductosSinStock { get; set; }

        public int ProductosBajoStock { get; set; }

        public int ProductosNormal { get; set; }

        public int ProductosSobreStock { get; set; }

        public int ProductosCriticos { get; set; }

        public decimal StockActualTotal { get; set; }

        public decimal StockReservadoTotal { get; set; }

        public decimal StockDisponibleTotal { get; set; }

        public decimal MargenPromedio { get; set; }

        public decimal PrecioCompraPromedio { get; set; }

        public decimal PrecioVentaPromedio { get; set; }

        public decimal ValorCompraDisponible { get; set; }

        public decimal ValorVentaDisponible { get; set; }

        public string ProductoMayorStock { get; set; } =
            "Sin información";

        public decimal MayorStockDisponible { get; set; }
    }
}