using Dapper;
using DataToolkit.Library.Metadata;
using DataToolkit.Library.ChangeTracking;
using System.Data;
using System.Linq.Expressions;

namespace DataToolkit.Library.Repositories;

public class GenericRepository<T> : IGenericRepository<T> where T : class
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

    public async Task<int> UpdateAsync(T entity, params Expression<Func<T, object>>[] includeProperties)
    {
        if (includeProperties == null || includeProperties.Length == 0)
            throw new ArgumentException("Debe especificar al menos una propiedad a actualizar.");

        var metadata = EntityMetadataHelper.GetMetadata<T>();
        var keyProps = metadata.KeyProperties;

        if (!keyProps.Any())
            throw new InvalidOperationException($"La entidad {typeof(T).Name} no tiene clave primaria definida.");

        var propertiesToUpdate = includeProperties
            .SelectMany(p => EntityMetadataHelper.GetPropertiesFromExpression(p))
            .ToList();

        if (!propertiesToUpdate.Any())
            return 0;

        var setClauses = string.Join(", ", propertiesToUpdate.Select(propName =>
        {
            var propMeta = metadata.Properties.Single(p => p.Name == propName);
            return $"{metadata.ColumnMappings[propMeta]} = @{propName}";
        }));

        var whereClause = string.Join(" AND ",
            keyProps.Select(k => $"{metadata.ColumnMappings[k]} = @{k.Name}"));

        var sql = $"UPDATE {metadata.TableName} SET {setClauses} WHERE {whereClause}";

        return await _connection.ExecuteAsync(sql, entity, _transaction);
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
        return await _connection.ExecuteAsync(query, entity, _transaction); // ✅ ahora sí
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
