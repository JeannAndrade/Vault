using Persistence.Bancos;
using Persistence.Context;
using Persistence.Managment;
using Microsoft.EntityFrameworkCore;

namespace Persistence.Test;

public class RepositoryManagerTests
{
  [Fact]
  public void RepositoryManager_CreatesAndCachesBancoRepository()
  {
    using var context = CreateContext();
    var manager = new RepositoryManager(context);

    var first = manager.Banco;
    var second = manager.Banco;

    Assert.IsType<BancoRepository>(first);
    Assert.Same(first, second);
  }

  private static VaultDbContext CreateContext()
  {
    var options = new DbContextOptionsBuilder<VaultDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;

    return new VaultDbContext(options);
  }
}