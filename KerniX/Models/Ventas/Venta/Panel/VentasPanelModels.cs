using System;
using System.Collections.Generic;

namespace KerniX.Models.Ventas.Panel;

public enum PanelTipo { Pagos, Comprobantes, Devoluciones, Reclamos }

public sealed class PanelRegistro
{
    public long Id { get; set; }
    public string Documento { get; set; } = "";
    public string Cliente { get; set; } = "";
    public DateTime? Fecha { get; set; }
    public decimal? Monto { get; set; }
    public string Tipo { get; set; } = "";
    public string Estado { get; set; } = "";
    public string Referencia { get; set; } = "";
    public string Categoria { get; set; } = "otro";
}

public sealed class PanelPunto
{
    public string Mes { get; set; } = "";
    public int Cantidad { get; set; }
    public decimal Monto { get; set; }
    public double Altura { get; set; }
}

public sealed class VentasPanelVm
{
    public PanelTipo Tipo { get; set; }
    public string Titulo => Tipo switch { PanelTipo.Pagos => "Pagos", PanelTipo.Comprobantes => "Comprobantes", PanelTipo.Devoluciones => "Devoluciones", _ => "Reclamos" };
    public string Icono => Tipo switch { PanelTipo.Pagos => "S/", PanelTipo.Comprobantes => "▤", PanelTipo.Devoluciones => "↶", _ => "◇" };
    public string Subtitulo => Tipo switch
    {
        PanelTipo.Pagos => "Cobros, métodos de pago y seguimiento de la recaudación.",
        PanelTipo.Comprobantes => "Boletas, facturas y seguimiento documental.",
        PanelTipo.Devoluciones => "Control de devoluciones y trazabilidad comercial.",
        _ => "Atención, seguimiento y resolución de reclamos de clientes."
    };
    public string TipoColumna => Tipo switch { PanelTipo.Pagos => "Método", PanelTipo.Comprobantes => "Comprobante", PanelTipo.Devoluciones => "Tipo", _ => "Motivo / tipo" };
    public string UltimaEtiqueta => Tipo switch { PanelTipo.Pagos => "Recaudación", PanelTipo.Comprobantes => "Total documentado", PanelTipo.Devoluciones => "Monto asociado", _ => "Monto asociado" };
    public string Buscar { get; set; } = "";
    public string EstadoFiltro { get; set; } = "todos";
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }
    public int Pagina { get; set; } = 1;
    public int TamanoPagina { get; } = 5;
    public int Total { get; set; }
    public int Completados { get; set; }
    public int Pendientes { get; set; }
    public int Otros => Math.Max(0, Total - Completados - Pendientes);
    public decimal MontoTotal { get; set; }
    public bool TieneMonto { get; set; }
    public int TotalPaginas => Math.Max(1, (int)Math.Ceiling((double)Total / TamanoPagina));
    public double PorcentajeOk => Total > 0 ? 100d * Completados / Total : 0;
    public double PorcentajePendiente => Total > 0 ? 100d * Pendientes / Total : 0;
    public string? Aviso { get; set; }
    public List<PanelRegistro> Registros { get; set; } = new();
    public List<PanelPunto> Serie { get; set; } = new();
    public string Ruta => "/Ventas/" + Titulo;
    public string Url(int pagina, bool exportar = false)
    {
        var partes = new List<string>();
        if (exportar) partes.Add("handler=Exportar");
        if (!string.IsNullOrWhiteSpace(Buscar)) partes.Add("buscar=" + Uri.EscapeDataString(Buscar));
        if (EstadoFiltro != "todos") partes.Add("estado=" + Uri.EscapeDataString(EstadoFiltro));
        if (Desde.HasValue) partes.Add("desde=" + Desde.Value.ToString("yyyy-MM-dd"));
        if (Hasta.HasValue) partes.Add("hasta=" + Hasta.Value.ToString("yyyy-MM-dd"));
        if (!exportar) partes.Add("pagina=" + Math.Max(1, pagina));
        return Ruta + (partes.Count == 0 ? "" : "?" + string.Join("&", partes));
    }
}
