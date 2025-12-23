using DataToolkit.Library.Connections;
using DataToolkit.Library.Fluent;
using DataToolkit.Library.Sql;
using DataToolkit.Library.UnitOfWorkLayer;
using Microsoft.Extensions.DependencyInjection;

namespace DataToolkit.Library.Extensions;

public class DataToolkitOptions
{
    internal Dictionary<string, (string connectionString, DatabaseProvider provider)> Connections { get; } = new();
    public string DefaultAlias { get; set; } = default!;

    public void AddConnection(string alias, string connectionString, DatabaseProvider provider)
    {
        Connections[alias] = (connectionString, provider);
    }
}

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registro principal estilo EF Core.
    /// Toda la configuración vive dentro del contenedor DI.
    /// </summary>
    public static IServiceCollection AddDataToolkitWith(
        this IServiceCollection services,
        Action<DataToolkitOptions> configure)
    {
        // 1️⃣ Construir opciones
        var options = new DataToolkitOptions();
        configure(options);

        if (string.IsNullOrWhiteSpace(options.DefaultAlias))
            throw new InvalidOperationException("DataToolkit: DefaultAlias no fue configurado.");

        // 2️⃣ Registrar opciones como singleton (CLAVE)
        services.AddSingleton(options);

        // 3️⃣ Factory de conexiones (singleton)
        services.AddSingleton<IDbConnectionFactory>(_ =>
            new MultiDbConnectionFactory(options.Connections));

        // 4️⃣ UnitOfWork (scoped)
        services.AddScoped<IUnitOfWork>(sp =>
        {
            var factory = sp.GetRequiredService<IDbConnectionFactory>();
            var opts = sp.GetRequiredService<DataToolkitOptions>();
            return new UnitOfWork(factory, opts.DefaultAlias);
        });

        // 5️⃣ SqlExecutor (scoped, EF-like)
        services.AddScoped<ISqlExecutor>(sp =>
        {
            var factory = sp.GetRequiredService<IDbConnectionFactory>();
            var opts = sp.GetRequiredService<DataToolkitOptions>();

            var connection = factory.CreateConnection(opts.DefaultAlias);
            return new SqlExecutor(connection);
        });

        // 6️⃣ FluentQuery (scoped)
        services.AddScoped<IFluentQuery, FluentQuery>();

        return services;
    }

    /// <summary>
    /// Acceso rápido SQL Server (single DB)
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
    /// Acceso rápido Sybase (single DB)
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
