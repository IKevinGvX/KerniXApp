namespace KerniX.Models.Usuarios;

public sealed class ApiOperationResult
{
    public bool Success { get; set; }

    public string Message { get; set; }
        = string.Empty;
}