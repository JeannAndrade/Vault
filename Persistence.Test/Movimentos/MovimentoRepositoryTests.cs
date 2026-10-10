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

    [Fact]
    public async Task ExistemPorObjetivoAsync_WhenExisteMovimentoParaOObjetivo_ReturnsTrue()
    {
        // Arrange
        var objetivoId = Guid.NewGuid();
        var movimento = new Movimento
        {
            ObjetivoId = objetivoId,
            TipoRendaId = Guid.NewGuid(),
            CorretoraId = Guid.NewGuid(),
            ProdutoId = Guid.NewGuid(),
            EmissorId = Guid.NewGuid(),
            DataInvestimento = DateTime.UtcNow,
            ValorAporte = 100m,
            ValorLiquidoAtual = 100m
        };

        await using var context = CreateContext();
        context.Add(movimento);
        await context.SaveChangesAsync();

        var repository = new MovimentoRepository(context);

        // Act
        var resultado = await repository.ExistemPorObjetivoAsync(objetivoId);

        // Assert
        Assert.True(resultado);
    }

    [Fact]
    public async Task ExistemPorObjetivoAsync_WhenNaoExisteMovimentoParaOObjetivo_ReturnsFalse()
    {
        // Arrange
        await using var context = CreateContext();
        var repository = new MovimentoRepository(context);

        // Act
        var resultado = await repository.ExistemPorObjetivoAsync(Guid.NewGuid());

        // Assert
        Assert.False(resultado);
    }

    [Fact]
    public async Task ExistemPorObjetivoAsync_WhenExisteMovimentoParaACorretora_ReturnsTrue()
    {
        // Arrange
        var corretoraId = Guid.NewGuid();
        var movimento = new Movimento
        {
            ObjetivoId = Guid.NewGuid(),
            TipoRendaId = Guid.NewGuid(),
            CorretoraId = corretoraId,
            ProdutoId = Guid.NewGuid(),
            EmissorId = Guid.NewGuid(),
            DataInvestimento = DateTime.UtcNow,
            ValorAporte = 100m,
            ValorLiquidoAtual = 100m
        };

        await using var context = CreateContext();
        context.Add(movimento);
        await context.SaveChangesAsync();

        var repository = new MovimentoRepository(context);

        // Act
        var resultado = await repository.ExistemPorCorretoraAsync(corretoraId);

        // Assert
        Assert.True(resultado);
    }

    [Fact]
    public async Task ExistemPorCorretoraAsync_WhenNaoExisteMovimentoParaACorretora_ReturnsFalse()
    {
        // Arrange
        await using var context = CreateContext();
        var repository = new MovimentoRepository(context);

        // Act
        var resultado = await repository.ExistemPorCorretoraAsync(Guid.NewGuid());

        // Assert
        Assert.False(resultado);
    }

    [Fact]
    public async Task GetProximosVencimentosAsync_RetornaSomenteAtivosOrdenadosPorDataVencimento()
    {
        var userId = Guid.NewGuid();
        await using var context = CreateContext();

        var movimentoVenceEm10Dias = AdicionarMovimento(context, userId, new DateTime(2026, 1, 1));
        movimentoVenceEm10Dias.EstaAtivo = true;
        movimentoVenceEm10Dias.DataVencimento = new DateTime(2026, 1, 10);

        var movimentoVenceEm5Dias = AdicionarMovimento(context, userId, new DateTime(2026, 1, 2));
        movimentoVenceEm5Dias.EstaAtivo = true;
        movimentoVenceEm5Dias.DataVencimento = new DateTime(2026, 1, 5);

        var movimentoJaVenceu = AdicionarMovimento(context, userId, new DateTime(2026, 1, 3));
        movimentoJaVenceu.EstaAtivo = true;
        movimentoJaVenceu.DataVencimento = new DateTime(2025, 12, 29);

        var movimentoInativo = AdicionarMovimento(context, userId, new DateTime(2026, 1, 4));
        movimentoInativo.EstaAtivo = false;
        movimentoInativo.DataVencimento = new DateTime(2026, 1, 7);

        var movimentoDeOutroUsuario = AdicionarMovimento(context, Guid.NewGuid(), new DateTime(2026, 1, 5));
        movimentoDeOutroUsuario.EstaAtivo = true;
        movimentoDeOutroUsuario.DataVencimento = new DateTime(2026, 1, 12);

        var movimentoSemDataVencimento = AdicionarMovimento(context, userId, new DateTime(2026, 1, 6));
        movimentoSemDataVencimento.EstaAtivo = true;
        movimentoSemDataVencimento.DataVencimento = null;

        await context.SaveChangesAsync();
        var repository = new MovimentoRepository(context);

        var resultado = await repository.GetProximosVencimentosAsync(userId, quantidade: 15);

        Assert.Equal(
            [movimentoJaVenceu.Id, movimentoVenceEm5Dias.Id, movimentoVenceEm10Dias.Id],
            resultado.Select(m => m.Id));
        Assert.All(resultado, m => Assert.True(m.EstaAtivo));
        Assert.All(resultado, m => Assert.NotNull(m.DataVencimento));
        Assert.DoesNotContain(resultado, m => m.Id == movimentoInativo.Id);
        Assert.DoesNotContain(resultado, m => m.Id == movimentoSemDataVencimento.Id);
        Assert.DoesNotContain(resultado, m => m.UserId == movimentoDeOutroUsuario.UserId);
    }

    [Fact]
    public async Task GetPagedWithRelatedEntitiesAsync_RetornaSomenteMovimentosDoUsuario()
    {
        var userId = Guid.NewGuid();
        await using var context = CreateContext();
        AdicionarMovimento(context, userId, new DateTime(2026, 1, 1));
        AdicionarMovimento(context, userId, new DateTime(2026, 1, 2));
        AdicionarMovimento(context, Guid.NewGuid(), new DateTime(2026, 1, 3));
        await context.SaveChangesAsync();
        var repository = new MovimentoRepository(context);

        var resultado = await repository.GetPagedWithRelatedEntitiesAsync(userId, page: 1, pageSize: 10);

        Assert.Equal(2, resultado.TotalCount);
        Assert.All(resultado.Items, m => Assert.Equal(userId, m.UserId));
    }

    [Fact]
    public async Task GetPagedWithRelatedEntitiesAsync_OrdenaPorDataInvestimentoDecrescenteEPagina()
    {
        var userId = Guid.NewGuid();
        await using var context = CreateContext();
        AdicionarMovimento(context, userId, new DateTime(2026, 1, 2));
        AdicionarMovimento(context, userId, new DateTime(2026, 1, 3));
        AdicionarMovimento(context, userId, new DateTime(2026, 1, 1));
        await context.SaveChangesAsync();
        var repository = new MovimentoRepository(context);

        var primeira = await repository.GetPagedWithRelatedEntitiesAsync(userId, page: 1, pageSize: 2);
        var segunda = await repository.GetPagedWithRelatedEntitiesAsync(userId, page: 2, pageSize: 2);

        Assert.Equal([new DateTime(2026, 1, 3), new DateTime(2026, 1, 2)], primeira.Items.Select(m => m.DataInvestimento));
        Assert.Equal([new DateTime(2026, 1, 1)], segunda.Items.Select(m => m.DataInvestimento));
        Assert.Equal(3, primeira.TotalCount);
        Assert.Equal(2, primeira.TotalPages);
    }

    [Fact]
    public async Task GetPagedWithRelatedEntitiesAsync_ComDatasIguais_PaginaSemRepetirNemOmitir()
    {
        var userId = Guid.NewGuid();
        var mesmaData = new DateTime(2026, 1, 1);
        await using var context = CreateContext();
        var movimentos = Enumerable.Range(0, 5).Select(_ => AdicionarMovimento(context, userId, mesmaData)).ToList();
        await context.SaveChangesAsync();
        var repository = new MovimentoRepository(context);

        var coletados = new List<Guid>();
        for (var pagina = 1; pagina <= 3; pagina++)
        {
            var resultado = await repository.GetPagedWithRelatedEntitiesAsync(userId, pagina, pageSize: 2);
            coletados.AddRange(resultado.Items.Select(m => m.Id));
        }

        Assert.Equal(movimentos.Select(m => m.Id).OrderDescending(), coletados);
    }

    [Fact]
    public async Task GetPagedWithRelatedEntitiesAsync_CarregaNomesDasEntidadesRelacionadas()
    {
        var userId = Guid.NewGuid();
        await using var context = CreateContext();
        AdicionarMovimento(context, userId, new DateTime(2026, 1, 1));
        await context.SaveChangesAsync();
        var repository = new MovimentoRepository(context);

        var resultado = await repository.GetPagedWithRelatedEntitiesAsync(userId, page: 1, pageSize: 10);

        var movimento = Assert.Single(resultado.Items);
        Assert.Equal("Objetivo", movimento.Objetivo.Nome);
        Assert.Equal("CDB", movimento.TipoRenda.Nome);
        Assert.Equal("XP", movimento.Corretora.Nome);
        Assert.Equal("CDB Banco Y", movimento.Produto.Nome);
        Assert.Equal("Banco Y", movimento.Emissor.Nome);
    }

    private static Movimento AdicionarMovimento(VaultDbContext context, Guid userId, DateTime dataInvestimento)
    {
        var objetivo = new Objetivo { Nome = "Objetivo", FontePagadora = "Salário", OndeAplicar = "Corretora X", UserId = userId };
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
            DataInvestimento = dataInvestimento,
            ValorAporte = 100m,
            ValorLiquidoAtual = 100m
        };

        context.AddRange(objetivo, tipoRenda, corretora, produto, emissor, movimento);

        return movimento;
    }

    private static VaultDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<VaultDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new VaultDbContext(options);
    }
}
