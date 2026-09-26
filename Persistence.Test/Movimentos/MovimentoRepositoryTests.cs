using Domain.Corretoras;
using Domain.Emissores;
using Domain.Movimentos;
using Domain.Objetivos;
using Domain.Produtos;
using Domain.TiposRenda;
using Microsoft.EntityFrameworkCore;
using Persistence.Context;
using Persistence.Movimentos;

namespace Persistence.Test.Movimentos;

public class MovimentoRepositoryTests
{
    [Fact]
    public async Task GetWithRelatedEntitiesAsync_RetornaMovimentoComNomesDasEntidadesRelacionadas()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var objetivo = new Objetivo { Nome = "Aposentadoria", FontePagadora = "Salário", OndeAplicar = "Corretora X", UserId = userId };
        var tipoRenda = new TipoRenda { Nome = "CDB", UserId = userId };
        var corretora = new Corretora { Nome = "XP", UserId = userId };
        var produto = new Produto { Nome = "CDB Banco Y", UserId = userId };
        var emissor = new Emissor { Nome = "Banco Y", UserId = userId };
        var movimento = new Movimento
        {
            UserId = userId,
            ObjetivoId = objetivo.Id,
            TipoRendaId = tipoRenda.Id,
            CorretoraId = corretora.Id,
            ProdutoId = produto.Id,
            EmissorId = emissor.Id,
            DataInvestimento = DateTime.UtcNow,
            ValorAporte = 1_000m,
            ValorLiquidoAtual = 1_000m
        };

        await using var context = CreateContext();
        context.AddRange(objetivo, tipoRenda, corretora, produto, emissor, movimento);
        await context.SaveChangesAsync();

        var repository = new MovimentoRepository(context);

        // Act
        var resultado = await repository.GetWithRelatedEntitiesAsync(userId, movimento.Id);

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal("Aposentadoria", resultado!.Objetivo.Nome);
        Assert.Equal("CDB", resultado.TipoRenda.Nome);
        Assert.Equal("XP", resultado.Corretora.Nome);
        Assert.Equal("CDB Banco Y", resultado.Produto.Nome);
        Assert.Equal("Banco Y", resultado.Emissor.Nome);
    }

    [Fact]
    public async Task GetWithRelatedEntitiesAsync_WhenMovimentoNaoPertenceAoUsuario_RetornaNull()
    {
        // Arrange
        var donoReal = Guid.NewGuid();
        var objetivo = new Objetivo { Nome = "Reserva", FontePagadora = "Salário", OndeAplicar = "Corretora X", UserId = donoReal };
        var tipoRenda = new TipoRenda { Nome = "Tesouro", UserId = donoReal };
        var corretora = new Corretora { Nome = "Rico", UserId = donoReal };
        var produto = new Produto { Nome = "Tesouro Selic", UserId = donoReal };
        var emissor = new Emissor { Nome = "Tesouro Nacional", UserId = donoReal };
        var movimento = new Movimento
        {
            UserId = donoReal,
            ObjetivoId = objetivo.Id,
            TipoRendaId = tipoRenda.Id,
            CorretoraId = corretora.Id,
            ProdutoId = produto.Id,
            EmissorId = emissor.Id,
            DataInvestimento = DateTime.UtcNow,
            ValorAporte = 500m,
            ValorLiquidoAtual = 500m
        };

        await using var context = CreateContext();
        context.AddRange(objetivo, tipoRenda, corretora, produto, emissor, movimento);
        await context.SaveChangesAsync();

        var repository = new MovimentoRepository(context);

        // Act
        var resultado = await repository.GetWithRelatedEntitiesAsync(Guid.NewGuid(), movimento.Id);

        // Assert
        Assert.Null(resultado);
    }

    private static VaultDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<VaultDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new VaultDbContext(options);
    }
}
