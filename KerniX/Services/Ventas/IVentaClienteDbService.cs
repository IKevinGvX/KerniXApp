using KerniX.Models.Ventas.Cliente;

namespace KerniX.Services.Ventas;

public interface IVentaClienteDbService
{
    Task<IReadOnlyList<VentaClienteBuscarResponse>> BuscarAsync(
        int idEmpresa,
        string? termino,
        int limite = 20);

    Task<VentaClienteResponse?> ObtenerPorIdAsync(
        int idEmpresa,
        int idCliente);
}