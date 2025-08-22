namespace DataToolkit.Library.Repositories;

public interface IRepository<T> where T : class
{
    /// <summary>Obtiene todos los registros.</summary>
    Task<IEnumerable<T>> GetAllAsync();

    /// <summary>Obtiene un registro por su clave compuesta.</summary>
    Task<T?> GetByIdAsync(Dictionary<string, object> keys);

    /// <summary>Inserta un nuevo registro.</summary>
    Task<int> InsertAsync(T entity);

    /// <summary>Actualiza un registro existente.</summary>
    Task<int> UpdateAsync(T entity);

    /// <summary>Elimina un registro por clave compuesta.</summary>
    Task<int> DeleteAsync(Dictionary<string, object> keys);

    /// <summary>Ejecuta un procedimiento almacenado que retorna el mismo tipo de entidad.</summary>
    Task<IEnumerable<T>> ExecuteStoredProcedureAsync(string storedProcedure, object parameters);

    /// <summary>Ejecuta un procedimiento almacenado que retorna un tipo personalizado.</summary>
    Task<IEnumerable<TResult>> ExecuteStoredProcedureAsync<TResult>(string storedProcedure, object parameters);
}
