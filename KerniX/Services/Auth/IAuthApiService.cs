using KerniX.DTOs.Auth;
using KerniX.DTOs.Common;

namespace KerniX.Services.Auth;

public interface IAuthApiService
{
    Task<ApiResponse<LoginResponse>?> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken);
}