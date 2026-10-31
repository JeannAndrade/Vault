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
    public async Task GetConsolidadoAsync_SomaSomenteAtivosDoUsuarioComPrecisao()
    {
        var userId = Guid.NewGuid();
        await using var context = CreateContext();
        var dimensoes = CriarDimensoes(context, userId, "A");
        AdicionarMovimentoConsolidado(context, userId, dimensoes, 10.25m, 11.30m, true);
        AdicionarMovimentoConsolidado(context, userId, dimensoes, 20.75m, 21.40m, true);
        AdicionarMovimentoConsolidado(context, userId, dimensoes, 999m, 999m, false);
        AdicionarMovimentoConsolidado(context, Guid.NewGuid(), dimensoes, 999m, 999m, true);
        await context.SaveChangesAsync();

        var resultado = await new MovimentoRepository(context).GetConsolidadoAsync(userId, new ConsolidadoFiltro(), CancellationToken.None);

        Assert.Equal(2, resultado.Quantidade);
        Assert.Equal(31.00m, resultado.TotalAportado);
        Assert.Equal(32.70m, resultado.TotalLiquido);
    }

    [Fact]
    public async Task GetConsolidadoAsync_AplicaFiltrosCombinadosEIgnoraObjetivoInativo()
    {
        var userId = Guid.NewGuid();
        await using var context = CreateContext();
        var a = CriarDimensoes(context, userId, "A");
        var b = CriarDimensoes(context, userId, "B");
        var objetivo = new Objetivo { Nome = "Inativo", FontePagadora = "Salário", OndeAplicar = "XP", UserId = userId, EstaAtivo = false };
        context.Add(objetivo);
        AdicionarMovimentoConsolidado(context, userId, a, 100m, 110m, true, objetivo.Id);
        AdicionarMovimentoConsolidado(context, userId, b, 200m, 220m, true);
        await context.SaveChangesAsync();

        var filtro = new ConsolidadoFiltro { TipoRendaId = a.TipoRenda.Id, CorretoraId = a.Corretora.Id };
        var resultado = await new MovimentoRepository(context).GetConsolidadoAsync(userId, filtro, CancellationToken.None);

        Assert.Equal(new ConsolidadoAgregado(1, 100m, 110m), resultado);
    }

    [Fact]
    public async Task GetConsolidadoAsync_SemCorrespondenciaRetornaZeros()
    {
        await using var context = CreateContext();
        var resultado = await new MovimentoRepository(context).GetConsolidadoAsync(Guid.NewGuid(),
            new ConsolidadoFiltro { EmissorId = Guid.Empty }, CancellationToken.None);
        Assert.Equal(new ConsolidadoAgregado(0, 0m, 0m), resultado);
    }

    [Theory]
    [InlineData(DimensaoConsolidado.TipoRenda)]
    [InlineData(DimensaoConsolidado.Corretora)]
    [InlineData(DimensaoConsolidado.Produto)]
    [InlineData(DimensaoConsolidado.Emissor)]
    public async Task GetConsolidadoAgrupadoAsync_AgrupaOrdenaEIsolaPorUsuario(DimensaoConsolidado dimensao)
    {
        var userId = Guid.NewGuid();
        await using var context = CreateContext();
        var menor = CriarDimensoes(context, userId, "Z");
        var maior = CriarDimensoes(context, userId, "A");
        AdicionarMovimentoConsolidado(context, userId, menor, 50m, 50m, true);
        AdicionarMovimentoConsolidado(context, userId, maior, 100m, 200m, true);
        AdicionarMovimentoConsolidado(context, Guid.NewGuid(), maior, 999m, 999m, true);
        await context.SaveChangesAsync();

        var resultado = await new MovimentoRepository(context).GetConsolidadoAgrupadoAsync(userId, new ConsolidadoFiltro(), dimensao, CancellationToken.None);

        Assert.Equal(2, resultado.Count);
        Assert.Equal(200m, resultado[0].TotalLiquido);
        Assert.Equal(50m, resultado[1].TotalLiquido);
        Assert.All(resultado, grupo => Assert.Equal(1, grupo.Quantidade));
    }

    [Fact]
    public async Task GetConsolidadoAgrupadoAsync_RespeitaFiltroDaDimensaoEEstaVazioSemDados()
    {
        var userId = Guid.NewGuid();
        await using var context = CreateContext();
        var dimensoes = CriarDimensoes(context, userId, "A");
        AdicionarMovimentoConsolidado(context, userId, dimensoes, 10m, 10m, true);
        await context.SaveChangesAsync();

        var resultado = await new MovimentoRepository(context).GetConsolidadoAgrupadoAsync(userId,
            new ConsolidadoFiltro { ProdutoId = Guid.NewGuid() }, DimensaoConsolidado.Produto, CancellationToken.None);

        Assert.Empty(resultado);
    }

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
        var resultado = await repository.GetWithRelatedEntitiesAsync(userId, movimento.Id, CancellationToken.None);

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
        var resultado = await repository.GetWithRelatedEntitiesAsync(Guid.NewGuid(), movimento.Id, CancellationToken.None);

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
        var resultado = await repository.ExistemPorObjetivoAsync(objetivoId, CancellationToken.None);

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
        var resultado = await repository.ExistemPorObjetivoAsync(Guid.NewGuid(), CancellationToken.None);

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
        var resultado = await repository.ExistemPorCorretoraAsync(corretoraId, CancellationToken.None);

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
        var resultado = await repository.ExistemPorCorretoraAsync(Guid.NewGuid(), CancellationToken.None);

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

        var resultado = await repository.GetProximosVencimentosAsync(userId, quantidade: 15, CancellationToken.None);

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

        var resultado = await repository.GetPagedWithRelatedEntitiesAsync(userId, page: 1, pageSize: 10, CancellationToken.None);

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

        var primeira = await repository.GetPagedWithRelatedEntitiesAsync(userId, page: 1, pageSize: 2, CancellationToken.None);
        var segunda = await repository.GetPagedWithRelatedEntitiesAsync(userId, page: 2, pageSize: 2, CancellationToken.None);

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
            var resultado = await repository.GetPagedWithRelatedEntitiesAsync(userId, pagina, pageSize: 2, CancellationToken.None);
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

        var resultado = await repository.GetPagedWithRelatedEntitiesAsync(userId, page: 1, pageSize: 10, CancellationToken.None);

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

    [Fact]
    public async Task GetDesatualizadosAsync_OrdenaPorCorretoraProdutoEmissorEDepoisDataEId()
    {
        var userId = Guid.NewGuid();
        var limite = new DateTime(2026, 2, 1);
        await using var context = CreateContext();

        // Inseridos fora de ordem de propósito.
        var c = AdicionarMovimentoDoTitulo(context, userId, "Corretora B", "Produto A", "Emissor A", limite.AddDays(-1));
        var b = AdicionarMovimentoDoTitulo(context, userId, "Corretora A", "Produto B", "Emissor A", limite.AddDays(-1));
        var a2 = AdicionarMovimentoDoTitulo(context, userId, "Corretora A", "Produto A", "Emissor B", limite.AddDays(-1));
        var a1 = AdicionarMovimentoDoTitulo(context, userId, "Corretora A", "Produto A", "Emissor A", limite.AddDays(-1));

        // Mesmo título e data: desempate por Id.
        var d1 = AdicionarMovimentoDoTitulo(context, userId, "Corretora C", "Produto A", "Emissor A", limite.AddDays(-1));
        var d2 = AdicionarMovimentoDoTitulo(context, userId, "Corretora C", "Produto A", "Emissor A", limite.AddDays(-1));
        // Desempate por data de atualização: mesma corretora/produto/emissor, data mais antiga primeiro.
        var e1 = AdicionarMovimentoDoTitulo(context, userId, "Corretora D", "Produto A", "Emissor A", limite.AddDays(-1));
        var e2 = AdicionarMovimentoDoTitulo(context, userId, "Corretora D", "Produto A", "Emissor A", limite.AddDays(-5));

        await context.SaveChangesAsync();

        var resultado = await new MovimentoRepository(context)
            .GetDesatualizadosAsync(userId, limite, quantidade: 20, CancellationToken.None);

        Assert.Equal(
            [a1.Id, a2.Id, b.Id, c.Id, .. new[] { d1.Id, d2.Id }.Order(), e2.Id, e1.Id],
            resultado.Select(m => m.Id));
    }

    [Fact]
    public async Task GetDesatualizadosAsync_AplicaTakeDepoisDaOrdenacaoPorNome()
    {
        var userId = Guid.NewGuid();
        var limite = new DateTime(2026, 2, 1);
        await using var context = CreateContext();
        // O mais antigo (por data) tem a corretora "Z": fica fora do Top 1.
        AdicionarMovimentoDoTitulo(context, userId, "Z", "P", "E", limite.AddDays(-100));
        var primeiro = AdicionarMovimentoDoTitulo(context, userId, "A", "P", "E", limite.AddDays(-1));
        await context.SaveChangesAsync();

        var resultado = await new MovimentoRepository(context)
            .GetDesatualizadosAsync(userId, limite, quantidade: 1, CancellationToken.None);

        Assert.Equal([primeiro.Id], resultado.Select(m => m.Id));
    }

    [Fact]
    public async Task GetAsync_RetornaMovimentoEncerradoComMotivoEDataPersistidos()
    {
        var userId = Guid.NewGuid();
        await using var context = CreateContext();

        var movimento = AdicionarMovimento(context, userId, new DateTime(2026, 1, 15));
        movimento.EstaAtivo = true;
        movimento.Encerrar(MotivoEncerramento.Reinvestido, new DateTime(2026, 2, 1), new DateTime(2026, 2, 5));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var carregado = await new MovimentoRepository(context)
            .GetAsync(userId, movimento.Id, trackChanges: false, CancellationToken.None);

        Assert.NotNull(carregado);
        Assert.False(carregado!.EstaAtivo);
        Assert.Equal(MotivoEncerramento.Reinvestido, carregado.MotivoEncerramento);
        Assert.Equal(new DateTime(2026, 2, 1), carregado.DataEncerramento);
    }

    [Fact]
    public async Task GetAsync_RetornaMovimentoAtivoComMotivoEDataNulos()
    {
        var userId = Guid.NewGuid();
        await using var context = CreateContext();

        var movimento = AdicionarMovimento(context, userId, new DateTime(2026, 1, 15));
        movimento.EstaAtivo = true;
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var carregado = await new MovimentoRepository(context)
            .GetAsync(userId, movimento.Id, trackChanges: false, CancellationToken.None);

        Assert.NotNull(carregado);
        Assert.True(carregado!.EstaAtivo);
        Assert.Null(carregado.MotivoEncerramento);
        Assert.Null(carregado.DataEncerramento);
    }

    [Fact]
    public async Task Reativar_PersisteEstadoAtivoELimpaMotivoEData()
    {
        var userId = Guid.NewGuid();
        await using var context = CreateContext();

        var movimento = AdicionarMovimento(context, userId, new DateTime(2026, 1, 15));
        movimento.EstaAtivo = true;
        movimento.Encerrar(MotivoEncerramento.Pendente, new DateTime(2026, 2, 1), new DateTime(2026, 2, 5));
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();
        var carregado = await new MovimentoRepository(context)
            .GetAsync(userId, movimento.Id, trackChanges: true, CancellationToken.None);

        carregado!.Reativar();
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var relembrado = await new MovimentoRepository(context)
            .GetAsync(userId, movimento.Id, trackChanges: false, CancellationToken.None);

        Assert.NotNull(relembrado);
        Assert.True(relembrado!.EstaAtivo);
        Assert.Null(relembrado.MotivoEncerramento);
        Assert.Null(relembrado.DataEncerramento);
    }

    private static Movimento AdicionarMovimentoDoTitulo(
        VaultDbContext context, Guid userId, string corretora, string produto, string emissor, DateTime dataAtualizacao)
    {
        var objetivo = new Objetivo { Nome = "Objetivo", FontePagadora = "Salário", OndeAplicar = "Corretora X", UserId = userId };
        var tipoRenda = new TipoRenda { Nome = "CDB", UserId = userId };
        var corretoraEntidade = new Corretora { Nome = corretora, UserId = userId };
        var produtoEntidade = new Produto { Nome = produto, UserId = userId };
        var emissorEntidade = new Emissor { Nome = emissor, UserId = userId };
        var movimento = new Movimento
        {
            UserId = userId,
            ObjetivoId = objetivo.Id,
            TipoRendaId = tipoRenda.Id,
            CorretoraId = corretoraEntidade.Id,
            ProdutoId = produtoEntidade.Id,
            EmissorId = emissorEntidade.Id,
            DataInvestimento = new DateTime(2026, 1, 1),
            ValorAporte = 100m,
            ValorLiquidoAtual = 100m,
            EstaAtivo = true,
            DataAtualizacaoValorLiquido = dataAtualizacao
        };

        context.AddRange(objetivo, tipoRenda, corretoraEntidade, produtoEntidade, emissorEntidade, movimento);
        return movimento;
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

    private static DimensoesConsolidado CriarDimensoes(VaultDbContext context, Guid userId, string sufixo)
    {
        var dimensoes = new DimensoesConsolidado(
            new TipoRenda { Nome = $"Renda {sufixo}", UserId = userId },
            new Corretora { Nome = $"Corretora {sufixo}", UserId = userId },
            new Produto { Nome = $"Produto {sufixo}", UserId = userId },
            new Emissor { Nome = $"Emissor {sufixo}", UserId = userId });
        context.AddRange(dimensoes.TipoRenda, dimensoes.Corretora, dimensoes.Produto, dimensoes.Emissor);
        return dimensoes;
    }

    private static Movimento AdicionarMovimentoConsolidado(
        VaultDbContext context, Guid userId, DimensoesConsolidado dimensoes,
        decimal aporte, decimal liquido, bool ativo, Guid? objetivoId = null)
    {
        var movimento = new Movimento
        {
            UserId = userId,
            ObjetivoId = objetivoId ?? Guid.NewGuid(),
            TipoRendaId = dimensoes.TipoRenda.Id,
            CorretoraId = dimensoes.Corretora.Id,
            ProdutoId = dimensoes.Produto.Id,
            EmissorId = dimensoes.Emissor.Id,
            DataInvestimento = new DateTime(2026, 1, 1),
            ValorAporte = aporte,
            ValorLiquidoAtual = liquido,
            EstaAtivo = ativo
        };
        context.Add(movimento);
        return movimento;
    }

    private sealed record DimensoesConsolidado(
        TipoRenda TipoRenda, Corretora Corretora, Produto Produto, Emissor Emissor);

    private static VaultDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<VaultDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new VaultDbContext(options);
    }
}
