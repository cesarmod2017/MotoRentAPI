using MotoRent.MessageConsumers.Events;

namespace MotoRent.MessageConsumers.Services
{
    public interface IMessageService
    {
        Task PublishErrorLogAsync(string message);
        Task PublishAsync<T>(string topic, T message);
        Task<IMotorcycleCreatedEvent> ReceiveAsync(string topic, CancellationToken cancellationToken);
    }
}