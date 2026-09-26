using Application.Movimentos.Commands.CreateMovimento;
using Application.Test.Movimentos.TestDoubles;
using LumiaFoundation.Core.Domain.Exceptions;

namespace Application.Test.Movimentos.Commands.CreateMovimento;

public class CreateMovimentoCommandTests
{
    [Fact]
    public async Task ExecuteAsync_WhenReferenciasSaoValidas_ValidaCriaEPersisteOMovimento()
    {
        // Arrange
        var repositoryManager = new FakeRepositoryManager();
        var validator = new FakeMovimentoReferenciasValidator();
        var command = new CreateMovimentoCommand(repositoryManager, validator);
        var userId = Guid.NewGuid();

        // Act
        var resultado = await command.ExecuteAsync(CriarModeloValido(), userId);

        // Assert
        Assert.True(validator.Chamado);
        Assert.True(repositoryManager.MovimentoRepository.CreateChamado);
        Assert.True(repositoryManager.SaveAsyncChamado);
        Assert.Equal(userId, resultado.UserId);
        Assert.Null(resultado.ObjetivoNome); // decisão registrada: nomes só nas Queries de leitura
    }

    [Fact]
    public async Task ExecuteAsync_WhenUmaReferenciaNaoExiste_NaoCriaNemPersisteOMovimento()
    {
        // Arrange
        var repositoryManager = new FakeRepositoryManager();
        var validator = new FakeMovimentoReferenciasValidator(deveFalhar: true);
        var command = new CreateMovimentoCommand(repositoryManager, validator);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() => command.ExecuteAsync(CriarModeloValido(), Guid.NewGuid()));
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
        DataInvestimento = DateTime.UtcNow,
        ValorAporte = 100m,
        ValorLiquidoAtual = 100m
    };
}
