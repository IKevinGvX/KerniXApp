namespace KerniX.Models.Clientes
{
    public class ClienteModel
    {
        public int IdCliente { get; set; }

        public int IdEmpresa { get; set; }

        public string Empresa { get; set; } = "";

        public string TipoCliente { get; set; } = "";

        public string TipoDocumento { get; set; } = "";

        public string NumeroDocumento { get; set; } = "";

        public string? RazonSocial { get; set; }

        public string? Nombres { get; set; }

        public string? Apellidos { get; set; }

        public string NombreCompleto { get; set; } = "";

        public string? Telefono { get; set; }

        public string? Correo { get; set; }

        public bool Estado { get; set; }

        public DateTime FechaRegistro { get; set; }

        public int TotalContactos { get; set; }

        public int TotalDirecciones { get; set; }

        public int TotalVentas { get; set; }

        public List<ContactoClienteModel> Contactos { get; set; }
            = new();

        public List<DireccionClienteModel> Direcciones { get; set; }
            = new();

        public IndicadoresClienteModel? Indicadores { get; set; }

        public string NombreMostrar
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(RazonSocial))
                    return RazonSocial;

                if (!string.IsNullOrWhiteSpace(NombreCompleto))
                    return NombreCompleto;

                return $"{Nombres} {Apellidos}".Trim();
            }
        }
    }

    public class IndicadoresClienteModel
    {
        public int TotalVentas { get; set; }

        public decimal TotalComprado { get; set; }

        public DateTime? FechaUltimaCompra { get; set; }

        public int TotalContactos { get; set; }

        public int TotalDirecciones { get; set; }
    }

    public class ClienteListadoModel
    {
        public List<ClienteModel> Items { get; set; } = new();

        public int Total { get; set; }
    }
}