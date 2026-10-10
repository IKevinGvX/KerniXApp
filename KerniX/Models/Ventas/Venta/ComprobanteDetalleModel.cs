namespace KerniX.Models.Ventas.Comprobante;

public sealed class ComprobanteDetalleModel
{
    public ComprobanteCabeceraModel Cabecera { get; set; }
        = new();

    public List<ComprobanteProductoModel> Productos { get; set; }
        = new();
}


public sealed class ComprobanteCabeceraModel
{
    public long IdVenta { get; set; }

    public string NumeroVenta { get; set; }
        = string.Empty;

    public DateTime FechaVenta { get; set; }

    public decimal VentaSubtotal { get; set; }

    public decimal VentaDescuento { get; set; }

    public decimal VentaImpuesto { get; set; }

    public decimal VentaTotal { get; set; }

    public string VentaEstado { get; set; }
        = string.Empty;

    public string? Observacion { get; set; }


    /* =========================
       COMPROBANTE
       ========================= */

    public long? IdComprobante { get; set; }

    public string TipoComprobante { get; set; }
        = string.Empty;

    public string Serie { get; set; }
        = string.Empty;

    public string Numero { get; set; }
        = string.Empty;

    public string NumeroComprobante { get; set; }
        = string.Empty;

    public DateTime? FechaEmision { get; set; }

    public decimal ComprobanteSubtotal { get; set; }

    public decimal ComprobanteImpuesto { get; set; }

    public decimal ComprobanteTotal { get; set; }

    public string? EstadoSunat { get; set; }

    public string? CodigoHash { get; set; }

    public string? RutaXml { get; set; }

    public string? RutaPdf { get; set; }

    public string? CodigoRespuestaSunat { get; set; }

    public string? MensajeRespuestaSunat { get; set; }

    public DateTime? FechaEnvioSunat { get; set; }

    public DateTime? FechaRespuestaSunat { get; set; }

    public string? RutaCdr { get; set; }

    public int IntentosSunat { get; set; }

    public string? TicketSunat { get; set; }


    /* =========================
       CLIENTE
       ========================= */

    public int IdCliente { get; set; }

    public string TipoCliente { get; set; }
        = string.Empty;

    public string ClienteTipoDocumento { get; set; }
        = string.Empty;

    public string ClienteNumeroDocumento { get; set; }
        = string.Empty;

    public string ClienteNombre { get; set; }
        = string.Empty;

    public string? ClienteRazonSocial { get; set; }

    public string? ClienteNombres { get; set; }

    public string? ClienteApellidos { get; set; }

    public string? ClienteTelefono { get; set; }

    public string? ClienteCorreo { get; set; }


    /* =========================
       EMPRESA
       ========================= */

    public int IdEmpresa { get; set; }

    public string EmpresaRazonSocial { get; set; }
        = string.Empty;

    public string? EmpresaNombreComercial { get; set; }

    public string EmpresaRuc { get; set; }
        = string.Empty;

    public string? EmpresaDireccionFiscal { get; set; }

    public string? EmpresaTelefono { get; set; }

    public string? EmpresaCorreo { get; set; }

    public string? EmpresaSitioWeb { get; set; }

    public string? EmpresaLogoUrl { get; set; }


    /* =========================
       SUCURSAL
       ========================= */

    public int IdSucursal { get; set; }

    public string SucursalCodigo { get; set; }
        = string.Empty;

    public string SucursalNombre { get; set; }
        = string.Empty;

    public string? SucursalDireccion { get; set; }

    public string? SucursalTelefono { get; set; }

    public string? SucursalCorreo { get; set; }

    public string? SucursalUbigeo { get; set; }


    /* =========================
       PAGO
       ========================= */

    public long? IdPago { get; set; }

    public string? MetodoPago { get; set; }

    public string? Moneda { get; set; }

    public decimal PagoMonto { get; set; }

    public string? NumeroOperacion { get; set; }

    public DateTime? FechaPago { get; set; }

    public string? PagoEstado { get; set; }
}


public sealed class ComprobanteProductoModel
{
    public long IdDetalleVenta { get; set; }

    public long IdVenta { get; set; }

    public int IdProducto { get; set; }

    public string CodigoProducto { get; set; }
        = string.Empty;

    public string ProductoNombre { get; set; }
        = string.Empty;

    public string? ProductoDescripcion { get; set; }

    public string? Categoria { get; set; }

    public string? Marca { get; set; }

    public string? UnidadCodigo { get; set; }

    public string? UnidadNombre { get; set; }

    public string? UnidadAbreviatura { get; set; }

    public decimal Cantidad { get; set; }

    public decimal PrecioUnitario { get; set; }

    public decimal Descuento { get; set; }

    public decimal Subtotal { get; set; }

    public decimal Impuesto { get; set; }

    public decimal Total { get; set; }
}