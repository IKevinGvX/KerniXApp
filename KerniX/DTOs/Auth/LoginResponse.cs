namespace KerniX.DTOs.Auth;

public sealed class LoginResponse
{
    public string AccessToken { get; set; }
        = string.Empty;

    public string? RefreshToken { get; set; }

    public string Usuario { get; set; }
        = string.Empty;
}