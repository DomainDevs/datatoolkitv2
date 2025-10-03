using Dapper;
using DataToolkit.Library.Metadata;
using DataToolkit.Library.ChangeTracking;
using System.Data;
using System.Linq.Expressions;
using System.Collections.Concurrent;

namespace DataToolkit.Library.Repositories;

public class GenericRepository<T> : IGenericRepository<T> where T : class
{
    private readonly IDbConnection _connection;
    private readonly IDbTransaction? _transaction;
    private readonly EntityMetadata _meta;

    // 👇 Cache estática por tipo de entidad (usa reflexión, pero con cacheo para evitar problemas de rendimiento).
    private static readonly ConcurrentDictionary<Type, bool> _typeMapCache = new();

    public GenericRepository(IDbConnection connection, IDbTransaction? transaction = null)
    {
        _connection = connection;
        _transaction = transaction;
        _meta = EntityMetadataHelper.GetMetadata<T>();

        EnsureDapperTypeMap(); //mapeo de la entidad con las columnas reales en la base de datos
    }

    // 👇 Mapeo de Columnas a los nombres reales de la tabla [Column("nombre_real_columna")]
    private void EnsureDapperTypeMap()
    {
        // Si ya está registrado, no volver a hacerlo (Borra siempre el anterior, la primera vez se realiza por reflexión, las ejecuciónes posteriores se toma de la caché).
        if (_typeMapCache.ContainsKey(typeof(T)))
            return;

        SqlMapper.SetTypeMap(
            typeof(T),
            new CustomPropertyTypeMap(
                typeof(T),
                (type, columnName) =>
                {
                    // Buscar en los metadatos si hay mapeo de columna
                    var prop = _meta.ColumnMappings
                        .FirstOrDefault(kv => kv.Value.Equals(columnName, StringComparison.OrdinalIgnoreCase))
                        .Key;
                    if (prop != null)
                        return prop;

                    // Si no hay atributo Column, usar coincidencia directa con el nombre
                    return type.GetProperties()
                        .FirstOrDefault(p => p.Name.Equals(columnName, StringComparison.OrdinalIgnoreCase));
                }
            )
        );

        // Registrar en cache que ya está configurado
        _typeMapCache[typeof(T)] = true;
    }

    public async Task<IEnumerable<T>> GetAllAsync(params Expression<Func<T, object>>[]? selectProperties)
    {
        string columns;

        if (selectProperties != null && selectProperties.Length > 0)
        {
            var selectedProps = selectProperties
                .SelectMany(p => EntityMetadataHelper.GetPropertiesFromExpression(p))
                .ToList();

            columns = string.Join(", ", selectedProps.Select(p =>
            {
                var propMeta = _meta.Properties.Single(pr => pr.Name == p);
                return _meta.ColumnMappings[propMeta];
            }));
        }
        else
        {
            columns = "*";
        }

        string query = $"SELECT {columns} FROM {_meta.TableName}";
        var list = (await _connection.QueryAsync<T>(query, transaction: _transaction)).ToList();
        return list;
    }

    public async Task<T?> GetByIdAsync(T entity, params Expression<Func<T, object>>[]? selectProperties)
    {
        string columns;

        if (selectProperties != null && selectProperties.Length > 0)
        {
            var selectedProps = selectProperties
                .SelectMany(p => EntityMetadataHelper.GetPropertiesFromExpression(p))
                .ToList();

            columns = string.Join(", ", selectedProps.Select(p =>
            {
                var propMeta = _meta.Properties.Single(pr => pr.Name == p);
                return _meta.ColumnMappings[propMeta];
            }));
        }
        else
        {
            columns = "*";
        }

        string whereClause = string.Join(" AND ",
            _meta.KeyProperties.Select(p => $"{_meta.ColumnMappings[p]} = @{p.Name}"));

        string query = $"SELECT {columns} FROM {_meta.TableName} WHERE {whereClause}";
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

        string whereClause = string.Join(" AND ",
            keyProps.Select(k => $"{metadata.ColumnMappings[k]} = @{k.Name}"));

        string sql = $"UPDATE {metadata.TableName} SET {setClauses} WHERE {whereClause}";

        return await _connection.ExecuteAsync(sql, entity, _transaction);
    }

    public async Task<int> InsertAsync(T entity)
    {
        var props = _meta.Properties
            .Where(p => !_meta.IdentityProperties.Contains(p))
            .ToList();

        string columns = string.Join(", ", props.Select(p => _meta.ColumnMappings[p]));
        string values = string.Join(", ", props.Select(p => "@" + p.Name));

        string query = $"INSERT INTO {_meta.TableName} ({columns}) VALUES ({values})";
        return await _connection.ExecuteAsync(query, entity, _transaction);
    }

    public async Task<int> DeleteAsync(T entity)
    {
        var whereClause = string.Join(" AND ",
            _meta.KeyProperties.Select(p => $"{_meta.ColumnMappings[p]} = @{p.Name}"));

        string query = $"DELETE FROM {_meta.TableName} WHERE {whereClause}";
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
