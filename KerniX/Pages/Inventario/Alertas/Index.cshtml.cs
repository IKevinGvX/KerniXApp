using KerniX.Models.Inventario.Producto;
using KerniX.Services.Inventario;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace KerniX.Pages.Inventario.Alertas;

public sealed class IndexModel
    : InventarioEnterprisePageModel
{
    private readonly IProductoDbService _productoService;
    private readonly ILogger<IndexModel> _logger;


    public IndexModel(
        IProductoDbService productoService,
        ILogger<IndexModel> logger)
    {
        _productoService =
            productoService;

        _logger =
            logger;
    }


    [BindProperty(SupportsGet = true)]
    public string? Buscar
    {
        get;
        set;
    }


    [BindProperty(SupportsGet = true)]
    public string? Nivel
    {
        get;
        set;
    }


    public List<ProductoListaItem> Items
    {
        get;
        private set;
    } = new();


    public int SinStock
    {
        get;
        private set;
    }


    public int BajoStock
    {
        get;
        private set;
    }


    public int SobreStock
    {
        get;
        private set;
    }


    public int TotalAlertas =>
        SinStock
        +
        BajoStock
        +
        SobreStock;


    public decimal StockDisponibleAlertado
    {
        get;
        private set;
    }


    public string? MensajeError
    {
        get;
        private set;
    }


    public async Task<IActionResult> OnGetAsync(
        CancellationToken cancellationToken)
    {
        if (
            !CargarSesion(
                requiereEmpleado: false
            )
        )
        {
            return IrLogin();
        }


        try
        {
            List<ProductoListaItem> productos =
                await CargarTodosLosProductosAsync(
                    cancellationToken
                );


            List<ProductoListaItem> alertas =
                productos
                    .Where(
                        x =>
                            x.Estado
                            &&
                            x.ControlaStock
                            &&
                            EsAlerta(
                                x.EstadoStock
                            )
                    )
                    .ToList();


            SinStock =
                alertas.Count(
                    x =>
                        EsEstado(
                            x.EstadoStock,
                            "sin_stock"
                        )
                );


            BajoStock =
                alertas.Count(
                    x =>
                        EsEstado(
                            x.EstadoStock,
                            "bajo_stock"
                        )
                        ||
                        EsEstado(
                            x.EstadoStock,
                            "bajo_minimo"
                        )
                );


            SobreStock =
                alertas.Count(
                    x =>
                        EsEstado(
                            x.EstadoStock,
                            "sobre_stock"
                        )
                );


            StockDisponibleAlertado =
                alertas.Sum(
                    x => x.StockDisponible
                );


            IEnumerable<ProductoListaItem> consulta =
                alertas;


            string nivel =
                (
                    Nivel
                    ??
                    string.Empty
                )
                .Trim()
                .ToLowerInvariant();


            consulta =
                nivel switch
                {
                    "critico" =>
                        consulta.Where(
                            x =>
                                EsEstado(
                                    x.EstadoStock,
                                    "sin_stock"
                                )
                        ),

                    "bajo" =>
                        consulta.Where(
                            x =>
                                EsEstado(
                                    x.EstadoStock,
                                    "bajo_stock"
                                )
                                ||
                                EsEstado(
                                    x.EstadoStock,
                                    "bajo_minimo"
                                )
                        ),

                    "sobre" =>
                        consulta.Where(
                            x =>
                                EsEstado(
                                    x.EstadoStock,
                                    "sobre_stock"
                                )
                        ),

                    _ =>
                        consulta
                };


            if (
                !string.IsNullOrWhiteSpace(
                    Buscar
                )
            )
            {
                string q =
                    Buscar.Trim();


                consulta =
                    consulta.Where(
                        x =>
                            x.Codigo.Contains(
                                q,
                                StringComparison.OrdinalIgnoreCase
                            )
                            ||
                            x.Nombre.Contains(
                                q,
                                StringComparison.OrdinalIgnoreCase
                            )
                            ||
                            (
                                x.EstadoStock
                                    ?.Contains(
                                        q,
                                        StringComparison.OrdinalIgnoreCase
                                    )
                                ??
                                false
                            )
                    );
            }


            Items =
                consulta
                    .OrderBy(
                        x =>
                            Prioridad(
                                x.EstadoStock
                            )
                    )
                    .ThenBy(
                        x => x.StockDisponible
                    )
                    .ThenBy(
                        x => x.Nombre
                    )
                    .ToList();


            _logger.LogInformation(
                "[ALERTAS INVENTARIO] Cargado. Empresa={Empresa} Sucursal={Sucursal} Total={Total} SinStock={SinStock} Bajo={Bajo} Sobre={Sobre}",
                IdEmpresa,
                IdSucursal,
                TotalAlertas,
                SinStock,
                BajoStock,
                SobreStock
            );


            return Page();
        }
        catch (SqlException ex)
        {
            _logger.LogError(
                ex,
                "[ALERTAS INVENTARIO] Error SQL."
            );


            MensajeError =
                $"SQL {ex.Number}: {ex.Message}";


            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[ALERTAS INVENTARIO] Error general."
            );


            MensajeError =
                ex.Message;


            return Page();
        }
    }


    private async Task<List<ProductoListaItem>>
        CargarTodosLosProductosAsync(
            CancellationToken cancellationToken)
    {
        const int tamanoPagina =
            100;

        const int maximoPaginas =
            100;


        List<ProductoListaItem> acumulado =
            new();


        for (
            int pagina = 1;
            pagina <= maximoPaginas;
            pagina++
        )
        {
            ProductoFiltro filtro =
                new()
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
                        true,

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


        return acumulado
            .GroupBy(
                x => x.IdProducto
            )
            .Select(
                g => g.First()
            )
            .ToList();
    }


    public string ClaseEstado(
        string? estado)
    {
        string valor =
            (
                estado
                ??
                string.Empty
            )
            .Trim()
            .ToLowerInvariant();


        return valor switch
        {
            "sin_stock" =>
                "critical",

            "bajo_stock" =>
                "warning",

            "bajo_minimo" =>
                "warning",

            "sobre_stock" =>
                "info",

            _ =>
                "neutral"
        };
    }


    public string TextoEstado(
        string? estado)
    {
        string valor =
            (
                estado
                ??
                string.Empty
            )
            .Trim()
            .ToLowerInvariant();


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

            _ =>
                "Sin clasificación"
        };
    }


    public string TextoPrioridad(
        string? estado)
    {
        string valor =
            (
                estado
                ??
                string.Empty
            )
            .Trim()
            .ToLowerInvariant();


        return valor switch
        {
            "sin_stock" =>
                "Crítica",

            "bajo_stock" =>
                "Alta",

            "bajo_minimo" =>
                "Alta",

            "sobre_stock" =>
                "Media",

            _ =>
                "Normal"
        };
    }


    private static int Prioridad(
        string? estado)
    {
        string valor =
            (
                estado
                ??
                string.Empty
            )
            .Trim()
            .ToLowerInvariant();


        return valor switch
        {
            "sin_stock" =>
                1,

            "bajo_stock" =>
                2,

            "bajo_minimo" =>
                2,

            "sobre_stock" =>
                3,

            _ =>
                9
        };
    }


    private static bool EsAlerta(
        string? estado)
    {
        return
            EsEstado(
                estado,
                "sin_stock"
            )
            ||
            EsEstado(
                estado,
                "bajo_stock"
            )
            ||
            EsEstado(
                estado,
                "bajo_minimo"
            )
            ||
            EsEstado(
                estado,
                "sobre_stock"
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
}
