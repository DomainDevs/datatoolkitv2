using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DataToolkit.Library.Connections;
using DataToolkit.Library.Repositories;
using DataToolkit.Library.Sql;
using DataToolkit.Library.StoredProcedures;
using DataToolkit.Library.UnitOfWorkLayer;

namespace DataToolkit.Library.Context;

public sealed class DataContext : IDisposable
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly Dictionary<Type, object> _repositoryCache = new();

    public DataContext(IDbConnectionFactory connectionFactory)
    {
        // UnitOfWork ya crea SqlExecutor, SP executor y maneja la conexión
        _unitOfWork = new UnitOfWork(connectionFactory);
    }

    // Exponer SQL y Stored Procedures vía UnitOfWork
    public SqlExecutor Sql => _unitOfWork.Sql;
    public IStoredProcedureExecutor StoredProcedures => _unitOfWork.StoredProcedureExecutor;

    // Obtener repositorio genérico
    public IGenericRepository<T> Repository<T>() where T : class
    {
        var type = typeof(T);
        if (!_repositoryCache.TryGetValue(type, out var repo))
        {
            repo = _unitOfWork.GetRepository<T>();
            _repositoryCache[type] = repo;
        }
        return (IGenericRepository<T>)repo;
    }

    // Manejo de transacciones
    public void BeginTransaction() => _unitOfWork.BeginTransaction();
    public void Commit() => _unitOfWork.Commit();
    public Task CommitAsync() => _unitOfWork.CommitAsync();
    public void Rollback() => _unitOfWork.Rollback();
    public Task RollbackAsync() => _unitOfWork.RollbackAsync();

    public void Dispose()
    {
        _repositoryCache.Clear();
        _unitOfWork.Dispose();
    }
}
