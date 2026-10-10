using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using KerniX.Models.IA;
using Microsoft.Data.SqlClient;

namespace KerniX.Services.IA;

/// <summary>
/// Solo lectura y cálculo estadístico bajo demanda. NO ejecuta IA generativa,
/// NO escribe a la base, NO usa SQL Jobs. Los cambios de negocio corresponden
/// a servicios autorizados y requieren integración posterior.
/// </summary>
public sealed class KerniXIaService : IKerniXIaService
{
    private readonly string _connectionString;
    private readonly ILogger<KerniXIaService> _logger;

    public KerniXIaService(IConfiguration configuration, ILogger<KerniXIaService> logger)
    {
        _connectionString = configuration.GetConnectionString("KerniXDb")
            ?? throw new InvalidOperationException("Falta ConnectionStrings:KerniXDb.");
        _logger = logger;
    }

    public async Task<IaContexto?> ObtenerContextoAsync(int idUsuario, CancellationToken ct = default)
    {
        if (idUsuario <= 0) return null;
        const string sql = """
            SELECT TOP (1) u.id_usuario, e.id_empresa, u.nombre_usuario
            FROM seg.usuario AS u
            INNER JOIN org.empleado AS e ON e.id_empleado = u.id_empleado
            WHERE u.id_usuario = @id_usuario AND e.id_empresa > 0 AND u.estado = 1;
            """;
        await using var db = new SqlConnection(_connectionString);
        await db.OpenAsync(ct);
        await using var cmd = new SqlCommand(sql, db);
        cmd.Parameters.Add("@id_usuario", SqlDbType.Int).Value = idUsuario;
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;
        return new IaContexto
        {
            IdUsuario = reader.GetInt32(0),
            IdEmpresa = Convert.ToInt32(reader.GetValue(1), CultureInfo.InvariantCulture),
            Usuario = reader.IsDBNull(2) ? "" : Convert.ToString(reader.GetValue(2), CultureInfo.InvariantCulture) ?? ""
        };
    }

    private static string Q(string s) => "[" + s.Replace("]", "]]", StringComparison.Ordinal) + "]";

    public async Task<IaAnalisisVentas> AnalizarVentasAsync(int idEmpresa, CancellationToken ct = default)
    {
        var result = new IaAnalisisVentas();
        if (idEmpresa <= 0) { result.Aviso = "No se identificó una empresa válida."; return result; }

        await using var db = new SqlConnection(_connectionString);
        await db.OpenAsync(ct);
        var columnas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using (var meta = new SqlCommand("SELECT name FROM sys.columns WHERE object_id = OBJECT_ID(N'ven.venta', N'U');", db))
        await using (var rd = await meta.ExecuteReaderAsync(ct))
        {
            while (await rd.ReadAsync(ct)) columnas.Add(rd.GetString(0));
        }

        string? fecha = new[] { "fecha_venta", "fecha_emision", "fecha_registro", "fecha" }.FirstOrDefault(columnas.Contains);
        string? total = new[] { "total", "monto_total", "importe_total" }.FirstOrDefault(columnas.Contains);
        if (!columnas.Contains("id_empresa") || fecha is null || total is null)
        {
            result.Aviso = "ven.venta no contiene las columnas necesarias (id_empresa, fecha y total). Revisa el esquema sin modificar la tabla.";
            return result;
        }
        // Los nombres de columnas solo provienen de metadatos SQL y de listas blancas fijas.
        string condiciones = columnas.Contains("estado")
            ? "AND (v.[estado] IS NULL OR LOWER(CONVERT(NVARCHAR(50),v.[estado])) NOT IN (N'anulada',N'anulado',N'cancelada',N'cancelado'))"
            : "";
        string sql = $"""
            SELECT CAST(v.{Q(fecha)} AS date) AS Dia,
                   COUNT_BIG(*) AS NumeroVentas,
                   COALESCE(SUM(TRY_CONVERT(decimal(18,4),v.{Q(total)})),0) AS Total
            FROM ven.venta AS v
            WHERE v.id_empresa = @id_empresa
              AND v.{Q(fecha)} >= @desde
              AND v.{Q(fecha)} < @hasta
              {condiciones}
            GROUP BY CAST(v.{Q(fecha)} AS date)
            ORDER BY Dia;
            """;
        var hoy = DateTime.Today;
        var desde = hoy.AddDays(-69);
        var hasta = hoy.AddDays(1);
        var capturados = new Dictionary<DateTime, (int n, decimal total)>();
        await using (var cmd = new SqlCommand(sql, db))
        {
            cmd.CommandTimeout = 30;
            cmd.Parameters.Add("@id_empresa", SqlDbType.Int).Value = idEmpresa;
            cmd.Parameters.Add("@desde", SqlDbType.DateTime2).Value = desde;
            cmd.Parameters.Add("@hasta", SqlDbType.DateTime2).Value = hasta;
            await using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                if (reader.IsDBNull(0)) continue;
                var dia = reader.GetDateTime(0).Date;
                long n = reader.IsDBNull(1) ? 0 : reader.GetInt64(1);
                decimal value = reader.IsDBNull(2) ? 0M : reader.GetDecimal(2);
                capturados[dia] = ((int)Math.Min(n, int.MaxValue), value);
            }
        }
        var dias = Enumerable.Range(0, 70).Select(i =>
        {
            var fechaDia = desde.AddDays(i);
            var valores = capturados.GetValueOrDefault(fechaDia);
            return new IaDiaVenta { Fecha = fechaDia, Operaciones = valores.n, Total = valores.total };
        }).ToList();
        result.DatosDisponibles = true;
        result.DiasAnalizados = dias.Count;
        result.DiasConVentas = dias.Count(d => d.Operaciones > 0);
        result.ImporteUltimos7Dias = dias.TakeLast(7).Sum(d => d.Total);
        result.VentasUltimos7Dias = dias.TakeLast(7).Sum(d => d.Operaciones);
        decimal max = Math.Max(1M, dias.TakeLast(21).Max(d => d.Total));
        foreach (var dia in dias.TakeLast(21))
        {
            dia.Intensidad = Math.Clamp(dia.Total / max * 100M, 0M, 100M);
            result.Serie.Add(dia);
        }
        if (result.DiasConVentas == 0)
        {
            result.Aviso = "Sin ventas en los últimos 70 días. No es posible entrenar una línea base estadística.";
            return result;
        }

        // Ventana móvil previa de 28 días, desviación absoluta mediana (MAD).
        // La señal se calcula sobre ingresos de ventas; no atribuye causalidad.
        for (int i = 56; i < dias.Count; i++)
        {
            var historial = dias.Skip(i - 28).Take(28).ToList();
            int diasActivos = historial.Count(x => x.Operaciones > 0);
            if (diasActivos < 7) continue;
            var values = historial.Select(x => (double)x.Total).OrderBy(x => x).ToList();
            double median = Median(values);
            double mad = Median(values.Select(x => Math.Abs(x - median)).OrderBy(x => x).ToList());
            double media = values.Average();
            double sd = Math.Sqrt(values.Sum(x => (x - media) * (x - media)) / values.Count);
            double escala = Math.Max(Math.Max(1.4826 * mad, sd * 0.50), Math.Max(Math.Abs(median) * 0.15, 1.0));
            double observado = (double)dias[i].Total;
            double z = (observado - median) / escala;
            double delta = Math.Abs(observado - median) / Math.Max(Math.Abs(median), 1.0);
            if (Math.Abs(z) < 3.2 || delta < 0.35) continue;
            string tipo = z > 0 ? "Alza inusual" : "Caída inusual";
            result.Anomalias.Add(new IaAnomalia
            {
                Fecha = dias[i].Fecha,
                Tipo = tipo,
                TotalObservado = dias[i].Total,
                MediaHistorica = (decimal)media,
                DesviacionesRobustas = Math.Round(z, 2),
                DiasConActividad = diasActivos,
                Certidumbre = diasActivos >= 18 ? "Evidencia moderada" : "Evidencia limitada",
                Explicacion = "Se detectó una desviación respecto de los 28 días anteriores. Revisar promociones, pedidos, anulaciones o eventos comerciales antes de actuar."
            });
        }
        result.Anomalias = result.Anomalias
            .OrderByDescending(x => Math.Abs(x.DesviacionesRobustas))
            .Take(5).ToList();
        if (result.DiasConVentas < 14)
            result.Aviso = "El historial es escaso; interpreta las señales como exploratorias, no como predicciones validadas.";
        _logger.LogInformation("[KERNIX IA] Empresa={Empresa} Ventas7d={Ventas} Senales={Senales}",
            idEmpresa, result.VentasUltimos7Dias, result.Anomalias.Count);
        return result;
    }

    private static double Median(IReadOnlyList<double> ordered)
    {
        if (ordered.Count == 0) return 0;
        int mid = ordered.Count / 2;
        return ordered.Count % 2 == 0 ? (ordered[mid - 1] + ordered[mid]) / 2 : ordered[mid];
    }
}
