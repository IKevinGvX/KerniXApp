namespace KerniX.Models.Clientes
{
    public class DireccionClienteModel
    {
        public int IdDireccionCliente { get; set; }

        public int IdCliente { get; set; }

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