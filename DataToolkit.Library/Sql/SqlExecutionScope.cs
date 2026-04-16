using DataToolkit.Library.Connections;
using System.Data;
using System.Linq;

namespace DataToolkit.Library.Sql;

public class SqlExecutionScope
{
    private readonly IDbConnectionFactory _factory;

    public SqlExecutionScope(IDbConnectionFactory factory)
    {
        _factory = factory;
    }

    /*
    public async Task<T> UseConnectionAsync<T>(
        Func<IDbConnection, Task<T>> action)
    {
        using var connection = _factory.CreateConnection();
        return null;  await action(connection);
    }
    */
}