using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Persistence.Context;
using Persistence.Extensions;

namespace Persistence.ContextFactory;

public class IdentityContextFactory : IDesignTimeDbContextFactory<VaultIdentityDbContext>
{
    public VaultIdentityDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
        .SetBasePath(Directory.GetCurrentDirectory())
        .AddEnvironmentVariables()
        .Build();

        var services = new ServiceCollection();

        services.ConfigureIdentityDatabase(configuration);

        return services.BuildServiceProvider().GetRequiredService<VaultIdentityDbContext>();
    }
}
