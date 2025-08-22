using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace DataToolkit.Library.Connections;

public class SqlServerConnectionFactory : IDbConnectionFactory
{
    private readonly IConfiguration _configuration;

    public SqlServerConnectionFactory(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public IDbConnection CreateConnection(string dbAlias)
    {
        var connectionString = _configuration.GetConnectionString(dbAlias);
        if (string.IsNullOrEmpty(connectionString))
            throw new InvalidOperationException($"No se encontró la cadena de conexión para el alias '{dbAlias}'");

        return new SqlConnection(connectionString);
    }
}
