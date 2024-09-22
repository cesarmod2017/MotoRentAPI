using MongoDB.Driver;
using MotoRent.Infrastructure.Data.Interfaces;
using MotoRent.Infrastructure.Data.Models;

namespace MotoRent.Infrastructure.Data.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly IMongoCollection<NotificationModel> _collection;

        public NotificationRepository(IMongoDbContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            _collection = context.GetCollection<NotificationModel>("notifications");
        }

        public async Task<NotificationModel> CreateAsync(NotificationModel notification)
        {
            if (notification == null)
                throw new ArgumentNullException(nameof(notification));

            await _collection.InsertOneAsync(notification);
            return notification;
        }

        public async Task<IEnumerable<NotificationModel>> GetAllAsync()
        {
            return await _collection.Find(_ => true).ToListAsync();
        }

        public async Task<NotificationModel> GetByIdAsync(string id)
        {
            return await _collection.Find(n => n.Id == id).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<NotificationModel>> GetRecentNotificationsAsync(int count)
        {
            return await _collection.Find(_ => true)
                                    .SortByDescending(n => n.CreatedAt)
                                    .Limit(count)
                                    .ToListAsync();
        }

        public async Task UpdateAsync(string id, NotificationModel notification)
        {
            if (notification == null)
                throw new ArgumentNullException(nameof(notification));

            await _collection.ReplaceOneAsync(n => n.Id == id, notification);
        }

        public async Task DeleteAsync(string id)
        {
            await _collection.DeleteOneAsync(n => n.Id == id);
        }

        public async Task<long> GetNotificationCountAsync()
        {
            return await _collection.CountDocumentsAsync(_ => true);
        }
    }
}