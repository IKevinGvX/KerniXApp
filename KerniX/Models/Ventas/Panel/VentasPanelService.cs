using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using KerniX.Models.Ventas.Panel;
using Microsoft.Data.SqlClient;

namespace KerniX.Services.Ventas.Panel;

/// <summary>
/// Lectura SQL real, sin inventar las columnas. Detecta metadatos en cada petición;
/// si no puede garantizar separación por empresa, NO ejecuta el listado.
/// No ejecuta INSERT, UPDATE, DELETE ni procedimientos con efectos secundarios.
/// </summary>
public sealed class VentasPanelService : IVentasPanelService
{
    private readonly string _connectionString;
    private readonly ILogger<VentasPanelService> _logger;
    private const int PageSize = 5;
    private const int MaxExport = 10000;
    private static readonly string[] Exitos = { "registrado", "confirmado", "pagado", "emitido", "aceptado", "completado", "aprobado", "cerrado", "atendido", "finalizado", "aplicada", "procesada", "1" };
    private static readonly string[] Espera = { "pendiente", "en proceso", "en_proceso", "observado", "abierto", "solicitada", "registrada", "por cobrar", "0" };

    public VentasPanelService(IConfiguration configuration, ILogger<VentasPanelService> logger)
    {
        _connectionString = configuration.GetConnectionString("KerniXDb")
            ?? throw new InvalidOperationException("Falta ConnectionStrings:KerniXDb.");
        _logger = logger;
    }

    public async Task<int?> ObtenerEmpresaDeUsuarioAsync(int idUsuario, CancellationToken ct = default)
    {
        if (idUsuario <= 0) return null;
        await using var db = new SqlConnection(_connectionString);
        await db.OpenAsync(ct);
        const string sql = """
            SELECT TOP (1) e.id_empresa
            FROM seg.usuario AS u
            INNER JOIN org.empleado AS e ON e.id_empleado = u.id_empleado
            WHERE u.id_usuario = @usuario AND e.id_empresa > 0;
            """;
        await using var command = new SqlCommand(sql, db);
        command.Parameters.Add("@usuario", SqlDbType.Int).Value = idUsuario;
        var result = await command.ExecuteScalarAsync(ct);
        return result is null || result is DBNull ? null : Convert.ToInt32(result, CultureInfo.InvariantCulture);
    }

    private static string TableName(PanelTipo tipo) => tipo switch
    {
        PanelTipo.Pagos => "ven.pago",
        PanelTipo.Comprobantes => "ven.comprobante",
        PanelTipo.Devoluciones => "ven.devolucion",
        _ => "ven.reclamo"
    };

    private static string? First(ISet<string> fields, params string[] choices) => choices.FirstOrDefault(fields.Contains);
    private static string Q(string col) => "[" + col.Replace("]", "]]", StringComparison.Ordinal) + "]";
    private static string ToText(string? col, string prefix = "t", int len = 160) => col is null
        ? "CAST(N'' AS NVARCHAR(160))"
        : $"COALESCE(TRY_CONVERT(NVARCHAR({len}), {prefix}.{Q(col)}), N'')";

    private async Task<HashSet<string>> FieldsAsync(SqlConnection db, string name, CancellationToken ct)
    {
        var fields = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var cmd = new SqlCommand("SELECT name FROM sys.columns WHERE object_id = OBJECT_ID(@obj, 'U')", db);
        cmd.Parameters.Add("@obj", SqlDbType.NVarChar, 256).Value = name;
        await using var rd = await cmd.ExecuteReaderAsync(ct);
        while (await rd.ReadAsync(ct)) fields.Add(rd.GetString(0));
        return fields;
    }

    private sealed class QueryShape
    {
        public string From { get; init; } = "";
        public string EmpresaCondition { get; init; } = "";
        public string Id { get; init; } = "";
        public string Document { get; init; } = "";
        public string Client { get; init; } = "";
        public string Date { get; init; } = "CAST(NULL AS DATETIME2)";
        public string Amount { get; init; } = "CAST(NULL AS DECIMAL(18,2))";
        public string Kind { get; init; } = "CAST(N'' AS NVARCHAR(160))";
        public string State { get; init; } = "CAST(N'' AS NVARCHAR(160))";
        public string Reference { get; init; } = "CAST(N'' AS NVARCHAR(160))";
        public string StatusGroup { get; init; } = "N'otro'";
        public bool HasAmount { get; init; }
        public bool HasDate { get; init; }
        public bool HasState { get; init; }
    }

    private static string ValuesSql(string[] values) => string.Join(",", values.Select(s => "N'" + s.Replace("'", "''", StringComparison.Ordinal) + "'"));

    private async Task<QueryShape?> ShapeAsync(SqlConnection db, PanelTipo tipo, CancellationToken ct)
    {
        string table = TableName(tipo);
        var t = await FieldsAsync(db, table, ct);
        var v = await FieldsAsync(db, "ven.venta", ct);
        var c = await FieldsAsync(db, "ven.cliente", ct);
        var id = First(t, tipo switch
        {
            PanelTipo.Pagos => new[] { "id_pago" },
            PanelTipo.Comprobantes => new[] { "id_comprobante" },
            PanelTipo.Devoluciones => new[] { "id_devolucion" },
            _ => new[] { "id_reclamo" }
        });
        if (t.Count == 0 || id is null) return null;

        bool hasVenta = t.Contains("id_venta") && v.Contains("id_venta") && v.Contains("id_empresa");
        string from = $"{(tipo switch { PanelTipo.Pagos => "ven.pago", PanelTipo.Comprobantes => "ven.comprobante", PanelTipo.Devoluciones => "ven.devolucion", _ => "ven.reclamo" })} AS t";
        if (hasVenta) from += " LEFT JOIN ven.venta AS v ON v.id_venta = t.id_venta AND v.id_empresa = @empresa";

        string empresaCondition;
        if (t.Contains("id_empresa")) empresaCondition = "t.id_empresa = @empresa";
        else if (hasVenta) empresaCondition = "v.id_empresa = @empresa";
        else throw new InvalidOperationException($"{table} no contiene id_empresa ni relación segura con ven.venta: se bloqueó la consulta.");

        string cliente = "N'Cliente no identificado'";
        bool hasClientTable = c.Contains("id_cliente");
        bool directClient = t.Contains("id_cliente");
        bool viaVentaClient = hasVenta && v.Contains("id_cliente");
        if (hasClientTable && (directClient || viaVentaClient))
        {
            string colJoin = directClient ? "t.id_cliente" : "v.id_cliente";
            // Al existir id_empresa en ven.cliente también se acota ese JOIN.
            from += $" LEFT JOIN ven.cliente AS cl ON cl.id_cliente = {colJoin}";
            if (c.Contains("id_empresa")) from += " AND cl.id_empresa = @empresa";
            string? razon = First(c, "razon_social", "nombre_comercial");
            string? nombres = First(c, "nombres", "nombre");
            string? apellidos = First(c, "apellidos", "apellido_paterno");
            var pieces = new List<string>();
            if (razon != null) pieces.Add($"NULLIF(LTRIM(RTRIM({ToText(razon, "cl", 300)})),N'')");
            if (nombres != null)
            {
                string fullName = apellidos is null ? ToText(nombres, "cl") : $"CONCAT({ToText(nombres, "cl")},N' ',{ToText(apellidos, "cl")})";
                pieces.Add($"NULLIF(LTRIM(RTRIM({fullName})),N'')");
            }
            if (pieces.Count != 0) cliente = "COALESCE(" + string.Join(",", pieces) + ",N'Cliente no identificado')";
        }

        string? colNumero = First(t, "numero_operacion", "numero_devolucion", "numero_reclamo", "numero_comprobante", "numero", "codigo");
        string documento = ToText(colNumero);
        if (tipo == PanelTipo.Comprobantes && t.Contains("serie") && t.Contains("numero"))
            documento = $"CONCAT({ToText("serie")},N'-',{ToText("numero")})";
        // Sin numero de documento no se fabrica una serie a partir del ID interno.
        // La columna Venta se obtiene de ven.venta.numero_venta si el JOIN es válido.
        string? colFecha = First(t, "fecha_pago", "fecha_emision", "fecha_devolucion", "fecha_reclamo", "fecha_registro", "fecha_creacion", "fecha");
        string? colMonto = First(t, "monto", "monto_total", "total", "importe", "total_devolucion");
        string? colEstado = First(t, tipo == PanelTipo.Comprobantes ? "estado_sunat" : "estado", "estado", "estado_sunat");
        string? colTipo = First(t, "metodo_pago", "tipo_comprobante", "tipo_devolucion", "tipo_reclamo", "tipo", "motivo");
        string date = colFecha is null ? "CAST(NULL AS DATETIME2)" : $"TRY_CONVERT(DATETIME2,{"t." + Q(colFecha)})";
        string amount = colMonto is null ? "CAST(NULL AS DECIMAL(18,2))" : $"TRY_CONVERT(DECIMAL(18,2),t.{Q(colMonto)})";
        string state = ToText(colEstado);
        string group = colEstado is null ? "N'otro'" : $"CASE WHEN LOWER(LTRIM(RTRIM({state}))) IN ({ValuesSql(Exitos)}) THEN N'ok' WHEN LOWER(LTRIM(RTRIM({state}))) IN ({ValuesSql(Espera)}) THEN N'pendiente' ELSE N'otro' END";
        return new QueryShape
        {
            From = from,
            EmpresaCondition = empresaCondition,
            Id = $"TRY_CONVERT(BIGINT,t.{Q(id)})",
            Document = documento,
            Client = cliente,
            Date = date,
            Amount = amount,
            Kind = ToText(colTipo),
            State = state,
            Reference = hasVenta && v.Contains("numero_venta")
                ? ToText("numero_venta", "v", 100)
                : "CAST(N'' AS NVARCHAR(160))",
            StatusGroup = group,
            HasDate = colFecha is not null,
            HasAmount = colMonto is not null,
            HasState = colEstado is not null
        };
    }

    private static (string where, string stateFilter) Filter(QueryShape shape, string? estado, DateTime? desde, DateTime? hasta)
    {
        var parts = new List<string> { shape.EmpresaCondition };
        if (shape.HasDate)
        {
            if (desde.HasValue) parts.Add($"{shape.Date} >= @desde");
            if (hasta.HasValue) parts.Add($"{shape.Date} < DATEADD(DAY, 1, @hasta)");
        }
        parts.Add($"(@buscar = N'' OR {shape.Document} LIKE N'%' + @buscar + N'%' OR {shape.Client} LIKE N'%' + @buscar + N'%' OR {shape.Reference} LIKE N'%' + @buscar + N'%')");
        string stateFilter = estado switch { "completados" => "ok", "pendientes" => "pendiente", "otros" => "otro", _ => "" };
        if (stateFilter.Length > 0) parts.Add($"({shape.StatusGroup}) = @grupo");
        return (" WHERE " + string.Join(" AND ", parts), stateFilter);
    }

    private static SqlCommand Command(SqlConnection db, string sql, int idEmpresa, string? buscar, string group,
        DateTime? desde, DateTime? hasta, int? pagina = null)
    {
        var cmd = new SqlCommand(sql, db) { CommandTimeout = 30 };
        cmd.Parameters.Add("@empresa", SqlDbType.Int).Value = idEmpresa;
        cmd.Parameters.Add("@buscar", SqlDbType.NVarChar, 120).Value = (buscar ?? "").Trim()[..Math.Min((buscar ?? "").Trim().Length, 120)];
        cmd.Parameters.Add("@grupo", SqlDbType.NVarChar, 30).Value = group;
        cmd.Parameters.Add("@desde", SqlDbType.Date).Value = desde.HasValue ? desde.Value.Date : DBNull.Value;
        cmd.Parameters.Add("@hasta", SqlDbType.Date).Value = hasta.HasValue ? hasta.Value.Date : DBNull.Value;
        if (pagina.HasValue)
        {
            cmd.Parameters.Add("@offset", SqlDbType.Int).Value = (pagina.Value - 1) * PageSize;
            cmd.Parameters.Add("@tamano", SqlDbType.Int).Value = PageSize;
        }
        return cmd;
    }

    private static PanelRegistro ReadRow(SqlDataReader r) => new()
    {
        Id = r.IsDBNull(0) ? 0 : r.GetInt64(0),
        Documento = r.IsDBNull(1) ? "" : r.GetString(1),
        Cliente = r.IsDBNull(2) ? "" : r.GetString(2),
        Fecha = r.IsDBNull(3) ? null : r.GetDateTime(3),
        Monto = r.IsDBNull(4) ? null : r.GetDecimal(4),
        Tipo = r.IsDBNull(5) ? "" : r.GetString(5),
        Estado = r.IsDBNull(6) ? "" : r.GetString(6),
        Referencia = r.IsDBNull(7) ? "" : r.GetString(7),
        Categoria = r.IsDBNull(8) ? "otro" : r.GetString(8)
    };

    private static string SelectRows(QueryShape sh) => $"SELECT {sh.Id} AS Id, {sh.Document} AS Documento, {sh.Client} AS Cliente, {sh.Date} AS Fecha, {sh.Amount} AS Monto, {sh.Kind} AS Tipo, {sh.State} AS Estado, {sh.Reference} AS Referencia, {sh.StatusGroup} AS Categoria FROM {sh.From}";

    public async Task<VentasPanelVm> ObtenerAsync(PanelTipo tipo, int idEmpresa, string? buscar, string? estado,
        int pagina, DateTime? desde, DateTime? hasta, CancellationToken ct = default)
    {
        var vm = new VentasPanelVm { Tipo = tipo, Buscar = (buscar ?? "").Trim(), EstadoFiltro = estado is "completados" or "pendientes" or "otros" ? estado : "todos", Pagina = Math.Clamp(pagina, 1, 100000), Desde = desde, Hasta = hasta };
        if (idEmpresa <= 0) { vm.Aviso = "No se identificó una empresa válida para esta sesión."; return vm; }
        try
        {
            await using var db = new SqlConnection(_connectionString);
            await db.OpenAsync(ct);
            var shape = await ShapeAsync(db, tipo, ct);
            if (shape is null) { vm.Aviso = $"No se encontró una tabla compatible para {vm.Titulo}. Revisa SQL/DIAGNOSTICO_MODULOS.sql."; return vm; }
            if (!shape.HasDate && (desde.HasValue || hasta.HasValue)) { vm.Aviso = "La tabla no tiene columna de fecha: no se aplicó el filtro temporal."; }
            if (!shape.HasState && vm.EstadoFiltro != "todos") { vm.Aviso = "La tabla no tiene columna de estado: no se aplicó ese filtro."; vm.EstadoFiltro = "todos"; }
            vm.TieneMonto = shape.HasAmount;
            var (where, group) = Filter(shape, vm.EstadoFiltro, desde, hasta);
            var metricsSql = $"SELECT COUNT(1), COALESCE(SUM(CASE WHEN {shape.StatusGroup} = N'ok' THEN 1 ELSE 0 END),0), COALESCE(SUM(CASE WHEN {shape.StatusGroup} = N'pendiente' THEN 1 ELSE 0 END),0), COALESCE(SUM({shape.Amount}),0) FROM {shape.From} {where};";
            await using (var cmd = Command(db, metricsSql, idEmpresa, buscar, group, desde, hasta))
            await using (var reader = await cmd.ExecuteReaderAsync(ct))
                if (await reader.ReadAsync(ct))
                {
                    vm.Total = reader.GetInt32(0);
                    vm.Completados = reader.GetInt32(1);
                    vm.Pendientes = reader.GetInt32(2);
                    vm.MontoTotal = reader.GetDecimal(3);
                }
            vm.Pagina = Math.Clamp(vm.Pagina, 1, vm.TotalPaginas);
            var listSql = SelectRows(shape) + $" {where} ORDER BY {shape.Id} DESC OFFSET @offset ROWS FETCH NEXT @tamano ROWS ONLY;";
            await using (var cmd = Command(db, listSql, idEmpresa, buscar, group, desde, hasta, vm.Pagina))
            await using (var reader = await cmd.ExecuteReaderAsync(ct))
                while (await reader.ReadAsync(ct)) vm.Registros.Add(ReadRow(reader));
            await LoadChartAsync(db, vm, shape, idEmpresa, ct);
            _logger.LogInformation("[KERNIX PANEL] {Tipo} Empresa={Empresa} Registros={Total}", tipo, idEmpresa, vm.Total);
        }
        catch (SqlException ex)
        {
            _logger.LogError(ex, "[KERNIX PANEL] Error SQL en {Tipo}", tipo);
            vm.Aviso = "No se pudo consultar SQL Server. Revisa la ventana Salida de Visual Studio.";
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "[KERNIX PANEL] Lectura bloqueada en {Tipo}", tipo);
            vm.Aviso = ex.Message;
        }
        return vm;
    }

    private static async Task LoadChartAsync(SqlConnection db, VentasPanelVm vm, QueryShape shape, int idEmpresa, CancellationToken ct)
    {
        var now = DateTime.Today;
        var months = Enumerable.Range(0, 6).Select(i => new DateTime(now.Year, now.Month, 1).AddMonths(i - 5)).ToList();
        var data = months.ToDictionary(x => (x.Year, x.Month), _ => (count: 0, amount: 0m));
        if (shape.HasDate)
        {
            string sql = $"SELECT YEAR({shape.Date}), MONTH({shape.Date}), COUNT(1), COALESCE(SUM({shape.Amount}),0) FROM {shape.From} WHERE {shape.EmpresaCondition} AND {shape.Date} >= @inicio AND {shape.Date} < @fin GROUP BY YEAR({shape.Date}),MONTH({shape.Date});";
            await using var cmd = new SqlCommand(sql, db);
            cmd.Parameters.Add("@empresa", SqlDbType.Int).Value = idEmpresa;
            cmd.Parameters.Add("@inicio", SqlDbType.Date).Value = months[0];
            cmd.Parameters.Add("@fin", SqlDbType.Date).Value = months[^1].AddMonths(1);
            await using var rd = await cmd.ExecuteReaderAsync(ct);
            while (await rd.ReadAsync(ct))
            {
                var key = (rd.GetInt32(0), rd.GetInt32(1));
                if (data.ContainsKey(key)) data[key] = (rd.GetInt32(2), rd.GetDecimal(3));
            }
        }
        double max = Math.Max(1, shape.HasAmount ? data.Values.Max(x => (double)x.amount) : data.Values.Max(x => (double)x.count));
        foreach (var m in months)
        {
            var v = data[(m.Year, m.Month)];
            double value = shape.HasAmount ? (double)v.amount : v.count;
            vm.Serie.Add(new PanelPunto { Mes = m.ToString("MMM", new CultureInfo("es-PE")).ToUpperInvariant(), Cantidad = v.count, Monto = v.amount, Altura = Math.Max(6, value / max * 100) });
        }
    }

    public async Task<IReadOnlyList<PanelRegistro>> ExportarAsync(PanelTipo tipo, int idEmpresa, string? buscar,
        string? estado, DateTime? desde, DateTime? hasta, CancellationToken ct = default)
    {
        if (idEmpresa <= 0) return Array.Empty<PanelRegistro>();
        await using var db = new SqlConnection(_connectionString);
        await db.OpenAsync(ct);
        var shape = await ShapeAsync(db, tipo, ct) ?? throw new InvalidOperationException("No existe una tabla compatible para exportación.");
        var (where, group) = Filter(shape, estado, desde, hasta);
        string sql = SelectRows(shape) + $" {where} ORDER BY {shape.Id} DESC OFFSET 0 ROWS FETCH NEXT {MaxExport} ROWS ONLY;";
        var list = new List<PanelRegistro>();
        await using var cmd = Command(db, sql, idEmpresa, buscar, group, desde, hasta);
        await using var rd = await cmd.ExecuteReaderAsync(ct);
        while (await rd.ReadAsync(ct)) list.Add(ReadRow(rd));
        return list;
    }
}
