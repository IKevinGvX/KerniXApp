using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using KerniX.Models.IA;
using Microsoft.Data.SqlClient;

namespace KerniX.Services.IA;

/// <summary>
/// Servicio analítico de KerniX.
///
/// - Solo lectura.
/// - No modifica ventas.
/// - No ejecuta procesos automáticos.
/// - No utiliza IA generativa.
/// - Detecta comportamientos atípicos mediante análisis estadístico.
/// </summary>
public sealed class KerniXIaService : IKerniXIaService
{
    private readonly string _connectionString;
    private readonly ILogger<KerniXIaService> _logger;

    private const int DiasHistoricos = 28;
    private const int MinimoDiasActivos = 7;

    public KerniXIaService(
        IConfiguration configuration,
        ILogger<KerniXIaService> logger)
    {
        _connectionString =
            configuration.GetConnectionString("KerniXDb")
            ?? throw new InvalidOperationException(
                "Falta ConnectionStrings:KerniXDb.");

        _logger = logger;
    }


    // =========================================================
    // CONTEXTO DEL USUARIO
    // =========================================================

    public async Task<IaContexto?> ObtenerContextoAsync(
        int idUsuario,
        CancellationToken ct = default)
    {
        if (idUsuario <= 0)
            return null;

        const string sql = """
            SELECT TOP (1)
                u.id_usuario,
                e.id_empresa,
                u.nombre_usuario
            FROM seg.usuario AS u
            INNER JOIN org.empleado AS e
                ON e.id_empleado = u.id_empleado
            WHERE u.id_usuario = @id_usuario
              AND e.id_empresa > 0
              AND u.estado = 1;
            """;

        await using var db =
            new SqlConnection(_connectionString);

        await db.OpenAsync(ct);

        await using var cmd =
            new SqlCommand(sql, db);

        cmd.Parameters
            .Add("@id_usuario", SqlDbType.Int)
            .Value = idUsuario;

        await using var reader =
            await cmd.ExecuteReaderAsync(ct);

        if (!await reader.ReadAsync(ct))
            return null;

        return new IaContexto
        {
            IdUsuario = reader.GetInt32(0),

            IdEmpresa = Convert.ToInt32(
                reader.GetValue(1),
                CultureInfo.InvariantCulture),

            Usuario = reader.IsDBNull(2)
                ? ""
                : Convert.ToString(
                    reader.GetValue(2),
                    CultureInfo.InvariantCulture) ?? ""
        };
    }


    // =========================================================
    // ANÁLISIS PREDETERMINADO
    // =========================================================
    // Mantiene compatibilidad con IA/Index.
    // Por defecto analiza los últimos 30 días.
    // =========================================================

    public Task<IaAnalisisVentas> AnalizarVentasAsync(
        int idEmpresa,
        CancellationToken ct = default)
    {
        DateTime hasta = DateTime.Today;
        DateTime desde = hasta.AddDays(-29);

        return AnalizarVentasAsync(
            idEmpresa,
            desde,
            hasta,
            ct);
    }


    // =========================================================
    // ANÁLISIS POR RANGO
    // =========================================================

    public async Task<IaAnalisisVentas> AnalizarVentasAsync(
        int idEmpresa,
        DateTime fechaDesde,
        DateTime fechaHasta,
        CancellationToken ct = default)
    {
        var result = new IaAnalisisVentas();

        // -----------------------------------------------------
        // VALIDACIONES
        // -----------------------------------------------------

        if (idEmpresa <= 0)
        {
            result.Aviso =
                "No se identificó una empresa válida.";

            return result;
        }

        fechaDesde = fechaDesde.Date;
        fechaHasta = fechaHasta.Date;

        if (fechaDesde > fechaHasta)
        {
            result.Aviso =
                "La fecha inicial no puede ser mayor que la fecha final.";

            return result;
        }

        int cantidadDias =
            (fechaHasta - fechaDesde).Days + 1;

        if (cantidadDias > 366)
        {
            result.Aviso =
                "El período máximo permitido es de 366 días.";

            return result;
        }


        // =====================================================
        // RANGO SQL
        // =====================================================
        //
        // Ejemplo:
        //
        // Usuario:
        // 01/10/2026 -> 25/10/2026
        //
        // SQL:
        // 03/09/2026 -> 26/10/2026
        //
        // Los 28 días anteriores sirven como referencia.
        // =====================================================

        DateTime inicioHistorico =
            fechaDesde.AddDays(-DiasHistoricos);

        DateTime finConsulta =
            fechaHasta.AddDays(1);


        const string sql = """
            SELECT
                CAST(v.fecha_venta AS date) AS Dia,
                COUNT_BIG(*) AS NumeroVentas,
                COALESCE(
                    SUM(
                        TRY_CONVERT(decimal(18,4), v.total)
                    ),
                    0
                ) AS Total
            FROM ven.venta AS v
            WHERE v.id_empresa = @id_empresa
              AND v.fecha_venta >= @desde
              AND v.fecha_venta < @hasta
              AND
              (
                    v.estado IS NULL
                    OR LOWER(
                        CONVERT(NVARCHAR(50), v.estado)
                    ) NOT IN
                    (
                        N'anulada',
                        N'anulado',
                        N'cancelada',
                        N'cancelado'
                    )
              )
            GROUP BY
                CAST(v.fecha_venta AS date)
            ORDER BY
                Dia;
            """;


        // =====================================================
        // CONSULTA SQL
        // =====================================================

        var capturados =
            new Dictionary<DateTime, (int operaciones, decimal total)>();

        await using var db =
            new SqlConnection(_connectionString);

        await db.OpenAsync(ct);

        await using (var cmd =
            new SqlCommand(sql, db))
        {
            cmd.CommandTimeout = 30;

            cmd.Parameters
                .Add("@id_empresa", SqlDbType.Int)
                .Value = idEmpresa;

            cmd.Parameters
                .Add("@desde", SqlDbType.DateTime2)
                .Value = inicioHistorico;

            cmd.Parameters
                .Add("@hasta", SqlDbType.DateTime2)
                .Value = finConsulta;

            await using var reader =
                await cmd.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                if (reader.IsDBNull(0))
                    continue;

                DateTime dia =
                    reader.GetDateTime(0).Date;

                long numeroVentas =
                    reader.IsDBNull(1)
                        ? 0
                        : reader.GetInt64(1);

                decimal total =
                    reader.IsDBNull(2)
                        ? 0M
                        : reader.GetDecimal(2);

                capturados[dia] =
                (
                    (int)Math.Min(
                        numeroVentas,
                        int.MaxValue),

                    total
                );
            }
        }


        // =====================================================
        // CONSTRUIR CALENDARIO COMPLETO
        // =====================================================
        //
        // Incluimos también los días sin ventas.
        //
        // Histórico:
        // fechaDesde - 28 días
        //
        // hasta:
        // fechaHasta
        // =====================================================

        int totalDiasConsulta =
            (fechaHasta - inicioHistorico).Days + 1;

        var todosLosDias =
            Enumerable
                .Range(0, totalDiasConsulta)
                .Select(i =>
                {
                    DateTime dia =
                        inicioHistorico.AddDays(i);

                    var valores =
                        capturados.GetValueOrDefault(dia);

                    return new IaDiaVenta
                    {
                        Fecha = dia,
                        Operaciones =
                            valores.operaciones,

                        Total =
                            valores.total
                    };
                })
                .ToList();


        // =====================================================
        // PERÍODO SOLICITADO POR EL USUARIO
        // =====================================================

        var periodo =
            todosLosDias
                .Where(x =>
                    x.Fecha >= fechaDesde &&
                    x.Fecha <= fechaHasta)
                .ToList();


        result.DatosDisponibles = true;

        result.DiasAnalizados =
            periodo.Count;

        result.DiasConVentas =
            periodo.Count(
                x => x.Operaciones > 0);


        // =====================================================
        // INDICADORES ÚLTIMOS 7 DÍAS DEL PERÍODO
        // =====================================================

        var ultimos7 =
            periodo
                .TakeLast(7)
                .ToList();

        result.ImporteUltimos7Dias =
            ultimos7.Sum(x => x.Total);

        result.VentasUltimos7Dias =
            ultimos7.Sum(x => x.Operaciones);


        // =====================================================
        // SERIE PARA EL GRÁFICO
        // =====================================================
        // Ahora muestra exactamente el período solicitado.
        // =====================================================

        decimal maximo =
            periodo.Count > 0
                ? Math.Max(
                    1M,
                    periodo.Max(x => x.Total))
                : 1M;

        foreach (var dia in periodo)
        {
            dia.Intensidad =
                Math.Clamp(
                    dia.Total / maximo * 100M,
                    0M,
                    100M);

            result.Serie.Add(dia);
        }


        // =====================================================
        // SIN VENTAS
        // =====================================================

        if (result.DiasConVentas == 0)
        {
            result.Aviso =
                "No existen ventas registradas en el período seleccionado.";

            return result;
        }


        // =====================================================
        // DETECCIÓN DE ANOMALÍAS
        // =====================================================
        //
        // Cada día solicitado se compara exclusivamente
        // contra sus 28 días anteriores.
        //
        // No utilizamos información futura.
        // =====================================================

        foreach (var diaActual in periodo)
        {
            DateTime inicioVentana =
                diaActual.Fecha.AddDays(-DiasHistoricos);

            DateTime finVentana =
                diaActual.Fecha.AddDays(-1);


            var historial =
                todosLosDias
                    .Where(x =>
                        x.Fecha >= inicioVentana &&
                        x.Fecha <= finVentana)
                    .ToList();


            // Deben existir 28 días calendario.
            if (historial.Count < DiasHistoricos)
                continue;


            int diasActivos =
                historial.Count(
                    x => x.Operaciones > 0);


            // Evita emitir señales con evidencia insuficiente.
            if (diasActivos < MinimoDiasActivos)
                continue;


            var valores =
                historial
                    .Select(x => (double)x.Total)
                    .OrderBy(x => x)
                    .ToList();


            // -------------------------------------------------
            // MEDIANA
            // -------------------------------------------------

            double mediana =
                Median(valores);


            // -------------------------------------------------
            // MAD
            // Median Absolute Deviation
            // -------------------------------------------------

            var desviaciones =
                valores
                    .Select(x =>
                        Math.Abs(x - mediana))
                    .OrderBy(x => x)
                    .ToList();

            double mad =
                Median(desviaciones);


            // -------------------------------------------------
            // MEDIA
            // Solo se utiliza como referencia informativa.
            // -------------------------------------------------

            double media =
                valores.Average();


            // -------------------------------------------------
            // DESVIACIÓN ESTÁNDAR
            // -------------------------------------------------

            double sd =
                Math.Sqrt(
                    valores.Sum(
                        x =>
                            (x - media) *
                            (x - media))
                    / valores.Count);


            // -------------------------------------------------
            // ESCALA ROBUSTA
            // -------------------------------------------------

            double escala =
                Math.Max(
                    Math.Max(
                        1.4826 * mad,
                        sd * 0.50),

                    Math.Max(
                        Math.Abs(mediana) * 0.15,
                        1.0));


            double observado =
                (double)diaActual.Total;


            double z =
                (observado - mediana)
                / escala;


            double variacion =
                Math.Abs(
                    observado - mediana)
                /
                Math.Max(
                    Math.Abs(mediana),
                    1.0);


            // =================================================
            // UMBRAL
            // =================================================
            //
            // Debe cumplir ambas condiciones:
            //
            // 1. Desviación robusta >= 3.2
            // 2. Diferencia >= 35 %
            // =================================================

            if (Math.Abs(z) < 3.2 ||
                variacion < 0.35)
            {
                continue;
            }


            string tipo =
                z > 0
                    ? "Alza inusual"
                    : "Caída inusual";


            string certidumbre =
                diasActivos >= 18
                    ? "Evidencia moderada"
                    : "Evidencia limitada";


            result.Anomalias.Add(
                new IaAnomalia
                {
                    Fecha =
                        diaActual.Fecha,

                    Tipo =
                        tipo,

                    TotalObservado =
                        diaActual.Total,

                    MediaHistorica =
                        (decimal)media,

                    DesviacionesRobustas =
                        Math.Round(z, 2),

                    DiasConActividad =
                        diasActivos,

                    Certidumbre =
                        certidumbre,

                    Explicacion =
                        "El importe registrado presenta una desviación relevante respecto de los 28 días anteriores. " +
                        "La señal requiere revisión de promociones, pedidos, anulaciones, devoluciones u otros eventos comerciales."
                });
        }


        // =====================================================
        // ORDENAR RESULTADOS
        // =====================================================

        result.Anomalias =
            result.Anomalias
                .OrderByDescending(
                    x =>
                        Math.Abs(
                            x.DesviacionesRobustas))
                .Take(20)
                .ToList();


        // =====================================================
        // AVISO POR POCO HISTORIAL
        // =====================================================

        if (result.DiasConVentas < 7)
        {
            result.Aviso =
                "El período seleccionado contiene poca actividad comercial. " +
                "Las señales deben interpretarse como exploratorias.";
        }


        // =====================================================
        // LOG
        // =====================================================

        _logger.LogInformation(
            "[KERNIX IA] Empresa={Empresa} Desde={Desde} Hasta={Hasta} Dias={Dias} DiasVenta={DiasVenta} Senales={Senales}",
            idEmpresa,
            fechaDesde,
            fechaHasta,
            result.DiasAnalizados,
            result.DiasConVentas,
            result.Anomalias.Count);


        return result;
    }


    // =========================================================
    // MEDIANA
    // =========================================================

    private static double Median(
        IReadOnlyList<double> ordered)
    {
        if (ordered.Count == 0)
            return 0;

        int mitad =
            ordered.Count / 2;

        if (ordered.Count % 2 == 0)
        {
            return
                (
                    ordered[mitad - 1] +
                    ordered[mitad]
                ) / 2;
        }

        return ordered[mitad];
    }
}