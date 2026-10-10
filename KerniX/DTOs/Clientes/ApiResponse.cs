using System.Text.Json;

namespace KerniX.Models.Clientes
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }

        public string? Message { get; set; }

        public T? Data { get; set; }

        public JsonElement? Errors { get; set; }
    }
}