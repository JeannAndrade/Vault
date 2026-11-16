using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
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
    var configuration = new ConfigurationBuilder()
      .AddInMemoryCollection(new Dictionary<string, string?>
      {
        ["VAULT_DB_HOST"] = "127.0.0.1",
        ["VAULT_DB_PORT"] = "3306",
        ["VAULT_DB_USER"] = "test-user",
        ["VAULT_DB_PASS"] = "test-password",
        ["VAULT_DB_NAME"] = "test-database",
        ["DBMAJORVERSION"] = "10",
        ["DBMINORVERSION"] = "0",
        ["DBBUILDVERSION"] = "0"
      })
      .Build();

    services.AddPersistence(configuration);

    Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IRepositoryManager));
    Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(VaultDbContext));
  }
}
