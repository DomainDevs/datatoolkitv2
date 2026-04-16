using DataToolkit.Library.Connections;
using DataToolkit.Library.Fluent; // ✅ NUEVO
using DataToolkit.Library.Repositories;
using DataToolkit.Library.Sql;
using DataToolkit.Library.StoredProcedures;
using Serilog;
using System.Data;
using System.IO.Pipelines;

namespace DataToolkit.Library.UnitOfWorkLayer;

public class UnitOfWork : IUnitOfWork
{
    private readonly IDbConnection _connection;
    private IDbTransaction? _transaction;
    private bool _disposed;
    private readonly Dictionary<Type, object> _repositories = new();
    private readonly ILogger _logger = Log.ForContext<UnitOfWork>();

    public IStoredProcedureExecutor StoredProcedureExecutor { get; private set; }
    public ISqlExecutor Sql { get; private set; }
    public IFluentQuery Fluent { get; private set; } // ✅ NUEVO

    public UnitOfWork(IDbConnectionFactory factory, string dbAlias = "SqlServer")
    {
        _logger.Information("Iniciando conexión a {Alias}...", dbAlias);
        _connection = factory.CreateConnection(dbAlias);
        // El factory crea la instancia, pero aquí registramos el intento
        _logger.Debug("Conexión creada para {Alias}. Estado: {State}", dbAlias, _connection.State);

        StoredProcedureExecutor = new StoredProcedureExecutor(_connection);
        Sql = new SqlExecutor(_connection);
        Fluent = new FluentQuery(Sql); // ✅ Inicializado
    }

    // Renombrado de GetRepository a Repository para cumplir la interfaz
    public IGenericRepository<T> Repository<T>() where T : class
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
        {
            _connection.Open();
        }
            

        _transaction = _connection.BeginTransaction();
        // ... re-inicializar ejecutores ...
        _logger.Information("Transacción iniciada correctamente.");

        StoredProcedureExecutor = new StoredProcedureExecutor(_connection, _transaction);
        Sql = new SqlExecutor(_connection, _transaction);
        // El Fluent no necesita transacción en el objeto, pero se podría inyectar si fuera necesario
    }

    public void Commit()
    {
        _logger.Information("Ejecutando Commit");
        _transaction?.Commit();
        _transaction = null;
        RefreshExecutors();
    }

    public void Rollback()
    {
        _logger.Error("Ejecutando Rollback");
        _transaction?.Rollback();
        _transaction = null;
        RefreshExecutors();
    }

    private void RefreshExecutors()
    {
        StoredProcedureExecutor = new StoredProcedureExecutor(_connection);
        Sql = new SqlExecutor(_connection);
    }

    public Task CommitAsync() { Commit(); return Task.CompletedTask; }
    public Task RollbackAsync() { Rollback(); return Task.CompletedTask; }

    public void Dispose()
    {
        if (_disposed) return;

        _logger.Debug("Cerrando y liberando recursos de conexión.");
        _transaction?.Dispose();
        _connection?.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}