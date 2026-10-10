using System;
using System.Collections.Generic;

namespace KerniX.Models.Inventario.Producto;

public sealed class ProductoListaItem
{
    public int IdProducto { get; set; }

    public int IdEmpresa { get; set; }

    public string Empresa { get; set; } =
        string.Empty;

    public int IdCategoriaProducto { get; set; }

    public string Categoria { get; set; } =
        string.Empty;

    public int? IdMarca { get; set; }

    public string Marca { get; set; } =
        string.Empty;

    public int IdUnidadMedida { get; set; }

    public string UnidadMedida { get; set; } =
        string.Empty;

    public string AbreviaturaUnidad { get; set; } =
        string.Empty;

    public string Codigo { get; set; } =
        string.Empty;

    public string? CodigoBarras { get; set; }

    public string Nombre { get; set; } =
        string.Empty;

    public string? Descripcion { get; set; }

    public decimal PrecioCompra { get; set; }

    public decimal PrecioVenta { get; set; }

    public decimal MargenMonto { get; set; }

    public decimal MargenPorcentaje { get; set; }

    public decimal StockActual { get; set; }

    public decimal StockReservado { get; set; }

    public decimal StockDisponible { get; set; }

    public decimal StockMinimo { get; set; }

    public decimal? StockMaximo { get; set; }

    public bool ControlaStock { get; set; }

    public string EstadoStock { get; set; } =
        string.Empty;

    public int TotalAlmacenes { get; set; }

    public bool Estado { get; set; }

    public DateTime FechaRegistro { get; set; }
}


public sealed class ProductoDetalle
{
    public int IdProducto { get; set; }

    public int IdEmpresa { get; set; }

    public int IdCategoriaProducto { get; set; }

    public int? IdMarca { get; set; }

    public int IdUnidadMedida { get; set; }

    public string Codigo { get; set; } =
        string.Empty;

    public string? CodigoBarras { get; set; }

    public string Nombre { get; set; } =
        string.Empty;

    public string? Descripcion { get; set; }

    public decimal PrecioCompra { get; set; }

    public decimal PrecioVenta { get; set; }

    public decimal StockMinimo { get; set; }

    public decimal? StockMaximo { get; set; }

    public bool ControlaStock { get; set; }

    public bool Estado { get; set; }

    public DateTime FechaRegistro { get; set; }

    public string Categoria { get; set; } =
        string.Empty;

    public string Marca { get; set; } =
        string.Empty;

    public string UnidadMedida { get; set; } =
        string.Empty;

    public string AbreviaturaUnidad { get; set; } =
        string.Empty;

    public decimal MargenMonto { get; set; }

    public decimal MargenPorcentaje { get; set; }
}


public sealed class ProductoFiltro
{
    public int IdEmpresa { get; set; }

    public int? IdCategoriaProducto { get; set; }

    public int? IdMarca { get; set; }

    public int? IdUnidadMedida { get; set; }

    public int? IdAlmacen { get; set; }

    public bool? Estado { get; set; }

    public bool? ControlaStock { get; set; }

    public string? EstadoStock { get; set; }

    public decimal? PrecioDesde { get; set; }

    public decimal? PrecioHasta { get; set; }

    public string? Buscar { get; set; }

    public string OrdenarPor { get; set; } =
        "nombre";

    public string DireccionOrden { get; set; } =
        "ASC";

    public int Pagina { get; set; } = 1;

    public int TamanoPagina { get; set; } = 20;
}


public sealed class ProductoListaResultado
{
    public List<ProductoListaItem> Items { get; set; } =
        new();

    public int TotalRegistros { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }

    public int TotalPaginas =>
        TamanoPagina <= 0
            ? 0
            : (int)Math.Ceiling(
                TotalRegistros /
                (double)TamanoPagina
            );
}
public class ProductoGuardarRequest
{
    public int IdEmpresa { get; set; }

    public int IdCategoriaProducto { get; set; }

    public int? IdMarca { get; set; }

    public int IdUnidadMedida { get; set; }

    public string Codigo { get; set; } =
        string.Empty;

    public string? CodigoBarras { get; set; }

    public string Nombre { get; set; } =
        string.Empty;

    public string? Descripcion { get; set; }

    public decimal PrecioCompra { get; set; }

    public decimal PrecioVenta { get; set; }

    public decimal StockMinimo { get; set; }

    public decimal? StockMaximo { get; set; }

    public bool ControlaStock { get; set; }

    public int IdUsuario { get; set; }
}


public sealed class ProductoActualizarRequest
    : ProductoGuardarRequest
{
    public int IdProducto { get; set; }

    public bool Estado { get; set; }
}
public sealed class ProductoCatalogos
{
    public List<CatalogoItem> Categorias { get; set; } =
        new();

    public List<CatalogoItem> Marcas { get; set; } =
        new();

    public List<CatalogoItem> UnidadesMedida { get; set; } =
        new();

    public List<CatalogoItem> Sucursales { get; set; } =
        new();

    public List<AlmacenCatalogoItem> Almacenes { get; set; } =
        new();
}


public class CatalogoItem
{
    public int Id { get; set; }

    public string Nombre { get; set; } =
        string.Empty;
}


public sealed class AlmacenCatalogoItem
    : CatalogoItem
{
    public int IdSucursal { get; set; }
}


public sealed class ProductoStockItem
{
    public long? IdStock { get; set; }

    public int IdAlmacen { get; set; }

    public string Almacen { get; set; } =
        string.Empty;

    public int? IdSucursal { get; set; }

    public string Sucursal { get; set; } =
        string.Empty;

    public decimal CantidadActual { get; set; }

    public decimal CantidadReservada { get; set; }

    public decimal StockDisponible { get; set; }

    public decimal StockMinimo { get; set; }

    public decimal? StockMaximo { get; set; }

    public string EstadoStock { get; set; } =
        string.Empty;

    public decimal ValorInventarioCompra { get; set; }

    public decimal ValorInventarioVenta { get; set; }
}


public sealed class ProductoStockResultado
{
    public List<ProductoStockItem> Almacenes { get; set; } =
        new();

    public decimal StockActualTotal { get; set; }

    public decimal StockReservadoTotal { get; set; }

    public decimal StockDisponibleTotal { get; set; }

    public decimal ValorCompraTotal { get; set; }

    public decimal ValorVentaTotal { get; set; }
}