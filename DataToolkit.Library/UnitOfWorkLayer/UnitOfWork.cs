using DataToolkit.Library.Common;
using DataToolkit.Library.Connections;
using DataToolkit.Library.Fluent;
using DataToolkit.Library.Repositories;
using DataToolkit.Library.Sql;
using DataToolkit.Library.StoredProcedures;
using Serilog;
using System.Data;
using System.Diagnostics; // Necesario para Stopwatch

namespace DataToolkit.Library.UnitOfWorkLayer;

public class UnitOfWork : IUnitOfWork
{
    private readonly IDbConnection _connection;
    private IDbTransaction? _transaction;
    private bool _disposed;
    private readonly Dictionary<Type, object> _repositories = new();
    private readonly DataToolkitOptions _options;
    private readonly ILogger _logger = Log.ForContext<UnitOfWork>();

    public IStoredProcedureExecutor StoredProcedureExecutor { get; private set; } = null!;
    public ISqlExecutor Sql { get; private set; } = null!;
    public IFluentQuery Fluent { get; private set; } = null!;

    public UnitOfWork(IDbConnectionFactory factory, string dbAlias = "SqlServer", DataToolkitOptions? options = null)
    {
        _options = options ?? new DataToolkitOptions();
        _connection = factory.CreateConnection(dbAlias)
            ?? throw new InvalidOperationException("Connection factory returned null.");

        RefreshExecutors();
    }

    public IGenericRepository<T> Repository<T>() where T : class
    {
        ThrowIfDisposed();
        var type = typeof(T);

        if (!_repositories.TryGetValue(type, out var repo))
        {
            EnsureConnectionOpen();
            repo = new GenericRepository<T>(_connection, _transaction);
            _repositories[type] = repo;
        }
        return (IGenericRepository<T>)repo;
    }

    public void BeginTransaction()
    {
        ThrowIfDisposed();
        var sw = Stopwatch.StartNew();
        try
        {
            EnsureConnectionOpen();
            _transaction?.Dispose();
            _transaction = _connection.BeginTransaction();
            RefreshExecutors(_transaction);
        }
        catch (Exception ex)
        {
            sw.Stop();
            // Solo se gasta rendimiento en telemetría si hay un fallo
            if (_options.Logging)
                ToolkitTelemetry.Error(_logger, _options, $"BeginTransaction falló tras {sw.ElapsedMilliseconds}ms", ex, null);
            throw;
        }
    }

    public void Commit()
    {
        ThrowIfDisposed();
        var sw = Stopwatch.StartNew();
        try
        {
            _transaction?.Commit();
            _transaction?.Dispose();
            _transaction = null;
            RefreshExecutors();
        }
        catch (Exception ex)
        {
            sw.Stop();
            if (_options.Logging)
                ToolkitTelemetry.Error(_logger, _options, $"Commit falló tras {sw.ElapsedMilliseconds}ms", ex, null);
            throw;
        }
    }

    public void Rollback()
    {
        ThrowIfDisposed();
        var sw = Stopwatch.StartNew();
        try
        {
            _transaction?.Rollback();
            _transaction?.Dispose();
            _transaction = null;
            RefreshExecutors();
        }
        catch (Exception ex)
        {
            sw.Stop();
            if (_options.Logging)
                ToolkitTelemetry.Error(_logger, _options, $"Rollback falló tras {sw.ElapsedMilliseconds}ms", ex, null);
            throw;
        }
    }

    private void RefreshExecutors(IDbTransaction? transaction = null)
    {
        Sql = new SqlExecutor(_connection, transaction);
        StoredProcedureExecutor = new StoredProcedureExecutor(_connection, transaction);
        Fluent = new FluentQuery();
        _repositories.Clear();
    }

    public async Task CommitAsync() => await Task.Run(Commit);
    public async Task RollbackAsync() => await Task.Run(Rollback);

    public void Dispose()
    {
        if (_disposed) return;
        _transaction?.Dispose();
        _connection?.Dispose();
        _repositories.Clear();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private void EnsureConnectionOpen()
    {
        if (_connection.State != ConnectionState.Open) _connection.Open();
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(UnitOfWork));
    }
}