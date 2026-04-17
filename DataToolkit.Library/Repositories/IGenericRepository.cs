using System.Linq.Expressions;

namespace DataToolkit.Library.Repositories
{
    public interface IGenericRepository<T>
    {
        Task<int> DeleteAsync(T entity);
        Task<IEnumerable<T>> ExecuteStoredProcedureAsync(string storedProcedure, object parameters);
        Task<IEnumerable<TResult>> ExecuteStoredProcedureAsync<TResult>(string storedProcedure, object parameters);
        Task<IEnumerable<T>> GetAllAsync(params Expression<Func<T, object>>[]? selectProperties);
        Task<T?> GetByIdAsync(T entity, params Expression<Func<T, object>>[]? selectProperties);
        Task<int> InsertAsync(T entity);
        Task<int> UpdateAsync(T entity, params Expression<Func<T, object>>[] includeProperties);
    }
}