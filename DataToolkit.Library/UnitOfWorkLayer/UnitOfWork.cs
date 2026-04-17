using DataToolkit.Library.Common;
using DataToolkit.Library.Connections;
using DataToolkit.Library.Fluent;
using DataToolkit.Library.Repositories;
using DataToolkit.Library.Sql;
using DataToolkit.Library.StoredProcedures;
using Serilog;
using System.Data;
using Microsoft.Extensions.Options; // Asegúrate de tener este using

namespace DataToolkit.Library.UnitOfWorkLayer;

/// <summary>
/// Implementación de la unidad de trabajo para gestionar transacciones y repositorios de forma centralizada.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly IDbConnection _connection;
    private IDbTransaction? _transaction;
    private bool _disposed;
    private readonly Dictionary<Type, object> _repositories = new();
    private readonly DataToolkitOptions _options;
    private readonly ILogger _logger = Log.ForContext<UnitOfWork>();

    /// <summary>
    /// Ejecutor de procedimientos almacenados.
    /// </summary>
    public IStoredProcedureExecutor StoredProcedureExecutor { get; private set; } = null!;

    /// <summary>
    /// Ejecutor de consultas SQL crudas.
    /// </summary>
    public ISqlExecutor Sql { get; private set; } = null!;

    /// <summary>
    /// Motor de consultas fluidas (Fluent API).
    /// </summary>
    public IFluentQuery Fluent { get; private set; } = null!;

    /// <summary>
    /// Inicializa una nueva instancia del UnitOfWork con soporte para telemetría.
    /// </summary>
    /// <param name="factory">Fabrica de conexiones.</param>
    /// <param name="dbAlias">Alias de la base de datos a conectar.</param>
    /// <param name="options">Configuración de diagnóstico y rendimiento.</param>
    public UnitOfWork(
        IDbConnectionFactory factory,
        string dbAlias = "SqlServer",
        IOptions<DataToolkitOptions>? options = null) // 👈 CAMBIO AQUÍ: Usar IOptions
    {
        // .Value es lo que extrae la clase mapeada desde el JSON
        _options = options?.Value ?? new DataToolkitOptions();
        _connection = factory.CreateConnection(dbAlias);

        RefreshExecutors();

        if (_options.Logging)
            _logger.Information("[{Prefix}] Motor listo para: {Alias}", _options.Prefix, dbAlias);
    }

    /// <summary>
    /// Obtiene o crea un repositorio genérico para la entidad especificada.
    /// </summary>
    public IGenericRepository<T> Repository<T>() where T : class
    {
        var type = typeof(T);
        if (!_repositories.ContainsKey(type))
        {
            if (_connection.State != ConnectionState.Open) _connection.Open();
            _repositories[type] = new GenericRepository<T>(_connection, _transaction);
        }
        return (IGenericRepository<T>)_repositories[type];
    }

    /// <summary>
    /// Inicia una nueva transacción en la base de datos con monitoreo de rendimiento.
    /// </summary>
    public void BeginTransaction()
    {
        var sw = ToolkitTelemetry.Iniciar(_options);
        try
        {
            if (_connection.State != ConnectionState.Open) _connection.Open();

            _transaction = _connection.BeginTransaction();

            // Re-sincroniza ejecutores para que utilicen la transacción actual
            RefreshExecutors(_transaction);

            ToolkitTelemetry.Finalizar(_logger, _options, "Apertura de Transacción", sw);
        }
        catch (Exception ex)
        {
            ToolkitTelemetry.Error(_logger, _options, "BeginTransaction", ex, sw);
            throw;
        }
    }

    /// <summary>
    /// Confirma los cambios realizados en la transacción actual.
    /// </summary>
    public void Commit()
    {
        var sw = ToolkitTelemetry.Iniciar(_options);
        try
        {
            _transaction?.Commit();
            _transaction = null;
            RefreshExecutors(); // Limpia la transacción de los ejecutores

            ToolkitTelemetry.Finalizar(_logger, _options, "Commit de cambios", sw);
        }
        catch (Exception ex)
        {
            ToolkitTelemetry.Error(_logger, _options, "Commit", ex, sw);
            throw;
        }
    }

    /// <summary>
    /// Revierte los cambios realizados en la transacción actual.
    /// </summary>
    public void Rollback()
    {
        var sw = ToolkitTelemetry.Iniciar(_options);
        try
        {
            _transaction?.Rollback();
            _transaction = null;
            RefreshExecutors();

            ToolkitTelemetry.Finalizar(_logger, _options, "Rollback ejecutado", sw);
        }
        catch (Exception ex)
        {
            ToolkitTelemetry.Error(_logger, _options, "Rollback", ex, sw);
            throw;
        }
    }

    /// <summary>
    /// Sincroniza los ejecutores con el estado actual de la conexión y transacción.
    /// </summary>
    private void RefreshExecutors(IDbTransaction? transaction = null)
    {
        Sql = new SqlExecutor(_connection, transaction);
        StoredProcedureExecutor = new StoredProcedureExecutor(_connection, transaction);
        Fluent = new FluentQuery(Sql);

        // Limpiar repositorios para que se re-instancien con la transacción correcta
        _repositories.Clear();
    }

    // Soporte Asíncrono
    public Task CommitAsync() { Commit(); return Task.CompletedTask; }
    public Task RollbackAsync() { Rollback(); return Task.CompletedTask; }

    /// <summary>
    /// Libera los recursos de conexión y transacciones activas.
    /// </summary>
    public void Dispose()
    {
        if (_disposed) return;

        _transaction?.Dispose();
        _connection?.Dispose();

        if (_options.Logging)
            _logger.Debug("[{Prefix}] Recursos liberados correctamente.", _options.Prefix);

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}