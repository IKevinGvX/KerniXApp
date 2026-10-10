using KerniX.DTOs.Auth;
using System.Threading;
using System.Threading.Tasks;
using KerniX.DTOs.Auth;
using KerniX.DTOs.Common;

namespace KerniX.Services.Auth;

public interface IAuthApiService
{
    Task<ApiResponse<LoginResponse>?> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken);

    // Opcional: se utiliza solo si ApiSettings:RefreshPath está configurado
    // y la API realmente ofrece la renovación de tokens.
    Task<ApiResponse<LoginResponse>?> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken);
}
