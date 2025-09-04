using Dapper;
using DataToolkit.Library.Metadata;
using System.Data;
using System.Linq.Expressions;

namespace DataToolkit.Library.Repositories;

public class GenericRepository<T> : IRepository<T>, IGenericRepository<T> where T : class
{
    private readonly IDbConnection _connection;
    private readonly IDbTransaction? _transaction;
    private readonly EntityMetadata _meta;

    public GenericRepository(IDbConnection connection, IDbTransaction? transaction = null)
    {
        _connection = connection;
        _transaction = transaction;
        _meta = EntityMetadataHelper.GetMetadata<T>();
    }

    public async Task<IEnumerable<T>> GetAllAsync()
    {
        var query = $"SELECT * FROM {_meta.TableName}";
        return await _connection.QueryAsync<T>(query, transaction: _transaction);
    }

    public async Task<T?> GetByIdAsync(T entity)
    {
        var whereClause = string.Join(" AND ",
            _meta.KeyProperties.Select(p => $"{_meta.ColumnMappings[p]} = @{p.Name}"));

        var query = $"SELECT * FROM {_meta.TableName} WHERE {whereClause}";
        return await _connection.QueryFirstOrDefaultAsync<T>(query, entity, _transaction);
    }

    public async Task<int> UpdateAsync(
        T entity,
        Expression<Func<T, object>>? includeProperties = null
    )
    {
        var tableName = EntityMetadataHelper.GetTableName<T>();
        var metadata = EntityMetadataHelper.GetMetadata<T>();
        var keyName = metadata.KeyProperties.FirstOrDefault()?.Name;

        if (string.IsNullOrEmpty(keyName))
            throw new InvalidOperationException($"La entidad {typeof(T).Name} no tiene clave primaria definida.");

        IEnumerable<string> properties;

        if (includeProperties == null)
        {
            // ✅ Caso 1: actualizar todos los campos excepto las keys y las identity
            properties = metadata.Properties
                .Where(p => !metadata.KeyProperties.Contains(p) && !metadata.IdentityProperties.Contains(p))
                .Select(p => p.Name);
        }
        else
        {
            // ✅ Caso 2: actualizar solo los campos pasados en la expresión
            properties = EntityMetadataHelper.GetPropertiesFromExpression(includeProperties);
        }

        var setClauses = string.Join(", ", properties.Select(p => $"{p} = @{p}"));
        var sql = $"UPDATE {tableName} SET {setClauses} WHERE {keyName} = @{keyName}";

        return await _connection.ExecuteAsync(sql, entity);
    }


    public async Task<int> InsertAsync(T entity)
    {
        var props = _meta.Properties
            .Where(p => !_meta.IdentityProperties.Contains(p))
            .ToList();

        var columns = string.Join(", ", props.Select(p => _meta.ColumnMappings[p]));
        var values = string.Join(", ", props.Select(p => "@" + p.Name));

        var query = $"INSERT INTO {_meta.TableName} ({columns}) VALUES ({values})";
        return await _connection.ExecuteAsync(query, entity, _transaction);
    }


    public async Task<int> DeleteAsync(T entity)
    {
        var whereClause = string.Join(" AND ",
            _meta.KeyProperties.Select(p => $"{_meta.ColumnMappings[p]} = @{p.Name}"));

        var query = $"DELETE FROM {_meta.TableName} WHERE {whereClause}";
        return await _connection.ExecuteAsync(query, entity, _transaction);
    }

    public async Task<IEnumerable<T>> ExecuteStoredProcedureAsync(string storedProcedure, object parameters)
    {
        return await _connection.QueryAsync<T>(
            storedProcedure, parameters, _transaction, commandType: CommandType.StoredProcedure);
    }

    public async Task<IEnumerable<TResult>> ExecuteStoredProcedureAsync<TResult>(string storedProcedure, object parameters)
    {
        return await _connection.QueryAsync<TResult>(
            storedProcedure, parameters, _transaction, commandType: CommandType.StoredProcedure);
    }
}
