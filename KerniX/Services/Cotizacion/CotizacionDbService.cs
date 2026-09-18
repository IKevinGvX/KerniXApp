using System.Data;
using KerniX.Models.Cotizacion;
using Microsoft.Data.SqlClient;

namespace KerniX.Services.Cotizacion;

public sealed class CotizacionDbService
    : ICotizacionDbService
{
    private readonly string _connectionString;

    public CotizacionDbService(
        IConfiguration configuration)
    {
        _connectionString =
            configuration.GetConnectionString("KerniXDb")
            ?? throw new InvalidOperationException(
                "No existe ConnectionStrings:KerniXDb."
            );
    }
    public async Task<VentaVinculadaCotizacion?>
    ObtenerVentaVinculadaAsync(
        long idCotizacion,
        int idEmpresa,
        int idSucursal)
    {
        if (idCotizacion <= 0 ||
            idEmpresa <= 0 ||
            idSucursal <= 0)
        {
            return null;
        }

        const string sql = """
        SELECT
            v.id_venta,
            v.numero_venta,
            v.estado,
            v.total
        FROM ven.venta v
        INNER JOIN ven.cotizacion c
            ON c.id_cotizacion = v.id_cotizacion
        WHERE v.id_cotizacion = @id_cotizacion
          AND v.id_empresa = @id_empresa
          AND v.id_sucursal = @id_sucursal
          AND c.id_empresa = @id_empresa
          AND c.id_sucursal = @id_sucursal;
        """;

        await using SqlConnection conexion =
            CrearConexion();

        await using SqlCommand command =
            new(sql, conexion);

        command.Parameters.Add(
            "@id_cotizacion",
            SqlDbType.BigInt
        ).Value = idCotizacion;

        command.Parameters.Add(
            "@id_empresa",
            SqlDbType.Int
        ).Value = idEmpresa;

        command.Parameters.Add(
            "@id_sucursal",
            SqlDbType.Int
        ).Value = idSucursal;

        await conexion.OpenAsync();

        await using SqlDataReader reader =
            await command.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new VentaVinculadaCotizacion
        {
            IdVenta =
                Convert.ToInt64(reader["id_venta"]),

            NumeroVenta =
                reader["numero_venta"]?.ToString()
                ?? string.Empty,

            Estado =
                reader["estado"]?.ToString()
                ?? string.Empty,

            Total =
                Convert.ToDecimal(reader["total"])
        };
    }
    // =========================================================
    // APROBAR COTIZACIÓN
    // =========================================================
    // =========================================================
    // LISTAR ALMACENES DE LA SUCURSAL
    // =========================================================

    public async Task<List<AlmacenCotizacionItem>>
        ListarAlmacenesAsync(
            int idEmpresa,
            int idSucursal)
    {
        List<AlmacenCotizacionItem> almacenes = new();

        if (idEmpresa <= 0 || idSucursal <= 0)
        {
            return almacenes;
        }

        await using SqlConnection conexion =
            CrearConexion();

        const string sql = """
        SELECT
            a.id_almacen
        FROM inv.almacen a
        INNER JOIN org.sucursal s
            ON s.id_sucursal = a.id_sucursal
        WHERE a.id_sucursal = @id_sucursal
          AND s.id_empresa = @id_empresa
          AND a.estado = 1
          AND s.estado = 1
        ORDER BY a.id_almacen;
        """;

        await using SqlCommand command =
            new(sql, conexion);

        command.Parameters.Add(
            "@id_empresa",
            SqlDbType.Int
        ).Value = idEmpresa;

        command.Parameters.Add(
            "@id_sucursal",
            SqlDbType.Int
        ).Value = idSucursal;

        await conexion.OpenAsync();

        await using SqlDataReader reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            int idAlmacen =
                Convert.ToInt32(reader["id_almacen"]);

            almacenes.Add(new AlmacenCotizacionItem
            {
                IdAlmacen = idAlmacen,

                Nombre = $"Almacén {idAlmacen}"
            });
        }

        return almacenes;
    }


    // =========================================================
    // CONVERTIR COTIZACIÓN EN VENTA
    // =========================================================

    public async Task<long> ConvertirVentaAsync(
        long idCotizacion,
        int idEmpresa,
        int idSucursal,
        int idAlmacen)
    {
        if (idCotizacion <= 0 ||
            idEmpresa <= 0 ||
            idSucursal <= 0 ||
            idAlmacen <= 0)
        {
            throw new ArgumentException(
                "Los identificadores de conversión no son válidos."
            );
        }

        await using SqlConnection conexion =
            CrearConexion();

        await conexion.OpenAsync();

        // El procedimiento SQL es responsable de ejecutar
        // la conversión dentro de una transacción.

        await using (
            SqlCommand command = new(
                "ven.sp_cotizacion_convertir_venta",
                conexion
            )
        )
        {
            command.CommandType =
                CommandType.StoredProcedure;

            command.CommandTimeout = 120;

            command.Parameters.Add(
                "@id_cotizacion",
                SqlDbType.BigInt
            ).Value = idCotizacion;

            command.Parameters.Add(
                "@id_empresa",
                SqlDbType.Int
            ).Value = idEmpresa;

            command.Parameters.Add(
                "@id_sucursal",
                SqlDbType.Int
            ).Value = idSucursal;

            command.Parameters.Add(
                "@id_almacen",
                SqlDbType.Int
            ).Value = idAlmacen;

            await command.ExecuteNonQueryAsync();
        }

        // Consultar la venta que quedó vinculada.
        // No dependemos del formato del número generado.

        const string sql = """
        SELECT id_venta
        FROM ven.venta
        WHERE id_cotizacion = @id_cotizacion
          AND id_empresa = @id_empresa
          AND id_sucursal = @id_sucursal;
        """;

        await using SqlCommand consultar =
            new(sql, conexion);

        consultar.Parameters.Add(
            "@id_cotizacion",
            SqlDbType.BigInt
        ).Value = idCotizacion;

        consultar.Parameters.Add(
            "@id_empresa",
            SqlDbType.Int
        ).Value = idEmpresa;

        consultar.Parameters.Add(
            "@id_sucursal",
            SqlDbType.Int
        ).Value = idSucursal;

        object? resultado =
            await consultar.ExecuteScalarAsync();

        if (resultado is null ||
            resultado == DBNull.Value)
        {
            throw new InvalidOperationException(
                "La conversión terminó, pero no se encontró la venta vinculada."
            );
        }

        return Convert.ToInt64(resultado);
    }
    public async Task AprobarAsync(
        long idCotizacion,
        int idEmpresa,
        int idSucursal)
    {
        if (idCotizacion <= 0 ||
            idEmpresa <= 0 ||
            idSucursal <= 0)
        {
            throw new ArgumentException(
                "Los identificadores de la cotización no son válidos."
            );
        }

        await using SqlConnection conexion =
            CrearConexion();

        await using SqlCommand command = new(
            "ven.sp_cotizacion_aprobar",
            conexion
        );

        command.CommandType =
            CommandType.StoredProcedure;

        command.Parameters.Add(
            "@id_cotizacion",
            SqlDbType.BigInt
        ).Value = idCotizacion;

        command.Parameters.Add(
            "@id_empresa",
            SqlDbType.Int
        ).Value = idEmpresa;

        command.Parameters.Add(
            "@id_sucursal",
            SqlDbType.Int
        ).Value = idSucursal;

        await conexion.OpenAsync();

        await command.ExecuteNonQueryAsync();
    }


    // =========================================================
    // RECHAZAR COTIZACIÓN
    // =========================================================

    public async Task RechazarAsync(
        long idCotizacion,
        int idEmpresa,
        int idSucursal)
    {
        if (idCotizacion <= 0 ||
            idEmpresa <= 0 ||
            idSucursal <= 0)
        {
            throw new ArgumentException(
                "Los identificadores de la cotización no son válidos."
            );
        }

        await using SqlConnection conexion =
            CrearConexion();

        await using SqlCommand command = new(
            "ven.sp_cotizacion_rechazar",
            conexion
        );

        command.CommandType =
            CommandType.StoredProcedure;

        command.Parameters.Add(
            "@id_cotizacion",
            SqlDbType.BigInt
        ).Value = idCotizacion;

        command.Parameters.Add(
            "@id_empresa",
            SqlDbType.Int
        ).Value = idEmpresa;

        command.Parameters.Add(
            "@id_sucursal",
            SqlDbType.Int
        ).Value = idSucursal;

        await conexion.OpenAsync();

        await command.ExecuteNonQueryAsync();
    }

    private SqlConnection CrearConexion()
    {
        return new SqlConnection(
            _connectionString
        );
    }


    private static void AgregarParametroNullable(
        SqlCommand command,
        string nombre,
        SqlDbType tipo,
        object? valor)
    {
        SqlParameter parametro =
            command.Parameters.Add(
                nombre,
                tipo
            );

        parametro.Value =
            valor ?? DBNull.Value;
    }


    public async Task<List<CotizacionListaItem>>
        ListarAsync(
            int? idEmpresa,
            int? idSucursal,
            int? idCliente,
            string? estado,
            DateTime? fechaDesde,
            DateTime? fechaHasta)
    {
        List<CotizacionListaItem> lista =
            new();

        await using SqlConnection conexion =
            CrearConexion();

        await using SqlCommand command =
            new(
                "ven.sp_cotizacion_listar",
                conexion
            );

        command.CommandType =
            CommandType.StoredProcedure;

        AgregarParametroNullable(
            command,
            "@id_empresa",
            SqlDbType.Int,
            idEmpresa
        );

        AgregarParametroNullable(
            command,
            "@id_sucursal",
            SqlDbType.Int,
            idSucursal
        );

        AgregarParametroNullable(
            command,
            "@id_cliente",
            SqlDbType.Int,
            idCliente
        );

        AgregarParametroNullable(
            command,
            "@estado",
            SqlDbType.NVarChar,
            string.IsNullOrWhiteSpace(estado)
                ? null
                : estado
        );

        AgregarParametroNullable(
            command,
            "@fecha_desde",
            SqlDbType.Date,
            fechaDesde?.Date
        );

        AgregarParametroNullable(
            command,
            "@fecha_hasta",
            SqlDbType.Date,
            fechaHasta?.Date
        );

        await conexion.OpenAsync();

        await using SqlDataReader reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            lista.Add(
                new CotizacionListaItem
                {
                    IdCotizacion =
                        Convert.ToInt64(
                            reader["id_cotizacion"]
                        ),

                    NumeroCotizacion =
                        reader["numero_cotizacion"]
                            ?.ToString()
                        ?? string.Empty,

                    FechaEmision =
                        Convert.ToDateTime(
                            reader["fecha_emision"]
                        ),

                    FechaVencimiento =
                        reader["fecha_vencimiento"]
                            == DBNull.Value
                            ? null
                            : Convert.ToDateTime(
                                reader["fecha_vencimiento"]
                            ),

                    IdCliente =
                        Convert.ToInt32(
                            reader["id_cliente"]
                        ),

                    Cliente =
                        reader["Cliente"]
                            ?.ToString()
                        ?? string.Empty,

                    TipoDocumento =
                        reader["tipo_documento"]
                            ?.ToString()
                        ?? string.Empty,

                    NumeroDocumento =
                        reader["numero_documento"]
                            ?.ToString()
                        ?? string.Empty,

                    Subtotal =
                        Convert.ToDecimal(
                            reader["subtotal"]
                        ),

                    Descuento =
                        Convert.ToDecimal(
                            reader["descuento"]
                        ),

                    Impuesto =
                        Convert.ToDecimal(
                            reader["impuesto"]
                        ),

                    Total =
                        Convert.ToDecimal(
                            reader["total"]
                        ),

                    Estado =
                        reader["estado"]
                            ?.ToString()
                        ?? string.Empty,

                    Observacion =
                        reader["observacion"]
                            == DBNull.Value
                            ? null
                            : reader["observacion"]
                                ?.ToString(),

                    IdEmpresa =
                        Convert.ToInt32(
                            reader["id_empresa"]
                        ),

                    IdSucursal =
                        Convert.ToInt32(
                            reader["id_sucursal"]
                        ),

                    IdEmpleado =
                        Convert.ToInt32(
                            reader["id_empleado"]
                        )
                }
            );
        }

        return lista;
    }


    public async Task<CotizacionResumen>
        ObtenerResumenAsync(
            int? idEmpresa,
            int? idSucursal,
            DateTime? fechaDesde,
            DateTime? fechaHasta)
    {
        CotizacionResumen resumen =
            new();

        await using SqlConnection conexion =
            CrearConexion();

        await using SqlCommand command =
            new(
                "ven.sp_cotizacion_resumen",
                conexion
            );

        command.CommandType =
            CommandType.StoredProcedure;

        AgregarParametroNullable(
            command,
            "@id_empresa",
            SqlDbType.Int,
            idEmpresa
        );

        AgregarParametroNullable(
            command,
            "@id_sucursal",
            SqlDbType.Int,
            idSucursal
        );

        AgregarParametroNullable(
            command,
            "@fecha_desde",
            SqlDbType.Date,
            fechaDesde?.Date
        );

        AgregarParametroNullable(
            command,
            "@fecha_hasta",
            SqlDbType.Date,
            fechaHasta?.Date
        );

        await conexion.OpenAsync();

        await using SqlDataReader reader =
            await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            resumen.TotalCotizaciones =
                reader["total_cotizaciones"]
                    == DBNull.Value
                    ? 0
                    : Convert.ToInt32(
                        reader["total_cotizaciones"]
                    );

            resumen.Pendientes =
                reader["pendientes"]
                    == DBNull.Value
                    ? 0
                    : Convert.ToInt32(
                        reader["pendientes"]
                    );

            resumen.Aprobadas =
                reader["aprobadas"]
                    == DBNull.Value
                    ? 0
                    : Convert.ToInt32(
                        reader["aprobadas"]
                    );

            resumen.Convertidas =
                reader["convertidas"]
                    == DBNull.Value
                    ? 0
                    : Convert.ToInt32(
                        reader["convertidas"]
                    );

            resumen.Rechazadas =
                reader["rechazadas"]
                    == DBNull.Value
                    ? 0
                    : Convert.ToInt32(
                        reader["rechazadas"]
                    );

            resumen.ImporteTotal =
                reader["importe_total"]
                    == DBNull.Value
                    ? 0
                    : Convert.ToDecimal(
                        reader["importe_total"]
                    );
        }

        return resumen;
    }


    public async Task<List<CotizacionEstadoResumen>>
        ObtenerEstadosAsync(
            int? idEmpresa,
            int? idSucursal,
            DateTime? fechaDesde,
            DateTime? fechaHasta)
    {
        List<CotizacionEstadoResumen> lista =
            new();

        await using SqlConnection conexion =
            CrearConexion();

        await using SqlCommand command =
            new(
                "ven.sp_cotizacion_estado_resumen",
                conexion
            );

        command.CommandType =
            CommandType.StoredProcedure;

        AgregarParametroNullable(
            command,
            "@id_empresa",
            SqlDbType.Int,
            idEmpresa
        );

        AgregarParametroNullable(
            command,
            "@id_sucursal",
            SqlDbType.Int,
            idSucursal
        );

        AgregarParametroNullable(
            command,
            "@fecha_desde",
            SqlDbType.Date,
            fechaDesde?.Date
        );

        AgregarParametroNullable(
            command,
            "@fecha_hasta",
            SqlDbType.Date,
            fechaHasta?.Date
        );

        await conexion.OpenAsync();

        await using SqlDataReader reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            lista.Add(
                new CotizacionEstadoResumen
                {
                    Estado =
                        reader["estado"]
                            ?.ToString()
                        ?? string.Empty,

                    Cantidad =
                        Convert.ToInt32(
                            reader["cantidad"]
                        ),

                    TotalImporte =
                        reader["total_importe"]
                            == DBNull.Value
                            ? 0
                            : Convert.ToDecimal(
                                reader["total_importe"]
                            )
                }
            );
        }

        return lista;
    }


    public async Task<List<CotizacionActividadItem>>
        ObtenerActividadRecienteAsync(
            int? idEmpresa,
            int? idSucursal,
            int top = 8)
    {
        List<CotizacionActividadItem> lista =
            new();

        await using SqlConnection conexion =
            CrearConexion();

        await using SqlCommand command =
            new(
                "ven.sp_cotizacion_actividad_reciente",
                conexion
            );

        command.CommandType =
            CommandType.StoredProcedure;

        AgregarParametroNullable(
            command,
            "@id_empresa",
            SqlDbType.Int,
            idEmpresa
        );

        AgregarParametroNullable(
            command,
            "@id_sucursal",
            SqlDbType.Int,
            idSucursal
        );

        command.Parameters.Add(
            "@top",
            SqlDbType.Int
        ).Value = top;

        await conexion.OpenAsync();

        await using SqlDataReader reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            lista.Add(
                new CotizacionActividadItem
                {
                    IdCotizacion =
                        Convert.ToInt64(
                            reader["id_cotizacion"]
                        ),

                    NumeroCotizacion =
                        reader["numero_cotizacion"]
                            ?.ToString()
                        ?? string.Empty,

                    Cliente =
                        reader["Cliente"]
                            ?.ToString()
                        ?? string.Empty,

                    Estado =
                        reader["estado"]
                            ?.ToString()
                        ?? string.Empty,

                    Total =
                        Convert.ToDecimal(
                            reader["total"]
                        ),

                    FechaEmision =
                        Convert.ToDateTime(
                            reader["fecha_emision"]
                        )
                }
            );
        }

        return lista;
    }


    public async Task<List<CotizacionTopProducto>>
        ObtenerTopProductosAsync(
            int? idEmpresa,
            int? idSucursal,
            DateTime? fechaDesde,
            DateTime? fechaHasta,
            int top = 5)
    {
        List<CotizacionTopProducto> lista =
            new();

        await using SqlConnection conexion =
            CrearConexion();

        await using SqlCommand command =
            new(
                "ven.sp_cotizacion_top_productos",
                conexion
            );

        command.CommandType =
            CommandType.StoredProcedure;

        AgregarParametroNullable(
            command,
            "@id_empresa",
            SqlDbType.Int,
            idEmpresa
        );

        AgregarParametroNullable(
            command,
            "@id_sucursal",
            SqlDbType.Int,
            idSucursal
        );

        AgregarParametroNullable(
            command,
            "@fecha_desde",
            SqlDbType.Date,
            fechaDesde?.Date
        );

        AgregarParametroNullable(
            command,
            "@fecha_hasta",
            SqlDbType.Date,
            fechaHasta?.Date
        );

        command.Parameters.Add(
            "@top",
            SqlDbType.Int
        ).Value = top;

        await conexion.OpenAsync();

        await using SqlDataReader reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            lista.Add(
                new CotizacionTopProducto
                {
                    IdProducto =
                        Convert.ToInt32(
                            reader["id_producto"]
                        ),

                    Codigo =
                        reader["codigo"]
                            ?.ToString()
                        ?? string.Empty,

                    Nombre =
                        reader["nombre"]
                            ?.ToString()
                        ?? string.Empty,

                    TotalCotizado =
                        Convert.ToDecimal(
                            reader["total_cotizado"]
                        ),

                    VecesCotizado =
                        Convert.ToInt32(
                            reader["veces_cotizado"]
                        ),

                    ImporteCotizado =
                        Convert.ToDecimal(
                            reader["importe_cotizado"]
                        )
                }
            );
        }

        return lista;
    }


    public async Task<List<CotizacionTopCliente>>
        ObtenerTopClientesAsync(
            int? idEmpresa,
            int? idSucursal,
            DateTime? fechaDesde,
            DateTime? fechaHasta,
            int top = 5)
    {
        List<CotizacionTopCliente> lista =
            new();

        await using SqlConnection conexion =
            CrearConexion();

        await using SqlCommand command =
            new(
                "ven.sp_cotizacion_top_clientes",
                conexion
            );

        command.CommandType =
            CommandType.StoredProcedure;

        AgregarParametroNullable(
            command,
            "@id_empresa",
            SqlDbType.Int,
            idEmpresa
        );

        AgregarParametroNullable(
            command,
            "@id_sucursal",
            SqlDbType.Int,
            idSucursal
        );

        AgregarParametroNullable(
            command,
            "@fecha_desde",
            SqlDbType.Date,
            fechaDesde?.Date
        );

        AgregarParametroNullable(
            command,
            "@fecha_hasta",
            SqlDbType.Date,
            fechaHasta?.Date
        );

        command.Parameters.Add(
            "@top",
            SqlDbType.Int
        ).Value = top;

        await conexion.OpenAsync();

        await using SqlDataReader reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            lista.Add(
                new CotizacionTopCliente
                {
                    IdCliente =
                        Convert.ToInt32(
                            reader["id_cliente"]
                        ),

                    Cliente =
                        reader["Cliente"]
                            ?.ToString()
                        ?? string.Empty,

                    CantidadCotizaciones =
                        Convert.ToInt32(
                            reader["cantidad_cotizaciones"]
                        ),

                    ImporteCotizado =
                        Convert.ToDecimal(
                            reader["importe_cotizado"]
                        ),

                    UltimaCotizacion =
                        Convert.ToDateTime(
                            reader["ultima_cotizacion"]
                        ),

                    CotizacionesAprobadas =
                        Convert.ToInt32(
                            reader["cotizaciones_aprobadas"]
                        ),

                    CotizacionesConvertidas =
                        Convert.ToInt32(
                            reader["cotizaciones_convertidas"]
                        )
                }
            );
        }

        return lista;
    }
    public async Task<List<ClienteCotizacionItem>>
    ListarClientesAsync(
        int idEmpresa)
    {
        List<ClienteCotizacionItem> lista =
            new();

        await using SqlConnection conexion =
            CrearConexion();

        await using SqlCommand command =
            new(
                "ven.sp_cotizacion_cliente_listar",
                conexion
            );

        command.CommandType =
            CommandType.StoredProcedure;


        command.Parameters.Add(
            "@id_empresa",
            SqlDbType.Int
        ).Value = idEmpresa;


        await conexion.OpenAsync();


        await using SqlDataReader reader =
            await command.ExecuteReaderAsync();


        while (
            await reader.ReadAsync()
        )
        {
            lista.Add(
                new ClienteCotizacionItem
                {
                    IdCliente =
                        Convert.ToInt32(
                            reader["id_cliente"]
                        ),

                    TipoCliente =
                        reader["tipo_cliente"]
                            ?.ToString()
                        ?? string.Empty,

                    TipoDocumento =
                        reader["tipo_documento"]
                            ?.ToString()
                        ?? string.Empty,

                    NumeroDocumento =
                        reader["numero_documento"]
                            ?.ToString()
                        ?? string.Empty,

                    Cliente =
                        reader["cliente"]
                            ?.ToString()
                        ?? string.Empty,

                    RazonSocial =
                        reader["razon_social"]
                            == DBNull.Value
                            ? null
                            : reader["razon_social"]
                                ?.ToString(),

                    Nombres =
                        reader["nombres"]
                            == DBNull.Value
                            ? null
                            : reader["nombres"]
                                ?.ToString(),

                    Apellidos =
                        reader["apellidos"]
                            == DBNull.Value
                            ? null
                            : reader["apellidos"]
                                ?.ToString(),

                    Telefono =
                        reader["telefono"]
                            == DBNull.Value
                            ? null
                            : reader["telefono"]
                                ?.ToString(),

                    Correo =
                        reader["correo"]
                            == DBNull.Value
                            ? null
                            : reader["correo"]
                                ?.ToString()
                }
            );
        }


        return lista;
    }
    public async Task<List<ProductoCotizacionItem>>
    ListarProductosAsync(
        int idEmpresa,
        string? buscar = null)
    {
        List<ProductoCotizacionItem> lista =
            new();

        await using SqlConnection conexion =
            CrearConexion();

        await using SqlCommand command =
            new(
                "ven.sp_cotizacion_producto_listar",
                conexion
            );

        command.CommandType =
            CommandType.StoredProcedure;


        command.Parameters.Add(
            "@id_empresa",
            SqlDbType.Int
        ).Value = idEmpresa;


        SqlParameter parametroBuscar =
            command.Parameters.Add(
                "@buscar",
                SqlDbType.NVarChar,
                200
            );

        parametroBuscar.Value =
            string.IsNullOrWhiteSpace(buscar)
                ? DBNull.Value
                : buscar.Trim();


        await conexion.OpenAsync();


        await using SqlDataReader reader =
            await command.ExecuteReaderAsync();


        while (
            await reader.ReadAsync()
        )
        {
            lista.Add(
                new ProductoCotizacionItem
                {
                    IdProducto =
                        Convert.ToInt32(
                            reader["id_producto"]
                        ),

                    Codigo =
                        reader["codigo"]
                            ?.ToString()
                        ?? string.Empty,

                    CodigoBarras =
                        reader["codigo_barras"]
                            == DBNull.Value
                            ? null
                            : reader["codigo_barras"]
                                ?.ToString(),

                    Nombre =
                        reader["nombre"]
                            ?.ToString()
                        ?? string.Empty,

                    Descripcion =
                        reader["descripcion"]
                            == DBNull.Value
                            ? null
                            : reader["descripcion"]
                                ?.ToString(),

                    PrecioVenta =
                        reader["precio_venta"]
                            == DBNull.Value
                            ? 0
                            : Convert.ToDecimal(
                                reader["precio_venta"]
                            ),

                    ControlaStock =
                        reader["controla_stock"]
                            != DBNull.Value
                        &&
                        Convert.ToBoolean(
                            reader["controla_stock"]
                        ),

                    Estado =
                        reader["estado"]
                            != DBNull.Value
                        &&
                        Convert.ToBoolean(
                            reader["estado"]
                        )
                }
            );
        }


        return lista;
    }
    public async Task<CotizacionRegistroResultado>
    RegistrarAsync(
        NuevaCotizacionRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(
                nameof(request)
            );
        }


        if (
            request.Detalles is null ||
            request.Detalles.Count == 0
        )
        {
            throw new InvalidOperationException(
                "La cotización debe contener al menos un producto."
            );
        }


        /*
         * =========================================================
         * CREAR DATATABLE DEL TVP
         * =========================================================
         */

        DataTable detalleTable =
            new();

        detalleTable.Columns.Add(
            "id_producto",
            typeof(int)
        );

        detalleTable.Columns.Add(
            "cantidad",
            typeof(decimal)
        );

        detalleTable.Columns.Add(
            "precio_unitario",
            typeof(decimal)
        );

        detalleTable.Columns.Add(
            "descuento",
            typeof(decimal)
        );

        detalleTable.Columns.Add(
            "impuesto",
            typeof(decimal)
        );

        detalleTable.Columns.Add(
            "subtotal",
            typeof(decimal)
        );


        foreach (
            NuevaCotizacionDetalleRequest detalle
            in request.Detalles
        )
        {
            if (
                detalle.IdProducto <= 0
            )
            {
                throw new InvalidOperationException(
                    "Existe un producto inválido."
                );
            }


            if (
                detalle.Cantidad <= 0
            )
            {
                throw new InvalidOperationException(
                    $"La cantidad de {detalle.Producto} debe ser mayor a cero."
                );
            }


            if (
                detalle.PrecioUnitario < 0
            )
            {
                throw new InvalidOperationException(
                    $"El precio de {detalle.Producto} no es válido."
                );
            }


            /*
             * Recalculamos el subtotal en servidor.
             * No confiamos únicamente en JavaScript.
             */

            decimal subtotal =
                Math.Round(
                    detalle.Cantidad *
                    detalle.PrecioUnitario,
                    4
                );


            detalleTable.Rows.Add(
                detalle.IdProducto,
                detalle.Cantidad,
                detalle.PrecioUnitario,
                detalle.Descuento,
                detalle.Impuesto,
                subtotal
            );
        }


        /*
         * =========================================================
         * SQL
         * =========================================================
         */

        await using SqlConnection conexion =
            CrearConexion();


        await using SqlCommand command =
            new(
                "ven.sp_cotizacion_registrar",
                conexion
            );


        command.CommandType =
            CommandType.StoredProcedure;


        command.Parameters.Add(
            "@id_empresa",
            SqlDbType.Int
        ).Value =
            request.IdEmpresa;


        command.Parameters.Add(
            "@id_sucursal",
            SqlDbType.Int
        ).Value =
            request.IdSucursal;


        command.Parameters.Add(
            "@id_cliente",
            SqlDbType.Int
        ).Value =
            request.IdCliente;


        command.Parameters.Add(
            "@id_empleado",
            SqlDbType.Int
        ).Value =
            request.IdEmpleado;


        SqlParameter fechaVencimiento =
            command.Parameters.Add(
                "@fecha_vencimiento",
                SqlDbType.DateTime2
            );

        fechaVencimiento.Value =
            request.FechaVencimiento.HasValue
                ? request.FechaVencimiento.Value
                : DBNull.Value;


        SqlParameter descuento =
            command.Parameters.Add(
                "@descuento",
                SqlDbType.Decimal
            );

        descuento.Precision = 18;
        descuento.Scale = 4;

        descuento.Value =
            request.Descuento;


        SqlParameter observacion =
            command.Parameters.Add(
                "@observacion",
                SqlDbType.NVarChar,
                1000
            );

        observacion.Value =
            string.IsNullOrWhiteSpace(
                request.Observacion
            )
                ? DBNull.Value
                : request.Observacion.Trim();


        /*
         * TVP
         */

        SqlParameter detalleParameter =
            command.Parameters.AddWithValue(
                "@detalle",
                detalleTable
            );

        detalleParameter.SqlDbType =
            SqlDbType.Structured;

        detalleParameter.TypeName =
            "ven.TVP_CotizacionDetalle";


        await conexion.OpenAsync();


        await using SqlDataReader reader =
            await command.ExecuteReaderAsync();


        if (
            !await reader.ReadAsync()
        )
        {
            throw new InvalidOperationException(
                "El procedimiento no devolvió información de la cotización registrada."
            );
        }


        return new CotizacionRegistroResultado
        {
            IdCotizacion =
                Convert.ToInt64(
                    reader["id_cotizacion"]
                ),

            NumeroCotizacion =
                reader["numero_cotizacion"]
                    ?.ToString()
                ?? string.Empty,

            FechaEmision =
                Convert.ToDateTime(
                    reader["fecha_emision"]
                ),

            FechaVencimiento =
                reader["fecha_vencimiento"]
                    == DBNull.Value
                    ? null
                    : Convert.ToDateTime(
                        reader["fecha_vencimiento"]
                    ),

            Subtotal =
                Convert.ToDecimal(
                    reader["subtotal"]
                ),

            Descuento =
                Convert.ToDecimal(
                    reader["descuento"]
                ),

            Impuesto =
                Convert.ToDecimal(
                    reader["impuesto"]
                ),

            Total =
                Convert.ToDecimal(
                    reader["total"]
                ),

            Estado =
                reader["estado"]
                    ?.ToString()
                ?? string.Empty
        };
    }    /*
     * =========================================================
     * OBTENER COTIZACIÓN COMPLETA
     * =========================================================
     */

    public async Task<CotizacionDetalleResponse?>
        ObtenerPorIdAsync(
            long idCotizacion,
            int idEmpresa,
            int? idSucursal = null)
    {
        if (idCotizacion <= 0 || idEmpresa <= 0)
        {
            return null;
        }

        await using SqlConnection conexion =
            CrearConexion();

        await conexion.OpenAsync();

        CotizacionDetalleResponse? cotizacion = null;

        // =====================================================
        // CABECERA
        // =====================================================

        await using (SqlCommand command = new(
            "ven.sp_cotizacion_obtener",
            conexion))
        {
            command.CommandType =
                CommandType.StoredProcedure;

            command.Parameters.Add(
                "@id_cotizacion",
                SqlDbType.BigInt
            ).Value = idCotizacion;

            await using SqlDataReader reader =
                await command.ExecuteReaderAsync();

            if (!await reader.ReadAsync())
            {
                return null;
            }

            // Comprobar pertenencia antes de cargar el detalle.
            int empresaCotizacion =
                Convert.ToInt32(reader["id_empresa"]);

            int sucursalCotizacion =
                Convert.ToInt32(reader["id_sucursal"]);

            if (empresaCotizacion != idEmpresa ||
                (idSucursal.HasValue &&
                 sucursalCotizacion != idSucursal.Value))
            {
                return null;
            }

            cotizacion = new CotizacionDetalleResponse
            {
                IdCotizacion =
                    Convert.ToInt64(reader["id_cotizacion"]),

                IdEmpresa = empresaCotizacion,

                IdSucursal = sucursalCotizacion,

                IdCliente =
                    Convert.ToInt32(reader["id_cliente"]),

                IdEmpleado =
                    Convert.ToInt32(reader["id_empleado"]),

                NumeroCotizacion =
                    reader["numero_cotizacion"]?.ToString()
                    ?? string.Empty,

                FechaEmision =
                    Convert.ToDateTime(reader["fecha_emision"]),

                FechaVencimiento =
                    reader["fecha_vencimiento"] == DBNull.Value
                        ? null
                        : Convert.ToDateTime(
                            reader["fecha_vencimiento"]),

                Subtotal =
                    Convert.ToDecimal(reader["subtotal"]),

                Descuento =
                    Convert.ToDecimal(reader["descuento"]),

                Impuesto =
                    Convert.ToDecimal(reader["impuesto"]),

                Total =
                    Convert.ToDecimal(reader["total"]),

                Estado =
                    reader["estado"]?.ToString()
                    ?? string.Empty,

                Observacion =
                    reader["observacion"] == DBNull.Value
                        ? null
                        : reader["observacion"]?.ToString(),

                Cliente =
                    reader["cliente"]?.ToString()
                    ?? string.Empty,

                TipoDocumento =
                    reader["tipo_documento"] == DBNull.Value
                        ? null
                        : reader["tipo_documento"]?.ToString(),

                NumeroDocumento =
                    reader["numero_documento"] == DBNull.Value
                        ? null
                        : reader["numero_documento"]?.ToString(),

                Telefono =
                    reader["telefono"] == DBNull.Value
                        ? null
                        : reader["telefono"]?.ToString(),

                Correo =
                    reader["correo"] == DBNull.Value
                        ? null
                        : reader["correo"]?.ToString()
            };
        }

        // =====================================================
        // PRODUCTOS
        // =====================================================

        await using (SqlCommand command = new(
            "ven.sp_cotizacion_detalle_listar",
            conexion))
        {
            command.CommandType =
                CommandType.StoredProcedure;

            command.Parameters.Add(
                "@id_cotizacion",
                SqlDbType.BigInt
            ).Value = idCotizacion;

            await using SqlDataReader reader =
                await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                cotizacion.Productos.Add(
                    new CotizacionDetalleProducto
                    {
                        IdDetalleCotizacion =
                            Convert.ToInt64(
                                reader["id_detalle_cotizacion"]),

                        IdCotizacion =
                            Convert.ToInt64(
                                reader["id_cotizacion"]),

                        IdProducto =
                            Convert.ToInt32(
                                reader["id_producto"]),

                        Codigo =
                            reader["codigo"]?.ToString()
                            ?? string.Empty,

                        CodigoBarras =
                            reader["codigo_barras"] == DBNull.Value
                                ? null
                                : reader["codigo_barras"]?.ToString(),

                        Nombre =
                            reader["nombre"]?.ToString()
                            ?? string.Empty,

                        Cantidad =
                            Convert.ToDecimal(reader["cantidad"]),

                        PrecioUnitario =
                            Convert.ToDecimal(
                                reader["precio_unitario"]),

                        Descuento =
                            Convert.ToDecimal(reader["descuento"]),

                        Impuesto =
                            Convert.ToDecimal(reader["impuesto"]),

                        Subtotal =
                            Convert.ToDecimal(reader["subtotal"])
                    }
                );
            }
        }

        return cotizacion;
    }
}