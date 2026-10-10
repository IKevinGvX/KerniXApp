using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using KerniX.Models.Inventario.Producto;
using KerniX.Security;
using KerniX.Services.Inventario;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace KerniX.Pages.Inventario.Productos;

public sealed class DetalleModel : PageModel
{
    // =========================================================
    // SERVICIOS
    // =========================================================

    private readonly IProductoDbService _productoService;
    private readonly ILogger<DetalleModel> _logger;


    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public DetalleModel(
        IProductoDbService productoService,
        ILogger<DetalleModel> logger)
    {
        _productoService =
            productoService;

        _logger =
            logger;
    }


    // =========================================================
    // CONTEXTO
    // =========================================================

    public int IdUsuario { get; private set; }

    public int IdEmpresa { get; private set; }

    public int IdSucursal { get; private set; }


    // =========================================================
    // DATOS
    // =========================================================

    public ProductoDetalle? Producto
    {
        get;
        private set;
    }


    public ProductoStockResultado Stock
    {
        get;
        private set;
    } = new();


    // =========================================================
    // MENSAJE
    // =========================================================

    public string? MensajeError
    {
        get;
        private set;
    }


    // =========================================================
    // PROPIEDADES CALCULADAS
    // =========================================================

    public decimal StockActual =>
        Stock.StockActualTotal;


    public decimal StockReservado =>
        Stock.StockReservadoTotal;


    public decimal StockDisponible =>
        Stock.StockDisponibleTotal;


    public decimal ValorCompra =>
        Producto == null
            ? 0M
            : StockDisponible *
              Producto.PrecioCompra;


    public decimal ValorVenta =>
        Producto == null
            ? 0M
            : StockDisponible *
              Producto.PrecioVenta;


    public decimal UtilidadPotencial =>
        ValorVenta -
        ValorCompra;


    public int TotalAlmacenes =>
        Stock.Almacenes.Count;


    public bool SinStock =>
        Producto != null &&
        Producto.ControlaStock &&
        StockDisponible <= 0;


    public bool BajoMinimo =>
        Producto != null &&
        Producto.ControlaStock &&
        StockDisponible > 0 &&
        StockDisponible <
        Producto.StockMinimo;


    public bool SobreStock =>
        Producto != null &&
        Producto.ControlaStock &&
        Producto.StockMaximo.HasValue &&
        StockDisponible >
        Producto.StockMaximo.Value;


    // =========================================================
    // GET
    // =========================================================

    public async Task<IActionResult> OnGetAsync(
        int idProducto,
        CancellationToken cancellationToken)
    {
        // =====================================================
        // VALIDAR ID
        // =====================================================

        if (idProducto <= 0)
        {
            return BadRequest(
                "El identificador del producto no es válido."
            );
        }


        // =====================================================
        // SESIÓN
        // =====================================================

        if (!CargarContextoSesion())
        {
            return RedirectToPage(
                "/Account/Login"
            );
        }


        _logger.LogInformation(
            "[PRODUCTOS DETALLE] Producto={Producto} Usuario={Usuario} Empresa={Empresa}",
            idProducto,
            IdUsuario,
            IdEmpresa
        );


        try
        {
            // =================================================
            // PRODUCTO
            // =================================================

            Producto =
                await _productoService
                    .ObtenerAsync(
                        idProducto,
                        cancellationToken
                    );


            if (Producto == null)
            {
                return NotFound(
                    "No se encontró el producto solicitado."
                );
            }


            // =================================================
            // SEGURIDAD POR EMPRESA
            // =================================================

            if (
                Producto.IdEmpresa > 0 &&
                Producto.IdEmpresa != IdEmpresa
            )
            {
                _logger.LogWarning(
                    "[PRODUCTOS DETALLE] Intento de acceso a producto de otra empresa. Producto={Producto} EmpresaProducto={EmpresaProducto} EmpresaSesion={EmpresaSesion}",
                    idProducto,
                    Producto.IdEmpresa,
                    IdEmpresa
                );


                return StatusCode(
                    StatusCodes.Status403Forbidden,
                    "El producto no pertenece a la empresa seleccionada."
                );
            }


            // =================================================
            // STOCK
            // =================================================

            Stock =
                await _productoService
                    .ObtenerStockAsync(
                        idProducto,
                        IdEmpresa,
                        cancellationToken
                    );


            _logger.LogInformation(
                "[PRODUCTOS DETALLE] Producto cargado. Producto={Producto} Almacenes={Almacenes} Disponible={Disponible}",
                idProducto,
                Stock.Almacenes.Count,
                StockDisponible
            );


            return Page();
        }
        catch (SqlException ex)
        {
            _logger.LogError(
                ex,
                "[PRODUCTOS DETALLE] Error SQL. Producto={Producto}",
                idProducto
            );


            MensajeError =
                $"No fue posible consultar el producto. {ex.Message}";


            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[PRODUCTOS DETALLE] Error general. Producto={Producto}",
                idProducto
            );


            MensajeError =
                "No fue posible cargar la información del producto.";


            return Page();
        }
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


        // =====================================================
        // TEMPORAL PARA TU ENTORNO ACTUAL
        // =====================================================

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
        int? numero =
            HttpContext.Session.GetInt32(
                clave
            );


        if (numero.HasValue)
        {
            return numero.Value;
        }


        string? texto =
            HttpContext.Session.GetString(
                clave
            );


        return int.TryParse(
            texto,
            out int resultado
        )
            ? resultado
            : 0;
    }


    // =========================================================
    // ESTADO STOCK
    // =========================================================

    public string ObtenerEstadoStock()
    {
        if (Producto == null)
        {
            return "No disponible";
        }


        if (!Producto.ControlaStock)
        {
            return "Sin control";
        }


        if (SinStock)
        {
            return "Sin stock";
        }


        if (BajoMinimo)
        {
            return "Bajo mínimo";
        }


        if (SobreStock)
        {
            return "Sobre stock";
        }


        return "Normal";
    }


    public string ObtenerClaseEstadoStock()
    {
        if (Producto == null)
        {
            return "kx-stock-neutral";
        }


        if (!Producto.ControlaStock)
        {
            return "kx-stock-neutral";
        }


        if (SinStock)
        {
            return "kx-stock-danger";
        }


        if (BajoMinimo)
        {
            return "kx-stock-warning";
        }


        if (SobreStock)
        {
            return "kx-stock-info";
        }


        return "kx-stock-success";
    }


    // =========================================================
    // PORCENTAJE STOCK ALMACÉN
    // =========================================================

    public decimal PorcentajeStock(
        ProductoStockItem item)
    {
        if (
            StockDisponible <= 0 ||
            item.StockDisponible <= 0
        )
        {
            return 0M;
        }


        decimal porcentaje =
            (
                item.StockDisponible /
                StockDisponible
            )
            * 100M;


        return Math.Clamp(
            porcentaje,
            0M,
            100M
        );
    }
}