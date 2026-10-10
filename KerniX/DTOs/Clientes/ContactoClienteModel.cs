namespace KerniX.Models.Clientes
{
    public class ContactoClienteModel
    {
        public int IdContactoCliente { get; set; }

        public int IdCliente { get; set; }

        public string Nombres { get; set; } = "";

        public string Cargo { get; set; } = "";

        public string Telefono { get; set; } = "";

        public string Correo { get; set; } = "";

        public bool EsPrincipal { get; set; }

        public bool Estado { get; set; } = true;
    }
}