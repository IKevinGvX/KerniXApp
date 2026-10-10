namespace KerniX.Models.IA;

public sealed record ResultadoConversacionIa(
    bool Ok,
    string Mensaje,
    int CodigoHttp = 200,
    string Proveedor = "",
    string Modelo = "");
