using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using KerniX.Models.Ventas.Panel;
using KerniX.Security;
using KerniX.Services.Ventas.Panel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace KerniX.Pages.Ventas;

public abstract class VentasPanelPageModel : PageModel
{
    private readonly IVentasPanelService _service;
    private readonly ILogger _logger;
    protected VentasPanelPageModel(IVentasPanelService service, ILogger logger)
    {
        _service = service;
        _logger = logger;
    }
    public VentasPanelVm Datos { get; private set; } = new();

    private async Task<int?> EmpresaAsync(CancellationToken ct)
    {
        int idUsuario = HttpContext.Session.GetInt32(SessionKeys.IdUsuario) ?? 0;
        if (idUsuario <= 0) return null;
        int idEmpresa = HttpContext.Session.GetInt32("IdEmpresa") ?? 0;
        if (idEmpresa <= 0)
        {
            // La empresa no se debe suponer: se obtiene de la relación usuario-empleado.
            int? encontrada = await _service.ObtenerEmpresaDeUsuarioAsync(idUsuario, ct);
            if (!encontrada.HasValue || encontrada.Value <= 0) return null;
            idEmpresa = encontrada.Value;
            HttpContext.Session.SetInt32("IdEmpresa", idEmpresa);
        }
        return idEmpresa;
    }

    protected async Task<IActionResult> CargarAsync(PanelTipo tipo, string? buscar, string? estado,
        int pagina, DateTime? desde, DateTime? hasta, CancellationToken ct)
    {
        if (!HttpContext.Session.GetInt32(SessionKeys.IdUsuario).HasValue)
            return Redirect("/Account/Login");
        try
        {
            var empresa = await EmpresaAsync(ct);
            if (!empresa.HasValue)
            {
                Datos = new VentasPanelVm { Tipo = tipo, Aviso = "No se pudo identificar la empresa del usuario autenticado." };
                return Page();
            }
            Datos = await _service.ObtenerAsync(tipo, empresa.Value, buscar, estado, pagina, desde, hasta, ct);
            return Page();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "[KERNIX PANEL] Falló lectura de {Tipo}", tipo);
            Datos = new VentasPanelVm { Tipo = tipo, Aviso = "No se pudo cargar el panel. Revisa la ventana Salida." };
            return Page();
        }
    }

    protected async Task<IActionResult> CsvAsync(PanelTipo tipo, string? buscar, string? estado,
        DateTime? desde, DateTime? hasta, CancellationToken ct)
    {
        if (!HttpContext.Session.GetInt32(SessionKeys.IdUsuario).HasValue)
            return Redirect("/Account/Login");
        int? empresa = await EmpresaAsync(ct);
        if (!empresa.HasValue) return BadRequest("Falta contexto de empresa.");
        try
        {
            var datos = await _service.ExportarAsync(tipo, empresa.Value, buscar, estado, desde, hasta, ct);
            static string Cell(object? value)
            {
                string v = Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
                // Evita la inyección de fórmulas al abrir CSV en Excel.
                if (v.TrimStart().StartsWith('=') || v.TrimStart().StartsWith('+') ||
                    v.TrimStart().StartsWith('-') || v.TrimStart().StartsWith('@') ||
                    v.Contains('\t') || v.Contains('\r')) v = "'" + v;
                return "\"" + v.Replace("\"", "\"\"") + "\"";
            }
            var sb = new StringBuilder("ID;Documento;Cliente;Fecha;Monto;Tipo;Estado;Venta\r\n");
            foreach (var x in datos)
            {
                sb.AppendLine(string.Join(';', new[] {
                    Cell(x.Id),Cell(x.Documento),Cell(x.Cliente),Cell(x.Fecha?.ToString("yyyy-MM-dd")),
                    Cell(x.Monto?.ToString("0.00",CultureInfo.InvariantCulture)),Cell(x.Tipo),Cell(x.Estado),Cell(x.Referencia)
                }));
            }
            var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
            return File(bytes, "text/csv; charset=utf-8", $"KerniX_{tipo}_{DateTime.Now:yyyyMMdd_HHmm}.csv");
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "[KERNIX PANEL] Error exportando {Tipo}", tipo);
            return StatusCode(500, "No se pudo exportar. Revisa la ventana Salida de Visual Studio.");
        }
    }
}

