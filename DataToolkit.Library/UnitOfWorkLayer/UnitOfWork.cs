using DataToolkit.Library.Common;
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
    //Logs
    private readonly DataToolkitOptions _options;
    private readonly ILogger _logger = Log.ForContext<UnitOfWork>();

    public IStoredProcedureExecutor StoredProcedureExecutor { get; private set; }
    public ISqlExecutor Sql { get; private set; }
    public IFluentQuery Fluent { get; private set; } // ✅ NUEVO

    // Prioridad: 1. Factory, 2. Identidad (Alias), 3. Configuración logs (Options)
    public UnitOfWork(IDbConnectionFactory factory, string dbAlias = "SqlServer", DataToolkitOptions? options = null)
    {
        // 1. Identificar y Crear Conexión (Lo más importante)
        _connection = factory.CreateConnection(dbAlias);

        // 2. Inicializar Ejecutores base
        Sql = new SqlExecutor(_connection);
        StoredProcedureExecutor = new StoredProcedureExecutor(_connection);
        Fluent = new FluentQuery(Sql);

        // 3. Cargar configuración de logs
        _options = options ?? new DataToolkitOptions();

        if (_options.EnableLogging)
        {
            _logger.Information("[{Prefix}] Motor listo para: {Alias}", _options.LogPrefix, dbAlias);
        }
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
        StoredProcedureExecutor = new StoredProcedureExecutor(_connection, _transaction);
        Sql = new SqlExecutor(_connection, _transaction);

        if (_options.EnableLogging)
            _logger.Warning("[{Prefix}] Transacción iniciada en base de datos.", _options.LogPrefix);

    }

    public void Commit()
    {
        _transaction?.Commit();
        _transaction = null;
        RefreshExecutors();

        if (_options.EnableLogging)
            _logger.Information("[{Prefix}] Commit realizado correctamente.", _options.LogPrefix);
    }

    public void Rollback()
    {
        _transaction?.Rollback();
        _transaction = null;
        RefreshExecutors();

        if (_options.EnableLogging)
            _logger.Error("[{Prefix}] Rollback ejecutado: Los cambios fueron revertidos.", _options.LogPrefix);
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

        _transaction?.Dispose();
        _connection?.Dispose();

        if (_options.EnableLogging)
            _logger.Debug("[{Prefix}] Recursos de conexión liberados.", _options.LogPrefix);

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}