using LumiaFoundation.Auth.Persistence;
using LumiaFoundation.Core.Utils;
using LumiaFoundation.EFRepository.Extensions;
using LumiaFoundation.EFRepository.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Persistence.Context;
using Persistence.Managment;

namespace Persistence.Extensions;

public static class ServiceExtensions
{
    public static void ConfigureRepositoryManager(this IServiceCollection services) =>
        services.AddScoped<IRepositoryManager, RepositoryManager>();

    public static IServiceCollection ConfigureDatabase(this IServiceCollection services, IConfiguration configuration) =>
    services.ConfigureMariaDbContext<VaultDbContext>(configuration);

    public static IServiceCollection ConfigureIdentityDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        services.ConfigureMariaDbContext<VaultIdentityDbContext>(configuration);
        services.AddScoped<IdentityContext>(provider =>
            provider.GetRequiredService<VaultIdentityDbContext>());

        return services;
    }

    private static IServiceCollection ConfigureMariaDbContext<TContext>(
    this IServiceCollection services,
    IConfiguration configuration)
    where TContext : DbContext
    {
        var dbConnectionHelper = BuildDbConnectionHelper(configuration);
        services.ConfigureMariaDbDatabase<TContext>(dbConnectionHelper, "Persistence");
        return services;
    }

    private static DbConnectionHelper BuildDbConnectionHelper(IConfiguration configuration) =>
    new(
        host: configuration["DBHOST"] ?? "localhost",
        port: configuration["DBPORT"] ?? "3307",
        user: configuration["VAULT_DB_USER"] ?? "example-user",
        password: configuration["VAULT_DB_PASS"] ?? "my_cool_secret",
        database: configuration["VAULT_DB_NAME"] ?? "my_database",
        majorVersion: ParseHelper.ToIntOrDefault(configuration["DBMAJORVERSION"] ?? "", 12),
        minorVersion: ParseHelper.ToIntOrDefault(configuration["DBMINORVERSION"] ?? "", 3),
        buildVersion: ParseHelper.ToIntOrDefault(configuration["DBBUILDVERSION"] ?? "", 3)
    );
}
