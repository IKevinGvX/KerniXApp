using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using ClosedXML.Excel;
using KerniX.Models.Ventas.Panel;

namespace KerniX.Services.Ventas.Panel;

/// <summary>
/// XLSX empresarial con información real exportada por VentasPanelService.
/// No utiliza IDs internos de ven.venta en la columna Venta.
/// No modifica SQL Server ni genera registros de ejemplo.
/// Requiere paquete NuGet ClosedXML.
/// </summary>
public static class KxVentasExcelExporter
{
    private static readonly XLColor Fondo = XLColor.FromHtml("#07111F");
    private static readonly XLColor Panel = XLColor.FromHtml("#10233A");
    private static readonly XLColor Oro = XLColor.FromHtml("#DFAE45");
    private static readonly XLColor Azul = XLColor.FromHtml("#1877B8");
    private static readonly XLColor Claro = XLColor.FromHtml("#EAF2FB");

    public static byte[] Generar(PanelTipo tipo, int idEmpresa,
        IReadOnlyList<PanelRegistro> registros, string? buscar,
        string? estado, DateTime? desde, DateTime? hasta)
    {
        if (idEmpresa <= 0) throw new ArgumentOutOfRangeException(nameof(idEmpresa));
        ArgumentNullException.ThrowIfNull(registros);

        using var libro = new XLWorkbook();
        var operaciones = libro.Worksheets.Add("OPERACIONES");
        var resumen = libro.Worksheets.Add("RESUMEN");

        EscribirOperaciones(operaciones, tipo, idEmpresa, registros, buscar, estado, desde, hasta);
        EscribirResumen(resumen, tipo, registros);

        using var memoria = new MemoryStream();
        libro.SaveAs(memoria);
        return memoria.ToArray();
    }

    private static string TextoSeguro(string? valor)
    {
        var texto = (valor ?? string.Empty).Trim();
        // Defensa adicional si el archivo es abierto/importado con software externo.
        if (texto.Length > 0 && "=+-@".Contains(texto[0])) texto = "'" + texto;
        return texto.Length > 400 ? texto[..400] : texto;
    }

    private static void EscribirOperaciones(IXLWorksheet h, PanelTipo tipo, int empresa,
        IReadOnlyList<PanelRegistro> datos, string? buscar, string? estado,
        DateTime? desde, DateTime? hasta)
    {
        h.ShowGridLines = false;
        h.SheetView.FreezeRows(13);
        h.Column("A").Width = 17;
        h.Column("B").Width = 24;
        h.Column("C").Width = 39;
        h.Column("D").Width = 26;
        h.Column("E").Width = 22; // Estado
        h.Column("F").Width = 22; // Venta comercial, nunca ID
        h.Column("G").Width = 20;
        h.Column("H").Width = 19;

        h.Range("A1:H2").Merge();
        h.Cell("A1").Value = "KerniX  /  REPORTE EMPRESARIAL";
        h.Range("A1:H2").Style.Fill.BackgroundColor = Fondo;
        h.Range("A1:H2").Style.Font.FontColor = Oro;
        h.Range("A1:H2").Style.Font.Bold = true;
        h.Range("A1:H2").Style.Font.FontSize = 21;
        h.Range("A1:H2").Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        h.Row(1).Height = 27;
        h.Row(2).Height = 18;

        h.Range("A3:H3").Merge();
        h.Cell("A3").Value = $"{tipo.ToString().ToUpperInvariant()}  •  KERNIX S.A.C.  •  CONTROL COMERCIAL";
        h.Range("A3:H3").Style.Fill.BackgroundColor = Panel;
        h.Range("A3:H3").Style.Font.FontColor = Claro;
        h.Range("A3:H3").Style.Font.Bold = true;
        h.Row(3).Height = 25;
        h.Row(4).Height = 13;

        Tarjeta(h, "A5:B5", "A6:B7", "=COUNTA(C14:C10013)", false);
        Tarjeta(h, "C5:D5", "C6:D7", "=COUNTIF(H14:H10013,\"ok\")", false);
        Tarjeta(h, "E5:F5", "E6:F7", "=COUNTIF(H14:H10013,\"pendiente\")", false);
        Tarjeta(h, "G5:H5", "G6:H7", "=SUM(G14:G10013)", true);
        h.Row(5).Height = 24;
        h.Row(6).Height = 25;
        h.Row(7).Height = 25;
        h.Row(8).Height = 12;

        h.Range("A9:H9").Merge();
        var filtroSeguro = string.IsNullOrWhiteSpace(buscar) ? "Todos" : TextoSeguro(buscar);
        var estadoSeguro = string.IsNullOrWhiteSpace(estado) ? "Todos" : TextoSeguro(estado);
        h.Cell("A9").Value = $"Empresa: {empresa}   |   Filtro: {filtroSeguro}   |   Estado: {estadoSeguro}";
        h.Range("A9:H9").Style.Fill.BackgroundColor = XLColor.FromHtml("#EAF1F9");
        h.Range("A9:H9").Style.Font.FontColor = XLColor.FromHtml("#26374D");
        h.Row(9).Height = 23;

        h.Range("A10:H10").Merge();
        h.Cell("A10").Value = $"Período: {(desde?.ToString("dd/MM/yyyy") ?? "Sin inicio")} — {(hasta?.ToString("dd/MM/yyyy") ?? "Sin fin")}    |    Exportado: {DateTime.Now:dd/MM/yyyy HH:mm}";
        h.Range("A10:H10").Style.Font.FontColor = XLColor.FromHtml("#667B95");
        h.Row(10).Height = 22;
        h.Range("A11:H11").Merge();
        h.Cell("A11").Value = "VENTA = NÚMERO COMERCIAL (ven.venta.numero_venta). No se exporta el ID interno. Las cifras reflejan los filtros aplicados.";
        h.Range("A11:H11").Style.Font.FontColor = XLColor.FromHtml("#176798");
        h.Range("A11:H11").Style.Font.FontSize = 9;
        h.Row(11).Height = 21;
        h.Row(12).Height = 11;

        var titulos = new[] { "FECHA", "DOCUMENTO", "CLIENTE", "TIPO / MÉTODO", "ESTADO", "VENTA", "MONTO (S/)", "CLASIFICACIÓN" };
        for (int i = 0; i < titulos.Length; i++) h.Cell(13, i + 1).Value = titulos[i];
        var cab = h.Range("A13:H13");
        cab.Style.Fill.BackgroundColor = Panel;
        cab.Style.Font.FontColor = Oro;
        cab.Style.Font.Bold = true;
        cab.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        h.Row(13).Height = 31;

        for (int i = 0; i < datos.Count; i++)
        {
            var dato = datos[i];
            int r = 14 + i;
            if (dato.Fecha.HasValue) h.Cell(r, 1).Value = dato.Fecha.Value;
            h.Cell(r, 1).Style.DateFormat.Format = "dd/mm/yyyy";
            h.Cell(r, 2).Value = TextoSeguro(dato.Documento);
            h.Cell(r, 3).Value = TextoSeguro(dato.Cliente);
            h.Cell(r, 4).Value = TextoSeguro(dato.Tipo);
            h.Cell(r, 5).Value = TextoSeguro(dato.Estado);
            h.Cell(r, 6).Value = string.IsNullOrWhiteSpace(dato.Referencia)
                ? "—" : TextoSeguro(dato.Referencia);
            if (dato.Monto.HasValue) h.Cell(r, 7).Value = dato.Monto.Value;
            h.Cell(r, 7).Style.NumberFormat.Format = "\"S/\" #,##0.00;[Red](\"S/\" #,##0.00)";
            h.Cell(r, 8).Value = TextoSeguro(dato.Categoria);

            var fila = h.Range(r, 1, r, 8);
            fila.Style.Fill.BackgroundColor = i % 2 == 0 ? XLColor.FromHtml("#F5F8FD") : XLColor.White;
            fila.Style.Font.FontColor = XLColor.FromHtml("#25354B");
            fila.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            fila.Style.Border.BottomBorder = XLBorderStyleValues.Hair;
            fila.Style.Border.BottomBorderColor = XLColor.FromHtml("#DEE7F2");
            h.Row(r).Height = 26;
            h.Cell(r, 2).Style.Font.Bold = true;
            h.Cell(r, 6).Style.Font.FontColor = Azul;
            h.Cell(r, 6).Style.Font.Bold = true;
            h.Cell(r, 7).Style.Font.Bold = true;

            var bg = dato.Categoria.ToLowerInvariant() switch
            {
                "ok" => XLColor.FromHtml("#DAF6E7"),
                "pendiente" => XLColor.FromHtml("#FFF1CE"),
                _ => XLColor.FromHtml("#E5EAF5")
            };
            h.Cell(r, 5).Style.Fill.BackgroundColor = bg;
            h.Cell(r, 5).Style.Font.FontColor = dato.Categoria.ToLowerInvariant() == "ok"
                ? XLColor.FromHtml("#11603A") : XLColor.FromHtml("#87521C");
            h.Cell(r, 5).Style.Font.Bold = true;
        }

        if (datos.Count == 0)
        {
            h.Range("A14:H14").Merge();
            h.Cell("A14").Value = "No existen operaciones que coincidan con los filtros seleccionados.";
            h.Range("A14:H14").Style.Font.FontColor = XLColor.FromHtml("#65758A");
            h.Row(14).Height = 32;
        }
        else h.Range(13, 1, datos.Count + 13, 8).SetAutoFilter();

        h.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        h.PageSetup.FitToPages(1, 0);
        h.PageSetup.SetRowsToRepeatAtTop(13, 13);
    }

    private static void Tarjeta(IXLWorksheet h, string etiqueta, string valor, string formula, bool soles)
    {
        h.Range(etiqueta).Merge();
        h.Range(valor).Merge();
        h.Cell(etiqueta.Split(':')[0]).Value = etiqueta switch
        {
            "A5:B5" => "OPERACIONES",
            "C5:D5" => "COMPLETADAS",
            "E5:F5" => "PENDIENTES",
            _ => "MONTO TOTAL (S/)"
        };
        h.Range(etiqueta).Style.Fill.BackgroundColor = Panel;
        h.Range(etiqueta).Style.Font.FontColor = Oro;
        h.Range(etiqueta).Style.Font.Bold = true;
        h.Range(etiqueta).Style.Font.FontSize = 10;
        h.Range(etiqueta).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        h.Cell(valor.Split(':')[0]).FormulaA1 = formula.TrimStart('=');
        h.Range(valor).Style.Fill.BackgroundColor = Fondo;
        h.Range(valor).Style.Font.FontColor = XLColor.White;
        h.Range(valor).Style.Font.Bold = true;
        h.Range(valor).Style.Font.FontSize = 24;
        h.Range(valor).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        h.Range(valor).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        if (soles) h.Range(valor).Style.NumberFormat.Format = "\"S/\" #,##0.00";
    }

    private static void EscribirResumen(IXLWorksheet h, PanelTipo tipo,
        IReadOnlyList<PanelRegistro> datos)
    {
        h.ShowGridLines = false;
        h.Column("A").Width = 18; h.Column("B").Width = 18;
        h.Column("C").Width = 20; h.Column("D").Width = 17;
        h.Column("E").Width = 17; h.Column("F").Width = 17;
        h.Column("G").Width = 17; h.Column("H").Width = 17;
        h.Range("A1:H2").Merge();
        h.Cell("A1").Value = $"KerniX   |   ANÁLISIS DE {tipo.ToString().ToUpperInvariant()}";
        h.Range("A1:H2").Style.Fill.BackgroundColor = Fondo;
        h.Range("A1:H2").Style.Font.FontColor = Oro;
        h.Range("A1:H2").Style.Font.Bold = true;
        h.Range("A1:H2").Style.Font.FontSize = 20;
        h.Row(1).Height = 30; h.Row(2).Height = 25;
        h.Range("A4:H4").Merge();
        h.Cell("A4").Value = "INDICADORES CALCULADOS SOBRE EL CONJUNTO EXPORTADO";
        h.Range("A4:H4").Style.Fill.BackgroundColor = Panel;
        h.Range("A4:H4").Style.Font.FontColor = Claro;
        h.Range("A4:H4").Style.Font.Bold = true;
        h.Row(4).Height = 30;

        TarjetaResumen(h, "A6:C6", "A7:C8", "OPERACIONES", "COUNTA(OPERACIONES!C14:C10013)");
        TarjetaResumen(h, "D6:F6", "D7:F8", "COMPLETADAS", "COUNTIF(OPERACIONES!H14:H10013,\"ok\")");
        TarjetaResumen(h, "G6:H6", "G7:H8", "PENDIENTES", "COUNTIF(OPERACIONES!H14:H10013,\"pendiente\")");
        h.Row(6).Height = 23; h.Row(7).Height = 28; h.Row(8).Height = 28;
        h.Range("A10:H10").Merge();
        h.Cell("A10").Value = "EVOLUCIÓN MENSUAL  •  DATOS REALES DEL ARCHIVO";
        h.Range("A10:H10").Style.Fill.BackgroundColor = Panel;
        h.Range("A10:H10").Style.Font.FontColor = Oro;
        h.Range("A10:H10").Style.Font.Bold = true;
        h.Row(10).Height = 27;
        h.Cell("A12").Value = "MES";
        h.Cell("B12").Value = "CANTIDAD";
        h.Cell("C12").Value = "MONTO (S/)";
        h.Range("A12:C12").Style.Fill.BackgroundColor = XLColor.FromHtml("#143452");
        h.Range("A12:C12").Style.Font.FontColor = Claro;
        h.Range("A12:C12").Style.Font.Bold = true;
        h.Row(12).Height = 27;
        DateTime referencia = datos.Where(x => x.Fecha.HasValue)
            .Select(x => x.Fecha!.Value).DefaultIfEmpty(DateTime.Today).Max();
        DateTime mesFinal = new(referencia.Year, referencia.Month, 1);
        int max = 1;
        var periodos = Enumerable.Range(0, 6).Select(i => mesFinal.AddMonths(i - 5)).ToList();
        var cantidades = periodos.Select(m => datos.Count(d => d.Fecha.HasValue && d.Fecha.Value.Year == m.Year && d.Fecha.Value.Month == m.Month)).ToList();
        if (cantidades.Count != 0) max = Math.Max(1, cantidades.Max());
        for (int i = 0; i < 6; i++)
        {
            int r = 13 + i;
            var mes = periodos[i];
            h.Cell(r, 1).Value = mes;
            h.Cell(r, 1).Style.DateFormat.Format = "mmm yyyy";
            h.Cell(r, 2).FormulaA1 = $"COUNTIFS(OPERACIONES!$A$14:$A$10013,\">=\"&A{r},OPERACIONES!$A$14:$A$10013,\"<\"&EDATE(A{r},1))";
            h.Cell(r, 3).FormulaA1 = $"SUMIFS(OPERACIONES!$G$14:$G$10013,OPERACIONES!$A$14:$A$10013,\">=\"&A{r},OPERACIONES!$A$14:$A$10013,\"<\"&EDATE(A{r},1))";
            h.Cell(r, 3).Style.NumberFormat.Format = "\"S/\" #,##0.00";
            h.Range(r, 1, r, 8).Style.Fill.BackgroundColor = i % 2 == 0 ? XLColor.FromHtml("#F2F6FC") : XLColor.White;
            h.Range(r, 1, r, 8).Style.Border.BottomBorder = XLBorderStyleValues.Hair;
            h.Row(r).Height = 28;
            int barras = cantidades[i] == 0 ? 0 : Math.Max(1, (int)Math.Round(cantidades[i] / (double)max * 5));
            for (int j = 0; j < 5; j++)
            {
                h.Cell(r, 4 + j).Value = j < barras ? "▰" : "·";
                h.Cell(r, 4 + j).Style.Font.FontSize = 17;
                h.Cell(r, 4 + j).Style.Font.FontColor = j < barras ? (i % 2 == 0 ? Oro : Azul) : XLColor.FromHtml("#DEE7F0");
                h.Cell(r, 4 + j).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }
        }
        h.Range("A21:H21").Merge();
        h.Cell("A21").Value = "Nota: la gráfica resume registros exportados; no predice ni altera cifras. Filtros y datos originales: hoja OPERACIONES.";
        h.Range("A21:H21").Style.Font.FontColor = XLColor.FromHtml("#4D6076");
        h.Row(21).Height = 26;
    }

    private static void TarjetaResumen(IXLWorksheet h, string label, string valor, string titulo, string formula)
    {
        h.Range(label).Merge(); h.Range(valor).Merge();
        h.Cell(label.Split(':')[0]).Value = titulo;
        h.Range(label).Style.Fill.BackgroundColor = Panel;
        h.Range(label).Style.Font.FontColor = Oro;
        h.Range(label).Style.Font.Bold = true;
        h.Range(label).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        h.Cell(valor.Split(':')[0]).FormulaA1 = formula;
        h.Range(valor).Style.Fill.BackgroundColor = Fondo;
        h.Range(valor).Style.Font.FontColor = XLColor.White;
        h.Range(valor).Style.Font.FontSize = 24;
        h.Range(valor).Style.Font.Bold = true;
        h.Range(valor).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        h.Range(valor).Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
    }
}
