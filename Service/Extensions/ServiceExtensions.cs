using LumiaFoundation.AspNetCore.HealthChecks;
using LumiaFoundation.EFRepository.Extensions;
using Persistence.Context;

namespace Service.Extensions;

public static class ServiceExtensions
{
    public const string DomainDatabaseCheckName = "banco-dominio";
    public const string IdentityDatabaseCheckName = "banco-identidade";

    public static void ConfigureCors(this IServiceCollection services) => services.AddCors(options =>
    {
        options.AddPolicy("CorsPolicy", builder => builder.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
    });

    /// <summary>
    /// Registra as checks de readiness: a Api só fica apta quando os dois contextos
    /// (domínio e identidade) conseguem se conectar ao banco.
    /// </summary>
    public static void ConfigureHealthChecks(this IServiceCollection services) => services
        .AddHealthChecks()
        .AddDbContextHealthCheck<VaultDbContext>(DomainDatabaseCheckName, tags: [LumiaHealthCheckTags.Ready])
        .AddDbContextHealthCheck<VaultIdentityDbContext>(IdentityDatabaseCheckName, tags: [LumiaHealthCheckTags.Ready]);
}
