using KerniX.Models.Inventario.Enterprise;
using KerniX.Services.Inventario;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace KerniX.Pages.Inventario.Reversiones;

public sealed class IndexModel : InventarioEnterprisePageModel
{
    private readonly IInventarioEnterpriseDbService _service;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(IInventarioEnterpriseDbService service, ILogger<IndexModel> logger)
    {
        _service = service;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public string? Buscar { get; set; }

    public List<MovimientoReversibleItem> Items { get; private set; } = new();

    public int Genericos => Items.Count(x => x.PermiteReversionGenerica);
    public int Protegidos => Items.Count(x => !x.PermiteReversionGenerica);

    public string? MensajeError { get; private set; }

    [TempData]
    public string? MensajeExito { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        if (!CargarSesion()) return IrLogin();
        await CargarAsync(ct);
        return Page();
    }

    public async Task<IActionResult> OnPostRevertirAsync(
        long idMovimiento,
        string motivo,
        CancellationToken ct)
    {
        if (!CargarSesion()) return IrLogin();

        _logger.LogInformation(
            "[REVERSA] POST Revertir recibido. Movimiento={Movimiento} Empleado={Empleado}",
            idMovimiento, IdEmpleado
        );

        try
        {
            if (idMovimiento <= 0)
                throw new InvalidOperationException("Movimiento inválido.");

            if (string.IsNullOrWhiteSpace(motivo))
                throw new InvalidOperationException("El motivo es obligatorio.");

            ReversionMovimientoResultado r =
                await _service.RevertirMovimientoAsync(
                    idMovimiento,
                    IdEmpleado,
                    motivo,
                    false,
                    ct
                );

            TempData["MensajeExito"] =
                $"Movimiento #{r.IdMovimientoOrigen} revertido con #{r.IdMovimientoReversa}. " +
                $"Stock {r.StockReversaAnterior:N2} → {r.StockReversaNuevo:N2}.";

            return RedirectToPage();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[REVERSA] Error.");
            MensajeError = ex is SqlException sql ? $"SQL {sql.Number}: {sql.Message}" : ex.Message;
        }

        await CargarAsync(ct);
        return Page();
    }

    private async Task CargarAsync(CancellationToken ct)
    {
        List<MovimientoReversibleItem> todos =
            await _service.ListarReversiblesAsync(
                IdEmpresa,
                IdSucursal,
                300,
                ct
            );

        if (string.IsNullOrWhiteSpace(Buscar))
        {
            Items = todos;
            return;
        }

        string q = Buscar.Trim();

        Items = todos
            .Where(x =>
                x.IdMovimientoInventario.ToString().Contains(q, StringComparison.OrdinalIgnoreCase)
                || x.CodigoMovimiento.Contains(q, StringComparison.OrdinalIgnoreCase)
                || x.CodigoProducto.Contains(q, StringComparison.OrdinalIgnoreCase)
                || x.Producto.Contains(q, StringComparison.OrdinalIgnoreCase)
                || x.Almacen.Contains(q, StringComparison.OrdinalIgnoreCase)
                || (x.Referencia?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false))
            .ToList();
    }
}
