using Dapper;
using DataToolkit.Library.Metadata;
using System.Data;

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

    public async Task<T?> GetByIdAsync(Dictionary<string, object> keys)
    {
        var where = string.Join(" AND ", keys.Keys.Select(k =>
        {
            var prop = typeof(T).GetProperty(k);
            return $"{EntityMetadataHelper.GetColumnName(prop!, typeof(T))} = @{k}";
        }));

        var query = $"SELECT * FROM {_meta.TableName} WHERE {where}";
        return await _connection.QueryFirstOrDefaultAsync<T>(query, keys, _transaction);
    }

    public async Task<int> InsertAsync(T entity)
    {
        var props = _meta.Properties
            .Where(p => !_meta.KeyProperties.Contains(p) || !_meta.IdentityProperties.Contains(p))
            .ToList();

        var columns = string.Join(", ", props.Select(p => _meta.ColumnMappings[p]));
        var values = string.Join(", ", props.Select(p => "@" + p.Name));

        var query = $"INSERT INTO {_meta.TableName} ({columns}) VALUES ({values})";
        return await _connection.ExecuteAsync(query, entity, _transaction);
    }

    public async Task<int> UpdateAsync(T entity)
    {
        var setProps = _meta.Properties.Except(_meta.KeyProperties).ToList();

        var setClause = string.Join(", ", setProps.Select(p => $"{_meta.ColumnMappings[p]} = @{p.Name}"));
        var whereClause = string.Join(" AND ", _meta.KeyProperties.Select(p => $"{_meta.ColumnMappings[p]} = @{p.Name}"));

        var query = $"UPDATE {_meta.TableName} SET {setClause} WHERE {whereClause}";
        return await _connection.ExecuteAsync(query, entity, _transaction);
    }

    public async Task<int> DeleteAsync(Dictionary<string, object> keys)
    {
        var where = string.Join(" AND ", keys.Keys.Select(k =>
        {
            var prop = typeof(T).GetProperty(k);
            return $"{EntityMetadataHelper.GetColumnName(prop!, typeof(T))} = @{k}";
        }));

        var query = $"DELETE FROM {_meta.TableName} WHERE {where}";
        return await _connection.ExecuteAsync(query, keys, _transaction);
    }

    public async Task<IEnumerable<T>> ExecuteStoredProcedureAsync(string storedProcedure, object parameters)
    {
        return await _connection.QueryAsync<T>(storedProcedure, parameters, _transaction, commandType: CommandType.StoredProcedure);
    }

    public async Task<IEnumerable<TResult>> ExecuteStoredProcedureAsync<TResult>(string storedProcedure, object parameters)
    {
        return await _connection.QueryAsync<TResult>(storedProcedure, parameters, _transaction, commandType: CommandType.StoredProcedure);
    }
}
