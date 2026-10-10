using KerniX.Models.Ventas.Operaciones;

namespace KerniX.Services.Ventas.Operaciones;

public interface IVentasRegistroService
{
    Task<RegistroOperacionResultado> RegistrarPagoAsync(
        int idUsuario, PagoRegistroInput input, CancellationToken ct = default);

    Task<RegistroOperacionResultado> RegistrarReclamoAsync(
        int idUsuario, ReclamoRegistroInput input, CancellationToken ct = default);
}
