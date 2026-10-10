using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace KerniX.Models.Proveedores;

public sealed class ProveedorFormulario : IValidatableObject
{
    [Required(ErrorMessage = "Selecciona un tipo de documento.")]
    [StringLength(20, ErrorMessage = "El tipo de documento no debe superar 20 caracteres.")]
    public string TipoDocumento { get; set; } = "ruc";

    [Required(ErrorMessage = "Ingresa el número de documento.")]
    [StringLength(20, ErrorMessage = "El documento no debe superar 20 caracteres.")]
    public string NumeroDocumento { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ingresa la razón social.")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "La razón social debe tener entre 2 y 200 caracteres.")]
    public string RazonSocial { get; set; } = string.Empty;

    [StringLength(150, ErrorMessage = "El nombre comercial no debe superar 150 caracteres.")]
    public string? NombreComercial { get; set; }

    [StringLength(20, ErrorMessage = "El teléfono no debe superar 20 caracteres.")]
    public string? Telefono { get; set; }

    [EmailAddress(ErrorMessage = "Ingresa un correo válido.")]
    [StringLength(150, ErrorMessage = "El correo no debe superar 150 caracteres.")]
    public string? Correo { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var tipo = (TipoDocumento ?? "").Trim().ToLowerInvariant();
        var documento = (NumeroDocumento ?? "").Trim();
        if (tipo == "ruc" && !Regex.IsMatch(documento, @"^\d{11}$"))
            yield return new ValidationResult("El RUC requiere exactamente 11 dígitos.", new[] { nameof(NumeroDocumento) });
        else if (tipo == "dni" && !Regex.IsMatch(documento, @"^\d{8}$"))
            yield return new ValidationResult("El DNI requiere exactamente 8 dígitos.", new[] { nameof(NumeroDocumento) });
        else if (documento.Length > 0 && documento.Length < 3)
            yield return new ValidationResult("El documento es demasiado corto.", new[] { nameof(NumeroDocumento) });
    }
}
