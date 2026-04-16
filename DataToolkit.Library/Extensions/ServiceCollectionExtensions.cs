using DataToolkit.Library.Connections;
using DataToolkit.Library.Context;
using DataToolkit.Library.Fluent;
using DataToolkit.Library.Sql;
using DataToolkit.Library.UnitOfWorkLayer;
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
            // 1. La fábrica es Singleton
            services.AddSingleton<IDbConnectionFactory>(_ =>
                new MultiDbConnectionFactory(new Dictionary<string, (string, DatabaseProvider)>
                {
            { alias, (connectionString, DatabaseProvider.SqlServer) }
                })
            );

            // 2. El UnitOfWork es la UNICA entrada Scoped
            services.AddScoped<IUnitOfWork>(sp =>
                new UnitOfWork(sp.GetRequiredService<IDbConnectionFactory>(), alias)
            );

            return services;
        }
    }
}
