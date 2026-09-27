using Application.Movimentos.Commands.DeleteMovimento;
using Domain.Movimentos;
using LumiaFoundation.Core.Domain.Exceptions;
using Moq;
using Persistence.Managment;
using Persistence.Movimentos;

namespace Application.Test.Movimentos.Commands.DeleteMovimento;

public class DeleteMovimentoCommandTests
{
    [Fact]
    public async Task ExecuteAsync_WhenMovimentoExiste_ExcluiEPersiste()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var movimento = new Movimento { UserId = ownerId };

        var movimentoRepository = new Mock<IMovimentoRepository>();
        movimentoRepository.Setup(r => r.GetAsync(ownerId, movimento.Id, true)).ReturnsAsync(movimento);

        var repositoryManager = new Mock<IRepositoryManager>();
        repositoryManager.SetupGet(r => r.Movimento).Returns(movimentoRepository.Object);

        var command = new DeleteMovimentoCommand(repositoryManager.Object);

        // Act
        await command.ExecuteAsync(ownerId, movimento.Id);

        // Assert
        movimentoRepository.Verify(r => r.Delete(movimento), Times.Once);
        repositoryManager.Verify(r => r.SaveAsync(), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenMovimentoNaoExiste_LancaEntityNotFoundException()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var movimentoId = Guid.NewGuid();

        var movimentoRepository = new Mock<IMovimentoRepository>();
        movimentoRepository.Setup(r => r.GetAsync(ownerId, movimentoId, true)).ReturnsAsync((Movimento?)null);

        var repositoryManager = new Mock<IRepositoryManager>();
        repositoryManager.SetupGet(r => r.Movimento).Returns(movimentoRepository.Object);

        var command = new DeleteMovimentoCommand(repositoryManager.Object);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() => command.ExecuteAsync(ownerId, movimentoId));
        repositoryManager.Verify(r => r.SaveAsync(), Times.Never);
    }
}
