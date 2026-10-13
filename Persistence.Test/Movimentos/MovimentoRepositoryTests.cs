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

    [Fact]
    public async Task GetPagedWithRelatedEntitiesAsync_ComFiltroObjetivoId_RetornaSomenteMovimentosDoObjetivo()
    {
        var userId = Guid.NewGuid();
        await using var context = CreateContext();

        var objetivoEmFiltro = new Objetivo { Nome = "Objetivo A", FontePagadora = "Salário", OndeAplicar = "Corretora X", UserId = userId };
        var objetivoForaFiltro = new Objetivo { Nome = "Objetivo B", FontePagadora = "Salário", OndeAplicar = "Corretora X", UserId = userId };
        var tipoRenda = new TipoRenda { Nome = "CDB", UserId = userId };
        var corretora = new Corretora { Nome = "XP", UserId = userId };
        var produto = new Produto { Nome = "CDB Banco Y", UserId = userId };
        var emissor = new Emissor { Nome = "Banco Y", UserId = userId };

        var movimentoA = new Movimento
        {
            UserId = userId,
            ObjetivoId = objetivoEmFiltro.Id,
            TipoRendaId = tipoRenda.Id,
            CorretoraId = corretora.Id,
            ProdutoId = produto.Id,
            EmissorId = emissor.Id,
            DataInvestimento = new DateTime(2026, 1, 1),
            ValorAporte = 100m,
            ValorLiquidoAtual = 100m,
            EstaAtivo = true
        };
        var movimentoB = new Movimento
        {
            UserId = userId,
            ObjetivoId = objetivoEmFiltro.Id,
            TipoRendaId = tipoRenda.Id,
            CorretoraId = corretora.Id,
            ProdutoId = produto.Id,
            EmissorId = emissor.Id,
            DataInvestimento = new DateTime(2026, 1, 2),
            ValorAporte = 200m,
            ValorLiquidoAtual = 200m,
            EstaAtivo = true
        };
        var movimentoFora = new Movimento
        {
            UserId = userId,
            ObjetivoId = objetivoForaFiltro.Id,
            TipoRendaId = tipoRenda.Id,
            CorretoraId = corretora.Id,
            ProdutoId = produto.Id,
            EmissorId = emissor.Id,
            DataInvestimento = new DateTime(2026, 1, 3),
            ValorAporte = 300m,
            ValorLiquidoAtual = 300m,
            EstaAtivo = true
        };

        context.AddRange(objetivoEmFiltro, objetivoForaFiltro, tipoRenda, corretora, produto, emissor, movimentoA, movimentoB, movimentoFora);
        await context.SaveChangesAsync();

        var resultado = await new MovimentoRepository(context)
            .GetPagedWithRelatedEntitiesAsync(userId, new MovimentoFiltro { ObjetivoId = objetivoEmFiltro.Id }, 1, 10, CancellationToken.None);

        Assert.Equal([movimentoB.Id, movimentoA.Id], resultado.Items.Select(m => m.Id));
        Assert.Equal(2, resultado.TotalCount);
        Assert.All(resultado.Items, m => Assert.Equal(objetivoEmFiltro.Id, m.ObjetivoId));
    }

    [Fact]
    public async Task GetPagedWithRelatedEntitiesAsync_ComFiltrosCombinados_UsaTodosOsCampos()
    {
        var userId = Guid.NewGuid();
        await using var context = CreateContext();

        var objetivo = new Objetivo { Nome = "Objetivo", FontePagadora = "Salário", OndeAplicar = "Corretora X", UserId = userId };
        var outroObjetivo = new Objetivo { Nome = "Objetivo 2", FontePagadora = "Salário", OndeAplicar = "Corretora X", UserId = userId };
        var tipoRenda = new TipoRenda { Nome = "CDB", UserId = userId };
        var outraTipoRenda = new TipoRenda { Nome = "Tesouro", UserId = userId };
        var corretora = new Corretora { Nome = "XP", UserId = userId };
        var produto = new Produto { Nome = "CDB Banco Y", UserId = userId };
        var emissor = new Emissor { Nome = "Banco Y", UserId = userId };

        var movimentoCorreto = new Movimento
        {
            UserId = userId,
            ObjetivoId = objetivo.Id,
            TipoRendaId = tipoRenda.Id,
            CorretoraId = corretora.Id,
            ProdutoId = produto.Id,
            EmissorId = emissor.Id,
            DataInvestimento = new DateTime(2026, 1, 2),
            ValorAporte = 100m,
            ValorLiquidoAtual = 100m,
            EstaAtivo = true
        };
        var movimentoForaObjetivo = new Movimento
        {
            UserId = userId,
            ObjetivoId = outroObjetivo.Id,
            TipoRendaId = tipoRenda.Id,
            CorretoraId = corretora.Id,
            ProdutoId = produto.Id,
            EmissorId = emissor.Id,
            DataInvestimento = new DateTime(2026, 1, 1),
            ValorAporte = 100m,
            ValorLiquidoAtual = 100m,
            EstaAtivo = true
        };
        var movimentoForaAtivo = new Movimento
        {
            UserId = userId,
            ObjetivoId = objetivo.Id,
            TipoRendaId = tipoRenda.Id,
            CorretoraId = corretora.Id,
            ProdutoId = produto.Id,
            EmissorId = emissor.Id,
            DataInvestimento = new DateTime(2026, 1, 3),
            ValorAporte = 100m,
            ValorLiquidoAtual = 100m,
            EstaAtivo = false
        };

        context.AddRange(objetivo, outroObjetivo, tipoRenda, outraTipoRenda, corretora, produto, emissor, movimentoCorreto, movimentoForaObjetivo, movimentoForaAtivo);
        await context.SaveChangesAsync();

        var resultado = await new MovimentoRepository(context)
            .GetPagedWithRelatedEntitiesAsync(
                userId,
                new MovimentoFiltro { ObjetivoId = objetivo.Id, TipoRendaId = tipoRenda.Id, CorretoraId = corretora.Id, ProdutoId = produto.Id, EmissorId = emissor.Id, EstaAtivo = true },
                1,
                10,
                CancellationToken.None);

        Assert.Single(resultado.Items);
        Assert.Equal(movimentoCorreto.Id, resultado.Items.Single().Id);
        Assert.Equal(1, resultado.TotalCount);
        Assert.Equal(1, resultado.TotalPages);
    }

    [Fact]
    public async Task GetPagedWithRelatedEntitiesAsync_QuandoFiltroVazioOuGuidEmpty_RetornaListaVaziaOuMesmoComportamento()
    {
        var userId = Guid.NewGuid();
        await using var context = CreateContext();
        var movimento = AdicionarMovimento(context, userId, new DateTime(2026, 1, 1));
        await context.SaveChangesAsync();

        var repository = new MovimentoRepository(context);
        var semFiltro = await repository.GetPagedWithRelatedEntitiesAsync(userId, MovimentoFiltro.Vazio, 1, 10, CancellationToken.None);
        var comGuidEmpty = await repository.GetPagedWithRelatedEntitiesAsync(userId, new MovimentoFiltro { ObjetivoId = Guid.Empty }, 1, 10, CancellationToken.None);

        Assert.Single(semFiltro.Items);
        Assert.Equal(1, semFiltro.TotalCount);
        Assert.Empty(comGuidEmpty.Items);
        Assert.Equal(0, comGuidEmpty.TotalCount);
    }

    [Fact]
    public async Task GetPagedWithRelatedEntitiesAsync_QuandoFiltroAtivo_IsolaPorUsuarioERespeitaPagina2()
    {
        var userId = Guid.NewGuid();
        await using var context = CreateContext();

        var movimentoUsuario1 = AdicionarMovimento(context, userId, new DateTime(2026, 1, 1));
        movimentoUsuario1.EstaAtivo = true;
        var movimentoUsuario2 = AdicionarMovimento(context, userId, new DateTime(2026, 1, 2));
        movimentoUsuario2.EstaAtivo = true;
        var movimentoUsuario3 = AdicionarMovimento(context, userId, new DateTime(2026, 1, 3));
        movimentoUsuario3.EstaAtivo = false;
        var movimentoOutroUsuario = AdicionarMovimento(context, Guid.NewGuid(), new DateTime(2026, 1, 4));
        movimentoOutroUsuario.EstaAtivo = true;

        await context.SaveChangesAsync();

        var resultado = await new MovimentoRepository(context)
            .GetPagedWithRelatedEntitiesAsync(userId, new MovimentoFiltro { EstaAtivo = true }, 2, 1, CancellationToken.None);

        Assert.Equal([movimentoUsuario1.Id], resultado.Items.Select(m => m.Id));
        Assert.Equal(2, resultado.TotalCount);
        Assert.Equal(2, resultado.TotalPages);
        Assert.All(resultado.Items, m => Assert.True(m.EstaAtivo));
        Assert.DoesNotContain(resultado.Items, m => m.UserId == movimentoOutroUsuario.UserId);
    }

    [Fact]
    public async Task GetDesatualizadosAsync_FiltraOrdenaECarregaEntidadesRelacionadas()
    {
        var userId = Guid.NewGuid();
        var limite = new DateTime(2026, 2, 1);
        await using var context = CreateContext();

        var maisAntigo = AdicionarMovimento(context, userId, new DateTime(2026, 1, 1));
        maisAntigo.EstaAtivo = true;
        maisAntigo.DataAtualizacaoValorLiquido = limite.AddDays(-2);
        var desempateMaior = AdicionarMovimento(context, userId, new DateTime(2026, 1, 2));
        desempateMaior.EstaAtivo = true;
        desempateMaior.DataAtualizacaoValorLiquido = limite.AddDays(-1);
        var desempateMenor = AdicionarMovimento(context, userId, new DateTime(2026, 1, 3));
        desempateMenor.EstaAtivo = true;
        desempateMenor.DataAtualizacaoValorLiquido = limite.AddDays(-1);

        var naFronteira = AdicionarMovimento(context, userId, new DateTime(2026, 1, 4));
        naFronteira.EstaAtivo = true;
        naFronteira.DataAtualizacaoValorLiquido = limite;
        var inativo = AdicionarMovimento(context, userId, new DateTime(2026, 1, 5));
        inativo.DataAtualizacaoValorLiquido = limite.AddDays(-1);
        var deOutroUsuario = AdicionarMovimento(context, Guid.NewGuid(), new DateTime(2026, 1, 6));
        deOutroUsuario.EstaAtivo = true;
        deOutroUsuario.DataAtualizacaoValorLiquido = limite.AddDays(-1);

        await context.SaveChangesAsync();
        var repository = new MovimentoRepository(context);

        var resultado = await repository.GetDesatualizadosAsync(userId, limite, quantidade: 10, CancellationToken.None);

        var desempatesOrdenados = new[] { desempateMaior.Id, desempateMenor.Id }.Order();
        Assert.Equal([maisAntigo.Id, .. desempatesOrdenados], resultado.Select(m => m.Id));
        Assert.DoesNotContain(resultado, m => m.Id == naFronteira.Id);
        Assert.DoesNotContain(resultado, m => m.Id == inativo.Id);
        Assert.DoesNotContain(resultado, m => m.Id == deOutroUsuario.Id);
        var movimento = resultado.Single(m => m.Id == maisAntigo.Id);
        Assert.Equal("Objetivo", movimento.Objetivo.Nome);
        Assert.Equal("CDB", movimento.TipoRenda.Nome);
        Assert.Equal("XP", movimento.Corretora.Nome);
        Assert.Equal("CDB Banco Y", movimento.Produto.Nome);
        Assert.Equal("Banco Y", movimento.Emissor.Nome);
    }

    [Fact]
    public async Task GetDesatualizadosAsync_RespeitaQuantidade()
    {
        var userId = Guid.NewGuid();
        var limite = new DateTime(2026, 2, 1);
        await using var context = CreateContext();
        var primeiro = AdicionarMovimento(context, userId, new DateTime(2026, 1, 1));
        primeiro.EstaAtivo = true;
        primeiro.DataAtualizacaoValorLiquido = limite.AddDays(-2);
        var segundo = AdicionarMovimento(context, userId, new DateTime(2026, 1, 2));
        segundo.EstaAtivo = true;
        segundo.DataAtualizacaoValorLiquido = limite.AddDays(-1);
        await context.SaveChangesAsync();

        var resultado = await new MovimentoRepository(context)
            .GetDesatualizadosAsync(userId, limite, quantidade: 1, CancellationToken.None);

        Assert.Equal([primeiro.Id], resultado.Select(m => m.Id));
    }

    [Fact]
    public async Task GetAsync_ComCancellationToken_IsolaPorUsuarioERespeitaTracking()
    {
        var userId = Guid.NewGuid();
        await using var context = CreateContext();
        var movimento = AdicionarMovimento(context, userId, new DateTime(2026, 1, 1));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var repository = new MovimentoRepository(context);

        var deOutroUsuario = await repository.GetAsync(Guid.NewGuid(), movimento.Id, trackChanges: false, CancellationToken.None);
        var semTracking = await repository.GetAsync(userId, movimento.Id, trackChanges: false, CancellationToken.None);
        context.ChangeTracker.Clear();
        var comTracking = await repository.GetAsync(userId, movimento.Id, trackChanges: true, CancellationToken.None);

        Assert.Null(deOutroUsuario);
        Assert.NotNull(semTracking);
        Assert.Equal(EntityState.Detached, context.Entry(semTracking!).State);
        Assert.NotNull(comTracking);
        Assert.Equal(EntityState.Unchanged, context.Entry(comTracking!).State);
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
