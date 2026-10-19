using Application.Movimentos.Queries.GetMovimentoList;
using Application.Pagination;
using Domain.Movimentos;
using LumiaFoundation.Core.Domain.Exceptions;
using LumiaFoundation.Core.Pagination;
using LumiaFoundation.Logger.Contracts;
using Moq;
using Persistence.Managment;
using Persistence.Movimentos;

namespace Application.Test.Movimentos.Queries.GetMovimentoList;

public class GetMovimentosListQueryTests
{
    private readonly Mock<IMovimentoRepository> _movimentoRepository = new();
    private readonly Mock<ILoggerManager> _logger = new();
    private readonly GetMovimentosListQuery _query;
    private readonly Guid _ownerId = Guid.NewGuid();

    public GetMovimentosListQueryTests()
    {
        var repositoryManager = new Mock<IRepositoryManager>();
        repositoryManager.SetupGet(r => r.Movimento).Returns(_movimentoRepository.Object);
        _query = new GetMovimentosListQuery(repositoryManager.Object, _logger.Object);
    }

    [Fact]
    public async Task ExecuteAsync_DelegaAoRepositorioEMapeiaMantendoMetadados()
    {
        using var cts = new CancellationTokenSource();
        var movimento = new Movimento { UserId = _ownerId, ValorAporte = 100m };
        _movimentoRepository
            .Setup(r => r.GetPagedWithRelatedEntitiesAsync(_ownerId, It.Is<MovimentoFiltro>(f => f == MovimentoFiltro.Vazio), 2, 10, cts.Token))
            .ReturnsAsync(new PagedList<Movimento>([movimento], page: 2, pageSize: 10, totalCount: 11));

        var resultado = await _query.ExecuteAsync(_ownerId, new PaginationParameters { Page = 2, PageSize = 10 }, cts.Token);

        var modelo = Assert.Single(resultado.Items);
        Assert.Equal(movimento.Id, modelo.Id);
        Assert.Equal(_ownerId, modelo.UserId);
        Assert.Equal(2, resultado.Page);
        Assert.Equal(10, resultado.PageSize);
        Assert.Equal(11, resultado.TotalCount);
        Assert.Equal(2, resultado.TotalPages);
    }

    [Fact]
    public async Task ExecuteAsync_ComParametrosPadrao_UsaPrimeiraPaginaCom20Itens()
    {
        _movimentoRepository
            .Setup(r => r.GetPagedWithRelatedEntitiesAsync(_ownerId, It.Is<MovimentoFiltro>(f => f == MovimentoFiltro.Vazio), 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedList<Movimento>([], 1, 20, 0));

        await _query.ExecuteAsync(_ownerId, new PaginationParameters(), CancellationToken.None);

        _movimentoRepository.Verify(
            r => r.GetPagedWithRelatedEntitiesAsync(_ownerId, It.Is<MovimentoFiltro>(f => f == MovimentoFiltro.Vazio), 1, 20, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(-1, 20)]
    [InlineData(1, 0)]
    [InlineData(1, 101)]
    public async Task ExecuteAsync_ComParametrosInvalidos_LancaCommandValidationExceptionSemConsultar(int page, int pageSize)
    {
        await Assert.ThrowsAsync<CommandValidationException>(
            () => _query.ExecuteAsync(_ownerId, new PaginationParameters { Page = page, PageSize = pageSize }, CancellationToken.None));

        _movimentoRepository.Verify(
            r => r.GetPagedWithRelatedEntitiesAsync(It.IsAny<Guid>(), It.IsAny<MovimentoFiltro>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ComFiltro_MapeiaEDelegaAoRepositorio()
    {
        using var cts = new CancellationTokenSource();
        var objetivoId = Guid.NewGuid();
        var tipoRendaId = Guid.NewGuid();
        var corretoraId = Guid.NewGuid();
        var produtoId = Guid.NewGuid();
        var emissorId = Guid.NewGuid();

        var movimento = new Movimento { UserId = _ownerId, ValorAporte = 100m, EstaAtivo = true };
        _movimentoRepository
            .Setup(r => r.GetPagedWithRelatedEntitiesAsync(
                _ownerId,
                It.Is<MovimentoFiltro>(f =>
                    f.ObjetivoId == objetivoId &&
                    f.TipoRendaId == tipoRendaId &&
                    f.CorretoraId == corretoraId &&
                    f.ProdutoId == produtoId &&
                    f.EmissorId == emissorId &&
                    f.EstaAtivo == true),
                2,
                10,
                cts.Token))
            .ReturnsAsync(new PagedList<Movimento>([movimento], page: 2, pageSize: 10, totalCount: 1));

        var resultado = await _query.ExecuteAsync(
            _ownerId,
            new MovimentoFiltroParameters
            {
                ObjetivoId = objetivoId,
                TipoRendaId = tipoRendaId,
                CorretoraId = corretoraId,
                ProdutoId = produtoId,
                EmissorId = emissorId,
                EstaAtivo = true
            },
            new PaginationParameters { Page = 2, PageSize = 10 },
            cts.Token);

        var modelo = Assert.Single(resultado.Items);
        Assert.Equal(movimento.Id, modelo.Id);
        Assert.Equal(_ownerId, modelo.UserId);
    }

    [Fact]
    public async Task ExecuteAsync_QuandoRepositorioFalha_LogaERelanca()
    {
        var falha = new InvalidOperationException("falha");
        _movimentoRepository
            .Setup(r => r.GetPagedWithRelatedEntitiesAsync(It.IsAny<Guid>(), It.IsAny<MovimentoFiltro>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(falha);

        var lancada = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _query.ExecuteAsync(_ownerId, new PaginationParameters(), CancellationToken.None));

        Assert.Same(falha, lancada);
        _logger.Verify(l => l.LogError(It.Is<string>(m => m.Contains(nameof(GetMovimentosListQuery)))), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_QuandoCancelada_RelancaSemLogarComoErro()
    {
        _movimentoRepository
            .Setup(r => r.GetPagedWithRelatedEntitiesAsync(It.IsAny<Guid>(), It.IsAny<MovimentoFiltro>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => _query.ExecuteAsync(_ownerId, new PaginationParameters(), CancellationToken.None));

        _logger.Verify(l => l.LogError(It.IsAny<string>()), Times.Never);
    }
}
