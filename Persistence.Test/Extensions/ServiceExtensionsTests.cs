using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Persistence.Context;
using Persistence.Extensions;
using Persistence.Managment;

namespace Persistence.Test;

public class ServiceExtensionsTests
{
  [Fact]
  public void ServiceExtensions_RegisterExpectedServices()
  {
    var services = new ServiceCollection();
    var configuration = new ConfigurationBuilder().Build();

    services.ConfigureRepositoryManager();
    services.ConfigureDatabase(configuration);

    Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IRepositoryManager));
    Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(VaultDbContext));
  }
}