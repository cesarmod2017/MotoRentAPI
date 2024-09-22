using MotoRent.Infrastructure.Data.Models;

namespace MotoRent.Infrastructure.Data.Interfaces
{
    public interface INotificationRepository
    {
        Task<NotificationModel> CreateAsync(NotificationModel notification);
        Task<IEnumerable<NotificationModel>> GetAllAsync();
        Task<NotificationModel> GetByIdAsync(string id);
        Task<IEnumerable<NotificationModel>> GetRecentNotificationsAsync(int count);
        Task UpdateAsync(string id, NotificationModel notification);
        Task DeleteAsync(string id);
        Task<long> GetNotificationCountAsync();
    }
}