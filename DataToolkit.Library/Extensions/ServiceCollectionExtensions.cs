using DataToolkit.Library.Connections;
using DataToolkit.Library.Sql;
using DataToolkit.Library.UnitOfWorkLayer;
using DataToolkit.Library.Fluent;
using Microsoft.Extensions.DependencyInjection;

namespace DataToolkit.Library.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Agrega DataToolkit con SQL Server usando solo la cadena de conexión.
        /// </summary>
        public static IServiceCollection AddDataToolkitSqlServer(
            this IServiceCollection services,
            string connectionString,
            string alias = "SqlServer")
        {
            services.AddSingleton<IDbConnectionFactory>(_ =>
                new MultiDbConnectionFactory(new Dictionary<string, (string, DatabaseProvider)>
                {
                    { alias, (connectionString, DatabaseProvider.SqlServer) }
                })
            );

            services.AddScoped<ISqlExecutor>(sp =>
            {
                var factory = sp.GetRequiredService<IDbConnectionFactory>();
                var conn = factory.CreateConnection(alias);
                return new SqlExecutor(conn);
            });

            services.AddScoped<IFluentQuery, FluentQuery>();

            services.AddScoped(sp =>
                new UnitOfWork(sp.GetRequiredService<IDbConnectionFactory>(), alias)
            );

            return services;
        }
    }
}
