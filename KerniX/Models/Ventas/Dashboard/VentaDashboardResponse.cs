using KerniX.Models.Ventas.Venta;

namespace KerniX.Models.Ventas.Dashboard;

public sealed class VentaDashboardResponse
{
    public VentaKpiResponse Kpis { get; set; }
        = new();

    public IReadOnlyList<VentaGraficoResponse> VentasPeriodo { get; set; }
        = Array.Empty<VentaGraficoResponse>();

    public IReadOnlyList<VentaCategoriaResponse> Categorias { get; set; }
        = Array.Empty<VentaCategoriaResponse>();

    public IReadOnlyList<VentaTopProductoResponse> TopProductos { get; set; }
        = Array.Empty<VentaTopProductoResponse>();

    public IReadOnlyList<VentaListadoResponse> UltimasVentas { get; set; }
        = Array.Empty<VentaListadoResponse>();

    public IReadOnlyList<VentaGraficoResponse> MetodosPago { get; set; }
        = Array.Empty<VentaGraficoResponse>();

    public IReadOnlyList<VentaActividadResponse> ActividadReciente { get; set; }
        = Array.Empty<VentaActividadResponse>();

    public IReadOnlyList<VentaIaInsightResponse> InsightsIa { get; set; }
        = Array.Empty<VentaIaInsightResponse>();


    // =====================================================
    // 8/10 - CLIENTES DESTACADOS
    // =====================================================

    public IReadOnlyList<ClienteDestacadoResponse> TopClientes { get; set; }
        = Array.Empty<ClienteDestacadoResponse>();


    // =====================================================
    // 9/10 - ALERTAS COMERCIALES
    // =====================================================

    public IReadOnlyList<AlertaComercialResponse> AlertasComerciales { get; set; }
        = Array.Empty<AlertaComercialResponse>();


    // =====================================================
    // 10/10 - PROYECCIÓN DE VENTAS
    // =====================================================

    public IReadOnlyList<ProyeccionVentaResponse> ProyeccionVentas { get; set; }
        = Array.Empty<ProyeccionVentaResponse>();
}