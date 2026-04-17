using Dapper;
using DataToolkit.Library.Common;
using Serilog;
using System.Data;

namespace DataToolkit.Library.Sql;

/// <summary>
/// Ejecuta consultas SQL y procedimientos almacenados usando Dapper,
/// con soporte para interpolación, multi-mapping, multi-result y OUTPUT.
/// </summary>
public class SqlExecutor : ISqlExecutor, IDisposable
{
    private readonly IDbConnection _connection;
    private readonly Func<IDbTransaction?> _transactionProvider;
    private readonly int? _defaultTimeout;
    private readonly ILogger _logger;

    private bool _disposed;

    // ---------------- CONSTRUCTOR COMPATIBLE (LEGACY) ----------------
    public SqlExecutor(
        IDbConnection connection,
        IDbTransaction? transaction = null,
        int? commandTimeout = null,
        ILogger? logger = null)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));

        // backward compatible (snapshot mode)
        _transactionProvider = () => transaction;

        _defaultTimeout = commandTimeout;
        _logger = logger ?? Log.Logger;
    }

    // ---------------- CONSTRUCTOR MODERNO (RECOMENDADO) ----------------
    public SqlExecutor(
        IDbConnection connection,
        Func<IDbTransaction?> transactionProvider,
        int? commandTimeout = null,
        ILogger? logger = null)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));

        _transactionProvider = transactionProvider ?? (() => null);

        _defaultTimeout = commandTimeout;
        _logger = logger ?? Log.Logger;
    }

    // =========================================================
    // CORE HELPERS
    // =========================================================

    private IDbTransaction? Tx => _transactionProvider?.Invoke();

    private void ValidateSql(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            throw new ArgumentException("SQL cannot be null or empty.");
    }

    // =========================================================
    // SQL INTERPOLATED
    // =========================================================

    public IEnumerable<T> FromSqlInterpolated<T>(FormattableString query)
        => FromSqlInterpolated<T>(query, null);

    public IEnumerable<T> FromSqlInterpolated<T>(FormattableString query, int? commandTimeout = null)
    {
        var (sql, parameters) = BuildInterpolatedSql(query);

        return ExecuteSafe(() =>
            _connection.Query<T>(
                sql,
                parameters,
                Tx,
                commandTimeout: commandTimeout ?? _defaultTimeout),
            sql);
    }

    public async Task<IEnumerable<T>> FromSqlInterpolatedAsync<T>(
        FormattableString query,
        int? commandTimeout = null,
        CancellationToken ct = default)
    {
        var (sql, parameters) = BuildInterpolatedSql(query);

        return await ExecuteSafeAsync(async () =>
            await _connection.QueryAsync<T>(
                new CommandDefinition(
                    sql,
                    parameters,
                    Tx,
                    commandTimeout ?? _defaultTimeout,
                    cancellationToken: ct)),
            sql);
    }

    // =========================================================
    // RAW SQL
    // =========================================================

    public IEnumerable<T> FromSql<T>(string sql)
        => FromSql<T>(sql, null, null);

    public IEnumerable<T> FromSql<T>(string sql, object? parameters)
        => FromSql<T>(sql, parameters, null);

    public IEnumerable<T> FromSql<T>(string sql, object? parameters = null, int? commandTimeout = null)
    {
        return ExecuteSafe(() =>
            _connection.Query<T>(
                sql,
                parameters,
                Tx,
                commandTimeout: commandTimeout ?? _defaultTimeout),
            sql);
    }

    public Task<IEnumerable<T>> FromSqlAsync<T>(string sql)
        => FromSqlAsync<T>(sql, null, null);

    public Task<IEnumerable<T>> FromSqlAsync<T>(string sql, object? parameters)
        => FromSqlAsync<T>(sql, parameters, null);

    public async Task<IEnumerable<T>> FromSqlAsync<T>(
        string sql,
        object? parameters = null,
        int? commandTimeout = null)
    {
        return await ExecuteSafeAsync(() =>
            _connection.QueryAsync<T>(
                sql,
                parameters,
                Tx,
                commandTimeout: commandTimeout ?? _defaultTimeout),
            sql);
    }

    // =========================================================
    // MULTI MAP
    // =========================================================

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
                transaction: Tx,
                commandType: CommandType.Text,
                commandTimeout: commandTimeout ?? _defaultTimeout
            );

            return result.Cast<T>();
        }, request.Sql);
    }

    public async Task<IEnumerable<T>> FromSqlMultiMapAsync<T>(
        MultiMapRequest request,
        int? commandTimeout = null)
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
                transaction: Tx,
                commandType: CommandType.Text,
                commandTimeout: commandTimeout ?? _defaultTimeout
            );

            return result.Cast<T>();
        }, request.Sql);
    }

    // =========================================================
    // QUERY MULTIPLE
    // =========================================================

    public async Task<List<IEnumerable<dynamic>>> QueryMultipleAsync(
        string sql,
        object? parameters = null,
        CommandType commandType = CommandType.StoredProcedure,
        int? commandTimeout = null)
    {
        return await ExecuteSafeAsync(async () =>
        {
            var resultSets = new List<IEnumerable<dynamic>>();

            using var reader = await _connection.QueryMultipleAsync(
                sql,
                parameters,
                Tx,
                commandType: commandType,
                commandTimeout: commandTimeout ?? _defaultTimeout);

            while (!reader.IsConsumed)
                resultSets.Add(await reader.ReadAsync());

            return resultSets;
        }, sql);
    }

    // =========================================================
    // EXECUTE
    // =========================================================

    public int Execute(string sql)
        => Execute(sql, null, null);

    public int Execute(string sql, object? parameters)
        => Execute(sql, parameters, null);

    public int Execute(string sql, object? parameters = null, int? commandTimeout = null)
    {
        return ExecuteSafe(() =>
            _connection.Execute(
                sql,
                parameters,
                Tx,
                commandTimeout: commandTimeout ?? _defaultTimeout),
            sql);
    }

    public Task<int> ExecuteAsync(string sql)
        => ExecuteAsync(sql, null, null);

    public Task<int> ExecuteAsync(string sql, object? parameters)
        => ExecuteAsync(sql, parameters, null);

    public async Task<int> ExecuteAsync(
        string sql,
        object? parameters = null,
        int? commandTimeout = null)
    {
        return await ExecuteSafeAsync(() =>
            _connection.ExecuteAsync(
                sql,
                parameters,
                Tx,
                commandTimeout: commandTimeout ?? _defaultTimeout),
            sql);
    }

    // =========================================================
    // OUTPUT
    // =========================================================

    public (int RowsAffected, Dictionary<string, object> OutputValues)
        ExecuteWithOutput(string storedProcedure, Action<DynamicParameters> configureParameters)
        => ExecuteWithOutput(storedProcedure, configureParameters, null);

    public (int RowsAffected, Dictionary<string, object> OutputValues)
        ExecuteWithOutput(
            string storedProcedure,
            Action<DynamicParameters> configureParameters,
            int? commandTimeout = null)
    {
        return ExecuteSafe(() =>
        {
            var parameters = new DynamicParameters();
            configureParameters(parameters);

            var rows = _connection.Execute(
                storedProcedure,
                parameters,
                Tx,
                commandType: CommandType.StoredProcedure,
                commandTimeout: commandTimeout ?? _defaultTimeout);

            var output = new Dictionary<string, object>();

            foreach (var name in parameters.ParameterNames)
                output[name] = parameters.Get<object>(name)!;

            return (rows, output);
        }, storedProcedure);
    }

    public async Task<(int RowsAffected, DynamicParameters Output)>
        ExecuteWithOutputAsync(
            string storedProcedure,
            Action<DynamicParameters> configureParameters,
            int? commandTimeout = null)
    {
        return await ExecuteSafeAsync(async () =>
        {
            var parameters = new DynamicParameters();
            configureParameters(parameters);

            var rows = await _connection.ExecuteAsync(
                storedProcedure,
                parameters,
                Tx,
                commandType: CommandType.StoredProcedure,
                commandTimeout: commandTimeout ?? _defaultTimeout);

            return (rows, parameters);
        }, storedProcedure);
    }

    // =========================================================
    // INTERPOLATION HELPER
    // =========================================================

    private static (string, DynamicParameters) BuildInterpolatedSql(FormattableString query)
    {
        var dParams = new DynamicParameters();
        var paramNames = new object[query.ArgumentCount];

        for (int i = 0; i < query.ArgumentCount; i++)
        {
            paramNames[i] = $"@p{i}";
            dParams.Add((string)paramNames[i], query.GetArgument(i));
        }

        var sql = string.Format(query.Format, paramNames);
        return (sql, dParams);
    }

    // =========================================================
    // SAFE WRAPPERS
    // =========================================================

    private T ExecuteSafe<T>(Func<T> func, string sql)
    {
        try
        {
            ValidateSql(sql);
            return func();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "SQL execution error: {Sql}", sql);
            throw new SqlExecutorException(sql, ex);
        }
    }

    private async Task<T> ExecuteSafeAsync<T>(Func<Task<T>> func, string sql)
    {
        try
        {
            ValidateSql(sql);
            return await func();
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "SQL async execution error: {Sql}", sql);
            throw new SqlExecutorException(sql, ex);
        }
    }

    // =========================================================
    // DISPOSE
    // =========================================================

    public void Dispose()
    {
        if (_disposed) return;

        _connection?.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}