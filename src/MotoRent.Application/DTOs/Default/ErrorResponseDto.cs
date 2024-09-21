using System.Text.Json.Serialization;

namespace MotoRent.Application.DTOs.Default
{
    public class ErrorResponseDto
    {
        [JsonPropertyName("mensagem")]
        public string Message { get; set; }
    }
}
