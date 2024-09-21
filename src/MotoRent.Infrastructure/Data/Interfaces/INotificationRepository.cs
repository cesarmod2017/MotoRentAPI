using MotoRent.Infrastructure.Data.Models;

namespace MotoRent.Infrastructure.Data.Interfaces
{
    public interface INotificationRepository : IRepository<NotificationModel>
    {
        // Você pode adicionar métodos específicos para notificações aqui, se necessário
    }
}