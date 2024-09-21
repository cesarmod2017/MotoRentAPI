namespace MotoRent.Infrastructure.Data.Interfaces
{
    public interface IRepository<T> where T : class
    {
        Task<IEnumerable<T>> GetAllAsync();
        Task<T> GetByIdAsync(string id);
        Task<T> GetByFieldStringAsync(string fieldName, string value);
        Task<T> CreateAsync(T entity);
        Task UpdateAsync(string id, T entity);
        Task UpdateByFieldAsync(string fieldName, string id, T entity);
        Task DeleteAsync(string id);
    }
}
