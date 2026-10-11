using Application.Movimentos.Queries.GetMovimentosDesatualizados;
using Domain.Movimentos;
using LumiaFoundation.Core.TimeService;
using LumiaFoundation.Logger.Contracts;
using Moq;
using Persistence.Managment;
using Persistence.Movimentos;

namespace Application.Test.Movimentos.Queries.GetMovimentosDesatualizados;

public class GetMovimentosDesatualizadosQueryTests
{
    private readonly Mock<IMovimentoRepository> _movimentos = new();
    private readonly Mock<ILoggerManager> _logger = new();
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly GetMovimentosDesatualizadosQuery _query;

    public GetMovimentosDesatualizadosQueryTests()
    {
        var repository = new Mock<IRepositoryManager>();
        repository.SetupGet(r => r.Movimento).Returns(_movimentos.Object);
        _clock.Setup(c => c.GetDateTime()).Returns(new DateTime(2026, 10, 10, 15, 30, 0, DateTimeKind.Utc));
        _query = new GetMovimentosDesatualizadosQuery(repository.Object, _logger.Object, _clock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_CalculaLimiteERepassaArgumentosAoRepositorio()
    {
        using var cts = new CancellationTokenSource();
        var movimento = new Movimento { UserId = _ownerId, ValorLiquidoAtual = 10m };
        _movimentos.Setup(r => r.GetDesatualizadosAsync(_ownerId, new DateTime(2026, 9, 10), 12, cts.Token))
            .ReturnsAsync([movimento]);

        var resultado = await _query.ExecuteAsync(_ownerId, cts.Token);

        Assert.Equal(movimento.Id, Assert.Single(resultado).Id);
        _movimentos.Verify(r => r.GetDesatualizadosAsync(_ownerId, new DateTime(2026, 9, 10), 12, cts.Token), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_QuandoRepositorioFalha_LogaERelanca()
    {
        var falha = new InvalidOperationException("falha");
        _movimentos.Setup(r => r.GetDesatualizadosAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(falha);

        var lancada = await Assert.ThrowsAsync<InvalidOperationException>(() => _query.ExecuteAsync(_ownerId, default));

        Assert.Same(falha, lancada);
        _logger.Verify(l => l.LogError(It.Is<string>(m => m.Contains(nameof(GetMovimentosDesatualizadosQuery)))), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_QuandoCancelada_RelancaSemLogar()
    {
        _movimentos.Setup(r => r.GetDesatualizadosAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException());

        await Assert.ThrowsAsync<OperationCanceledException>(() => _query.ExecuteAsync(_ownerId, default));

        _logger.Verify(l => l.LogError(It.IsAny<string>()), Times.Never);
    }
}
