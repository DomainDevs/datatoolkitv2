using DataToolkit.Library.Repositories;
using DataToolkit.Library.Sql;
using DataToolkit.Library.StoredProcedures;

namespace DataToolkit.Library.UnitOfWorkLayer
{
    public interface IUnitOfWork
    {
        SqlExecutor Sql { get; }
        IStoredProcedureExecutor StoredProcedureExecutor { get; }

        void BeginTransaction();
        void Commit();
        void Dispose();
        IGenericRepository<T> GetRepository<T>() where T : class;
        void Rollback();
        Task CommitAsync();
        Task RollbackAsync();
    }
}