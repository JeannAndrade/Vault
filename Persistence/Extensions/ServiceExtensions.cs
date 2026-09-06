using LumiaFoundation.Core.Utils;
using LumiaFoundation.EFRepository.Extensions;
using LumiaFoundation.EFRepository.Utils;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Persistence.Context;
using Persistence.Managment;

namespace Persistence.Extensions
{
    public static class ServiceExtensions
    {
        public static void ConfigureRepositoryManager(this IServiceCollection services) =>
            services.AddScoped<IRepositoryManager, RepositoryManager>();

        public static IServiceCollection ConfigureDatabase(this IServiceCollection services, IConfiguration configuration)
        {
            var host = configuration["DBHOST"] ?? "localhost";
            var port = configuration["DBPORT"] ?? "3306";
            var user = configuration["CODEMAZE_DB_USER"] ?? "example-user";
            var password = configuration["CODEMAZE_DB_PASS"] ?? "my_cool_secret";
            var database = configuration["CODEMAZE_DB_NAME"] ?? "my_database";
            var majorVersion = ParseHelper.ToIntOrDefault(configuration["DBMAJORVERSION"] ?? "", 9);
            var minorVersion = ParseHelper.ToIntOrDefault(configuration["DBMINORVERSION"] ?? "", 7);
            var buildVersion = ParseHelper.ToIntOrDefault(configuration["DBBUILDVERSION"] ?? "", 2);

            var dbConnectionHelper = new DbConnectionHelper(host: host, port: port, user: user, password: password, database: database, majorVersion: majorVersion, minorVersion: minorVersion, buildVersion: buildVersion);

            services.ConfigureMySqlDbDatabase<VaultDbContext>(dbConnectionHelper, "Persistence");

            return services;
        }
    }
}