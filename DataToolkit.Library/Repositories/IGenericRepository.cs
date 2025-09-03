
namespace DataToolkit.Library.Repositories
{
    public interface IGenericRepository<T> where T : class
    {
        Task<int> DeleteAsync(T entity);
        Task<IEnumerable<T>> ExecuteStoredProcedureAsync(string storedProcedure, object parameters);
        Task<IEnumerable<TResult>> ExecuteStoredProcedureAsync<TResult>(string storedProcedure, object parameters);
        Task<IEnumerable<T>> GetAllAsync();
        Task<T?> GetByIdAsync(T entity);
        Task<int> InsertAsync(T entity);
        Task<int> UpdateAsync(T entity);
    }
}