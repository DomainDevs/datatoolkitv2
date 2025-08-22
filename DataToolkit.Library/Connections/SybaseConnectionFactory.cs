using AdoNetCore.AseClient;
using Microsoft.Extensions.Configuration;
using System.Data;

namespace DataToolkit.Library.Connections;

public class SybaseConnectionFactory : IDbConnectionFactory
{
    private readonly IConfiguration _configuration;

    public SybaseConnectionFactory(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public IDbConnection CreateConnection(string dbAlias)
    {
        var connectionString = _configuration.GetConnectionString(dbAlias);
        if (string.IsNullOrEmpty(connectionString))
            throw new InvalidOperationException($"No se encontró la cadena de conexión para el alias '{dbAlias}'");

        return new AseConnection(connectionString);
    }
}