namespace MotoRent.Infrastructure.Data.Interfaces
{
    public interface IMessagePublisher
    {
        Task PublishAsync<T>(string topic, T message);
    }
}