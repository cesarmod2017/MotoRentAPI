using System.Text.Json.Serialization;

namespace MotoRent.Application.DTOs.Rental
{
    public class UpdateReturnDateDto
    {
        [JsonPropertyName("data_devolucao")]
        public DateTime ReturnDate { get; set; }
    }
}