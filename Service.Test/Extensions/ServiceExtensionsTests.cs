using LumiaFoundation.AspNetCore.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Persistence.Extensions;
using VaultServiceExtensions = Service.Extensions.ServiceExtensions;

namespace Service.Test.Extensions;

public class ServiceExtensionsTests
{
    [Fact]
    public void ConfigureHealthChecks_RegistersBothDatabaseChecksWithReadyTag()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        Service.Extensions.ServiceExtensions.ConfigureHealthChecks(services);
        using var provider = services.BuildServiceProvider();
        var registrations = provider.GetRequiredService<IOptions<HealthCheckServiceOptions>>().Value.Registrations;

        // Assert
        Assert.Equal(2, registrations.Count);
        Assert.Contains(registrations, r => r.Name == VaultServiceExtensions.DomainDatabaseCheckName);
        Assert.Contains(registrations, r => r.Name == VaultServiceExtensions.IdentityDatabaseCheckName);
        Assert.All(registrations, r => Assert.Contains(LumiaHealthCheckTags.Ready, r.Tags));
    }

    [Fact]
    public async Task ReadinessChecks_WhenDatabaseIsUnreachable_ReportsBothContextsUnhealthy()
    {
        // Arrange: porta 1 em loopback recusa a conexão imediatamente
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DBHOST"] = "127.0.0.1",
                ["DBPORT"] = "1"
            })
            .Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.ConfigureDatabase(configuration);
        services.ConfigureIdentityDatabase(configuration);
        Service.Extensions.ServiceExtensions.ConfigureHealthChecks(services);
        using var provider = services.BuildServiceProvider();

        // Act
        var report = await provider.GetRequiredService<HealthCheckService>()
            .CheckHealthAsync(registration => registration.Tags.Contains(LumiaHealthCheckTags.Ready));

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, report.Status);
        Assert.Equal(HealthStatus.Unhealthy, report.Entries[VaultServiceExtensions.DomainDatabaseCheckName].Status);
        Assert.Equal(HealthStatus.Unhealthy, report.Entries[VaultServiceExtensions.IdentityDatabaseCheckName].Status);
    }
}
