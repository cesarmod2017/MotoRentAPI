using MotoRent.MessageConsumers.Consumers;
using MotoRent.MessageConsumers.Services;

namespace MotoRent.API.BackgroundServices
{
    public class MotorcycleCreatedBackgroundService : BackgroundService
    {
        private readonly IMessageService _messageService;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MotorcycleCreatedBackgroundService> _logger;

        public MotorcycleCreatedBackgroundService(
            IMessageService messageService,
            IServiceProvider serviceProvider,
            ILogger<MotorcycleCreatedBackgroundService> logger)
        {
            _messageService = messageService;
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("MotorcycleCreatedBackgroundService is starting.");

            stoppingToken.Register(() =>
                _logger.LogInformation("MotorcycleCreatedBackgroundService is stopping."));

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessMessageAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while processing message");
                }

                await Task.Delay(1000, stoppingToken); // Aguarda 1 segundo antes de processar a próxima mensagem
            }
        }

        private async Task ProcessMessageAsync(CancellationToken stoppingToken)
        {
            var message = await _messageService.ReceiveAsync("motorcycle-created", stoppingToken);

            if (message != null)
            {
                using (var scope = _serviceProvider.CreateScope())
                {
                    var consumer = scope.ServiceProvider.GetRequiredService<IMotorcycleCreatedConsumer>();
                    await consumer.ConsumeAsync(message);
                }
            }
        }
    }
}
