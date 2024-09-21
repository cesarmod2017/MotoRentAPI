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
            _notificationRepository = notificationRepository;
            _logger = logger;
        }

        public async Task ConsumeAsync(IMotorcycleCreatedEvent @event)
        {
            if (@event.Year == 2024)
            {
                var notification = new NotificationModel
                {
                    Message = $"New 2024 motorcycle created: {@event.Model} (Plate: {@event.LicensePlate})",
                    CreatedAt = DateTime.UtcNow
                };

                await _notificationRepository.CreateAsync(notification);

                _logger.LogInformation("Notification created for 2024 motorcycle: {@Notification}", notification);
            }
        }
    }
}