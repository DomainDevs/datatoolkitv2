using System.Data;

namespace DataToolkit.Library.Context;
public interface IDbContext
{
    IDbConnection Connection { get; }
    IDbTransaction? Transaction { get; }
}