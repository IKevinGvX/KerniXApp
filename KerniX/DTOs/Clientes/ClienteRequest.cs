using System.ComponentModel.DataAnnotations;

namespace KerniX.Models.Clientes
{
    public class ClienteRequest
    {
        [Range(1, int.MaxValue,
            ErrorMessage = "La empresa es obligatoria.")]
        public int IdEmpresa { get; set; }

        [Required]
        [RegularExpression(
            "^(natural|juridico)$",
            ErrorMessage = "Tipo de cliente no válido.")]
        public string TipoCliente { get; set; } = "natural";

        [Required]
        public string TipoDocumento { get; set; } = "DNI";

        [Required]
        [MaxLength(20)]
        public string NumeroDocumento { get; set; } = "";

        public string? RazonSocial { get; set; }

        public string? Nombres { get; set; }

        public string? Apellidos { get; set; }

        public string? Telefono { get; set; }

        [EmailAddress]
        public string? Correo { get; set; }

        public bool Estado { get; set; } = true;

        public List<ContactoClienteRequest> Contactos { get; set; }
            = new();

        public List<DireccionClienteRequest> Direcciones { get; set; }
            = new();
    }

    public class ContactoClienteRequest
    {
        public string Nombres { get; set; } = "";

        public string Cargo { get; set; } = "";

        public string Telefono { get; set; } = "";

        public string Correo { get; set; } = "";

        public bool EsPrincipal { get; set; }

        public bool Estado { get; set; } = true;
    }

    public class DireccionClienteRequest
    {
        public string TipoDireccion { get; set; } = "";

        public string Direccion { get; set; } = "";

        public string Referencia { get; set; } = "";

        public string Distrito { get; set; } = "";

        public string Provincia { get; set; } = "";

        public string Departamento { get; set; } = "";

        public string Ubigeo { get; set; } = "";

        public bool EsPrincipal { get; set; }

        public bool Estado { get; set; } = true;
    }
}