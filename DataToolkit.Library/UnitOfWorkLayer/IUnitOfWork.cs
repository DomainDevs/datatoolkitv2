using DataToolkit.Library.Fluent;
using DataToolkit.Library.Repositories;
using DataToolkit.Library.Sql;
using DataToolkit.Library.StoredProcedures;

namespace DataToolkit.Library.UnitOfWorkLayer;

public interface IUnitOfWork : IDisposable
{
    // Ejecutores de comandos
    ISqlExecutor Sql { get; }
    IFluentQuery Fluent { get; }
    IStoredProcedureExecutor StoredProcedureExecutor { get; }

    // Acceso a datos tipados
    IGenericRepository<T> Repository<T>() where T : class;

    // Gestión de Transacciones (Sincrónico)
    void BeginTransaction();
    void Commit();
    void Rollback();
}