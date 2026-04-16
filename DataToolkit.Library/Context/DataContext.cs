using DataToolkit.Library.Fluent;
using DataToolkit.Library.Repositories;
using DataToolkit.Library.Sql;
using DataToolkit.Library.StoredProcedures;
using DataToolkit.Library.UnitOfWorkLayer;
using System.Collections.Concurrent; // Para Thread-safety

namespace DataToolkit.Library.Context;

public sealed class DataContext : IDisposable
{
    private readonly IUnitOfWork _unitOfWork;
    // Cambiamos Dictionary por ConcurrentDictionary para seguridad total
    private readonly ConcurrentDictionary<Type, object> _repositoryCache = new();
    private bool _disposed;

    public DataContext(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    // Herramientas core
    public ISqlExecutor Sql => _unitOfWork.Sql;
    public IFluentQuery Fluent => _unitOfWork.Fluent;
    public IStoredProcedureExecutor StoredProcedures => _unitOfWork.StoredProcedureExecutor;

    // Repositorio con GetOrAdd para máxima eficiencia y seguridad
    public IGenericRepository<T> Repository<T>() where T : class
    {
        return (IGenericRepository<T>)_repositoryCache.GetOrAdd(typeof(T), _ =>
            _unitOfWork.Repository<T>());
    }

    // Transacciones con validación de estado
    public void BeginTransaction() => _unitOfWork.BeginTransaction();
    public void Commit() => _unitOfWork.Commit();
    public void Rollback() => _unitOfWork.Rollback();

    // Implementación robusta de IDisposable
    public void Dispose()
    {
        if (_disposed) return;

        _repositoryCache.Clear();
        _unitOfWork.Dispose();
        _disposed = true;
    }
}