using DataToolkit.Library.Common;
using DataToolkit.Library.Connections;
using DataToolkit.Library.Context;
using DataToolkit.Library.Fluent;
using DataToolkit.Library.Repositories;
using DataToolkit.Library.Sql;
using DataToolkit.Library.StoredProcedures;
using DataToolkit.Library.UnitOfWorkLayer;
using Serilog;
using System.Data;
using System.Diagnostics;

public class UnitOfWork : IUnitOfWork, IDbContext, IDisposable
{
    private readonly IDbConnection _connection;
    private IDbTransaction? _transaction;
    private bool _disposed;

    private readonly Dictionary<Type, object> _repositories = new();
    private readonly DataToolkitOptions _options;
    private readonly ILogger _logger = Log.ForContext<UnitOfWork>();

    public IDbConnection Connection => _connection;
    public IDbTransaction? Transaction => _transaction;

    public IStoredProcedureExecutor StoredProcedureExecutor { get; private set; } = null!;
    public ISqlExecutor Sql { get; private set; } = null!;
    public IFluentQuery Fluent { get; private set; } = null!;

    public UnitOfWork(
        IDbConnectionFactory factory,
        string dbAlias = "SqlServer",
        DataToolkitOptions? options = null)
    {
        _options = options ?? new DataToolkitOptions();

        _connection = factory.CreateConnection(dbAlias)
            ?? throw new InvalidOperationException("Connection factory returned null.");

        EnsureConnectionOpen();
        BuildExecutors();
    }

    // ---------------- REPOSITORY ----------------
    public IGenericRepository<T> Repository<T>() where T : class
    {
        ThrowIfDisposed();

        var type = typeof(T);

        if (_repositories.TryGetValue(type, out var repo))
            return (IGenericRepository<T>)repo;

        EnsureConnectionOpen();

        repo = new GenericRepository<T>(_connection, _transaction);

        _repositories[type] = repo;

        return (IGenericRepository<T>)repo;
    }

    // ---------------- TRANSACTIONS ----------------
    public void BeginTransaction()
    {
        ThrowIfDisposed();

        var sw = Stopwatch.StartNew();

        try
        {
            EnsureConnectionOpen();

            _transaction?.Dispose();
            _transaction = _connection.BeginTransaction();

            BuildExecutors(); // importante: reconstruye estado
            _repositories.Clear();
        }
        catch (Exception ex)
        {
            sw.Stop();

            if (_options.Logging)
                ToolkitTelemetry.Error(_logger, _options,
                    $"BeginTransaction falló tras {sw.ElapsedMilliseconds}ms", ex, null);

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
        }
        catch (Exception ex)
        {
            sw.Stop();

            if (_options.Logging)
                ToolkitTelemetry.Error(_logger, _options,
                    $"Commit falló tras {sw.ElapsedMilliseconds}ms", ex, null);

            throw;
        }
        finally
        {
            CleanupTransaction();
        }
    }

    public void Rollback()
    {
        ThrowIfDisposed();

        var sw = Stopwatch.StartNew();

        try
        {
            _transaction?.Rollback();
        }
        catch (Exception ex)
        {
            sw.Stop();

            if (_options.Logging)
                ToolkitTelemetry.Error(_logger, _options,
                    $"Rollback falló tras {sw.ElapsedMilliseconds}ms", ex, null);

            throw;
        }
        finally
        {
            CleanupTransaction();
        }
    }

    // ---------------- EXECUTORS ----------------
    private void BuildExecutors()
    {
        Sql = new SqlExecutor(_connection, () => _transaction);
        StoredProcedureExecutor = new StoredProcedureExecutor(_connection, _transaction);
        Fluent = new FluentQuery();
    }

    private void CleanupTransaction()
    {
        _transaction?.Dispose();
        _transaction = null;

        BuildExecutors();
        _repositories.Clear();
    }

    // ---------------- HELPERS ----------------
    private void EnsureConnectionOpen()
    {
        if (_connection.State != ConnectionState.Open)
            _connection.Open();
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(UnitOfWork));
    }

    // ---------------- ASYNC WRAPPERS ----------------
    public Task CommitAsync()
    {
        Commit();
        return Task.CompletedTask;
    }

    public Task RollbackAsync()
    {
        Rollback();
        return Task.CompletedTask;
    }

    // ---------------- DISPOSE ----------------
    public void Dispose()
    {
        if (_disposed) return;

        try
        {
            _transaction?.Dispose();
            _connection?.Dispose();
            _repositories.Clear();
        }
        finally
        {
            _disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}