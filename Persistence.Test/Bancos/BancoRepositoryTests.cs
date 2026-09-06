using Domain.Bancos;
using Microsoft.EntityFrameworkCore;
using Persistence.Bancos;
using Persistence.Context;

namespace Persistence.Test;

public class BancoRepositoryTests
{
  [Fact]
  public async Task GetAllBancosAsync_ReturnsBancosOrderedByName()
  {
    await using var context = CreateContext();
    context.Bancos.AddRange(
        new Banco { Nome = "Zeta" },
        new Banco { Nome = "Alfa" },
        new Banco { Nome = "Beta" });
    await context.SaveChangesAsync();
    context.ChangeTracker.Clear();

    var repository = new BancoRepository(context);

    var bancos = (await repository.GetAllBancosAsync(trackChanges: false)).ToList();

    Assert.Equal(["Alfa", "Beta", "Zeta"], bancos.Select(banco => banco.Nome));
    Assert.Empty(context.ChangeTracker.Entries<Banco>());
  }

  [Fact]
  public async Task GetBancoAsync_ReturnsMatchingBancoAndHonorsTrackingOption()
  {
    await using var context = CreateContext();
    var banco = new Banco { Nome = "Inter" };
    context.Bancos.Add(banco);
    await context.SaveChangesAsync();
    var repository = new BancoRepository(context);

    var result = await repository.GetBancoAsync(banco.Id, trackChanges: true);
    var missing = await repository.GetBancoAsync(Guid.NewGuid(), trackChanges: false);

    Assert.Equal(banco.Id, result!.Id);
    Assert.Null(missing);
    Assert.Equal(EntityState.Unchanged, context.Entry(result!).State);
  }

  [Fact]
  public async Task CreateBanco_AddsBancoToContext()
  {
    await using var context = CreateContext();
    var repository = new BancoRepository(context);
    var banco = new Banco { Nome = "Nubank" };

    repository.CreateBanco(banco);
    await context.SaveChangesAsync();

    Assert.Same(banco, await context.Bancos.SingleAsync());
  }

  private static VaultDbContext CreateContext()
  {
    var options = new DbContextOptionsBuilder<VaultDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;

    return new VaultDbContext(options);
  }
}