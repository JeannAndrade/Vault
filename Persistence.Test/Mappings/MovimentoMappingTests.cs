using Domain.Movimentos;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;

namespace Persistence.Test.Mappings;

public class MovimentoMappingTests
{
    [Fact]
    public void Movimento_PossuiIndiceCompostoUserIdDataInvestimento()
    {
        // Sustenta a listagem paginada (ADR 0009). Se este teste falhar, o índice foi removido
        // ou alterado: reavalie a ordenação de MovimentoRepository.GetPagedWithRelatedEntitiesAsync.
        var opcoes = new DbContextOptionsBuilder<VaultDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var contexto = new VaultDbContext(opcoes);

        var entidade = contexto.Model.FindEntityType(typeof(Movimento))!;

        var indice = entidade.GetIndexes().SingleOrDefault(i =>
            i.Properties.Select(p => p.Name)
                .SequenceEqual([nameof(Movimento.UserId), nameof(Movimento.DataInvestimento)]));

        Assert.NotNull(indice);
    }

    [Fact]
    public void Movimento_PossuiIndiceCompostoUserIdEstaAtivoDataVencimento()
    {
        // Sustenta os próximos vencimentos (ADR 0010). Se este teste falhar, o índice foi removido
        // ou alterado: reavalie a ordenação de MovimentoRepository.GetProximosVencimentosAsync.
        var opcoes = new DbContextOptionsBuilder<VaultDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var contexto = new VaultDbContext(opcoes);

        var entidade = contexto.Model.FindEntityType(typeof(Movimento))!;

        var indice = entidade.GetIndexes().SingleOrDefault(i =>
            i.Properties.Select(p => p.Name)
                .SequenceEqual([nameof(Movimento.UserId), nameof(Movimento.EstaAtivo), nameof(Movimento.DataVencimento)]));

        Assert.NotNull(indice);
    }
}
