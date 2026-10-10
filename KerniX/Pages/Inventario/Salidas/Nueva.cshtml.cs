using System.Globalization;

using KerniX.Models.Inventario.SalidaStock;
using KerniX.Security;
using KerniX.Services.Inventario;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Primitives;

namespace KerniX.Pages.Inventario.Salidas;

public sealed class NuevaModel : PageModel
{
    private readonly ISalidaStockDbService _salidaStockService;
    private readonly ILogger<NuevaModel> _logger;


    public NuevaModel(
        ISalidaStockDbService salidaStockService,
        ILogger<NuevaModel> logger)
    {
        _salidaStockService =
            salidaStockService;

        _logger =
            logger;
    }


    [BindProperty]
    public SalidaStockGuardarRequest Input { get; set; } =
        new()
        {
            Cantidad = 1M,
            CodigoMovimiento = "salida"
        };


    public SalidaStockCatalogos Catalogos { get; private set; } =
        new();


    public SalidaStockDisponible? StockSeleccionado { get; private set; }


    public int IdUsuario { get; private set; }

    public int IdEmpleado { get; private set; }

    public int IdEmpresa { get; private set; }

    public int IdSucursal { get; private set; }


    public string? MensajeError { get; private set; }


    // =========================================================
    // GET
    // =========================================================

    public async Task<IActionResult> OnGetAsync(
        CancellationToken cancellationToken)
    {
        if (!CargarContextoSesion())
        {
            return RedirectToPage(
                "/Account/Login"
            );
        }


        try
        {
            await CargarCatalogosAsync(
                cancellationToken
            );


            Input =
                new SalidaStockGuardarRequest
                {
                    IdEmpresa =
                        IdEmpresa,

                    IdSucursal =
                        IdSucursal,

                    IdEmpleado =
                        IdEmpleado,

                    Cantidad =
                        1M,

                    // La salida manual NO permite elegir
                    // ajuste, transferencia, devolución, etc.
                    CodigoMovimiento =
                        "salida"
                };


            _logger.LogInformation(
                "[SALIDA NUEVA] GET. Usuario={Usuario} Empleado={Empleado} Empresa={Empresa} Sucursal={Sucursal} Almacenes={Almacenes} Productos={Productos}",
                IdUsuario,
                IdEmpleado,
                IdEmpresa,
                IdSucursal,
                Catalogos.Almacenes.Count,
                Catalogos.Productos.Count
            );


            return Page();
        }
        catch (SqlException ex)
        {
            _logger.LogError(
                ex,
                "[SALIDA NUEVA] ERROR SQL cargando formulario. Numero={Numero} Procedimiento={Procedimiento} Linea={Linea}",
                ex.Number,
                ex.Procedure,
                ex.LineNumber
            );


            MensajeError =
                $"SQL {ex.Number} | {ex.Message}";


            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[SALIDA NUEVA] Error cargando formulario."
            );


            MensajeError =
                "No fue posible cargar los datos necesarios para registrar la salida.";


            return Page();
        }
    }


    // =========================================================
    // GET AJAX - STOCK
    // /Inventario/Salidas/Nueva?handler=Stock&idAlmacen=1&idProducto=14
    // =========================================================

    public async Task<IActionResult> OnGetStockAsync(
        int idAlmacen,
        int idProducto,
        CancellationToken cancellationToken)
    {
        if (!CargarContextoSesion())
        {
            return new JsonResult(
                new
                {
                    success = false,
                    message =
                        "La sesión ha finalizado."
                }
            )
            {
                StatusCode =
                    StatusCodes.Status401Unauthorized
            };
        }


        if (
            idAlmacen <= 0
            ||
            idProducto <= 0
        )
        {
            return new JsonResult(
                new
                {
                    success = false,
                    message =
                        "Selecciona un almacén y un producto."
                }
            )
            {
                StatusCode =
                    StatusCodes.Status400BadRequest
            };
        }


        try
        {
            SalidaStockDisponible? stock =
                await _salidaStockService
                    .ObtenerStockDisponibleAsync(
                        IdEmpresa,
                        idAlmacen,
                        idProducto,
                        cancellationToken
                    );


            if (stock is null)
            {
                return new JsonResult(
                    new
                    {
                        success = false,
                        message =
                            "No se encontró stock para el producto en el almacén seleccionado."
                    }
                )
                {
                    StatusCode =
                        StatusCodes.Status404NotFound
                };
            }


            return new JsonResult(
                new
                {
                    success = true,

                    data = new
                    {
                        stock.IdProducto,
                        stock.CodigoProducto,
                        stock.Producto,
                        stock.IdAlmacen,
                        stock.CodigoAlmacen,
                        stock.Almacen,
                        stock.StockActual,
                        stock.StockReservado,
                        stock.StockDisponible
                    }
                }
            );
        }
        catch (SqlException ex)
        {
            _logger.LogError(
                ex,
                "[SALIDA NUEVA] Error SQL consultando stock. Almacen={Almacen} Producto={Producto}",
                idAlmacen,
                idProducto
            );


            return new JsonResult(
                new
                {
                    success = false,
                    message =
                        $"SQL {ex.Number} | {ex.Message}"
                }
            )
            {
                StatusCode =
                    StatusCodes.Status400BadRequest
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[SALIDA NUEVA] Error consultando stock."
            );


            return new JsonResult(
                new
                {
                    success = false,
                    message =
                        ex.Message
                }
            )
            {
                StatusCode =
                    StatusCodes.Status400BadRequest
            };
        }
    }


    // =========================================================
    // POST
    // =========================================================

    public async Task<IActionResult> OnPostAsync(
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[SALIDA NUEVA] POST recibido."
        );


        if (!CargarContextoSesion())
        {
            _logger.LogWarning(
                "[SALIDA NUEVA] POST sin sesión válida."
            );


            return RedirectToPage(
                "/Account/Login"
            );
        }


        try
        {
            if (!Request.HasFormContentType)
            {
                throw new InvalidOperationException(
                    "La solicitud no contiene datos de formulario."
                );
            }


            IFormCollection form =
                await Request.ReadFormAsync(
                    cancellationToken
                );


            _logger.LogInformation(
                "[SALIDA NUEVA] Campos recibidos: {Campos}",
                string.Join(
                    ", ",
                    form.Keys
                )
            );


            Input =
                new SalidaStockGuardarRequest
                {
                    // Contexto sensible desde sesión.
                    IdEmpresa =
                        IdEmpresa,

                    IdSucursal =
                        IdSucursal,

                    IdEmpleado =
                        IdEmpleado,

                    IdAlmacen =
                        LeerInt(
                            form,
                            "Input.IdAlmacen"
                        ),

                    IdProducto =
                        LeerInt(
                            form,
                            "Input.IdProducto"
                        ),

                    Cantidad =
                        LeerDecimal(
                            form,
                            "Input.Cantidad"
                        ),

                    Referencia =
                        LeerTextoNullable(
                            form,
                            "Input.Referencia"
                        ),

                    Motivo =
                        LeerTextoNullable(
                            form,
                            "Input.Motivo"
                        ),

                    // IMPORTANTE:
                    // No confiamos en un código enviado por HTML.
                    // Esta página representa exclusivamente
                    // una salida manual.
                    CodigoMovimiento =
                        "salida"
                };


            // Mismo patrón robusto usado en Nuevo Producto/Entrada.
            ModelState.Clear();


            await CargarCatalogosSeguroAsync(
                cancellationToken
            );


            await ValidarFormularioAsync(
                cancellationToken
            );


            _logger.LogInformation(
                "[SALIDA NUEVA] Datos POST. Usuario={Usuario} Empleado={Empleado} Empresa={Empresa} Sucursal={Sucursal} Almacen={Almacen} Producto={Producto} Cantidad={Cantidad} Referencia={Referencia}",
                IdUsuario,
                Input.IdEmpleado,
                Input.IdEmpresa,
                Input.IdSucursal,
                Input.IdAlmacen,
                Input.IdProducto,
                Input.Cantidad,
                Input.Referencia
            );


            if (!ModelState.IsValid)
            {
                LogModelState();


                MensajeError =
                    "Existen datos inválidos. Revisa los mensajes mostrados en el formulario.";


                return Page();
            }


            SalidaStockResultado resultado =
                await _salidaStockService
                    .RegistrarAsync(
                        Input,
                        cancellationToken
                    );


            _logger.LogInformation(
                "[SALIDA NUEVA] Salida registrada. Movimiento={Movimiento} Numero={Numero} StockAnterior={Anterior} StockNuevo={Nuevo}",
                resultado.IdMovimientoInventario,
                resultado.NumeroSalida,
                resultado.StockAnterior,
                resultado.StockNuevo
            );


            TempData["SalidaStockExito"] =
                $"Salida {resultado.NumeroSalida} registrada correctamente. " +
                $"Stock: {resultado.StockAnterior:0.####} → {resultado.StockNuevo:0.####}.";


            return RedirectToPage(
                "./Index"
            );
        }
        catch (SqlException ex)
        {
            _logger.LogError(
                ex,
                "[SALIDA NUEVA] ERROR SQL. Numero={Numero} Procedimiento={Procedimiento} Linea={Linea} Mensaje={Mensaje}",
                ex.Number,
                ex.Procedure,
                ex.LineNumber,
                ex.Message
            );


            await CargarCatalogosSeguroAsync(
                cancellationToken
            );


            await CargarStockSeleccionadoSeguroAsync(
                cancellationToken
            );


            MensajeError =
                $"SQL {ex.Number} | {ex.Message}";


            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[SALIDA NUEVA] Error registrando salida."
            );


            await CargarCatalogosSeguroAsync(
                cancellationToken
            );


            await CargarStockSeleccionadoSeguroAsync(
                cancellationToken
            );


            MensajeError =
                ex.Message;


            return Page();
        }
    }


    // =========================================================
    // VALIDACIÓN
    // =========================================================

    private async Task ValidarFormularioAsync(
        CancellationToken cancellationToken)
    {
        if (Input.IdEmpresa <= 0)
        {
            ModelState.AddModelError(
                "Input.IdEmpresa",
                "No se pudo identificar la empresa."
            );
        }


        if (Input.IdSucursal <= 0)
        {
            ModelState.AddModelError(
                "Input.IdSucursal",
                "No se pudo identificar la sucursal."
            );
        }


        if (Input.IdEmpleado <= 0)
        {
            ModelState.AddModelError(
                "Input.IdEmpleado",
                "No se pudo identificar al empleado."
            );
        }


        if (Input.IdAlmacen <= 0)
        {
            ModelState.AddModelError(
                "Input.IdAlmacen",
                "Selecciona un almacén."
            );
        }


        if (Input.IdProducto <= 0)
        {
            ModelState.AddModelError(
                "Input.IdProducto",
                "Selecciona un producto."
            );
        }


        if (Input.Cantidad <= 0)
        {
            ModelState.AddModelError(
                "Input.Cantidad",
                "La cantidad debe ser mayor que cero."
            );
        }


        if (
            Input.Referencia?.Length > 100
        )
        {
            ModelState.AddModelError(
                "Input.Referencia",
                "La referencia no puede superar los 100 caracteres."
            );
        }


        if (
            Input.Motivo?.Length > 300
        )
        {
            ModelState.AddModelError(
                "Input.Motivo",
                "El motivo no puede superar los 300 caracteres."
            );
        }


        if (
            !string.Equals(
                Input.CodigoMovimiento,
                "salida",
                StringComparison.OrdinalIgnoreCase
            )
        )
        {
            ModelState.AddModelError(
                "Input.CodigoMovimiento",
                "Esta pantalla solo permite salidas manuales."
            );
        }


        if (
            Input.IdAlmacen > 0
            &&
            Catalogos.Almacenes.Count > 0
            &&
            !Catalogos.Almacenes.Any(
                x =>
                    x.IdAlmacen
                    ==
                    Input.IdAlmacen
            )
        )
        {
            ModelState.AddModelError(
                "Input.IdAlmacen",
                "El almacén seleccionado no está disponible para esta sucursal."
            );
        }


        if (
            Input.IdProducto > 0
            &&
            Catalogos.Productos.Count > 0
            &&
            !Catalogos.Productos.Any(
                x =>
                    x.IdProducto
                    ==
                    Input.IdProducto
            )
        )
        {
            ModelState.AddModelError(
                "Input.IdProducto",
                "El producto seleccionado no está disponible o no controla stock."
            );
        }


        if (
            Input.IdAlmacen <= 0
            ||
            Input.IdProducto <= 0
            ||
            Input.Cantidad <= 0
        )
        {
            return;
        }


        StockSeleccionado =
            await _salidaStockService
                .ObtenerStockDisponibleAsync(
                    IdEmpresa,
                    Input.IdAlmacen,
                    Input.IdProducto,
                    cancellationToken
                );


        if (StockSeleccionado is null)
        {
            ModelState.AddModelError(
                "Input.IdProducto",
                "No existe stock del producto en el almacén seleccionado."
            );

            return;
        }


        if (
            Input.Cantidad
            >
            StockSeleccionado.StockDisponible
        )
        {
            ModelState.AddModelError(
                "Input.Cantidad",
                $"Stock insuficiente. Disponible: {StockSeleccionado.StockDisponible:0.####}."
            );
        }
    }


    // =========================================================
    // CATÁLOGOS
    // =========================================================

    private async Task CargarCatalogosAsync(
        CancellationToken cancellationToken)
    {
        Catalogos =
            await _salidaStockService
                .ObtenerCatalogosAsync(
                    IdEmpresa,
                    IdSucursal,
                    Input.IdAlmacen > 0
                        ? Input.IdAlmacen
                        : null,
                    cancellationToken
                );
    }


    private async Task CargarCatalogosSeguroAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            await CargarCatalogosAsync(
                cancellationToken
            );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "[SALIDA NUEVA] No se pudieron recargar los catálogos."
            );
        }
    }


    private async Task CargarStockSeleccionadoSeguroAsync(
        CancellationToken cancellationToken)
    {
        if (
            Input.IdAlmacen <= 0
            ||
            Input.IdProducto <= 0
        )
        {
            return;
        }


        try
        {
            StockSeleccionado =
                await _salidaStockService
                    .ObtenerStockDisponibleAsync(
                        IdEmpresa,
                        Input.IdAlmacen,
                        Input.IdProducto,
                        cancellationToken
                    );
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "[SALIDA NUEVA] No se pudo recargar el stock seleccionado."
            );
        }
    }


    // =========================================================
    // SESIÓN
    // =========================================================

    private bool CargarContextoSesion()
    {
        IdUsuario =
            ObtenerEnteroSesion(
                SessionKeys.IdUsuario
            );


        // MISMA CLAVE DEL LOGIN.
        IdEmpleado =
            ObtenerEnteroSesion(
                SessionKeys.IdEmpleado
            );


        IdEmpresa =
            ObtenerEnteroSesion(
                "IdEmpresa"
            );


        IdSucursal =
            ObtenerEnteroSesion(
                "IdSucursal"
            );


        if (IdUsuario <= 0)
        {
            return false;
        }


        if (IdEmpleado <= 0)
        {
            _logger.LogWarning(
                "[SALIDA NUEVA] Sesión sin SessionKeys.IdEmpleado. Usuario={Usuario}",
                IdUsuario
            );


            return false;
        }


        if (IdEmpresa <= 0)
        {
            IdEmpresa = 1;
        }


        if (IdSucursal <= 0)
        {
            IdSucursal = 1;
        }


        return true;
    }


    private int ObtenerEnteroSesion(
        string clave)
    {
        int? entero =
            HttpContext
                .Session
                .GetInt32(
                    clave
                );


        if (entero.HasValue)
        {
            return entero.Value;
        }


        string? texto =
            HttpContext
                .Session
                .GetString(
                    clave
                );


        return int.TryParse(
            texto,
            out int resultado
        )
            ? resultado
            : 0;
    }


    // =========================================================
    // FORM
    // =========================================================

    private static string LeerTexto(
        IFormCollection form,
        string clave)
    {
        return form.TryGetValue(
            clave,
            out StringValues valor
        )
            ? valor.ToString().Trim()
            : string.Empty;
    }


    private static string? LeerTextoNullable(
        IFormCollection form,
        string clave)
    {
        string valor =
            LeerTexto(
                form,
                clave
            );


        return string.IsNullOrWhiteSpace(
            valor
        )
            ? null
            : valor;
    }


    private static int LeerInt(
        IFormCollection form,
        string clave)
    {
        string valor =
            LeerTexto(
                form,
                clave
            );


        return int.TryParse(
            valor,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out int resultado
        )
            ? resultado
            : 0;
    }


    private static decimal LeerDecimal(
        IFormCollection form,
        string clave)
    {
        string valor =
            LeerTexto(
                form,
                clave
            );


        if (
            decimal.TryParse(
                valor,
                NumberStyles.Number,
                CultureInfo.InvariantCulture,
                out decimal invariant
            )
        )
        {
            return invariant;
        }


        if (
            decimal.TryParse(
                valor,
                NumberStyles.Number,
                CultureInfo.CurrentCulture,
                out decimal current
            )
        )
        {
            return current;
        }


        return 0M;
    }


    private void LogModelState()
    {
        foreach (
            KeyValuePair<string, ModelStateEntry?> item
            in ModelState
        )
        {
            if (
                item.Value is null
                ||
                item.Value.Errors.Count == 0
            )
            {
                continue;
            }


            foreach (
                ModelError error
                in item.Value.Errors
            )
            {
                string mensaje =
                    !string.IsNullOrWhiteSpace(
                        error.ErrorMessage
                    )
                        ? error.ErrorMessage
                        : error.Exception?.Message
                          ?? "Valor inválido.";


                _logger.LogWarning(
                    "[SALIDA NUEVA] ModelState inválido. Campo={Campo} Error={Error}",
                    item.Key,
                    mensaje
                );
            }
        }
    }


    public string DecimalHtml(
        decimal valor)
    {
        return valor.ToString(
            "0.####",
            CultureInfo.InvariantCulture
        );
    }
}
