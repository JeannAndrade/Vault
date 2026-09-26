using Application.Movimentos.Commands.UpdateMovimento;
using Application.Test.Movimentos.TestDoubles;
using Domain.Movimentos;
using LumiaFoundation.Core.Domain.Exceptions;

namespace Application.Test.Movimentos.Commands.UpdateMovimento;

public class UpdateMovimentoCommandTests
{
    [Fact]
    public async Task ExecuteAsync_WhenReferenciasSaoValidas_ValidaAtualizaEPersisteOMovimento()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var movimentoExistente = new Movimento { UserId = ownerId };
        var repositoryManager = new FakeRepositoryManager();
        repositoryManager.MovimentoRepository.MovimentoParaRetornar = movimentoExistente;
        var validator = new FakeMovimentoReferenciasValidator();
        var command = new UpdateMovimentoCommand(repositoryManager, validator);

        // Act
        await command.ExecuteAsync(CriarModeloValido(), ownerId, movimentoExistente.Id);

        // Assert
        Assert.True(validator.Chamado);
        Assert.True(repositoryManager.MovimentoRepository.UpdateChamado);
        Assert.True(repositoryManager.SaveAsyncChamado);
    }

    [Fact]
    public async Task ExecuteAsync_WhenUmaReferenciaNaoExiste_NaoBuscaNemAtualizaOMovimento()
    {
        // Arrange
        var repositoryManager = new FakeRepositoryManager();
        var validator = new FakeMovimentoReferenciasValidator(deveFalhar: true);
        var command = new UpdateMovimentoCommand(repositoryManager, validator);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => command.ExecuteAsync(CriarModeloValido(), Guid.NewGuid(), Guid.NewGuid()));
        Assert.False(repositoryManager.MovimentoRepository.UpdateChamado);
        Assert.False(repositoryManager.SaveAsyncChamado);
    }

    private static MovimentoModelForUpdate CriarModeloValido() => new()
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
