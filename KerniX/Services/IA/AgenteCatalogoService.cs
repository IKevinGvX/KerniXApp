using System.Data;
using System.Globalization;
using KerniX.Models.IA;
using Microsoft.Data.SqlClient;

namespace KerniX.Services.IA;

/// <summary>
/// Catálogo de productos para el agente: lectura por empresa, sin escrituras.
/// Solo selecciona columnas verificadas mediante sys.columns.
/// </summary>
public sealed class AgenteCatalogoService : IAgenteCatalogoService
{
    private readonly string _connectionString;
    private readonly ILogger<AgenteCatalogoService> _logger;

    public AgenteCatalogoService(
        IConfiguration configuration,
        ILogger<AgenteCatalogoService> logger)
    {
        _connectionString = configuration.GetConnectionString("KerniXDb")
            ?? throw new InvalidOperationException("Falta ConnectionStrings:KerniXDb.");
        _logger = logger;
    }

    public async Task<IReadOnlyList<AgenteCatalogoProducto>> BuscarAsync(
        int idEmpresa,
        string consulta,
        CancellationToken cancellationToken = default)
    {
        if (idEmpresa <= 0)
            throw new ArgumentOutOfRangeException(nameof(idEmpresa), "La empresa debe ser válida.");

        consulta = (consulta ?? "").Trim();
        if (consulta.Length < 2 || consulta.Length > 120)
            throw new ArgumentException("Escribe entre 2 y 120 caracteres.", nameof(consulta));

        return await ConsultarAsync(idEmpresa, consulta, cancellationToken);
    }

    public Task<IReadOnlyList<AgenteCatalogoProducto>> ListarAsync(
        int idEmpresa,
        CancellationToken cancellationToken = default)
    {
        if (idEmpresa <= 0)
            throw new ArgumentOutOfRangeException(nameof(idEmpresa));

        return ConsultarAsync(idEmpresa, null, cancellationToken);
    }

    public Task<IReadOnlyList<AgenteCatalogoProducto>> ObtenerPorIdsAsync(
        int idEmpresa,
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken = default)
    {
        if (idEmpresa <= 0) throw new ArgumentOutOfRangeException(nameof(idEmpresa));
        if (ids is null || ids.Count < 1 || ids.Count > 18 || ids.Any(id => id <= 0) || ids.Distinct().Count() != ids.Count)
            throw new ArgumentException("Selecciona de 1 a 18 productos con identificadores válidos.", nameof(ids));

        return ConsultarAsync(idEmpresa, null, cancellationToken, ids);
    }

    private async Task<IReadOnlyList<AgenteCatalogoProducto>> ConsultarAsync(
        int idEmpresa,
        string? consulta,
        CancellationToken cancellationToken,
        IReadOnlyCollection<int>? ids = null)
    {
        await using var db = new SqlConnection(_connectionString);
        await db.OpenAsync(cancellationToken);

        var producto = await ObtenerColumnasAsync(db, "inv.producto", cancellationToken);
        if (!producto.IsSupersetOf(new[] { "id_producto", "id_empresa", "nombre" }))
            throw new InvalidOperationException(
                "El catálogo inv.producto no contiene id_producto, id_empresa y nombre.");

        var stock = await ObtenerColumnasAsync(db, "inv.stock", cancellationToken);
        var almacen = await ObtenerColumnasAsync(db, "inv.almacen", cancellationToken);
        var sucursal = await ObtenerColumnasAsync(db, "org.sucursal", cancellationToken);

        bool tieneStock =
            stock.IsSupersetOf(new[] { "id_producto", "id_almacen", "cantidad_actual", "cantidad_reservada" }) &&
            almacen.IsSupersetOf(new[] { "id_almacen", "id_sucursal" }) &&
            sucursal.IsSupersetOf(new[] { "id_sucursal", "id_empresa" });

        // Los identificadores son constantes controladas por el servidor.
        string codigo = producto.Contains("codigo")
            ? "COALESCE(CONVERT(nvarchar(100), p.[codigo]), N'')"
            : "CONVERT(nvarchar(100), N'')";
        string descripcion = producto.Contains("descripcion")
            ? "COALESCE(CONVERT(nvarchar(1000), p.[descripcion]), N'')"
            : "CONVERT(nvarchar(1000), N'')";
        string precio = producto.Contains("precio_venta")
            ? "TRY_CONVERT(decimal(18,4),p.[precio_venta])"
            : "CAST(NULL AS decimal(18,4))";
        string selectStock = tieneStock
            ? "s.[Disponible]"
            : "CAST(NULL AS decimal(18,4))";
        string joinStock = tieneStock ? """
            LEFT JOIN
            (
                SELECT st.[id_producto],
                       SUM(TRY_CONVERT(decimal(18,4), st.[cantidad_actual]) -
                           COALESCE(TRY_CONVERT(decimal(18,4), st.[cantidad_reservada]), 0)) AS [Disponible]
                FROM inv.stock AS st
                INNER JOIN inv.almacen AS a ON a.[id_almacen] = st.[id_almacen]
                INNER JOIN org.sucursal AS su ON su.[id_sucursal] = a.[id_sucursal]
                WHERE su.[id_empresa] = @id_empresa
                GROUP BY st.[id_producto]
            ) AS s ON s.[id_producto] = p.[id_producto]
            """ : "";
        string filtroEstado = producto.Contains("estado")
            ? "AND TRY_CONVERT(bit, p.[estado]) = 1"
            : "";
        string filtroBusqueda = consulta is null ? "" : producto.Contains("codigo")
            ? "AND (p.[nombre] LIKE @buscar ESCAPE N'~' OR p.[codigo] LIKE @buscar ESCAPE N'~')"
            : "AND p.[nombre] LIKE @buscar ESCAPE N'~'";

        // Solo IDs enteros validados por servidor; parámetros SQL para cada valor.
        var idsOrdenados = ids?.ToArray();
        string filtroIds = idsOrdenados is null ? "" :
            "AND p.[id_producto] IN (" + string.Join(",", idsOrdenados.Select((_, i) => $"@producto{i}")) + ")";

        string sql = $"""
            SELECT TOP (18)
                   p.[id_producto] AS [IdProducto],
                   {codigo} AS [Codigo],
                   COALESCE(CONVERT(nvarchar(300), p.[nombre]), N'') AS [Nombre],
                   {descripcion} AS [Descripcion],
                   {precio} AS [PrecioVenta],
                   {selectStock} AS [StockDisponible]
            FROM inv.producto AS p
            {joinStock}
            WHERE p.[id_empresa] = @id_empresa
                  {filtroEstado}
                  {filtroBusqueda}
                  {filtroIds}
            ORDER BY p.[nombre], p.[id_producto];
            """;

        // El usuario no puede introducir patrones LIKE, solo texto literal.
        await using var command = new SqlCommand(sql, db) { CommandTimeout = 20 };
        command.Parameters.Add("@id_empresa", SqlDbType.Int).Value = idEmpresa;
        if (consulta is not null)
        {
            // Trata la búsqueda como TEXTO, jamás como fragmento SQL ni como patrón LIKE.
            string buscar = consulta.Replace("~", "~~", StringComparison.Ordinal)
                                    .Replace("%", "~%", StringComparison.Ordinal)
                                    .Replace("_", "~_", StringComparison.Ordinal)
                                    .Replace("[", "~[", StringComparison.Ordinal);
            command.Parameters.Add("@buscar", SqlDbType.NVarChar, 250).Value = $"%{buscar}%";
        }

        if (idsOrdenados is not null)
        {
            for (int i = 0; i < idsOrdenados.Length; i++)
                command.Parameters.Add($"@producto{i}", SqlDbType.Int).Value = idsOrdenados[i];
        }

        var encontrados = new List<AgenteCatalogoProducto>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            encontrados.Add(new AgenteCatalogoProducto
            {
                IdProducto = Convert.ToInt32(reader[0], CultureInfo.InvariantCulture),
                Codigo = Texto(reader, 1),
                Nombre = Texto(reader, 2),
                Descripcion = Texto(reader, 3),
                PrecioVenta = DecimalNullable(reader, 4),
                StockDisponible = DecimalNullable(reader, 5)
            });
        }

        _logger.LogInformation(
            "[KERNIX AGENTE] Catálogo Empresa={Empresa} Coincidencias={Coincidencias}",
            idEmpresa, encontrados.Count);
        return encontrados;
    }

    private static string Texto(SqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? "" : Convert.ToString(reader.GetValue(ordinal), CultureInfo.InvariantCulture) ?? "";

    private static decimal? DecimalNullable(SqlDataReader reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : Convert.ToDecimal(reader.GetValue(ordinal), CultureInfo.InvariantCulture);

    private static async Task<HashSet<string>> ObtenerColumnasAsync(
        SqlConnection db,
        string tabla,
        CancellationToken cancellationToken)
    {
        const string sql = "SELECT [name] FROM sys.columns WHERE object_id = OBJECT_ID(@tabla, N'U')";
        await using var command = new SqlCommand(sql, db);
        command.Parameters.Add("@tabla", SqlDbType.NVarChar, 128).Value = tabla;
        var columnas = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            columnas.Add(reader.GetString(0));
        return columnas;
    }
}

