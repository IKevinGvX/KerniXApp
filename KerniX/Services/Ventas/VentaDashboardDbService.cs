using System.Data;
using KerniX.Models.Ventas.Dashboard;
using KerniX.Models.Ventas.Venta;
using Microsoft.Data.SqlClient;

namespace KerniX.Services.Ventas;

public sealed class VentaDashboardDbService
    : IVentaDashboardDbService
{
    private readonly string _connectionString;
    private readonly ILogger<VentaDashboardDbService> _logger;

    public VentaDashboardDbService(
        IConfiguration configuration,
        ILogger<VentaDashboardDbService> logger)
    {
        _connectionString =
            configuration.GetConnectionString(
                "KerniXDb")
            ?? throw new InvalidOperationException(
                "No se encontró la conexión KerniXDb.");

        _logger = logger;
    }

    // =========================================================
    // DASHBOARD
    // =========================================================

    public async Task<VentaDashboardResponse> ObtenerAsync(
        int idEmpresa,
        int? idSucursal = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null)
    {
        if (idEmpresa <= 0)
        {
            throw new ArgumentException(
                "La empresa no es válida.",
                nameof(idEmpresa));
        }

        DateTime hasta =
            (fechaHasta ?? DateTime.Today)
            .Date
            .AddDays(1);

        DateTime desde =
            (fechaDesde ??
             DateTime.Today.AddDays(-29))
            .Date;

        if (desde >= hasta)
        {
            throw new ArgumentException(
                "El rango de fechas no es válido.");
        }

        var dashboard =
            new VentaDashboardResponse();

        await using var connection =
            new SqlConnection(
                _connectionString);

        await connection.OpenAsync();

        await using var command =
            new SqlCommand(
                ObtenerSqlDashboard(),
                connection);

        command.CommandType =
            CommandType.Text;

        command.CommandTimeout =
            60;

        command.Parameters.Add(
            new SqlParameter(
                "@id_empresa",
                SqlDbType.Int)
            {
                Value = idEmpresa
            });

        command.Parameters.Add(
            new SqlParameter(
                "@id_sucursal",
                SqlDbType.Int)
            {
                Value =
                    idSucursal.HasValue &&
                    idSucursal.Value > 0
                        ? idSucursal.Value
                        : DBNull.Value
            });

        command.Parameters.Add(
            new SqlParameter(
                "@fecha_desde",
                SqlDbType.DateTime2)
            {
                Value = desde
            });

        command.Parameters.Add(
            new SqlParameter(
                "@fecha_hasta",
                SqlDbType.DateTime2)
            {
                Value = hasta
            });

        try
        {
            await using var reader =
                await command.ExecuteReaderAsync();

            // =================================================
            // 1. KPI
            // =================================================

            if (await reader.ReadAsync())
            {
                dashboard.Kpis =
                    new VentaKpiResponse
                    {
                        VentasMes =
                            GetDecimal(
                                reader,
                                "ventas_mes"),

                        VentasHoy =
                            GetDecimal(
                                reader,
                                "ventas_hoy"),

                        TicketPromedio =
                            GetDecimal(
                                reader,
                                "ticket_promedio"),

                        TotalVentasMes =
                            GetInt32(
                                reader,
                                "total_ventas_mes"),

                        TotalVentasHoy =
                            GetInt32(
                                reader,
                                "total_ventas_hoy"),

                        ClientesActivos =
                            GetInt32(
                                reader,
                                "clientes_activos"),

                        VentasAnuales =
                            GetDecimal(
                                reader,
                                "ventas_anuales"),

                        VariacionMes =
                            GetDecimal(
                                reader,
                                "variacion_mes"),

                        VariacionHoy =
                            GetDecimal(
                                reader,
                                "variacion_hoy"),

                        VariacionTicket =
                            GetDecimal(
                                reader,
                                "variacion_ticket")
                    };
            }

            // =================================================
            // 2. VENTAS POR PERIODO
            // =================================================

            if (await reader.NextResultAsync())
            {
                dashboard.VentasPeriodo =
                    await LeerVentasPeriodoAsync(
                        reader);
            }

            // =================================================
            // 3. CATEGORIAS
            // =================================================

            if (await reader.NextResultAsync())
            {
                dashboard.Categorias =
                    await LeerCategoriasAsync(
                        reader);
            }

            // =================================================
            // 4. TOP PRODUCTOS
            // =================================================

            if (await reader.NextResultAsync())
            {
                dashboard.TopProductos =
                    await LeerTopProductosAsync(
                        reader);
            }

            // =================================================
            // 5. ULTIMAS VENTAS
            // =================================================

            if (await reader.NextResultAsync())
            {
                dashboard.UltimasVentas =
                    await LeerUltimasVentasAsync(
                        reader);
            }

            // =================================================
            // 6. METODOS DE PAGO
            // =================================================

            if (await reader.NextResultAsync())
            {
                dashboard.MetodosPago =
                    await LeerMetodosPagoAsync(
                        reader);
            }

            // =================================================
            // 7. ACTIVIDAD RECIENTE
            // =================================================

            if (await reader.NextResultAsync())
            {
                dashboard.ActividadReciente =
                    await LeerActividadAsync(
                        reader);
            }

            // =================================================
            // IA
            // =================================================

            dashboard.InsightsIa =
                CrearInsights(
                    dashboard);

            return dashboard;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error cargando dashboard de ventas. " +
                "Empresa={IdEmpresa} | Sucursal={IdSucursal}",
                idEmpresa,
                idSucursal);

            throw;
        }
    }

    // =========================================================
    // SQL DASHBOARD
    // =========================================================

    private static string ObtenerSqlDashboard()
    {
        return """
        ---------------------------------------------------------
        -- 1. KPI
        ---------------------------------------------------------

        DECLARE @inicio_hoy DATE =
            CONVERT(DATE, SYSDATETIME());

        DECLARE @inicio_mes DATE =
            DATEFROMPARTS(
                YEAR(SYSDATETIME()),
                MONTH(SYSDATETIME()),
                1
            );

        DECLARE @inicio_mes_anterior DATE =
            DATEADD(
                MONTH,
                -1,
                @inicio_mes
            );

        DECLARE @inicio_anio DATE =
            DATEFROMPARTS(
                YEAR(SYSDATETIME()),
                1,
                1
            );

        DECLARE @ventas_mes DECIMAL(18,4) = 0;
        DECLARE @ventas_mes_anterior DECIMAL(18,4) = 0;

        DECLARE @ventas_hoy DECIMAL(18,4) = 0;
        DECLARE @ventas_ayer DECIMAL(18,4) = 0;

        DECLARE @ticket_actual DECIMAL(18,4) = 0;
        DECLARE @ticket_anterior DECIMAL(18,4) = 0;


        SELECT
            @ventas_mes =
                ISNULL(SUM(v.total), 0),

            @ticket_actual =
                ISNULL(AVG(v.total), 0)

        FROM ven.venta v

        WHERE
            v.id_empresa = @id_empresa

            AND
            (
                @id_sucursal IS NULL
                OR v.id_sucursal = @id_sucursal
            )

            AND v.estado <> 'anulada'

            AND v.fecha_venta >= @inicio_mes

            AND v.fecha_venta <
                DATEADD(
                    MONTH,
                    1,
                    @inicio_mes
                );


        SELECT
            @ventas_mes_anterior =
                ISNULL(SUM(v.total), 0),

            @ticket_anterior =
                ISNULL(AVG(v.total), 0)

        FROM ven.venta v

        WHERE
            v.id_empresa = @id_empresa

            AND
            (
                @id_sucursal IS NULL
                OR v.id_sucursal = @id_sucursal
            )

            AND v.estado <> 'anulada'

            AND v.fecha_venta >= @inicio_mes_anterior

            AND v.fecha_venta < @inicio_mes;


        SELECT
            @ventas_hoy =
                ISNULL(SUM(v.total), 0)

        FROM ven.venta v

        WHERE
            v.id_empresa = @id_empresa

            AND
            (
                @id_sucursal IS NULL
                OR v.id_sucursal = @id_sucursal
            )

            AND v.estado <> 'anulada'

            AND v.fecha_venta >= @inicio_hoy

            AND v.fecha_venta <
                DATEADD(
                    DAY,
                    1,
                    @inicio_hoy
                );


        SELECT
            @ventas_ayer =
                ISNULL(SUM(v.total), 0)

        FROM ven.venta v

        WHERE
            v.id_empresa = @id_empresa

            AND
            (
                @id_sucursal IS NULL
                OR v.id_sucursal = @id_sucursal
            )

            AND v.estado <> 'anulada'

            AND v.fecha_venta >=
                DATEADD(
                    DAY,
                    -1,
                    @inicio_hoy
                )

            AND v.fecha_venta < @inicio_hoy;


        SELECT

            @ventas_mes
                AS ventas_mes,

            @ventas_hoy
                AS ventas_hoy,

            @ticket_actual
                AS ticket_promedio,

            (
                SELECT COUNT(*)

                FROM ven.venta v

                WHERE
                    v.id_empresa = @id_empresa

                    AND
                    (
                        @id_sucursal IS NULL
                        OR v.id_sucursal = @id_sucursal
                    )

                    AND v.estado <> 'anulada'

                    AND v.fecha_venta >= @inicio_mes

                    AND v.fecha_venta <
                        DATEADD(
                            MONTH,
                            1,
                            @inicio_mes
                        )
            )
            AS total_ventas_mes,

            (
                SELECT COUNT(*)

                FROM ven.venta v

                WHERE
                    v.id_empresa = @id_empresa

                    AND
                    (
                        @id_sucursal IS NULL
                        OR v.id_sucursal = @id_sucursal
                    )

                    AND v.estado <> 'anulada'

                    AND v.fecha_venta >= @inicio_hoy

                    AND v.fecha_venta <
                        DATEADD(
                            DAY,
                            1,
                            @inicio_hoy
                        )
            )
            AS total_ventas_hoy,

            (
                SELECT COUNT(
                    DISTINCT v.id_cliente
                )

                FROM ven.venta v

                WHERE
                    v.id_empresa = @id_empresa

                    AND
                    (
                        @id_sucursal IS NULL
                        OR v.id_sucursal = @id_sucursal
                    )

                    AND v.estado <> 'anulada'

                    AND v.fecha_venta >=
                        DATEADD(
                            DAY,
                            -90,
                            SYSDATETIME()
                        )
            )
            AS clientes_activos,

            (
                SELECT
                    ISNULL(
                        SUM(v.total),
                        0
                    )

                FROM ven.venta v

                WHERE
                    v.id_empresa = @id_empresa

                    AND
                    (
                        @id_sucursal IS NULL
                        OR v.id_sucursal = @id_sucursal
                    )

                    AND v.estado <> 'anulada'

                    AND v.fecha_venta >= @inicio_anio
            )
            AS ventas_anuales,

            CASE
                WHEN @ventas_mes_anterior = 0
                    THEN
                        CASE
                            WHEN @ventas_mes > 0
                                THEN 100
                            ELSE 0
                        END

                ELSE
                    (
                        (
                            @ventas_mes -
                            @ventas_mes_anterior
                        )
                        /
                        @ventas_mes_anterior
                    )
                    * 100
            END
            AS variacion_mes,

            CASE
                WHEN @ventas_ayer = 0
                    THEN
                        CASE
                            WHEN @ventas_hoy > 0
                                THEN 100
                            ELSE 0
                        END

                ELSE
                    (
                        (
                            @ventas_hoy -
                            @ventas_ayer
                        )
                        /
                        @ventas_ayer
                    )
                    * 100
            END
            AS variacion_hoy,

            CASE
                WHEN @ticket_anterior = 0
                    THEN
                        CASE
                            WHEN @ticket_actual > 0
                                THEN 100
                            ELSE 0
                        END

                ELSE
                    (
                        (
                            @ticket_actual -
                            @ticket_anterior
                        )
                        /
                        @ticket_anterior
                    )
                    * 100
            END
            AS variacion_ticket;


        ---------------------------------------------------------
        -- 2. VENTAS POR DIA
        ---------------------------------------------------------

        SELECT
            CONVERT(
                DATE,
                v.fecha_venta
            ) AS fecha,

            CONVERT(
                NVARCHAR(10),
                CONVERT(
                    DATE,
                    v.fecha_venta
                ),
                103
            ) AS etiqueta,

            SUM(v.total)
                AS total,

            COUNT(*)
                AS cantidad_ventas

        FROM ven.venta v

        WHERE
            v.id_empresa = @id_empresa

            AND
            (
                @id_sucursal IS NULL
                OR v.id_sucursal = @id_sucursal
            )

            AND v.estado <> 'anulada'

            AND v.fecha_venta >= @fecha_desde

            AND v.fecha_venta < @fecha_hasta

        GROUP BY
            CONVERT(
                DATE,
                v.fecha_venta
            )

        ORDER BY
            fecha;


        ---------------------------------------------------------
        -- 3. VENTAS POR CATEGORIA
        ---------------------------------------------------------

        ;WITH categoria AS
        (
            SELECT
                p.id_categoria_producto,

                SUM(dv.cantidad)
                    AS cantidad_vendida,

                SUM(dv.total)
                    AS total_venta

            FROM ven.detalle_venta dv

            INNER JOIN ven.venta v
                ON v.id_venta =
                   dv.id_venta

            INNER JOIN inv.producto p
                ON p.id_producto =
                   dv.id_producto

            WHERE
                v.id_empresa = @id_empresa

                AND
                (
                    @id_sucursal IS NULL
                    OR v.id_sucursal = @id_sucursal
                )

                AND v.estado <> 'anulada'

                AND v.fecha_venta >= @fecha_desde

                AND v.fecha_venta < @fecha_hasta

            GROUP BY
                p.id_categoria_producto
        ),
        total_general AS
        (
            SELECT
                SUM(total_venta) AS total

            FROM categoria
        )

        SELECT
            c.id_categoria_producto
                AS id_categoria,

            ISNULL(
                cp.nombre,
                N'Sin categoría'
            ) AS categoria,

            c.total_venta,

            CASE
                WHEN ISNULL(
                    tg.total,
                    0
                ) = 0
                    THEN 0

                ELSE
                    (
                        c.total_venta /
                        tg.total
                    ) * 100
            END
            AS porcentaje,

            CONVERT(
                INT,
                c.cantidad_vendida
            ) AS cantidad_vendida

        FROM categoria c

        LEFT JOIN inv.categoria_producto cp
            ON cp.id_categoria_producto =
               c.id_categoria_producto

        CROSS JOIN total_general tg

        ORDER BY
            c.total_venta DESC;


        ---------------------------------------------------------
        -- 4. TOP PRODUCTOS
        ---------------------------------------------------------

        SELECT TOP (10)

            p.id_producto,
            p.codigo,
            p.nombre AS producto,

            SUM(dv.cantidad)
                AS cantidad_vendida,

            SUM(dv.total)
                AS total_venta,

            ROW_NUMBER()
                OVER
                (
                    ORDER BY
                        SUM(dv.total) DESC
                )
                AS posicion

        FROM ven.detalle_venta dv

        INNER JOIN ven.venta v
            ON v.id_venta =
               dv.id_venta

        INNER JOIN inv.producto p
            ON p.id_producto =
               dv.id_producto

        WHERE
            v.id_empresa = @id_empresa

            AND
            (
                @id_sucursal IS NULL
                OR v.id_sucursal = @id_sucursal
            )

            AND v.estado <> 'anulada'

            AND v.fecha_venta >= @fecha_desde

            AND v.fecha_venta < @fecha_hasta

        GROUP BY
            p.id_producto,
            p.codigo,
            p.nombre

        ORDER BY
            total_venta DESC;


        ---------------------------------------------------------
        -- 5. ULTIMAS VENTAS
        ---------------------------------------------------------

        SELECT TOP (10)

            v.id_venta,
            v.numero_venta,
            v.fecha_venta,

            COALESCE(
                NULLIF(
                    c.razon_social,
                    ''
                ),
                CONCAT(
                    ISNULL(c.nombres, ''),
                    ' ',
                    ISNULL(c.apellidos, '')
                )
            ) AS cliente,

            CONCAT(
                e.nombres,
                ' ',
                e.apellido_paterno
            ) AS vendedor,

            v.total,
            v.estado

        FROM ven.venta v

        INNER JOIN ven.cliente c
            ON c.id_cliente =
               v.id_cliente

        INNER JOIN org.empleado e
            ON e.id_empleado =
               v.id_empleado

        WHERE
            v.id_empresa = @id_empresa

            AND
            (
                @id_sucursal IS NULL
                OR v.id_sucursal = @id_sucursal
            )

        ORDER BY
            v.fecha_venta DESC,
            v.id_venta DESC;


        ---------------------------------------------------------
        -- 6. METODOS DE PAGO
        ---------------------------------------------------------

        SELECT
            p.metodo_pago
                AS etiqueta,

            SUM(p.monto)
                AS total,

            COUNT(*)
                AS cantidad_ventas

        FROM ven.pago p

        INNER JOIN ven.venta v
            ON v.id_venta =
               p.id_venta

        WHERE
            v.id_empresa = @id_empresa

            AND
            (
                @id_sucursal IS NULL
                OR v.id_sucursal = @id_sucursal
            )

            AND v.estado <> 'anulada'

            AND p.estado <> 'anulado'

            AND v.fecha_venta >= @fecha_desde

            AND v.fecha_venta < @fecha_hasta

        GROUP BY
            p.metodo_pago

        ORDER BY
            total DESC;


        ---------------------------------------------------------
        -- 7. ACTIVIDAD RECIENTE
        ---------------------------------------------------------

        SELECT TOP (15)

            v.id_venta,

            N'VENTA'
                AS tipo,

            CONCAT(
                N'Venta ',
                v.numero_venta
            ) AS titulo,

            CONCAT(
                N'Cliente: ',
                COALESCE(
                    NULLIF(
                        c.razon_social,
                        ''
                    ),
                    CONCAT(
                        ISNULL(c.nombres, ''),
                        ' ',
                        ISNULL(c.apellidos, '')
                    )
                )
            ) AS descripcion,

            v.fecha_venta
                AS fecha,

            v.total
                AS monto

        FROM ven.venta v

        INNER JOIN ven.cliente c
            ON c.id_cliente =
               v.id_cliente

        WHERE
            v.id_empresa = @id_empresa

            AND
            (
                @id_sucursal IS NULL
                OR v.id_sucursal = @id_sucursal
            )

        ORDER BY
            v.fecha_venta DESC,
            v.id_venta DESC;
        """;
    }

    // =========================================================
    // VENTAS PERIODO
    // =========================================================

    private static async Task<
        IReadOnlyList<VentaGraficoResponse>>
        LeerVentasPeriodoAsync(
            SqlDataReader reader)
    {
        var lista =
            new List<VentaGraficoResponse>();

        while (await reader.ReadAsync())
        {
            lista.Add(
                new VentaGraficoResponse
                {
                    Etiqueta =
                        GetString(
                            reader,
                            "etiqueta"),

                    Fecha =
                        GetDateTimeNullable(
                            reader,
                            "fecha"),

                    Total =
                        GetDecimal(
                            reader,
                            "total"),

                    CantidadVentas =
                        GetInt32(
                            reader,
                            "cantidad_ventas")
                });
        }

        return lista;
    }

    // =========================================================
    // CATEGORIAS
    // =========================================================

    private static async Task<
        IReadOnlyList<VentaCategoriaResponse>>
        LeerCategoriasAsync(
            SqlDataReader reader)
    {
        var lista =
            new List<VentaCategoriaResponse>();

        while (await reader.ReadAsync())
        {
            lista.Add(
                new VentaCategoriaResponse
                {
                    IdCategoria =
                        GetInt32(
                            reader,
                            "id_categoria"),

                    Categoria =
                        GetString(
                            reader,
                            "categoria"),

                    TotalVenta =
                        GetDecimal(
                            reader,
                            "total_venta"),

                    Porcentaje =
                        GetDecimal(
                            reader,
                            "porcentaje"),

                    CantidadVendida =
                        GetInt32(
                            reader,
                            "cantidad_vendida")
                });
        }

        return lista;
    }

    // =========================================================
    // TOP PRODUCTOS
    // =========================================================

    private static async Task<
        IReadOnlyList<VentaTopProductoResponse>>
        LeerTopProductosAsync(
            SqlDataReader reader)
    {
        var lista =
            new List<VentaTopProductoResponse>();

        while (await reader.ReadAsync())
        {
            lista.Add(
                new VentaTopProductoResponse
                {
                    IdProducto =
                        GetInt32(
                            reader,
                            "id_producto"),

                    Codigo =
                        GetString(
                            reader,
                            "codigo"),

                    Producto =
                        GetString(
                            reader,
                            "producto"),

                    CantidadVendida =
                        GetDecimal(
                            reader,
                            "cantidad_vendida"),

                    TotalVenta =
                        GetDecimal(
                            reader,
                            "total_venta"),

                    Posicion =
                        GetInt32(
                            reader,
                            "posicion")
                });
        }

        return lista;
    }

    // =========================================================
    // ULTIMAS VENTAS
    // =========================================================

    private static async Task<
        IReadOnlyList<VentaListadoResponse>>
        LeerUltimasVentasAsync(
            SqlDataReader reader)
    {
        var lista =
            new List<VentaListadoResponse>();

        while (await reader.ReadAsync())
        {
            lista.Add(
                new VentaListadoResponse
                {
                    IdVenta =
                        GetInt64(
                            reader,
                            "id_venta"),

                    NumeroVenta =
                        GetString(
                            reader,
                            "numero_venta"),

                    FechaVenta =
                        GetDateTime(
                            reader,
                            "fecha_venta"),

                    Cliente =
                        GetString(
                            reader,
                            "cliente"),

                    Vendedor =
                        GetString(
                            reader,
                            "vendedor"),

                    Total =
                        GetDecimal(
                            reader,
                            "total"),

                    Estado =
                        GetString(
                            reader,
                            "estado")
                });
        }

        return lista;
    }

    // =========================================================
    // METODOS PAGO
    // =========================================================

    private static async Task<
        IReadOnlyList<VentaGraficoResponse>>
        LeerMetodosPagoAsync(
            SqlDataReader reader)
    {
        var lista =
            new List<VentaGraficoResponse>();

        while (await reader.ReadAsync())
        {
            lista.Add(
                new VentaGraficoResponse
                {
                    Etiqueta =
                        GetString(
                            reader,
                            "etiqueta"),

                    Total =
                        GetDecimal(
                            reader,
                            "total"),

                    CantidadVentas =
                        GetInt32(
                            reader,
                            "cantidad_ventas")
                });
        }

        return lista;
    }

    // =========================================================
    // ACTIVIDAD
    // =========================================================

    private static async Task<
        IReadOnlyList<VentaActividadResponse>>
        LeerActividadAsync(
            SqlDataReader reader)
    {
        var lista =
            new List<VentaActividadResponse>();

        while (await reader.ReadAsync())
        {
            lista.Add(
                new VentaActividadResponse
                {
                    IdVenta =
                        GetInt64(
                            reader,
                            "id_venta"),

                    Tipo =
                        GetString(
                            reader,
                            "tipo"),

                    Titulo =
                        GetString(
                            reader,
                            "titulo"),

                    Descripcion =
                        GetStringNullable(
                            reader,
                            "descripcion"),

                    Fecha =
                        GetDateTime(
                            reader,
                            "fecha"),

                    Monto =
                        GetDecimal(
                            reader,
                            "monto")
                });
        }

        return lista;
    }

    // =========================================================
    // IA
    // =========================================================

    private static IReadOnlyList<VentaIaInsightResponse>
        CrearInsights(
            VentaDashboardResponse dashboard)
    {
        var insights =
            new List<VentaIaInsightResponse>();

        if (dashboard.Kpis.VariacionMes > 0)
        {
            insights.Add(
                new VentaIaInsightResponse
                {
                    Tipo = "TENDENCIA",
                    Nivel = "POSITIVO",
                    Titulo =
                        "Crecimiento de ventas",

                    Mensaje =
                        $"Las ventas del mes aumentaron " +
                        $"{dashboard.Kpis.VariacionMes:N1}% " +
                        "respecto al mes anterior.",

                    Variacion =
                        dashboard.Kpis.VariacionMes,

                    Accion =
                        "Revisar productos con mayor crecimiento."
                });
        }
        else if (dashboard.Kpis.VariacionMes < 0)
        {
            insights.Add(
                new VentaIaInsightResponse
                {
                    Tipo = "TENDENCIA",
                    Nivel = "ALERTA",
                    Titulo =
                        "Reducción de ventas",

                    Mensaje =
                        $"Las ventas del mes disminuyeron " +
                        $"{Math.Abs(dashboard.Kpis.VariacionMes):N1}% " +
                        "respecto al mes anterior.",

                    Variacion =
                        dashboard.Kpis.VariacionMes,

                    Accion =
                        "Analizar categorías y productos con menor rotación."
                });
        }

        var producto =
            dashboard.TopProductos
                .FirstOrDefault();

        if (producto is not null)
        {
            insights.Add(
                new VentaIaInsightResponse
                {
                    Tipo = "PRODUCTO",
                    Nivel = "INFO",
                    Titulo =
                        "Producto destacado",

                    Mensaje =
                        $"{producto.Producto} lidera las ventas " +
                        $"con {producto.CantidadVendida:N2} unidades.",

                    Valor =
                        producto.TotalVenta,

                    Accion =
                        "Revisar disponibilidad de inventario."
                });
        }

        if (dashboard.Kpis.VentasHoy > 0)
        {
            insights.Add(
                new VentaIaInsightResponse
                {
                    Tipo = "VENTAS",
                    Nivel = "INFO",
                    Titulo =
                        "Actividad del día",

                    Mensaje =
                        $"Hoy se han registrado " +
                        $"{dashboard.Kpis.TotalVentasHoy} ventas " +
                        $"por S/ {dashboard.Kpis.VentasHoy:N2}.",

                    Valor =
                        dashboard.Kpis.VentasHoy
                });
        }

        return insights;
    }

    // =========================================================
    // HELPERS
    // =========================================================

    private static int GetOrdinal(
        SqlDataReader reader,
        string column)
    {
        return reader.GetOrdinal(
            column);
    }

    private static string GetString(
        SqlDataReader reader,
        string column)
    {
        int ordinal =
            GetOrdinal(
                reader,
                column);

        return reader.IsDBNull(
            ordinal)
                ? string.Empty
                : Convert.ToString(
                    reader.GetValue(
                        ordinal))
                  ?? string.Empty;
    }

    private static string? GetStringNullable(
        SqlDataReader reader,
        string column)
    {
        int ordinal =
            GetOrdinal(
                reader,
                column);

        return reader.IsDBNull(
            ordinal)
                ? null
                : Convert.ToString(
                    reader.GetValue(
                        ordinal));
    }

    private static int GetInt32(
        SqlDataReader reader,
        string column)
    {
        int ordinal =
            GetOrdinal(
                reader,
                column);

        return reader.IsDBNull(
            ordinal)
                ? 0
                : Convert.ToInt32(
                    reader.GetValue(
                        ordinal));
    }

    private static long GetInt64(
        SqlDataReader reader,
        string column)
    {
        int ordinal =
            GetOrdinal(
                reader,
                column);

        return reader.IsDBNull(
            ordinal)
                ? 0L
                : Convert.ToInt64(
                    reader.GetValue(
                        ordinal));
    }

    private static decimal GetDecimal(
        SqlDataReader reader,
        string column)
    {
        int ordinal =
            GetOrdinal(
                reader,
                column);

        return reader.IsDBNull(
            ordinal)
                ? 0m
                : Convert.ToDecimal(
                    reader.GetValue(
                        ordinal));
    }

    private static DateTime GetDateTime(
        SqlDataReader reader,
        string column)
    {
        int ordinal =
            GetOrdinal(
                reader,
                column);

        return reader.IsDBNull(
            ordinal)
                ? DateTime.MinValue
                : Convert.ToDateTime(
                    reader.GetValue(
                        ordinal));
    }

    private static DateTime? GetDateTimeNullable(
        SqlDataReader reader,
        string column)
    {
        int ordinal =
            GetOrdinal(
                reader,
                column);

        return reader.IsDBNull(
            ordinal)
                ? null
                : Convert.ToDateTime(
                    reader.GetValue(
                        ordinal));
    }
}