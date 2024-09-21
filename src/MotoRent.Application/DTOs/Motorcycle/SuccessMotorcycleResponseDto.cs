using System.Text.Json.Serialization;

namespace MotoRent.Application.DTOs.Motorcycle
{
    public class SuccessMotorcycleResponseDto
    {
        [JsonPropertyName("mensagem")]
        public string Message { get; set; }
    }
}