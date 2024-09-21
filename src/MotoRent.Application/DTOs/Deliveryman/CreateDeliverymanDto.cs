using System.Text.Json.Serialization;

namespace MotoRent.Application.DTOs.Deliveryman
{
    public class CreateDeliverymanDto
    {
        [JsonPropertyName("identificador")]
        public string Identifier { get; set; }

        [JsonPropertyName("nome")]
        public string Name { get; set; }

        [JsonPropertyName("cnpj")]
        public string CNPJ { get; set; }

        [JsonPropertyName("data_nascimento")]
        public DateTime BirthDate { get; set; }

        [JsonPropertyName("numero_cnh")]
        public string LicenseNumber { get; set; }

        [JsonPropertyName("tipo_cnh")]
        public string LicenseType { get; set; }

        [JsonPropertyName("imagem_cnh")]
        public string LicenseImage { get; set; }
    }
}