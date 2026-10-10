using System.Data;
using KerniX.Models.Proveedores;
using Microsoft.Data.SqlClient;

namespace KerniX.Services.Proveedores;

/// <summary>
/// Catálogo de proveedores. Columnas adaptadas al esquema com.proveedor confirmado.
/// Inspecciona columnas reales, limita operaciones a la empresa y parametriza datos.
/// Si la tabla es global, requiere habilitación explícita en appsettings.
/// </summary>
public sealed class ProveedorDbService : IProveedorDbService
{
    private readonly string _connectionString;
    private readonly ILogger<ProveedorDbService> _logger;

    public ProveedorDbService(IConfiguration config, ILogger<ProveedorDbService> logger)
    {
        _connectionString = config.GetConnectionString("KerniXDb")
            ?? throw new InvalidOperationException("Falta ConnectionStrings:KerniXDb.");
        _logger = logger;
    }

    private sealed record Columna(string Nombre, string Tipo, bool Nula,
        bool Identity, bool Computada, bool TieneDefault, short MaxLength);

    private sealed class Estructura
    {
        public required Dictionary<string, Columna> Columnas { get; init; }
        public required string Id { get; init; }
        public required string Estado { get; init; }
        public required string NumeroDocumento { get; init; }
        public required string TipoDocumento { get; init; }
        public required string Razon { get; init; }
        public required string Empresa { get; init; }
        public string? Comercial { get; init; }
        public string? Telefono { get; init; }
        public string? Correo { get; init; }

        public string FiltroEmpresa => $" AND {SqlIdent(Empresa)} = @IdEmpresa";
    }

    private static string SqlIdent(string value) =>
        "[" + value.Replace("]", "]]", StringComparison.Ordinal) + "]";

    private static string? BuscarCol(Dictionary<string, Columna> cols, params string[] names) =>
        names.FirstOrDefault(cols.ContainsKey);

    private static string RequerirCol(Dictionary<string, Columna> cols, params string[] names) =>
        BuscarCol(cols, names) ?? throw new InvalidOperationException(
            "La tabla com.proveedor requiere una columna compatible: " + string.Join(" / ", names));

    private async Task<Estructura> EsquemaAsync(SqlConnection cn, CancellationToken ct)
    {
        const string consulta = """
            SELECT c.name, t.name AS Tipo, c.is_nullable, c.is_identity,
                   c.is_computed, CAST(CASE WHEN c.default_object_id <> 0 THEN 1 ELSE 0 END AS bit),
                   c.max_length
            FROM sys.columns c
            INNER JOIN sys.types t ON t.user_type_id = c.user_type_id
            WHERE c.object_id = OBJECT_ID(N'com.proveedor', N'U');
            """;
        var columnas = new Dictionary<string, Columna>(StringComparer.OrdinalIgnoreCase);
        await using (var cmd = new SqlCommand(consulta, cn))
        await using (var rd = await cmd.ExecuteReaderAsync(ct))
        {
            while (await rd.ReadAsync(ct))
            {
                var c = new Columna(rd.GetString(0), rd.GetString(1),
                    rd.GetBoolean(2), rd.GetBoolean(3), rd.GetBoolean(4),
                    rd.GetBoolean(5), rd.GetInt16(6));
                columnas[c.Nombre] = c;
            }
        }
        if (columnas.Count == 0)
            throw new InvalidOperationException("No se encontró com.proveedor o faltan permisos SQL.");

        string empresa = RequerirCol(columnas, "id_empresa");

        var estado = RequerirCol(columnas, "estado");
        if (!string.Equals(columnas[estado].Tipo, "bit", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("com.proveedor.estado debe ser BIT para activar/desactivar de forma segura.");

        return new Estructura
        {
            Columnas = columnas,
            Id = RequerirCol(columnas, "id_proveedor"),
            Estado = estado,
            NumeroDocumento = RequerirCol(columnas, "numero_documento"),
            TipoDocumento = RequerirCol(columnas, "tipo_documento"),
            Razon = RequerirCol(columnas, "razon_social"),
            Empresa = empresa,
            Comercial = BuscarCol(columnas, "nombre_comercial"),
            Telefono = BuscarCol(columnas, "telefono", "celular"),
            Correo = BuscarCol(columnas, "correo", "email")
        };
    }

    private static void ArgsLectura(SqlCommand cmd, int idEmpresa, string? buscar = null, bool? estado = null)
    {
        cmd.Parameters.Add("@IdEmpresa", SqlDbType.Int).Value = idEmpresa;
        cmd.Parameters.Add("@Buscar", SqlDbType.NVarChar, 300).Value =
            string.IsNullOrWhiteSpace(buscar) ? DBNull.Value : $"%{EscaparLike(buscar.Trim())}%";
        cmd.Parameters.Add("@Estado", SqlDbType.Bit).Value = estado.HasValue ? (object)estado.Value : DBNull.Value;
    }

    private static string EscaparLike(string value) => value
        .Replace("~", "~~", StringComparison.Ordinal)
        .Replace("%", "~%", StringComparison.Ordinal)
        .Replace("_", "~_", StringComparison.Ordinal)
        .Replace("[", "~[", StringComparison.Ordinal);

    private static string Texto(SqlDataReader rd, string col) => rd[col] is DBNull
        ? "" : Convert.ToString(rd[col]) ?? "";

    private static ProveedorItem Mapear(SqlDataReader rd) => new()
    {
        IdProveedor = Convert.ToInt32(rd["IdProveedor"]),
        TipoDocumento = Texto(rd, "TipoDocumento"),
        NumeroDocumento = Texto(rd, "NumeroDocumento"),
        RazonSocial = Texto(rd, "RazonSocial"),
        NombreComercial = Texto(rd, "NombreComercial"),
        Telefono = Texto(rd, "Telefono"),
        Correo = Texto(rd, "Correo"),
        Estado = Convert.ToBoolean(rd["Estado"]),
        FechaRegistro = Convert.ToDateTime(rd["FechaRegistro"])
    };

    private static string Proyectar(string? columna, string alias) => columna is null
        ? $"CAST(N'' AS NVARCHAR(400)) AS {SqlIdent(alias)}"
        : $"COALESCE(CONVERT(NVARCHAR(400), {SqlIdent(columna)}), N'') AS {SqlIdent(alias)}";

    private static string CamposSelect(Estructura s) => $"""
        {SqlIdent(s.Id)} AS IdProveedor,
        {Proyectar(s.TipoDocumento, "TipoDocumento")},
        {Proyectar(s.NumeroDocumento, "NumeroDocumento")},
        {Proyectar(s.Razon, "RazonSocial")},
        {Proyectar(s.Comercial, "NombreComercial")},
        {Proyectar(s.Telefono, "Telefono")},
        {Proyectar(s.Correo, "Correo")},
        CONVERT(BIT, {SqlIdent(s.Estado)}) AS Estado,
        [fecha_registro] AS FechaRegistro
        """;

    public async Task<ProveedorListado> ListarAsync(int idEmpresa, string? buscar, bool? estado,
        int pagina, int tamanoPagina = 12, CancellationToken cancellationToken = default)
    {
        if (idEmpresa <= 0) throw new InvalidOperationException("Empresa inválida.");
        pagina = Math.Max(1, pagina);
        tamanoPagina = Math.Clamp(tamanoPagina, 5, 50);
        buscar = string.IsNullOrWhiteSpace(buscar) ? null : buscar.Trim()[..Math.Min(buscar.Trim().Length, 120)];

        await using var cn = new SqlConnection(_connectionString);
        await cn.OpenAsync(cancellationToken);
        var s = await EsquemaAsync(cn, cancellationToken);
        var termino = new[] { s.NumeroDocumento, s.TipoDocumento, s.Razon, s.Comercial, s.Correo, s.Telefono }
            .Where(x => x is not null).Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(x => $"CONVERT(NVARCHAR(400), {SqlIdent(x!)}) LIKE @Buscar ESCAPE N'~'");
        var filtroBuscar = " AND (@Buscar IS NULL OR (" + string.Join(" OR ", termino) + "))";
        var from = "FROM [com].[proveedor] WHERE 1=1" + s.FiltroEmpresa;
        var where = from + filtroBuscar + $" AND (@Estado IS NULL OR {SqlIdent(s.Estado)} = @Estado)";
        var listado = new ProveedorListado { Pagina = pagina, TamanoPagina = tamanoPagina };

        var sqlCount = $"""
            SELECT COUNT(*) AS Total,
                COALESCE(SUM(CASE WHEN {SqlIdent(s.Estado)} = 1 THEN 1 ELSE 0 END), 0) AS Activos,
                COALESCE(SUM(CASE WHEN {SqlIdent(s.Estado)} = 0 THEN 1 ELSE 0 END), 0) AS Inactivos
            {from};
            SELECT COUNT(*) AS Filtrados {where};
            """;
        await using (var cmd = new SqlCommand(sqlCount, cn))
        {
            ArgsLectura(cmd, idEmpresa, buscar, estado);
            await using var rd = await cmd.ExecuteReaderAsync(cancellationToken);
            if (await rd.ReadAsync(cancellationToken))
            {
                listado.Total = rd.GetInt32(0);
                listado.Activos = rd.GetInt32(1);
                listado.Inactivos = rd.GetInt32(2);
            }
            if (await rd.NextResultAsync(cancellationToken) && await rd.ReadAsync(cancellationToken))
                listado.TotalFiltrado = rd.GetInt32(0);
        }
        listado.Pagina = Math.Min(pagina, listado.TotalPaginas);
        var consulta = $"""
            SELECT {CamposSelect(s)} {where}
            ORDER BY {SqlIdent(s.Id)} DESC
            OFFSET @Offset ROWS FETCH NEXT @Tamano ROWS ONLY;
            """;
        await using (var cmd = new SqlCommand(consulta, cn))
        {
            ArgsLectura(cmd, idEmpresa, buscar, estado);
            cmd.Parameters.Add("@Offset", SqlDbType.Int).Value = (listado.Pagina - 1) * tamanoPagina;
            cmd.Parameters.Add("@Tamano", SqlDbType.Int).Value = tamanoPagina;
            await using var rd = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await rd.ReadAsync(cancellationToken)) listado.Items.Add(Mapear(rd));
        }
        _logger.LogInformation("[PROVEEDORES] Empresa={Empresa} Total={Total} Filtrados={Filtrados}",
            idEmpresa, listado.Total, listado.TotalFiltrado);
        return listado;
    }

    public async Task<ProveedorItem?> ObtenerAsync(int idEmpresa, int idProveedor,
        CancellationToken cancellationToken = default)
    {
        if (idEmpresa <= 0 || idProveedor <= 0) return null;
        await using var cn = new SqlConnection(_connectionString);
        await cn.OpenAsync(cancellationToken);
        var s = await EsquemaAsync(cn, cancellationToken);
        var sql = $"SELECT TOP (1) {CamposSelect(s)} FROM [com].[proveedor] " +
            $"WHERE {SqlIdent(s.Id)} = @IdProveedor{s.FiltroEmpresa};";
        await using var cmd = new SqlCommand(sql, cn);
        ArgsLectura(cmd, idEmpresa);
        cmd.Parameters.Add("@IdProveedor", SqlDbType.Int).Value = idProveedor;
        await using var rd = await cmd.ExecuteReaderAsync(cancellationToken);
        return await rd.ReadAsync(cancellationToken) ? Mapear(rd) : null;
    }

    private static string? Limpio(string? value) => string.IsNullOrWhiteSpace(value)
        ? null : value.Trim();

    private static Dictionary<string, string?> Valores(Estructura s, ProveedorFormulario f) =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            [s.TipoDocumento] = Limpio(f.TipoDocumento),
            [s.NumeroDocumento] = Limpio(f.NumeroDocumento),
            [s.Razon] = Limpio(f.RazonSocial),
            // Los campos no existentes no se incluyen.
        };

    private static Dictionary<string, string?> ValoresCompletos(Estructura s, ProveedorFormulario f)
    {
        var resultado = Valores(s, f);
        if (s.Comercial is not null) resultado[s.Comercial] = Limpio(f.NombreComercial);
        if (s.Telefono is not null) resultado[s.Telefono] = Limpio(f.Telefono);
        if (s.Correo is not null) resultado[s.Correo] = Limpio(f.Correo);
        return resultado;
    }

    private static void ValidarLongitudes(Estructura s, Dictionary<string, string?> valores)
    {
        foreach (var (nombre, valor) in valores)
        {
            var col = s.Columnas[nombre];
            if (valor is null)
            {
                if (!col.Nula)
                    throw new InvalidOperationException($"El campo {nombre} no admite valores vacíos según la base de datos.");
                continue;
            }
            if (col.Tipo is not ("nvarchar" or "nchar" or "varchar" or "char"))
                throw new InvalidOperationException($"La columna {nombre} no es de texto. Revisa el SQL de proveedores.");
            if (col.MaxLength == -1) continue;
            int max = col.Tipo.StartsWith('n') ? col.MaxLength / 2 : col.MaxLength;
            if (valor.Length > max)
                throw new InvalidOperationException($"El valor de {nombre} supera el máximo de {max} caracteres de la base de datos.");
        }
    }

    private static void PonerValores(SqlCommand cmd, Dictionary<string, string?> valores,
        IReadOnlyList<string> nombres)
    {
        for (int i = 0; i < nombres.Count; i++)
        {
            cmd.Parameters.Add("@P" + i, SqlDbType.NVarChar, -1).Value =
                valores[nombres[i]] is { } val ? (object)val : DBNull.Value;
        }
    }

    private static string DuplicadoSql(Estructura s, int? idExcluir) =>
        $"SELECT COUNT(1) FROM [com].[proveedor] WITH (UPDLOCK, HOLDLOCK) " +
        $"WHERE {SqlIdent(s.TipoDocumento)} = @TipoDocumento AND {SqlIdent(s.NumeroDocumento)} = @NumeroDocumento{s.FiltroEmpresa}" +
        (idExcluir.HasValue ? $" AND {SqlIdent(s.Id)} <> @IdProveedor" : "") + ";";

    private static async Task VerificarDuplicadoAsync(SqlConnection cn, SqlTransaction tx,
        Estructura s, int empresa, string tipoDocumento, string numeroDocumento, int? idExcluir, CancellationToken ct)
    {
        await using var cmd = new SqlCommand(DuplicadoSql(s, idExcluir), cn, tx);
        cmd.Parameters.Add("@TipoDocumento", SqlDbType.NVarChar, 20).Value = tipoDocumento;
        cmd.Parameters.Add("@NumeroDocumento", SqlDbType.NVarChar, 20).Value = numeroDocumento;
        cmd.Parameters.Add("@IdEmpresa", SqlDbType.Int).Value = empresa;
        if (idExcluir.HasValue) cmd.Parameters.Add("@IdProveedor", SqlDbType.Int).Value = idExcluir.Value;
        if ((int)(await cmd.ExecuteScalarAsync(ct) ?? 0) > 0)
            throw new InvalidOperationException("Ya existe un proveedor con ese tipo y número de documento en esta empresa.");
    }

    private static bool EsFecha(string tipo) => tipo is "date" or "datetime" or "datetime2" or "smalldatetime";

    private static void ValidarColumnasParaInsert(Estructura s,
        HashSet<string> incluidos, List<(string Nombre, string Expresion)> automaticos)
    {
        string[] fechas = { "fecha_registro", "fecha_creacion", "fecha_alta", "fecha_actualizacion", "fecha_modificacion" };
        foreach (var c in s.Columnas.Values)
        {
            if (incluidos.Contains(c.Nombre) || c.Identity || c.Computada || c.Nula || c.TieneDefault ||
                c.Tipo is "timestamp" or "rowversion")
                continue;
            if (fechas.Contains(c.Nombre, StringComparer.OrdinalIgnoreCase) && EsFecha(c.Tipo))
            {
                automaticos.Add((c.Nombre, "SYSDATETIME()"));
                continue;
            }
            throw new InvalidOperationException(
                $"No se puede crear proveedor: la columna obligatoria '{c.Nombre}' " +
                "no tiene valor por defecto. Ajusta la integración a la estructura real de com.proveedor.");
        }
    }

    public async Task<int> CrearAsync(int idEmpresa, int idUsuario, ProveedorFormulario formulario,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(formulario);
        if (idEmpresa <= 0 || idUsuario <= 0) throw new InvalidOperationException("Contexto de usuario o empresa inválido.");
        await using var cn = new SqlConnection(_connectionString);
        await cn.OpenAsync(cancellationToken);
        var s = await EsquemaAsync(cn, cancellationToken);
        var valores = ValoresCompletos(s, formulario);
        ValidarLongitudes(s, valores);

        if (!s.Columnas[s.Id].Identity)
            throw new InvalidOperationException("La clave id_proveedor no es IDENTITY. Este proveedor necesita una estrategia de generación de ID específica.");
        var nombres = valores.Keys.ToList();
        var columnas = nombres.Select(SqlIdent).ToList();
        var expresiones = Enumerable.Range(0, nombres.Count).Select(i => "@P" + i).ToList();
        if (s.Empresa is not null) { columnas.Add(SqlIdent(s.Empresa)); expresiones.Add("@IdEmpresa"); }
        columnas.Add(SqlIdent(s.Estado)); expresiones.Add("CAST(1 AS bit)");
        var automaticos = new List<(string Nombre, string Expresion)>();
        var incluidos = new HashSet<string>(nombres, StringComparer.OrdinalIgnoreCase) { s.Estado };
        if (s.Empresa is not null) incluidos.Add(s.Empresa);
        ValidarColumnasParaInsert(s, incluidos, automaticos);
        foreach (var (nombre, expr) in automaticos) { columnas.Add(SqlIdent(nombre)); expresiones.Add(expr); }

        await using var tx = cn.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            await VerificarDuplicadoAsync(cn, tx, s, idEmpresa, formulario.TipoDocumento.Trim(), formulario.NumeroDocumento.Trim(), null, cancellationToken);
            var sql = $"INSERT INTO [com].[proveedor] ({string.Join(",", columnas)}) " +
                $"VALUES ({string.Join(",", expresiones)}); SELECT CONVERT(INT, SCOPE_IDENTITY());";
            await using var cmd = new SqlCommand(sql, cn, tx);
            PonerValores(cmd, valores, nombres);
            cmd.Parameters.Add("@IdEmpresa", SqlDbType.Int).Value = idEmpresa;
            int id = Convert.ToInt32(await cmd.ExecuteScalarAsync(cancellationToken));
            await GuardarEventoAsync(cn, tx, id, idEmpresa, idUsuario, "CREAR", cancellationToken);
            await tx.CommitAsync(cancellationToken);
            _logger.LogInformation("[PROVEEDORES] Registrado Id={Id} Empresa={Empresa}", id, idEmpresa);
            return id;
        }
        catch { await tx.RollbackAsync(CancellationToken.None); throw; }
    }

    public async Task<bool> ActualizarAsync(int idEmpresa, int idUsuario, int idProveedor, ProveedorFormulario formulario,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(formulario);
        if (idEmpresa <= 0 || idUsuario <= 0 || idProveedor <= 0) return false;
        await using var cn = new SqlConnection(_connectionString);
        await cn.OpenAsync(cancellationToken);
        var s = await EsquemaAsync(cn, cancellationToken);
        var valores = ValoresCompletos(s, formulario);
        ValidarLongitudes(s, valores);
        var nombres = valores.Keys.ToList();
        var set = Enumerable.Range(0, nombres.Count)
            .Select(i => SqlIdent(nombres[i]) + " = @P" + i).ToList();
        if (s.Columnas.TryGetValue("fecha_actualizacion", out var fecha) && EsFecha(fecha.Tipo))
            set.Add("[fecha_actualizacion] = SYSDATETIME()");
        await using var tx = cn.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            await VerificarDuplicadoAsync(cn, tx, s, idEmpresa, formulario.TipoDocumento.Trim(), formulario.NumeroDocumento.Trim(), idProveedor, cancellationToken);
            var sql = $"UPDATE [com].[proveedor] SET {string.Join(", ", set)} " +
                $"WHERE {SqlIdent(s.Id)} = @IdProveedor{s.FiltroEmpresa};";
            await using var cmd = new SqlCommand(sql, cn, tx);
            PonerValores(cmd, valores, nombres);
            cmd.Parameters.Add("@IdEmpresa", SqlDbType.Int).Value = idEmpresa;
            cmd.Parameters.Add("@IdProveedor", SqlDbType.Int).Value = idProveedor;
            bool actualizado = await cmd.ExecuteNonQueryAsync(cancellationToken) > 0;
            if (actualizado)
                await GuardarEventoAsync(cn, tx, idProveedor, idEmpresa, idUsuario, "EDITAR", cancellationToken);
            await tx.CommitAsync(cancellationToken);
            if (actualizado) _logger.LogInformation("[PROVEEDORES] Actualizado Id={Id} Empresa={Empresa}", idProveedor, idEmpresa);
            return actualizado;
        }
        catch { await tx.RollbackAsync(CancellationToken.None); throw; }
    }

    public async Task<bool> CambiarEstadoAsync(int idEmpresa, int idUsuario, int idProveedor, bool activo,
        CancellationToken cancellationToken = default)
    {
        if (idEmpresa <= 0 || idUsuario <= 0 || idProveedor <= 0) return false;
        await using var cn = new SqlConnection(_connectionString);
        await cn.OpenAsync(cancellationToken);
        var s = await EsquemaAsync(cn, cancellationToken);
        var sql = $"UPDATE [com].[proveedor] SET {SqlIdent(s.Estado)} = @Estado " +
            $"WHERE {SqlIdent(s.Id)} = @IdProveedor{s.FiltroEmpresa};";
        await using var tx = cn.BeginTransaction(IsolationLevel.ReadCommitted);
        try
        {
            await using var cmd = new SqlCommand(sql, cn, tx);
            cmd.Parameters.Add("@Estado", SqlDbType.Bit).Value = activo;
            cmd.Parameters.Add("@IdEmpresa", SqlDbType.Int).Value = idEmpresa;
            cmd.Parameters.Add("@IdProveedor", SqlDbType.Int).Value = idProveedor;
            bool changed = await cmd.ExecuteNonQueryAsync(cancellationToken) > 0;
            if (changed)
                await GuardarEventoAsync(cn, tx, idProveedor, idEmpresa, idUsuario,
                    activo ? "ACTIVAR" : "DESACTIVAR", cancellationToken);
            await tx.CommitAsync(cancellationToken);
            if (changed) _logger.LogInformation("[PROVEEDORES] Estado actualizado Id={Id} Activo={Activo} Empresa={Empresa}",
                idProveedor, activo, idEmpresa);
            return changed;
        }
        catch { await tx.RollbackAsync(CancellationToken.None); throw; }
    }

    private static async Task GuardarEventoAsync(SqlConnection cn, SqlTransaction tx,
        int idProveedor, int idEmpresa, int idUsuario, string accion, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO com.proveedor_auditoria_web
                (id_proveedor, id_empresa, id_usuario, accion)
            VALUES (@IdProveedor, @IdEmpresa, @IdUsuario, @Accion);
            """;
        await using var cmd = new SqlCommand(sql, cn, tx);
        cmd.Parameters.Add("@IdProveedor", SqlDbType.Int).Value = idProveedor;
        cmd.Parameters.Add("@IdEmpresa", SqlDbType.Int).Value = idEmpresa;
        cmd.Parameters.Add("@IdUsuario", SqlDbType.Int).Value = idUsuario;
        cmd.Parameters.Add("@Accion", SqlDbType.NVarChar, 30).Value = accion;
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task<IReadOnlyList<ProveedorEvento>> HistorialAsync(int idEmpresa,
        int idProveedor, CancellationToken cancellationToken = default)
    {
        if (idEmpresa <= 0 || idProveedor <= 0) return Array.Empty<ProveedorEvento>();
        // Verifica que el proveedor aún pertenece a la empresa consultante.
        if (await ObtenerAsync(idEmpresa, idProveedor, cancellationToken) is null)
            return Array.Empty<ProveedorEvento>();
        const string sql = """
            SELECT TOP (200) a.id_evento, a.id_proveedor, a.id_usuario,
                   ISNULL(u.nombre_usuario, N'Usuario desconocido') AS usuario,
                   a.accion, a.fecha_utc
            FROM com.proveedor_auditoria_web a
            LEFT JOIN seg.usuario u ON u.id_usuario = a.id_usuario
            WHERE a.id_empresa = @IdEmpresa AND a.id_proveedor = @IdProveedor
            ORDER BY a.id_evento DESC;
            """;
        await using var cn = new SqlConnection(_connectionString);
        await cn.OpenAsync(cancellationToken);
        await using var cmd = new SqlCommand(sql, cn);
        cmd.Parameters.Add("@IdEmpresa", SqlDbType.Int).Value = idEmpresa;
        cmd.Parameters.Add("@IdProveedor", SqlDbType.Int).Value = idProveedor;
        var items = new List<ProveedorEvento>();
        await using var rd = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await rd.ReadAsync(cancellationToken))
        {
            items.Add(new ProveedorEvento
            {
                IdEvento = Convert.ToInt64(rd["id_evento"]),
                IdProveedor = Convert.ToInt32(rd["id_proveedor"]),
                IdUsuario = Convert.ToInt32(rd["id_usuario"]),
                Usuario = Texto(rd, "usuario"),
                Accion = Texto(rd, "accion"),
                FechaUtc = Convert.ToDateTime(rd["fecha_utc"])
            });
        }
        return items;
    }

}
