using System.Linq.Expressions;

namespace DataToolkit.Library.Repositories;

public interface IGenericRepository<T> where T : class
{
    Task<int> InsertAsync(T entity);
    Task<int> DeleteAsync(T entity);
    Task<IEnumerable<T>> ExecuteStoredProcedureAsync(string storedProcedure, object parameters);
    Task<IEnumerable<TResult>> ExecuteStoredProcedureAsync<TResult>(string storedProcedure, object parameters);
    
    // 👇 permite limitar columnas opcionalmente (selectProperties)
    Task<IEnumerable<T>> GetAllAsync(params Expression<Func<T, object>>[]? selectProperties); //Task<IEnumerable<T>> GetAllAsync();
    Task<T?> GetByIdAsync(T entity, params Expression<Func<T, object>>[]? selectProperties); //Task<T?> GetByIdAsync(T entity);

    // Update parcial por propiedades
    Task<int> UpdateAsync(T entity, params Expression<Func<T, object>>[] includeProperties);
}