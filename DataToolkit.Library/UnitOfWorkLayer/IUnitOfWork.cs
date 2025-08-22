using DataToolkit.Library.Repositories;
using DataToolkit.Library.Sql;
using DataToolkit.Library.StoredProcedures;

namespace DataToolkit.Library.UnitOfWorkLayer;

public interface IUnitOfWork : IDisposable
{
    IRepository<T> GetRepository<T>() where T : class;

    void BeginTransaction();
    void Commit();
    void Rollback();

    IStoredProcedureExecutor StoredProcedureExecutor { get; }
    SqlExecutor Sql { get; } // ✅ NUEVO
}