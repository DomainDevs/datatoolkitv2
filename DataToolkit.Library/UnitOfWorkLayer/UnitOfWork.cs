using DataToolkit.Library.Connections;
using DataToolkit.Library.Repositories;
using DataToolkit.Library.Sql;
using DataToolkit.Library.StoredProcedures;
using Serilog;
using System.Data;

namespace DataToolkit.Library.UnitOfWorkLayer;

public class UnitOfWork : IUnitOfWork, IDisposable
{
    private readonly IDbConnection _connection;
    private IDbTransaction? _transaction;
    private bool _disposed;

    private readonly Dictionary<Type, object> _repositories = new();

    public IStoredProcedureExecutor StoredProcedureExecutor { get; private set; }
    public SqlExecutor Sql { get; private set; } // ✅ NUEVO

    public UnitOfWork(IDbConnectionFactory factory, string dbAlias = "SqlServer")
    {
        _connection = factory.CreateConnection(dbAlias);
        StoredProcedureExecutor = new StoredProcedureExecutor(_connection);
        Sql = new SqlExecutor(_connection);
    }

    public IGenericRepository<T> GetRepository<T>() where T : class
    {
        var type = typeof(T);

        if (!_repositories.ContainsKey(type))
        {
            if (_connection.State != ConnectionState.Open)
                _connection.Open();

            var repo = new GenericRepository<T>(_connection, _transaction);
            _repositories[type] = repo;
        }

        return (IGenericRepository<T>)_repositories[type];
    }

    public void BeginTransaction()
    {
        if (_connection.State != ConnectionState.Open)
            _connection.Open();

        _transaction = _connection.BeginTransaction();
        StoredProcedureExecutor = new StoredProcedureExecutor(_connection, _transaction);
        Sql = new SqlExecutor(_connection, _transaction); // ✅ CON TRANSACCIÓN
    }

    public void Commit()
    {
        _transaction?.Commit();
        _transaction = null;
        StoredProcedureExecutor = new StoredProcedureExecutor(_connection);
        Sql = new SqlExecutor(_connection);
    }

    public void Rollback()
    {
        _transaction?.Rollback();
        _transaction = null;
        StoredProcedureExecutor = new StoredProcedureExecutor(_connection);
        Sql = new SqlExecutor(_connection);
    }

    // 🔒 Método CommitAsync Asincrono
    public Task CommitAsync()
    {
        Commit();
        return Task.CompletedTask;
    }

    // 🔒 Método RollbackAsync Asincrono
    public Task RollbackAsync()
    {
        Rollback();
        return Task.CompletedTask;
    }

    // 🔒 Método Dispose público
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    // 🛡️ Método protegido para herencia segura (evita más de un llamado)
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            _transaction?.Dispose();
            _connection?.Dispose();
        }

        _disposed = true;
    }

}