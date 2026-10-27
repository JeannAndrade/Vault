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

    [Fact]
    public void Movimento_PossuiIndiceCompostoUserIdEstaAtivoDataAtualizacaoValorLiquido()
    {
        var opcoes = new DbContextOptionsBuilder<VaultDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var contexto = new VaultDbContext(opcoes);

        var entidade = contexto.Model.FindEntityType(typeof(Movimento))!;

        var indice = entidade.GetIndexes().SingleOrDefault(i =>
            i.Properties.Select(p => p.Name).SequenceEqual(
                [nameof(Movimento.UserId), nameof(Movimento.EstaAtivo), nameof(Movimento.DataAtualizacaoValorLiquido)]));

        Assert.NotNull(indice);
        Assert.Equal("IX_Movimentos_UserId_EstaAtivo_DataAtualizacaoValorLiquido", indice!.GetDatabaseName());
    }

    [Fact]
    public void Movimento_PossuiMotivoEncerramentoAnulavelComConversorTextoEComprimento20()
    {
        var opcoes = new DbContextOptionsBuilder<VaultDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var contexto = new VaultDbContext(opcoes);

        var entidade = contexto.Model.FindEntityType(typeof(Movimento))!;
        var propriedade = entidade.FindProperty(nameof(Movimento.MotivoEncerramento));

        Assert.NotNull(propriedade);
        Assert.True(propriedade!.IsNullable);
        Assert.Equal(20, propriedade.GetMaxLength());
        Assert.Equal(typeof(string), propriedade.GetProviderClrType());
    }

    [Fact]
    public void Movimento_PossuiDataEncerramentoAnulavelComoDateTimeNullable()
    {
        var opcoes = new DbContextOptionsBuilder<VaultDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        using var contexto = new VaultDbContext(opcoes);

        var entidade = contexto.Model.FindEntityType(typeof(Movimento))!;
        var propriedade = entidade.FindProperty(nameof(Movimento.DataEncerramento));

        Assert.NotNull(propriedade);
        Assert.True(propriedade!.IsNullable);
        Assert.Equal(typeof(DateTime?), propriedade.ClrType);
    }
}
