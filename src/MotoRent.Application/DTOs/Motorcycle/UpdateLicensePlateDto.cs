using System.Text.Json.Serialization;

namespace MotoRent.Application.DTOs.Motorcycle
{
    public class UpdateLicensePlateDto
    {
        [JsonPropertyName("placa")]
        public string LicensePlate { get; set; }
    }
}