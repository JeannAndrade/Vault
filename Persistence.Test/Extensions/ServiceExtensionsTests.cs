using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Persistence.Context;
using Persistence.Extensions;
using Application.Repositories;

namespace Persistence.Test;

public class ServiceExtensionsTests
{
  [Fact]
  public void ServiceExtensions_RegisterExpectedServices()
  {
    var services = new ServiceCollection();
    var configuration = new ConfigurationBuilder().Build();

    services.AddPersistence(configuration);

    Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IRepositoryManager));
    Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(VaultDbContext));
  }
}
