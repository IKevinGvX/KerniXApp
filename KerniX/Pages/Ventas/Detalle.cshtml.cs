using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

using KerniX.Security;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;

namespace KerniX.Pages.Ventas;

public sealed class DetalleModel : PageModel
{
    // =========================================================
    // SERVICIOS
    // =========================================================

    private readonly string _connectionString;
    private readonly ILogger<DetalleModel> _logger;

    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public DetalleModel(
        IConfiguration configuration,
        ILogger<DetalleModel> logger)
    {
        _connectionString =
            configuration.GetConnectionString("KerniXDb")
            ?? throw new InvalidOperationException(
                "Falta ConnectionStrings:KerniXDb."
            );

        _logger = logger;
    }

    // =========================================================
    // PROPIEDADES DE LA PÁGINA
    // =========================================================

    public VentaInfo? Venta
    {
        get;
        private set;
    }

    public List<LineaVenta> Productos { get; } = new();

    public string? MensajeError
    {
        get;
        private set;
    }

    // =========================================================
    // GET - DETALLE DE VENTA
    // =========================================================

    public async Task<IActionResult> OnGetAsync(long idVenta)
    {
        // =====================================================
        // VALIDAR IDENTIFICADOR
        // =====================================================

        if (idVenta <= 0)
        {
            return BadRequest(
                "ID de venta inválido."
            );
        }

        // =====================================================
        // VALIDAR USUARIO
        // =====================================================

        int idUsuario =
            HttpContext.Session.GetInt32(
                SessionKeys.IdUsuario
            ) ?? 0;

        if (idUsuario <= 0)
        {
            return RedirectToPage(
                "/Account/Login"
            );
        }

        // =====================================================
        // EMPRESA Y SUCURSAL
        // MISMO PROCESO TEMPORAL DE NUEVA COTIZACIÓN
        // =====================================================

        int empresa = ObtenerIdEmpresa();

        int sucursal = ObtenerIdSucursal();

        _logger.LogInformation(
            "Detalle venta. Id={Venta} | Empresa={Empresa} | Sucursal={Sucursal} | Usuario={Usuario}",
            idVenta,
            empresa,
            sucursal,
            idUsuario
        );

        if (empresa <= 0 || sucursal <= 0)
        {
            return StatusCode(
                403,
                "No se pudo identificar la empresa o sucursal."
            );
        }

        // =====================================================
        // LIMPIAR INFORMACIÓN
        // =====================================================

        Venta = null;
        Productos.Clear();
        MensajeError = null;

        try
        {
            // =================================================
            // CONEXIÓN SQL
            // =================================================

            await using SqlConnection conexion =
                new(_connectionString);

            await conexion.OpenAsync();

            // =================================================
            // CONSULTAR CABECERA DE VENTA
            // =================================================

            const string sqlCabecera = @"

                SELECT
                    id_venta,
                    numero_venta,
                    fecha_venta,
                    id_cliente,
                    id_empleado,
                    subtotal,
                    descuento,
                    impuesto,
                    total,
                    estado,
                    observacion,
                    id_cotizacion

                FROM ven.venta

                WHERE id_venta = @id_venta
                  AND id_empresa = @id_empresa
                  AND id_sucursal = @id_sucursal;

            ";

            await using (
                SqlCommand cmd = new(
                    sqlCabecera,
                    conexion
                )
            )
            {
                cmd.Parameters.Add(
                    "@id_venta",
                    SqlDbType.BigInt
                ).Value = idVenta;

                cmd.Parameters.Add(
                    "@id_empresa",
                    SqlDbType.Int
                ).Value = empresa;

                cmd.Parameters.Add(
                    "@id_sucursal",
                    SqlDbType.Int
                ).Value = sucursal;

                await using SqlDataReader reader =
                    await cmd.ExecuteReaderAsync();

                if (!await reader.ReadAsync())
                {
                    return NotFound(
                        "La venta no existe o no pertenece a la empresa y sucursal seleccionadas."
                    );
                }

                Venta = new VentaInfo
                {
                    IdVenta = Convert.ToInt64(
                        reader["id_venta"]
                    ),

                    NumeroVenta = Texto(
                        reader,
                        "numero_venta"
                    ),

                    FechaVenta = Fecha(
                        reader,
                        "fecha_venta"
                    ),

                    IdCliente = Entero(
                        reader,
                        "id_cliente"
                    ),

                    IdEmpleado = Entero(
                        reader,
                        "id_empleado"
                    ),

                    Subtotal = Dinero(
                        reader,
                        "subtotal"
                    ),

                    Descuento = Dinero(
                        reader,
                        "descuento"
                    ),

                    Impuesto = Dinero(
                        reader,
                        "impuesto"
                    ),

                    Total = Dinero(
                        reader,
                        "total"
                    ),

                    Estado = Texto(
                        reader,
                        "estado"
                    ),

                    Observacion = Texto(
                        reader,
                        "observacion"
                    ),

                    IdCotizacion = EnteroLargoNullable(
                        reader,
                        "id_cotizacion"
                    )
                };
            }

            // =================================================
            // CONSULTAR CLIENTE
            // =================================================

            const string sqlCliente = @"

                SELECT *

                FROM ven.cliente

                WHERE id_cliente = @id_cliente
                  AND id_empresa = @id_empresa;

            ";

            await using (
                SqlCommand cmd = new(
                    sqlCliente,
                    conexion
                )
            )
            {
                cmd.Parameters.Add(
                    "@id_cliente",
                    SqlDbType.Int
                ).Value = Venta!.IdCliente;

                cmd.Parameters.Add(
                    "@id_empresa",
                    SqlDbType.Int
                ).Value = empresa;

                await using SqlDataReader reader =
                    await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    string nombre = PrimerTexto(
                        reader,
                        "razon_social",
                        "nombre_completo",
                        "nombre",
                        "nombres",
                        "cliente",
                        "nombre_razon_social"
                    );

                    string apellidos = PrimerTexto(
                        reader,
                        "apellidos"
                    );

                    Venta.Cliente =
                        string.IsNullOrWhiteSpace(nombre)
                            ? $"Cliente #{Venta.IdCliente}"
                            : (
                                nombre + " " + apellidos
                              ).Trim();

                    Venta.Documento = PrimerTexto(
                        reader,
                        "numero_documento",
                        "documento",
                        "ruc",
                        "dni"
                    );
                }
            }

            // =================================================
            // VENDEDOR
            // OBTENER NOMBRE COMPLETO DESDE LA SESIÓN
            // =================================================

            int idEmpleadoSesion =
                ObtenerEnteroSesion(
                    SessionKeys.IdEmpleado
                );

            string nombreCompletoSesion =
                HttpContext.Session.GetString(
                    SessionKeys.NombreCompleto
                ) ?? string.Empty;

            // La sesión identifica únicamente al empleado
            // que está conectado actualmente.
            //
            // No debemos mostrar ese nombre en una venta
            // registrada por otro empleado.

            if (
                Venta != null &&
                idEmpleadoSesion > 0 &&
                Venta.IdEmpleado == idEmpleadoSesion &&
                !string.IsNullOrWhiteSpace(
                    nombreCompletoSesion
                )
            )
            {
                Venta.Vendedor =
                    nombreCompletoSesion.Trim();
            }
            else
            {
                // No inventar el nombre del vendedor.
                // Posteriormente puede consultarse
                // desde org.empleado o el servicio existente.

                Venta!.Vendedor =
                    "Nombre no disponible";
            }

            _logger.LogInformation(
                "Vendedor de venta. Venta={Venta} | EmpleadoVenta={EmpleadoVenta} | EmpleadoSesion={EmpleadoSesion} | Nombre={Nombre}",
                idVenta,
                Venta.IdEmpleado,
                idEmpleadoSesion,
                Venta.Vendedor
            );

            // =================================================
            // CONSULTAR PRODUCTOS DE LA VENTA
            // =================================================

            const string sqlDetalle = @"

                SELECT
                    d.id_producto,
                    d.cantidad,
                    d.precio_unitario,
                    d.subtotal,
                    d.descuento,
                    d.impuesto,
                    d.total,

                    p.nombre AS nombre_producto

                FROM ven.detalle_venta AS d

                LEFT JOIN inv.producto AS p
                    ON p.id_producto = d.id_producto
                   AND p.id_empresa = @id_empresa

                WHERE d.id_venta = @id_venta

                ORDER BY d.id_producto;

            ";

            await using (
                SqlCommand cmd = new(
                    sqlDetalle,
                    conexion
                )
            )
            {
                cmd.Parameters.Add(
                    "@id_venta",
                    SqlDbType.BigInt
                ).Value = idVenta;

                cmd.Parameters.Add(
                    "@id_empresa",
                    SqlDbType.Int
                ).Value = empresa;

                await using SqlDataReader reader =
                    await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    int idProducto = Entero(
                        reader,
                        "id_producto"
                    );

                    Productos.Add(
                        new LineaVenta
                        {
                            IdProducto = idProducto,

                            Producto =
                                NombreProducto(
                                    reader,
                                    idProducto
                                ),

                            Cantidad = Dinero(
                                reader,
                                "cantidad"
                            ),

                            PrecioUnitario = Dinero(
                                reader,
                                "precio_unitario"
                            ),

                            Subtotal = Dinero(
                                reader,
                                "subtotal"
                            ),

                            Descuento = Dinero(
                                reader,
                                "descuento"
                            ),

                            Impuesto = Dinero(
                                reader,
                                "impuesto"
                            ),

                            Total = Dinero(
                                reader,
                                "total"
                            )
                        }
                    );
                }
            }

            // =================================================
            // FINALIZAR
            // =================================================

            _logger.LogInformation(
                "Detalle de venta cargado correctamente. Venta={Venta} | Productos={Productos}",
                idVenta,
                Productos.Count
            );

            return Page();
        }
        catch (SqlException ex)
        {
            _logger.LogError(
                ex,
                "Error SQL consultando venta {Venta} | Empresa={Empresa} | Sucursal={Sucursal}",
                idVenta,
                empresa,
                sucursal
            );

            MensajeError =
                "No se pudo consultar la venta. Revisa el registro de errores del servidor.";

            return Page();
        }
    }

    // =========================================================
    // EMPRESA
    // =========================================================

    private int ObtenerIdEmpresa()
    {
        int id =
            ObtenerEnteroSesion("IdEmpresa");

        // Temporal para pruebas locales:
        // mismo proceso que Nueva Cotización.
        //
        // En producción debe obtenerse de un contexto
        // autenticado y autorizado.

        return id > 0 ? id : 1;
    }

    // =========================================================
    // SUCURSAL
    // =========================================================

    private int ObtenerIdSucursal()
    {
        int id =
            ObtenerEnteroSesion("IdSucursal");

        // Temporal para pruebas locales.
        // No mantener este valor fijo en producción.

        return id > 0 ? id : 1;
    }

    // =========================================================
    // LEER ENTEROS DESDE LA SESIÓN
    // =========================================================

    private int ObtenerEnteroSesion(
        string clave)
    {
        int? valorInt =
            HttpContext.Session.GetInt32(clave);

        if (valorInt.HasValue)
        {
            return valorInt.Value;
        }

        string? valorString =
            HttpContext.Session.GetString(clave);

        if (
            int.TryParse(
                valorString,
                out int valor
            )
        )
        {
            return valor;
        }

        return 0;
    }

    // =========================================================
    // MÉTODOS AUXILIARES SQL
    // =========================================================

    private static int Indice(
        SqlDataReader reader,
        string columna)
    {
        for (
            int i = 0;
            i < reader.FieldCount;
            i++
        )
        {
            if (
                string.Equals(
                    reader.GetName(i),
                    columna,
                    StringComparison.OrdinalIgnoreCase
                )
            )
            {
                return i;
            }
        }

        return -1;
    }

    private static string Texto(
        SqlDataReader reader,
        string columna)
    {
        int i =
            Indice(
                reader,
                columna
            );

        if (
            i < 0 ||
            reader.IsDBNull(i)
        )
        {
            return string.Empty;
        }

        return Convert.ToString(
            reader.GetValue(i)
        ) ?? string.Empty;
    }

    private static string PrimerTexto(
        SqlDataReader reader,
        params string[] columnas)
    {
        foreach (string columna in columnas)
        {
            string texto =
                Texto(
                    reader,
                    columna
                );

            if (
                !string.IsNullOrWhiteSpace(texto)
            )
            {
                return texto;
            }
        }

        return string.Empty;
    }

    private static int Entero(
        SqlDataReader reader,
        string columna)
    {
        int i =
            Indice(
                reader,
                columna
            );

        if (
            i < 0 ||
            reader.IsDBNull(i)
        )
        {
            return 0;
        }

        return Convert.ToInt32(
            reader.GetValue(i)
        );
    }

    private static long? EnteroLargoNullable(
        SqlDataReader reader,
        string columna)
    {
        int i =
            Indice(
                reader,
                columna
            );

        if (
            i < 0 ||
            reader.IsDBNull(i)
        )
        {
            return null;
        }

        return Convert.ToInt64(
            reader.GetValue(i)
        );
    }

    private static decimal Dinero(
        SqlDataReader reader,
        string columna)
    {
        int i =
            Indice(
                reader,
                columna
            );

        if (
            i < 0 ||
            reader.IsDBNull(i)
        )
        {
            return 0;
        }

        return Convert.ToDecimal(
            reader.GetValue(i)
        );
    }

    private static DateTime Fecha(
        SqlDataReader reader,
        string columna)
    {
        int i =
            Indice(
                reader,
                columna
            );

        if (
            i < 0 ||
            reader.IsDBNull(i)
        )
        {
            return DateTime.MinValue;
        }

        return Convert.ToDateTime(
            reader.GetValue(i)
        );
    }

    private static string NombreProducto(
        SqlDataReader reader,
        int idProducto)
    {
        string nombre =
            PrimerTexto(
                reader,
                "nombre_producto",
                "nombre",
                "descripcion",
                "producto"
            );

        return string.IsNullOrWhiteSpace(nombre)
            ? $"Producto #{idProducto}"
            : nombre;
    }

    // =========================================================
    // MODELO DE VENTA
    // =========================================================

    public sealed class VentaInfo
    {
        public long IdVenta { get; set; }

        public string NumeroVenta { get; set; } =
            string.Empty;

        public DateTime FechaVenta { get; set; }

        public int IdCliente { get; set; }

        public int IdEmpleado { get; set; }

        // CORRECCIÓN:
        // Vendedor pertenece a VentaInfo.
        public string Vendedor { get; set; } =
            string.Empty;

        public string Cliente { get; set; } =
            string.Empty;

        public string Documento { get; set; } =
            string.Empty;

        public string Estado { get; set; } =
            string.Empty;

        public string Observacion { get; set; } =
            string.Empty;

        public decimal Subtotal { get; set; }

        public decimal Descuento { get; set; }

        public decimal Impuesto { get; set; }

        public decimal Total { get; set; }

        public long? IdCotizacion { get; set; }
    }

    // =========================================================
    // MODELO DE PRODUCTOS
    // =========================================================

    public sealed class LineaVenta
    {
        public int IdProducto { get; set; }

        public string Producto { get; set; } =
            string.Empty;

        public decimal Cantidad { get; set; }

        public decimal PrecioUnitario { get; set; }

        public decimal Subtotal { get; set; }

        public decimal Descuento { get; set; }

        public decimal Impuesto { get; set; }

        public decimal Total { get; set; }
    }
}