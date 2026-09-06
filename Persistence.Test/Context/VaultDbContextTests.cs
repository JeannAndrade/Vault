using Domain.Bancos;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Test;

public class VaultDbContextTests
{
  [Fact]
  public void Context_ExposesExpectedMappingAndSeedData()
  {
    using var context = CreateContext();
    var entity = context.Model.FindEntityType(typeof(Banco))!;
    var nome = entity.FindProperty(nameof(Banco.Nome))!;
    var id = entity.FindProperty(nameof(Banco.Id))!;

    Assert.Equal("Bancos", entity.GetTableName());
    Assert.Equal("BancoId", id.GetColumnName());
    Assert.Equal(60, nome.GetMaxLength());
    Assert.False(nome.IsNullable);
    Assert.Equal(4, BancoMappingSeedNames());
  }

  private static VaultDbContext CreateContext()
  {
    var options = new DbContextOptionsBuilder<VaultDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;

    return new VaultDbContext(options);
  }

  private static int BancoMappingSeedNames() =>
      Persistence.Mappings.BancoMapping.GetBancos().Count(banco => !string.IsNullOrWhiteSpace(banco.Nome));
}