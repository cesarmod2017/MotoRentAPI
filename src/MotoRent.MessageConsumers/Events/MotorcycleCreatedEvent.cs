namespace MotoRent.MessageConsumers.Events
{
    public class MotorcycleCreatedEvent : IMotorcycleCreatedEvent
    {
        public string Id { get; set; }
        public string Identifier { get; set; }
        public int Year { get; set; }
        public string Model { get; set; }
        public string LicensePlate { get; set; }
    }
}