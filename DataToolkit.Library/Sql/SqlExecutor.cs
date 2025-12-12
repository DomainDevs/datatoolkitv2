using AdoNetCore.AseClient.Internal;
using Dapper;
using DataToolkit.Library.Common;
using DataToolkit.Library.Sql;
using Microsoft.VisualBasic;
using System;
using System.Data;
using System.Drawing;
using System.Reflection;
using Serilog;

using static System.Runtime.InteropServices.JavaScript.JSType;
using Serilog.Core;

namespace DataToolkit.Library.Sql;


//📁 FromSqlInterpolatedAsync<T>(FormattableString query) → para queries interpoladas(seguros contra inyección SQL)
//📁 FromSqlMultiMapAsync<T>(MultiMapRequest request) → para mapeos múltiples(joins)
//📁 QueryMultipleAsync(...) → para múltiples resultados(varios SELECT)
//Mensajes enriquecidos con SQL original en errores personalizados ✅
//📁 Disposición adecuada con IDisposable ✅
//📁 Métodos segmentados por tipo de operación(lectura, multi-mapping, múltiples resultados, ejecución con OUTPUT) ✅
// Uso de DynamicParameters correctamente con Dapper ✅
//📁 Modelo Try/Catch centralizado, Ventajas de este enfoque
//  ✅ Centralizado No repites try/catch en cada método (Centralización de try/catch con ExecuteSafe / ExecuteSafeAsync ✅)
//  ✅ Informativo Incluyes el SQL en el error
//  ✅ Limpio Todos los métodos siguen igual de legibles
//  ✅ Controlado Puedes loguear, envolver o transformar errores fácilmente

/// <summary>
/// Ejecuta consultas SQL y procedimientos almacenados usando Dapper, 
/// proporcionando soporte mapeo simple, multi-mapping, multi-result y parámetros OUTPUT
/// </summary>
public class SqlExecutor : IDisposable, ISqlExecutor
{
    private readonly IDbConnection _connection;
    private readonly IDbTransaction? _transaction;
    private readonly int? _defaultTimeout;
    private readonly ILogger _logger;
    private bool _disposed;

    /// <summary>
    /// Constructor principal del ejecutor SQL.
    /// </summary>
    /// <param name="connection">Conexión a la base de datos</param>
    /// <param name="transaction">Transacción activa (opcional)</param>
    /// <param name="commandTimeout">Tiempo máximo en segundos para ejecutar el comando (opcional)</param>
    public SqlExecutor(IDbConnection connection, IDbTransaction? transaction = null, int? commandTimeout = null, ILogger logger = null)
    {
        _connection = connection;
        _transaction = transaction;
        _defaultTimeout = commandTimeout;
        _logger = logger ?? new LoggerConfiguration().CreateLogger(); //_logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ---------------------------------------------------------------------------
    // CONSULTAS INTERPOLADAS (seguros frente a inyección SQL)
    // ---------------------------------------------------------------------------
    #region SqlInterpolated
    /// <summary>
    /// Ejecuta una consulta SQL interpolada y devuelve una colección de resultados tipados.
    /// </summary>
    public IEnumerable<T> FromSqlInterpolated<T>(FormattableString query)
    => FromSqlInterpolated<T>(query, null);

    public IEnumerable<T> FromSqlInterpolated<T>(FormattableString query, int? commandTimeout = null)
    {
        //var (sql, parameters) = BuildInterpolatedSql(query);
        //return _connection.Query<T>(sql, parameters, _transaction);
        var (sql, parameters) = BuildInterpolatedSql(query);
        return ExecuteSafe(() => _connection.Query<T>(sql, parameters, _transaction, commandTimeout: commandTimeout ?? _defaultTimeout), sql);

    }
    /// <summary>
    /// Ejecuta una consulta SQL interpolada de forma asíncrona.
    /// </summary>
    public Task<IEnumerable<T>> FromSqlInterpolatedAsync<T>(FormattableString query)
    => FromSqlInterpolatedAsync<T>(query, null);

    public async Task<IEnumerable<T>> FromSqlInterpolatedAsync<T>(FormattableString query, int? commandTimeout = null)
    {
        //var (sql, parameters) = BuildInterpolatedSql(query);
        //return await _connection.QueryAsync<T>(sql, parameters, _transaction);
        var (sql, parameters) = BuildInterpolatedSql(query);
        return await ExecuteSafeAsync(() => _connection.QueryAsync<T>(sql, parameters, _transaction, commandTimeout: commandTimeout ?? _defaultTimeout), sql);
    }
    #endregion

    // ---------------------------------------------------------------------------
    // Quieres ejecutar queries parametrizadas manualmente sin interpolación.
    // ---------------------------------------------------------------------------
    #region FromSql
    /// <summary>
    /// Ejecuta una consulta SQL no interpolada, No necesitas joins o multi-mapping.
    /// Estás reutilizando consultas ya formadas (como scripts SQL definidos en otra capa).
    /// </summary>
    public IEnumerable<T> FromSql<T>(string sql)
    => FromSql<T>(sql, null, null);

    public IEnumerable<T> FromSql<T>(string sql, object? parameters)
        => FromSql<T>(sql, parameters, null);

    public IEnumerable<T> FromSql<T>(string sql, object? parameters = null, int? commandTimeout = null)
    {
        //return _connection.Query<T>(sql, parameters, _transaction);
        return ExecuteSafe(() => _connection.Query<T>(sql, parameters, _transaction, commandTimeout: commandTimeout ?? _defaultTimeout), sql);
    }
    /// <summary>
    /// Ejecuta una consulta SQL no interpolada, No necesitas joins o multi-mapping.
    /// Estás reutilizando consultas ya formadas (como scripts SQL definidos en otra capa).
    /// </summary>
    public Task<IEnumerable<T>> FromSqlAsync<T>(string sql)
        => FromSqlAsync<T>(sql, null, null);

    public Task<IEnumerable<T>> FromSqlAsync<T>(string sql, object? parameters)
        => FromSqlAsync<T>(sql, parameters, null);

    public async Task<IEnumerable<T>> FromSqlAsync<T>(string sql, object? parameters = null, int? commandTimeout = null)
    {
        //return await _connection.QueryAsync<T>(sql, parameters, _transaction);
        return await ExecuteSafeAsync(() => _connection.QueryAsync<T>(sql, parameters, _transaction, commandTimeout: commandTimeout ?? _defaultTimeout), sql);
    }
    #endregion

    // ---------------------------------------------------------------------------
    // CONSULTAS CON MAPEOS MÚLTIPLES (JOIN de múltiples entidades)
    // ---------------------------------------------------------------------------
    #region SqlMultiMap
    /// <summary>
    /// Ejecuta una consulta con múltiples mapeos entre tablas relacionadas (JOIN).
    /// </summary>
    public IEnumerable<T> FromSqlMultiMap<T>(MultiMapRequest request)
    => FromSqlMultiMap<T>(request, null);

    public IEnumerable<T> FromSqlMultiMap<T>(MultiMapRequest request, int? commandTimeout = null)
    {

        return ExecuteSafe(() =>
        {
            var result = SqlMapper.Query(
                _connection,
                request.Sql,
                request.Types,
                objects => request.MapFunction(objects),
                param: request.Parameters,
                splitOn: request.SplitOn,
                transaction: _transaction,
                commandType: CommandType.Text,
                commandTimeout: commandTimeout ?? _defaultTimeout
            );

            return result.Cast<T>();
        }, request.Sql);

    }
    /// <summary>
    /// Ejecuta una consulta con múltiples mapeos de forma asíncrona.
    /// </summary>
    public Task<IEnumerable<T>> FromSqlMultiMapAsync<T>(MultiMapRequest request)
    => FromSqlMultiMapAsync<T>(request, null);

    public async Task<IEnumerable<T>> FromSqlMultiMapAsync<T>(MultiMapRequest request, int? commandTimeout = null)
    {

        return await ExecuteSafeAsync(async () =>
        {
            var result = await SqlMapper.QueryAsync(
                _connection,
                request.Sql,
                request.Types,
                objects => request.MapFunction(objects),
                param: request.Parameters,
                splitOn: request.SplitOn,
                transaction: _transaction,
                commandType: CommandType.Text,
                commandTimeout: commandTimeout ?? _defaultTimeout
            );

            return result.Cast<T>();
        }, request.Sql);
    }
    #endregion

    // ---------------------------------------------------------------------------
    // CONSULTAS MULTI-RESULTADO (varios SELECT dentro de un SP)
    // ---------------------------------------------------------------------------
    #region QueryMultiple
    /// <summary>
    /// Ejecuta un procedimiento almacenado o query que retorna múltiples conjuntos de resultados.
    /// </summary>
    /// 
    public Task<List<IEnumerable<dynamic>>> QueryMultipleAsync(string sql)
        => QueryMultipleAsync(sql, null, CommandType.StoredProcedure, null);

    public Task<List<IEnumerable<dynamic>>> QueryMultipleAsync(string sql, object? parameters)
        => QueryMultipleAsync(sql, parameters, CommandType.StoredProcedure, null);

    public Task<List<IEnumerable<dynamic>>> QueryMultipleAsync(string sql, object? parameters, CommandType commandType)
        => QueryMultipleAsync(sql, parameters, commandType, null);

    public async Task<List<IEnumerable<dynamic>>> QueryMultipleAsync(
        string sql,
        object? parameters = null,
        CommandType commandType = CommandType.StoredProcedure,
        int? commandTimeout = null)
    {

        return await ExecuteSafeAsync(async () =>
        {
            var resultSets = new List<IEnumerable<dynamic>>();

            using var reader = await _connection.QueryMultipleAsync(sql, parameters, _transaction, commandType: commandType, commandTimeout: commandTimeout ?? _defaultTimeout);

            while (!reader.IsConsumed)
            {
                var result = await reader.ReadAsync();
                resultSets.Add(result);
            }

            return resultSets;
        }, sql);

    }
    #endregion QueryMultiple

    // ---------------------------------------------------------------------------
    // EJECUCIÓN DE SQL (INSERT, UPDATE, DELETE)
    // ---------------------------------------------------------------------------
    #region Execute
    /// <summary>
    /// Ejecuta una instrucción SQL (INSERT/UPDATE/DELETE).
    /// </summary>
    public int Execute(string sql) => Execute(sql, null, null);
    public int Execute(string sql, object? parameters) => Execute(sql, parameters, null);
    public int Execute(string sql, object? parameters = null, int? commandTimeout = null)
    {
        //return _connection.Execute(sql, parameters, _transaction);
        return ExecuteSafe(() => _connection.Execute(sql, parameters, _transaction, commandTimeout: commandTimeout ?? _defaultTimeout), sql);
    }

    /// <summary>
    /// Ejecuta una instrucción SQL de forma asíncrona.
    /// </summary>
    public Task<int> ExecuteAsync(string sql) => ExecuteAsync(sql, null, null);
    public Task<int> ExecuteAsync(string sql, object? parameters) => ExecuteAsync(sql, parameters, null);
    public async Task<int> ExecuteAsync(string sql, object? parameters = null, int? commandTimeout = null)
    {
        //return await _connection.ExecuteAsync(sql, parameters, _transaction);
        return await ExecuteSafeAsync(() => _connection.ExecuteAsync(sql, parameters, _transaction, commandTimeout: commandTimeout ?? _defaultTimeout), sql);
    }
    #endregion Execute

    // ---------------------------------------------------------------------------
    // EJECUCIÓN CON PARÁMETROS DE SALIDA (OUTPUT)
    // ---------------------------------------------------------------------------
    #region ExecuteWithOutput
    /// <summary>
    /// Ejecuta un procedimiento almacenado que contiene parámetros OUTPUT (modo síncrono).
    /// </summary>
    public (int RowsAffected, Dictionary<string, object> OutputValues) ExecuteWithOutput(string storedProcedure, Action<DynamicParameters> configureParameters)
    => ExecuteWithOutput(storedProcedure, configureParameters, null);

    public (int RowsAffected, Dictionary<string, object> OutputValues) ExecuteWithOutput(
        string storedProcedure,
        Action<DynamicParameters> configureParameters,
         int? commandTimeout = null)
    {

        return ExecuteSafe(() =>
        {
            var parameters = new DynamicParameters();
            configureParameters(parameters);

            var rowsAffected = _connection.Execute(
                storedProcedure,
                parameters,
                _transaction,
                commandType: CommandType.StoredProcedure,
                commandTimeout: commandTimeout ?? _defaultTimeout
            );

            var outputValues = new Dictionary<string, object>();
            foreach (var paramName in parameters.ParameterNames)
            {
                var value = parameters.Get<object>(paramName);
                outputValues[paramName] = value!;
            }

            return (rowsAffected, outputValues);
        }, storedProcedure);

    }

    /// <summary>
    /// Ejecuta un procedimiento almacenado que contiene parámetros OUTPUT (modo asíncrono).
    /// </summary>
    public Task<(int RowsAffected, DynamicParameters Output)> ExecuteWithOutputAsync(string storedProcedure, Action<DynamicParameters> configureParameters)
        => ExecuteWithOutputAsync(storedProcedure, configureParameters, null);

    public async Task<(int RowsAffected, DynamicParameters Output)> ExecuteWithOutputAsync(
        string storedProcedure,
        Action<DynamicParameters> configureParameters,
        int? commandTimeout = null)
    {

        return await ExecuteSafeAsync(async () =>
        {
            var parameters = new DynamicParameters();
            configureParameters(parameters);

            var rows = await _connection.ExecuteAsync(storedProcedure, parameters, _transaction, commandType: CommandType.StoredProcedure, commandTimeout: commandTimeout ?? _defaultTimeout);
            return (rows, parameters);
        }, storedProcedure);
    }
    #endregion

    // ---------------------------------------------------------------------------
    // MÉTODO AUXILIAR PARA INTERPOLACIÓN SEGURA
    // ---------------------------------------------------------------------------
    /// <summary>
    /// Convierte una cadena interpolada en SQL con parámetros seguros para Dapper.
    /// </summary>
    private static (string, DynamicParameters) BuildInterpolatedSql(FormattableString query)
    {
        var dParams = new DynamicParameters();
        var sql = query.Format;

        for (int i = 0; i < query.ArgumentCount; i++)
        {
            var paramName = $"@p{i}";
            sql = sql.Replace("{" + i + "}", paramName);
            dParams.Add(paramName, query.GetArgument(i));
        }

        return (sql, dParams);
    }

    // ---------------------------------------------------------------------------
    // IMPLEMENTACIÓN IDisposable
    // ---------------------------------------------------------------------------
    public void Dispose()
    {
        if (!_disposed)
        {
            _connection?.Dispose();
            _disposed = true;
        }
    }

    // ---------------------------------------------------------------------------
    // Agrega un método wrapper centralizado en SqlExecutor
    // ---------------------------------------------------------------------------
    private T ExecuteSafe<T>(Func<T> func, string? sql = null)
    {
        try
        {
            return func();
        }
        catch (Exception ex)
        {
            ////throw new SqlExecutorException("Error al ejecutar SQL", ex, sql);
            _logger.Error(ex, "Error al ejecutar el procedimiento almacenado '{Procedure}'", sql);
            throw new SqlExecutorException($"Error al ejecutar procedimiento '{sql}'", ex, sql);
        }
    }

    /// <summary>
    /// Método protegido para liberar recursos de forma segura, asíncrono.
    /// </summary>
    private async Task<T> ExecuteSafeAsync<T>(Func<Task<T>> func, string? sql = null)
    {
        try
        {
            return await func();
        }
        catch (Exception ex)
        {
            //throw new SqlExecutorException("Error al ejecutar SQL asincrónico", ex, sql);
            _logger.Error(ex, "Error asincrónico al ejecutar el procedimiento almacenado '{Procedure}'", sql);
            throw new SqlExecutorException($"Error asincrónico al ejecutar procedimiento '{sql}'", ex, sql);
        }
    }

}
