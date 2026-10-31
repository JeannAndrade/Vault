using Application.Dashboards.Consolidado;
using Application.Dashboards.Queries.GetConsolidado;
using LumiaFoundation.Core.Domain.Exceptions;
using LumiaFoundation.Core.Validators;
using LumiaFoundation.Logger.Contracts;
using Moq;
using Persistence.Managment;
using Persistence.Movimentos;

namespace Application.Test.Dashboards.Queries.GetConsolidado;

public class GetConsolidadoQueryTests
{
    private readonly Mock<IMovimentoRepository> _movimentos = new();
    private readonly Mock<ILoggerManager> _logger = new();
    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly GetConsolidadoQuery _query;

    public GetConsolidadoQueryTests()
    {
        var repository = new Mock<IRepositoryManager>();
        repository.SetupGet(r => r.Movimento).Returns(_movimentos.Object);
        _query = new GetConsolidadoQuery(repository.Object, _logger.Object);
    }

    [Fact]
    public async Task ExecuteAsync_SemFiltro_LancaValidacaoSemConsultarRepositorio()
    {
        await Assert.ThrowsAsync<CommandValidationException>(() => _query.ExecuteAsync(_ownerId, new(), default));
        _movimentos.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExecuteAsync_SemAgrupamento_MapeiaFiltroEAgregado()
    {
        using var cts = new CancellationTokenSource();
        var filtro = new ConsolidadoFiltro { ProdutoId = Guid.NewGuid(), EmissorId = Guid.NewGuid() };
        _movimentos.Setup(r => r.GetConsolidadoAsync(_ownerId, It.Is<ConsolidadoFiltro>(f =>
            f.ProdutoId == filtro.ProdutoId && f.EmissorId == filtro.EmissorId), cts.Token))
            .ReturnsAsync(new ConsolidadoAgregado(3, 125.50m, 150m));

        var resultado = await _query.ExecuteAsync(_ownerId, new ConsolidadoParameters
        { ProdutoId = filtro.ProdutoId, EmissorId = filtro.EmissorId }, cts.Token);

        Assert.Equal(new ConsolidadoLinhaModel(null, null, 3, 125.50m, 150m, 1), resultado.Total);
        Assert.Empty(resultado.Grupos);
        _movimentos.VerifyAll();
    }

    [Fact]
    public async Task ExecuteAsync_ComAgrupamento_TotalizaLinhasECalculaPercentuais()
    {
        var filtroId = Guid.NewGuid();
        var grupos = new List<ConsolidadoGrupoItem>
        {
            new() { Id = Guid.NewGuid(), Nome = "A", Quantidade = 2, TotalAportado = 600m, TotalLiquido = 600m },
            new() { Id = Guid.NewGuid(), Nome = "B", Quantidade = 1, TotalAportado = 400m, TotalLiquido = 400m }
        };
        _movimentos.Setup(r => r.GetConsolidadoAgrupadoAsync(_ownerId,
            It.Is<ConsolidadoFiltro>(f => f.TipoRendaId == filtroId), DimensaoConsolidado.Corretora, It.IsAny<CancellationToken>()))
            .ReturnsAsync(grupos);

        var resultado = await _query.ExecuteAsync(_ownerId,
            new ConsolidadoParameters { TipoRendaId = filtroId, AgruparPor = DimensaoConsolidado.Corretora }, default);

        Assert.Equal(new ConsolidadoLinhaModel(null, null, 3, 1000m, 1000m, 1), resultado.Total);
        Assert.Equal([0.6m, 0.4m], resultado.Grupos.Select(g => g.PercentualLiquido));
    }

    [Fact]
    public async Task ExecuteAsync_TotalLiquidoZero_UsaPercentualZero()
    {
        _movimentos.Setup(r => r.GetConsolidadoAgrupadoAsync(It.IsAny<Guid>(), It.IsAny<ConsolidadoFiltro>(),
            It.IsAny<DimensaoConsolidado>(), It.IsAny<CancellationToken>())).ReturnsAsync([
                new ConsolidadoGrupoItem { Id = Guid.NewGuid(), Nome = "A", Quantidade = 1, TotalLiquido = 0 }
            ]);

        var resultado = await _query.ExecuteAsync(_ownerId,
            new ConsolidadoParameters { EmissorId = Guid.NewGuid(), AgruparPor = DimensaoConsolidado.Emissor }, default);

        Assert.Equal(0, resultado.Total.PercentualLiquido);
        Assert.Equal(0, Assert.Single(resultado.Grupos).PercentualLiquido);
    }

    [Fact]
    public async Task ExecuteAsync_FalhaDoRepositorio_LogaERelanca()
    {
        var falha = new InvalidOperationException("falha");
        _movimentos.Setup(r => r.GetConsolidadoAsync(It.IsAny<Guid>(), It.IsAny<ConsolidadoFiltro>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(falha);

        var lancada = await Assert.ThrowsAsync<InvalidOperationException>(() => _query.ExecuteAsync(
            _ownerId, new ConsolidadoParameters { ProdutoId = Guid.NewGuid() }, default));

        Assert.Same(falha, lancada);
        _logger.Verify(l => l.LogError(It.Is<string>(m => m.Contains(nameof(GetConsolidadoQuery)))), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_CancelamentoRelancaSemLogar()
    {
        _movimentos.Setup(r => r.GetConsolidadoAsync(It.IsAny<Guid>(), It.IsAny<ConsolidadoFiltro>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        await Assert.ThrowsAsync<OperationCanceledException>(() => _query.ExecuteAsync(
            _ownerId, new ConsolidadoParameters { ProdutoId = Guid.NewGuid() }, default));
        _logger.Verify(l => l.LogError(It.IsAny<string>()), Times.Never);
    }
}
