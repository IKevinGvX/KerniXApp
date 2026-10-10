using System;

namespace KerniX.Models.Ventas.Operaciones;

public sealed class PagoRegistroInput
{
    public long IdVenta { get; set; }
    public decimal Monto { get; set; }
    public string MetodoPago { get; set; } = "EFECTIVO";
    public string Moneda { get; set; } = "PEN";
    public string? NumeroOperacion { get; set; }
}

public sealed class ReclamoRegistroInput
{
    public long IdVenta { get; set; }
    public string TipoReclamo { get; set; } = "producto";
    public string Descripcion { get; set; } = "";
}

public sealed record RegistroOperacionResultado(bool Correcto, string Mensaje, string? Referencia = null);
