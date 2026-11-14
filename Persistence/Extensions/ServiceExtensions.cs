using LumiaFoundation.Auth.Persistence;
using LumiaFoundation.Core.Utils;
using LumiaFoundation.EFRepository.Extensions;
using LumiaFoundation.EFRepository.Utils;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Persistence.Context;
using Application.Repositories;
using Persistence.Management;

namespace Persistence.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.ConfigureDatabase(configuration);
        services.ConfigureIdentityDatabase(configuration);
        services.AddScoped<IRepositoryManager, RepositoryManager>();
        return services;
    }

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
        host: GetRequired(configuration, "DBHOST"),
        port: GetRequired(configuration, "DBPORT"),
        user: GetRequired(configuration, "VAULT_DB_USER"),
        password: GetRequired(configuration, "VAULT_DB_PASS"),
        database: GetRequired(configuration, "VAULT_DB_NAME"),
        majorVersion: ParseHelper.ToIntOrDefault(GetRequired(configuration, "DBMAJORVERSION"), 0),
        minorVersion: ParseHelper.ToIntOrDefault(GetRequired(configuration, "DBMINORVERSION"), 0),
        buildVersion: ParseHelper.ToIntOrDefault(GetRequired(configuration, "DBBUILDVERSION"), 0)
    );

    private static string GetRequired(IConfiguration configuration, string key) =>
        configuration[key] is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException(
                $"A configuração obrigatória '{key}' não foi definida.");
}
