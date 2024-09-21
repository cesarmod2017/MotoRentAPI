using System.Text.Json.Serialization;

namespace MotoRent.Application.DTOs.Motorcycle
{
    public class UpdateLicenseImageDto
    {
        [JsonPropertyName("imagem_cnh")]
        public string LicenseImage { get; set; }
    }
}