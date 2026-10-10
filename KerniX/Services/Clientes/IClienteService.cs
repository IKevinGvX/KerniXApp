using KerniX.Models.Clientes;

namespace KerniX.Services.Clientes
{
    public interface IClienteService
    {
        Task<ApiResponse<ClienteListadoModel>> ListarAsync();

        Task<ApiResponse<ClienteModel>> ObtenerAsync(
            int idCliente);

        Task<ApiResponse<ClienteModel>> RegistrarAsync(
            ClienteRequest request);

        Task<ApiResponse<ClienteModel>> ActualizarAsync(
            int idCliente,
            ClienteRequest request);

        Task<ApiResponse<bool>> ActivarAsync(
            int idCliente);

        Task<ApiResponse<bool>> DesactivarAsync(
            int idCliente);
    }
}