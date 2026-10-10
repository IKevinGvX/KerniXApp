using System;
using System.Collections.Generic;

namespace KerniX.Models.IA;

public sealed class IaContexto
{
    public int IdUsuario { get; init; }
    public int IdEmpresa { get; init; }
    public string Usuario { get; init; } = "";
}

public sealed class IaDiaVenta
{
    public DateTime Fecha { get; init; }
    public int Operaciones { get; init; }
    public decimal Total { get; init; }
    public decimal Intensidad { get; set; }
}

public sealed class IaAnomalia
{
    public DateTime Fecha { get; init; }
    public string Tipo { get; init; } = "";
    public decimal TotalObservado { get; init; }
    public decimal MediaHistorica { get; init; }
    public double DesviacionesRobustas { get; init; }
    public int DiasConActividad { get; init; }
    public string Certidumbre { get; init; } = "";
    public string Explicacion { get; init; } = "";
}

public sealed class IaAnalisisVentas
{
    public bool DatosDisponibles { get; set; }
    public string Aviso { get; set; } = "";
    public int DiasAnalizados { get; set; }
    public int DiasConVentas { get; set; }
    public int VentasUltimos7Dias { get; set; }
    public decimal ImporteUltimos7Dias { get; set; }
    public List<IaDiaVenta> Serie { get; set; } = new();
    public List<IaAnomalia> Anomalias { get; set; } = new();
    public int TotalAlertas => Anomalias.Count;
}
