namespace MotoRent.Application.DTOs.Deliveryman
{
    public class DeliverymanDto
    {
        public string Id { get; set; }
        public string Identifier { get; set; }
        public string Name { get; set; }
        public string CNPJ { get; set; }
        public DateTime BirthDate { get; set; }
        public string LicenseNumber { get; set; }
        public string LicenseType { get; set; }
        public string LicenseImage { get; set; }
    }
}