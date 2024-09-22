using Microsoft.Extensions.Logging;
using MotoRent.Infrastructure.Data.Interfaces;
using MotoRent.Infrastructure.Data.Models;
using MotoRent.MessageConsumers.Events;

namespace MotoRent.MessageConsumers.Consumers
{
    public class MotorcycleCreatedConsumer : IMotorcycleCreatedConsumer
    {
        private readonly INotificationRepository _notificationRepository;
        private readonly ILogger<MotorcycleCreatedConsumer> _logger;

        public MotorcycleCreatedConsumer(INotificationRepository notificationRepository, ILogger<MotorcycleCreatedConsumer> logger)
        {
            _notificationRepository = notificationRepository ?? throw new ArgumentNullException(nameof(notificationRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task ConsumeAsync(IMotorcycleCreatedEvent @event)
        {
            if (@event.Year == 2024)
            {
                var notification = new NotificationModel
                {
                    Message = $"Nova moto de 2024 criada: {@event.Model} (Placa: {@event.LicensePlate})",
                    CreatedAt = DateTime.UtcNow
                };

                await _notificationRepository.CreateAsync(notification);

                _logger.LogInformation("Notificação criada para moto de 2024: {@Notification}", notification);
            }
        }
    }
}
