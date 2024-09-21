using MotoRent.Infrastructure.Data.Interfaces;
using MotoRent.Infrastructure.Data.Models;

namespace MotoRent.Infrastructure.Data.Repositories
{
    public class NotificationRepository : BaseRepository<NotificationModel>, INotificationRepository
    {
        public NotificationRepository(IMongoDbContext context) : base(context, "notifications")
        {
        }

        // Implemente métodos específicos para notificações aqui, se necessário
    }
}