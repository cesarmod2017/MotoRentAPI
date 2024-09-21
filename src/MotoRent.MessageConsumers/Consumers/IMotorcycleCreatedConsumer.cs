using MotoRent.MessageConsumers.Events;

namespace MotoRent.MessageConsumers.Consumers
{
    public interface IMotorcycleCreatedConsumer
    {
        Task ConsumeAsync(IMotorcycleCreatedEvent @event);
    }
}