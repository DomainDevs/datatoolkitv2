using AdoNetCore.AseClient;
using Microsoft.Data.SqlClient;
using System.Collections.Concurrent;
using System.Data;

namespace DataToolkit.Library.Connections;

/// <summary>
/// Fábrica que permite crear múltiples conexiones a diferentes motores de base de datos,
/// diferenciadas por alias.
/// </summary>
public class MultiDbConnectionFactory : IDbConnectionFactory
{
    private readonly ConcurrentDictionary<string, (string connectionString, DatabaseProvider provider)> _config =
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Inicializa la fábrica con múltiples alias y motores de base de datos.
    /// </summary>
    /// <param name="configurations">Diccionario con alias, cadena de conexión y tipo de proveedor.</param>
    public MultiDbConnectionFactory(Dictionary<string, (string connectionString, DatabaseProvider provider)> configurations)
    {
        if (configurations == null || configurations.Count == 0)
            throw new ArgumentException("Debe proporcionar al menos una cadena de conexión.");

        foreach (var kv in configurations)
            _config[kv.Key] = kv.Value;
    }

    /// <summary>
    /// Crea una nueva conexión basada en el alias especificado.
    /// </summary>
    /// <param name="alias">Alias definido en la configuración</param>
    /// <returns>IDbConnection abierta o lista para usar</returns>
    public IDbConnection CreateConnection(string alias = "MainSql")
    {
        if (!_config.TryGetValue(alias, out var entry))
            throw new InvalidOperationException($"No se encontró una configuración para el alias: '{alias}'");

        return entry.provider switch
        {
            DatabaseProvider.SqlServer => new SqlConnection(entry.connectionString),
            DatabaseProvider.Sybase => new AseConnection(entry.connectionString),
            _ => throw new NotSupportedException($"Proveedor no soportado: {entry.provider}")
        };
    }
}
