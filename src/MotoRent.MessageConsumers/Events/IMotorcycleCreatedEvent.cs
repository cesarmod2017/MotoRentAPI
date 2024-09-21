namespace MotoRent.MessageConsumers.Events
{
    public interface IMotorcycleCreatedEvent
    {
        string Id { get; }
        string Identifier { get; }
        int Year { get; }
        string Model { get; }
        string LicensePlate { get; }
    }
}