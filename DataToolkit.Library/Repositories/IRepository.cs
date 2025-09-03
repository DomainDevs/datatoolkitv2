public interface IRepository<T> where T : class
{
    Task<IEnumerable<T>> GetAllAsync();
    Task<T?> GetByIdAsync(T entityWithKeys);
    Task<int> InsertAsync(T entity);
    Task<int> UpdateAsync(T entity);
    Task<int> DeleteAsync(T entity);
    Task<IEnumerable<T>> ExecuteStoredProcedureAsync(string storedProcedure, object parameters);
    Task<IEnumerable<TResult>> ExecuteStoredProcedureAsync<TResult>(string storedProcedure, object parameters);
}
