using System.Data;

namespace DataToolkit.Library.Connections;
public interface IDbConnectionFactory
{
    IDbConnection CreateConnection(string dbAlias);
}
