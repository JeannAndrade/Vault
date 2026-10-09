using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Persistence.Context;

namespace Persistence.Test.Context;

public class RoleConfigurationTests
{
    [Fact]
    public void ValoresDoSeed_SaoOsJaGravadosNoSnapshotDeIdentidade()
    {
        // Se este teste falhar porque alguém alterou as constantes, a próxima migration de identidade
        // apagará a role Administrator (e, em cascata, seus vínculos com usuários) e criará outra.
        Assert.Equal("a6555a52-0a06-44c6-977b-3183faa171d9", RoleConfiguration.AdministratorId);
        Assert.Equal("7729cb8f-9cd8-4292-8f0d-67810bd24e5c", RoleConfiguration.AdministratorConcurrencyStamp);
    }

    [Fact]
    public void ModeloDeIdentidade_SemeiaAdministratorComIdEConcurrencyStampFixos()
    {
        var opcoes = new DbContextOptionsBuilder<VaultIdentityDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var contexto = new VaultIdentityDbContext(opcoes);

        var entidade = contexto.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(IdentityRole))!;
        var seed = Assert.Single(entidade.GetSeedData());

        Assert.Equal(RoleConfiguration.AdministratorId, seed["Id"]);
        Assert.Equal(RoleConfiguration.AdministratorConcurrencyStamp, seed["ConcurrencyStamp"]);
        Assert.Equal("Administrator", seed["Name"]);
        Assert.Equal("ADMINISTRATOR", seed["NormalizedName"]);
    }
}
