using Domain.Movimentos;
using Domain.Objetivos;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;
using Persistence.Objetivos;

namespace Persistence.Test.Objetivos;

public class ObjetivoRepositoryTests
{
    [Fact]
    public async Task GetAllWithRelatedEntitiesAsync_FiltersByOwnerOrdersAndLoadsMovementsWithoutTracking()
    {
        var ownerId = Guid.NewGuid();
        var outroOwnerId = Guid.NewGuid();
        var objetivoB = CreateObjective(ownerId, "Viagem");
        var objetivoA = CreateObjective(ownerId, "Aposentadoria");
        var objetivoDeOutroUsuario = CreateObjective(outroOwnerId, "Objetivo alheio");
        objetivoA.Movimentos.Add(new Movimento
        {
            UserId = ownerId,
            ObjetivoId = objetivoA.Id,
            ValorAporte = 100m,
            ValorLiquidoAtual = 110m
        });
        objetivoB.Movimentos.Add(new Movimento
        {
            UserId = ownerId,
            ObjetivoId = objetivoB.Id,
            ValorAporte = 200m,
            ValorLiquidoAtual = 220m
        });

        await using var context = CreateContext();
        context.AddRange(objetivoB, objetivoDeOutroUsuario, objetivoA);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var repository = new ObjetivoRepository(context);

        var result = (await repository.GetAllWithRelatedEntitiesAsync(ownerId)).ToList();

        Assert.Equal(new[] { "Aposentadoria", "Viagem" }, result.Select(objective => objective.Nome));
        Assert.Equal(100m, Assert.Single(result[0].Movimentos).ValorAporte);
        Assert.Equal(200m, Assert.Single(result[1].Movimentos).ValorAporte);
        Assert.All(result, objective => Assert.Equal(EntityState.Detached, context.Entry(objective).State));
        Assert.All(
            result.SelectMany(objective => objective.Movimentos),
            movement => Assert.Equal(EntityState.Detached, context.Entry(movement).State));
    }

    private static Objetivo CreateObjective(Guid ownerId, string name) =>
        new() { UserId = ownerId, Nome = name };

    private static VaultDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<VaultDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new VaultDbContext(options);
    }
}
