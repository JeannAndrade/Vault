using Application.Movimentos.Commands.CreateMovimento;
using Application.Test.Movimentos.TestDoubles;
using LumiaFoundation.Core.Domain.Exceptions;
using LumiaFoundation.Core.TimeService;
using Moq;

namespace Application.Test.Movimentos.Commands.CreateMovimento;

public class CreateMovimentoCommandTests
{
    [Fact]
    public async Task ExecuteAsync_WhenReferenciasSaoValidas_ValidaCriaEPersisteOMovimento()
    {
        // Arrange
        var repositoryManager = new FakeRepositoryManager();
        var validator = new FakeMovimentoReferenciasValidator();
        var hoje = new DateTime(2026, 10, 10, 17, 0, 0, DateTimeKind.Utc);
        var clock = new Mock<IDateTimeProvider>();
        clock.Setup(c => c.GetDateTime()).Returns(hoje);
        var command = new CreateMovimentoCommand(repositoryManager, validator, clock.Object);
        var userId = Guid.NewGuid();

        // Act
        var resultado = await command.ExecuteAsync(CriarModeloValido(), userId, CancellationToken.None);

        // Assert
        Assert.True(validator.Chamado);
        Assert.True(repositoryManager.MovimentoRepository.CreateChamado);
        Assert.True(repositoryManager.SaveAsyncChamado);
        Assert.Equal(userId, resultado.UserId);
        Assert.Equal(hoje.Date, repositoryManager.MovimentoRepository.MovimentoCriado!.DataAtualizacaoValorLiquido);
        Assert.Null(resultado.ObjetivoNome); // decisão registrada: nomes só nas Queries de leitura
    }

    [Fact]
    public async Task ExecuteAsync_WhenUmaReferenciaNaoExiste_NaoCriaNemPersisteOMovimento()
    {
        // Arrange
        var repositoryManager = new FakeRepositoryManager();
        var validator = new FakeMovimentoReferenciasValidator(deveFalhar: true);
        var command = new CreateMovimentoCommand(repositoryManager, validator, new Mock<IDateTimeProvider>().Object);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() => command.ExecuteAsync(CriarModeloValido(), Guid.NewGuid(), CancellationToken.None));
        Assert.False(repositoryManager.MovimentoRepository.CreateChamado);
        Assert.False(repositoryManager.SaveAsyncChamado);
    }

    private static MovimentoModelForCreation CriarModeloValido() => new()
    {
        ObjetivoId = Guid.NewGuid(),
        TipoRendaId = Guid.NewGuid(),
        CorretoraId = Guid.NewGuid(),
        ProdutoId = Guid.NewGuid(),
        EmissorId = Guid.NewGuid(),
        DataInvestimento = new DateTime(2020, 1, 1),
        ValorAporte = 100m,
        ValorLiquidoAtual = 100m
    };
}
