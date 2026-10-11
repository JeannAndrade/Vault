using Application.Movimentos.Commands.UpdateValorLiquidoMovimento;
using Domain.Movimentos;
using LumiaFoundation.Core.Domain.Exceptions;
using LumiaFoundation.Core.TimeService;
using Moq;
using Persistence.Managment;
using Persistence.Movimentos;

namespace Application.Test.Movimentos.Commands.UpdateValorLiquidoMovimento;

public class UpdateValorLiquidoMovimentoCommandTests
{
    private readonly Mock<IMovimentoRepository> _movimentos = new();
    private readonly Mock<IRepositoryManager> _repository = new();
    private readonly Mock<IDateTimeProvider> _clock = new();
    private readonly Guid _ownerId = Guid.NewGuid();
    private readonly UpdateValorLiquidoMovimentoCommand _command;

    public UpdateValorLiquidoMovimentoCommandTests()
    {
        _repository.SetupGet(r => r.Movimento).Returns(_movimentos.Object);
        _clock.Setup(c => c.GetDateTime()).Returns(new DateTime(2026, 10, 10, 14, 0, 0, DateTimeKind.Utc));
        _command = new UpdateValorLiquidoMovimentoCommand(_repository.Object, _clock.Object);
    }

    [Fact]
    public async Task ExecuteAsync_AlteraSomenteValorEDataEPropagaToken()
    {
        using var cts = new CancellationTokenSource();
        var movimento = new Movimento { UserId = _ownerId, Observacao = "inalterada", ValorAporte = 20m, ValorLiquidoAtual = 20m };
        _movimentos.Setup(r => r.GetAsync(_ownerId, movimento.Id, true, cts.Token)).ReturnsAsync(movimento);

        await _command.ExecuteAsync(new MovimentoModelForUpdateValorLiquido { ValorLiquidoAtual = 30m }, _ownerId, movimento.Id, cts.Token);

        Assert.Equal(30m, movimento.ValorLiquidoAtual);
        Assert.Equal(new DateTime(2026, 10, 10), movimento.DataAtualizacaoValorLiquido);
        Assert.Equal("inalterada", movimento.Observacao);
        Assert.Equal(20m, movimento.ValorAporte);
        _repository.Verify(r => r.SaveAsync(cts.Token), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_QuandoMovimentoNaoExiste_LancaEntidadeNaoEncontrada()
    {
        _movimentos.Setup(r => r.GetAsync(_ownerId, It.IsAny<Guid>(), true, It.IsAny<CancellationToken>())).ReturnsAsync((Movimento?)null);

        await Assert.ThrowsAsync<EntityNotFoundException>(() => _command.ExecuteAsync(
            new MovimentoModelForUpdateValorLiquido { ValorLiquidoAtual = 1m }, _ownerId, Guid.NewGuid(), default));
    }

    [Fact]
    public async Task ExecuteAsync_QuandoValorNegativo_NaoConsultaNemPersiste()
    {
        await Assert.ThrowsAsync<CommandValidationException>(() => _command.ExecuteAsync(
            new MovimentoModelForUpdateValorLiquido { ValorLiquidoAtual = -1m }, _ownerId, Guid.NewGuid(), default));

        _movimentos.Verify(r => r.GetAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()), Times.Never);
        _repository.Verify(r => r.SaveAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
