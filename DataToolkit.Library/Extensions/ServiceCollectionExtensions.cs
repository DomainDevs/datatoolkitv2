using DataToolkit.Library.Connections;
using DataToolkit.Library.Fluent;
using DataToolkit.Library.Sql;
using DataToolkit.Library.UnitOfWorkLayer;
using Microsoft.Extensions.DependencyInjection;

namespace DataToolkit.Library.Extensions;

public class DataToolkitOptions
{
    internal Dictionary<string, (string connectionString, DatabaseProvider provider)> Connections { get; } = new();
    public string DefaultAlias { get; set; }

    public void AddConnection(string alias, string connectionString, DatabaseProvider provider)
    {
        Connections[alias] = (connectionString, provider);
    }
}

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Agrega DataToolkit con múltiples conexiones y alias personalizables.
    /// AddScoped, .NET creará una instancia de SqlExecutor por cada scope (por cada petición HTTP).
    /// </summary>
    public static IServiceCollection AddDataToolkitWith(
        this IServiceCollection services,
        Action<DataToolkitOptions> configure)
    {
        var options = new DataToolkitOptions();
        configure(options);

        var factory = new MultiDbConnectionFactory(options.Connections);
        services.AddSingleton<IDbConnectionFactory>(factory);

        services.AddScoped<IUnitOfWork>(_ =>
            new UnitOfWork(factory, options.DefaultAlias));

        services.AddScoped<SqlExecutor>(_ =>
        {
            var conn = factory.CreateConnection(options.DefaultAlias);
            return new SqlExecutor(conn);
        });

        // 🔥 NUEVO
        services.AddScoped<IFluentQuery, FluentQuery>();

        return services;
    }

    /// <summary>
    /// Acceso rápido para conexión SQL Server
    /// </summary>
    public static IServiceCollection AddDataToolkitSqlServer(
        this IServiceCollection services,
        string connectionString,
        string alias = "SqlServer")
    {
        return services.AddDataToolkitWith(options =>
        {
            options.AddConnection(alias, connectionString, DatabaseProvider.SqlServer);
            options.DefaultAlias = alias;
        });
    }

    /// <summary>
    /// Acceso rápido para conexión Sybase
    /// </summary>
    public static IServiceCollection AddDataToolkitSybase(
        this IServiceCollection services,
        string connectionString,
        string alias = "Sybase")
    {
        return services.AddDataToolkitWith(options =>
        {
            options.AddConnection(alias, connectionString, DatabaseProvider.Sybase);
            options.DefaultAlias = alias;
        });
    }
}
