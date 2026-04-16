using DataToolkit.Library.Fluent;
using DataToolkit.Library.Repositories;
using DataToolkit.Library.Sql;
using DataToolkit.Library.StoredProcedures;

namespace DataToolkit.Library.UnitOfWorkLayer;

public interface IUnitOfWork : IDisposable
{
    ISqlExecutor Sql { get; }
    IFluentQuery Fluent { get; } // <--- Agregado
    IStoredProcedureExecutor StoredProcedureExecutor { get; }

    // Cambiamos el nombre a Repository para estandarizar
    IGenericRepository<T> Repository<T>() where T : class;

    void BeginTransaction();
    void Commit();
    Task CommitAsync();
    void Rollback();
    Task RollbackAsync();
}