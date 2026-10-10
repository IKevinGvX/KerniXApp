using System.Globalization;

using KerniX.Models.Inventario.EntradaStock;
using KerniX.Security;
using KerniX.Services.Inventario;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Primitives;

namespace KerniX.Pages.Inventario.Entradas;

public sealed class NuevaModel : PageModel
{
    private readonly IEntradaStockDbService _entradaStockService;
    private readonly ILogger<NuevaModel> _logger;

    public NuevaModel(
        IEntradaStockDbService entradaStockService,
        ILogger<NuevaModel> logger)
    {
        _entradaStockService = entradaStockService;
        _logger = logger;
    }

    [BindProperty]
    public EntradaStockGuardarRequest Input { get; set; } = new()
    {
        Cantidad = 1M,
        CodigoMovimiento = "entrada"
    };

    public EntradaStockCatalogos Catalogos { get; private set; } = new();

    public int IdUsuario { get; private set; }
    public int IdEmpleado { get; private set; }
    public int IdEmpresa { get; private set; }
    public int IdSucursal { get; private set; }

    public string? MensajeError { get; private set; }

    public async Task<IActionResult> OnGetAsync(
        CancellationToken cancellationToken)
    {
        if (!CargarContextoSesion())
        {
            return RedirectToPage("/Account/Login");
        }

        try
        {
            await CargarCatalogosAsync(cancellationToken);

            Input = new EntradaStockGuardarRequest
            {
                IdEmpresa = IdEmpresa,
                IdSucursal = IdSucursal,
                IdEmpleado = IdEmpleado,
                Cantidad = 1M,
                CodigoMovimiento =
                    Catalogos.TiposMovimiento
                        .FirstOrDefault(x =>
                            string.Equals(
                                x.Codigo,
                                "entrada",
                                StringComparison.OrdinalIgnoreCase))
                        ?.Codigo
                    ?? "entrada"
            };

            _logger.LogInformation(
                "[ENTRADA NUEVA] GET. Usuario={Usuario} Empleado={Empleado} Empresa={Empresa} Sucursal={Sucursal} Almacenes={Almacenes} Productos={Productos}",
                IdUsuario,
                IdEmpleado,
                IdEmpresa,
                IdSucursal,
                Catalogos.Almacenes.Count,
                Catalogos.Productos.Count
            );

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[ENTRADA NUEVA] Error cargando formulario."
            );

            MensajeError =
                "No fue posible cargar los datos necesarios para registrar la entrada.";

            return Page();
        }
    }

    public async Task<IActionResult> OnPostAsync(
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[ENTRADA NUEVA] POST recibido."
        );

        if (!CargarContextoSesion())
        {
            _logger.LogWarning(
                "[ENTRADA NUEVA] POST sin sesión válida."
            );

            return RedirectToPage("/Account/Login");
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
                await Request.ReadFormAsync(cancellationToken);

            _logger.LogInformation(
                "[ENTRADA NUEVA] Campos recibidos: {Campos}",
                string.Join(", ", form.Keys)
            );

            Input = new EntradaStockGuardarRequest
            {
                // El contexto sensible SIEMPRE viene de sesión.
                IdEmpresa = IdEmpresa,
                IdSucursal = IdSucursal,
                IdEmpleado = IdEmpleado,

                IdAlmacen =
                    LeerInt(form, "Input.IdAlmacen"),

                IdProducto =
                    LeerInt(form, "Input.IdProducto"),

                Cantidad =
                    LeerDecimal(form, "Input.Cantidad"),

                Referencia =
                    LeerTextoNullable(form, "Input.Referencia"),

                Motivo =
                    LeerTextoNullable(form, "Input.Motivo"),

                CodigoMovimiento =
                    LeerTexto(form, "Input.CodigoMovimiento")
            };

            if (string.IsNullOrWhiteSpace(Input.CodigoMovimiento))
            {
                Input.CodigoMovimiento = "entrada";
            }

            // Igual que Nuevo Producto: ignoramos errores previos
            // del binder y validamos el request reconstruido.
            ModelState.Clear();

            await CargarCatalogosSeguroAsync(cancellationToken);

            ValidarFormulario();

            _logger.LogInformation(
                "[ENTRADA NUEVA] Datos POST. Usuario={Usuario} Empleado={Empleado} Empresa={Empresa} Sucursal={Sucursal} Almacen={Almacen} Producto={Producto} Cantidad={Cantidad} Tipo={Tipo} Referencia={Referencia}",
                IdUsuario,
                Input.IdEmpleado,
                Input.IdEmpresa,
                Input.IdSucursal,
                Input.IdAlmacen,
                Input.IdProducto,
                Input.Cantidad,
                Input.CodigoMovimiento,
                Input.Referencia
            );

            if (!ModelState.IsValid)
            {
                LogModelState();

                MensajeError =
                    "Existen datos inválidos. Revisa los mensajes mostrados en el formulario.";

                return Page();
            }

            EntradaStockResultado resultado =
                await _entradaStockService.RegistrarAsync(
                    Input,
                    cancellationToken
                );

            _logger.LogInformation(
                "[ENTRADA NUEVA] Entrada registrada. Movimiento={Movimiento} Numero={Numero} StockAnterior={Anterior} StockNuevo={Nuevo}",
                resultado.IdMovimientoInventario,
                resultado.NumeroEntrada,
                resultado.StockAnterior,
                resultado.StockNuevo
            );

            TempData["EntradaStockExito"] =
                $"Entrada {resultado.NumeroEntrada} registrada correctamente. " +
                $"Stock: {resultado.StockAnterior:0.####} → {resultado.StockNuevo:0.####}.";

            return RedirectToPage("./Index");
        }
        catch (SqlException ex)
        {
            _logger.LogError(
                ex,
                "[ENTRADA NUEVA] ERROR SQL. Numero={Numero} Procedimiento={Procedimiento} Linea={Linea} Mensaje={Mensaje}",
                ex.Number,
                ex.Procedure,
                ex.LineNumber,
                ex.Message
            );

            await CargarCatalogosSeguroAsync(cancellationToken);

            MensajeError =
                $"SQL {ex.Number} | {ex.Message}";

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "[ENTRADA NUEVA] Error registrando entrada."
            );

            await CargarCatalogosSeguroAsync(cancellationToken);

            MensajeError = ex.Message;

            return Page();
        }
    }

    private void ValidarFormulario()
    {
        if (Input.IdEmpresa <= 0)
            ModelState.AddModelError(
                "Input.IdEmpresa",
                "No se pudo identificar la empresa."
            );

        if (Input.IdSucursal <= 0)
            ModelState.AddModelError(
                "Input.IdSucursal",
                "No se pudo identificar la sucursal."
            );

        if (Input.IdEmpleado <= 0)
            ModelState.AddModelError(
                "Input.IdEmpleado",
                "No se pudo identificar al empleado."
            );

        if (Input.IdAlmacen <= 0)
            ModelState.AddModelError(
                "Input.IdAlmacen",
                "Selecciona un almacén."
            );

        if (Input.IdProducto <= 0)
            ModelState.AddModelError(
                "Input.IdProducto",
                "Selecciona un producto."
            );

        if (Input.Cantidad <= 0)
            ModelState.AddModelError(
                "Input.Cantidad",
                "La cantidad debe ser mayor que cero."
            );

        if (Input.Referencia?.Length > 100)
            ModelState.AddModelError(
                "Input.Referencia",
                "La referencia no puede superar los 100 caracteres."
            );

        if (Input.Motivo?.Length > 300)
            ModelState.AddModelError(
                "Input.Motivo",
                "El motivo no puede superar los 300 caracteres."
            );

        if (string.IsNullOrWhiteSpace(Input.CodigoMovimiento))
            ModelState.AddModelError(
                "Input.CodigoMovimiento",
                "Selecciona un tipo de entrada."
            );

        if (
            Catalogos.Almacenes.Count > 0 &&
            !Catalogos.Almacenes.Any(x => x.IdAlmacen == Input.IdAlmacen)
        )
        {
            ModelState.AddModelError(
                "Input.IdAlmacen",
                "El almacén seleccionado no está disponible para esta sucursal."
            );
        }

        if (
            Catalogos.Productos.Count > 0 &&
            !Catalogos.Productos.Any(x => x.IdProducto == Input.IdProducto)
        )
        {
            ModelState.AddModelError(
                "Input.IdProducto",
                "El producto seleccionado no está disponible o no controla stock."
            );
        }

        if (
            Catalogos.TiposMovimiento.Count > 0 &&
            !Catalogos.TiposMovimiento.Any(x =>
                string.Equals(
                    x.Codigo,
                    Input.CodigoMovimiento,
                    StringComparison.OrdinalIgnoreCase))
        )
        {
            ModelState.AddModelError(
                "Input.CodigoMovimiento",
                "El tipo de entrada seleccionado no está disponible."
            );
        }
    }

    private async Task CargarCatalogosAsync(
        CancellationToken cancellationToken)
    {
        Catalogos =
            await _entradaStockService.ObtenerCatalogosAsync(
                IdEmpresa,
                IdSucursal,
                cancellationToken
            );
    }

    private async Task CargarCatalogosSeguroAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            await CargarCatalogosAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "[ENTRADA NUEVA] No se pudieron recargar los catálogos."
            );
        }
    }

    private bool CargarContextoSesion()
    {
        IdUsuario =
            ObtenerEnteroSesion(SessionKeys.IdUsuario);

        IdEmpleado =
            ObtenerEnteroSesion(SessionKeys.IdEmpleado);

        IdEmpresa =
            ObtenerEnteroSesion("IdEmpresa");

        IdSucursal =
            ObtenerEnteroSesion("IdSucursal");

        if (IdUsuario <= 0)
            return false;

        // La entrada SQL exige un empleado real.
        if (IdEmpleado <= 0)
        {
            _logger.LogWarning(
                "[ENTRADA NUEVA] Sesión sin IdEmpleado. Usuario={Usuario}",
                IdUsuario
            );

            return false;
        }

        if (IdEmpresa <= 0)
            IdEmpresa = 1;

        if (IdSucursal <= 0)
            IdSucursal = 1;

        return true;
    }

    private int ObtenerEnteroSesion(string clave)
    {
        int? entero = HttpContext.Session.GetInt32(clave);

        if (entero.HasValue)
            return entero.Value;

        string? texto = HttpContext.Session.GetString(clave);

        return int.TryParse(texto, out int resultado)
            ? resultado
            : 0;
    }

    private static string LeerTexto(
        IFormCollection form,
        string clave)
    {
        return form.TryGetValue(clave, out StringValues valor)
            ? valor.ToString().Trim()
            : string.Empty;
    }

    private static string? LeerTextoNullable(
        IFormCollection form,
        string clave)
    {
        string valor = LeerTexto(form, clave);

        return string.IsNullOrWhiteSpace(valor)
            ? null
            : valor;
    }

    private static int LeerInt(
        IFormCollection form,
        string clave)
    {
        string valor = LeerTexto(form, clave);

        return int.TryParse(
            valor,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out int resultado)
            ? resultado
            : 0;
    }

    private static decimal LeerDecimal(
        IFormCollection form,
        string clave)
    {
        string valor = LeerTexto(form, clave);

        if (decimal.TryParse(
            valor,
            NumberStyles.Number,
            CultureInfo.InvariantCulture,
            out decimal invariant))
        {
            return invariant;
        }

        if (decimal.TryParse(
            valor,
            NumberStyles.Number,
            CultureInfo.CurrentCulture,
            out decimal current))
        {
            return current;
        }

        return 0M;
    }

    private void LogModelState()
    {
        foreach (KeyValuePair<string, ModelStateEntry?> item in ModelState)
        {
            if (item.Value == null || item.Value.Errors.Count == 0)
                continue;

            foreach (ModelError error in item.Value.Errors)
            {
                string mensaje =
                    !string.IsNullOrWhiteSpace(error.ErrorMessage)
                        ? error.ErrorMessage
                        : error.Exception?.Message ?? "Valor inválido.";

                _logger.LogWarning(
                    "[ENTRADA NUEVA] ModelState inválido. Campo={Campo} Error={Error}",
                    item.Key,
                    mensaje
                );
            }
        }
    }

    public string DecimalHtml(decimal valor)
    {
        return valor.ToString(
            "0.####",
            CultureInfo.InvariantCulture
        );
    }
}
