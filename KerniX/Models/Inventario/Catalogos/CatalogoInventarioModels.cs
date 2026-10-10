using System;

namespace KerniX.Models.Inventario.Catalogos;


// ============================================================
// CATEGORÍA DE PRODUCTO
// ============================================================

public sealed class CategoriaProductoAdminItem
{
    public int IdCategoriaProducto { get; set; }

    public int IdEmpresa { get; set; }

    public string Nombre { get; set; } =
        string.Empty;

    public string? Descripcion { get; set; }

    public bool Estado { get; set; }

    public DateTime FechaRegistro { get; set; }

    public int TotalProductos { get; set; }
}


public class CategoriaProductoGuardarRequest
{
    public int IdEmpresa { get; set; }

    public string Nombre { get; set; } =
        string.Empty;

    public string? Descripcion { get; set; }

    public bool Estado { get; set; } =
        true;
}


public sealed class CategoriaProductoActualizarRequest
    : CategoriaProductoGuardarRequest
{
    public int IdCategoriaProducto { get; set; }
}


// ============================================================
// UNIDAD DE MEDIDA
// ============================================================

public sealed class UnidadMedidaAdminItem
{
    public int IdUnidadMedida { get; set; }

    public string Codigo { get; set; } =
        string.Empty;

    public string Nombre { get; set; } =
        string.Empty;

    public string Abreviatura { get; set; } =
        string.Empty;

    public bool Estado { get; set; }

    public int TotalProductos { get; set; }
}


public class UnidadMedidaGuardarRequest
{
    public string Codigo { get; set; } =
        string.Empty;

    public string Nombre { get; set; } =
        string.Empty;

    public string Abreviatura { get; set; } =
        string.Empty;

    public bool Estado { get; set; } =
        true;
}


public sealed class UnidadMedidaActualizarRequest
    : UnidadMedidaGuardarRequest
{
    public int IdUnidadMedida { get; set; }
}


// ============================================================
// MARCA
// ============================================================

public sealed class MarcaAdminItem
{
    public int IdMarca { get; set; }

    public int IdEmpresa { get; set; }

    public string Nombre { get; set; } =
        string.Empty;

    public string? Descripcion { get; set; }

    public bool Estado { get; set; }

    public DateTime FechaRegistro { get; set; }

    public int TotalProductos { get; set; }
}


public class MarcaGuardarRequest
{
    public int IdEmpresa { get; set; }

    public string Nombre { get; set; } =
        string.Empty;

    public string? Descripcion { get; set; }

    public bool Estado { get; set; } =
        true;
}


public sealed class MarcaActualizarRequest
    : MarcaGuardarRequest
{
    public int IdMarca { get; set; }
}
