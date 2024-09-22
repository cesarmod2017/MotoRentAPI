using Microsoft.Extensions.Configuration;
using MotoRent.MessageConsumers.Events;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace MotoRent.MessageConsumers.Services
{
    public class RabbitMQService : IMessageService
    {
        private readonly ConnectionFactory _factory;
        private readonly string _queueName = "error_logs";

        public RabbitMQService(IConfiguration configuration)
        {
            _factory = new ConnectionFactory()
            {
                HostName = configuration["RabbitMQ:HostName"],
                UserName = configuration["RabbitMQ:UserName"],
                Password = configuration["RabbitMQ:Password"]
            };
        }

        public async Task PublishAsync<T>(string topic, T message)
        {
            using (var connection = _factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: topic,
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                var json = JsonSerializer.Serialize(message);
                var body = Encoding.UTF8.GetBytes(json);

                channel.BasicPublish(exchange: "",
                                     routingKey: topic,
                                     basicProperties: null,
                                     body: body);
            }

            await Task.CompletedTask;
        }

        public async Task PublishErrorLogAsync(string message)
        {
            using (var connection = _factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: _queueName,
                                     durable: false,
                                     exclusive: false,
                                     autoDelete: false,
                                     arguments: null);

                var body = Encoding.UTF8.GetBytes(message);

                channel.BasicPublish(exchange: "",
                                     routingKey: _queueName,
                                     basicProperties: null,
                                     body: body);
            }

            await Task.CompletedTask;
        }

        public async Task<IMotorcycleCreatedEvent> ReceiveAsync(string topic, CancellationToken cancellationToken)
        {
            using (var connection = _factory.CreateConnection())
            using (var channel = connection.CreateModel())
            {
                channel.QueueDeclare(queue: topic, durable: false, exclusive: false, autoDelete: false, arguments: null);

                var consumer = new EventingBasicConsumer(channel);
                var tcs = new TaskCompletionSource<IMotorcycleCreatedEvent>();

                consumer.Received += (model, ea) =>
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var motorcycleCreatedEvent = JsonSerializer.Deserialize<MotorcycleCreatedEvent>(message);
                    tcs.SetResult(motorcycleCreatedEvent);
                };

                var consumerTag = channel.BasicConsume(queue: topic, autoAck: true, consumer: consumer);

                using (cancellationToken.Register(() => channel.BasicCancel(consumerTag)))
                {
                    return await tcs.Task;
                }
            }
        }
    }
}