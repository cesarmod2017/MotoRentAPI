using System.Text.Json.Serialization;

namespace MotoRent.Application.DTOs.Rental
{
    public class RentalCalculationResultDto
    {
        [JsonIgnore]
        public decimal TotalCost { get; set; }
        [JsonPropertyName("mensagem")]
        public string Message { get; set; }
    }
}