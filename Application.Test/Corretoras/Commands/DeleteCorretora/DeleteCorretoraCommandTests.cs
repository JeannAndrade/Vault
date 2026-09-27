using Application.Corretoras.Commands.DeleteCorretora;
using Domain.Corretoras;
using LumiaFoundation.Core.Domain.Exceptions;
using Moq;
using Persistence.Corretoras;
using Persistence.Managment;
using Persistence.Movimentos;

namespace Application.Test.Corretoras.Commands.DeleteCorretora;

public class DeleteCorretoraCommandTests
{
    [Fact]
    public async Task ExecuteAsync_WhenCorretoraNaoTemMovimentosAssociados_ExcluiEPersiste()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var corretora = new Corretora { UserId = ownerId };

        var corretoraRepository = new Mock<ICorretoraRepository>();
        corretoraRepository.Setup(r => r.GetAsync(ownerId, corretora.Id, true)).ReturnsAsync(corretora);

        var movimentoRepository = new Mock<IMovimentoRepository>();
        movimentoRepository.Setup(r => r.ExistemPorCorretoraAsync(corretora.Id)).ReturnsAsync(false);

        var repositoryManager = new Mock<IRepositoryManager>();
        repositoryManager.SetupGet(r => r.Corretora).Returns(corretoraRepository.Object);
        repositoryManager.SetupGet(r => r.Movimento).Returns(movimentoRepository.Object);

        var command = new DeleteCorretoraCommand(repositoryManager.Object);

        // Act
        await command.ExecuteAsync(ownerId, corretora.Id);

        // Assert
        corretoraRepository.Verify(r => r.Delete(corretora), Times.Once);
        repositoryManager.Verify(r => r.SaveAsync(), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCorretoraTemMovimentosAssociados_LancaEntityInUseExceptionSemExcluir()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var corretora = new Corretora { UserId = ownerId };

        var corretoraRepository = new Mock<ICorretoraRepository>();
        corretoraRepository.Setup(r => r.GetAsync(ownerId, corretora.Id, true)).ReturnsAsync(corretora);

        var movimentoRepository = new Mock<IMovimentoRepository>();
        movimentoRepository.Setup(r => r.ExistemPorCorretoraAsync(corretora.Id)).ReturnsAsync(true);

        var repositoryManager = new Mock<IRepositoryManager>();
        repositoryManager.SetupGet(r => r.Corretora).Returns(corretoraRepository.Object);
        repositoryManager.SetupGet(r => r.Movimento).Returns(movimentoRepository.Object);

        var command = new DeleteCorretoraCommand(repositoryManager.Object);

        // Act & Assert
        await Assert.ThrowsAsync<EntityInUseException>(() => command.ExecuteAsync(ownerId, corretora.Id));
        corretoraRepository.Verify(r => r.Delete(It.IsAny<Corretora>()), Times.Never);
        repositoryManager.Verify(r => r.SaveAsync(), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCorretoraNaoExiste_LancaEntityNotFoundException()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var corretoraId = Guid.NewGuid();

        var corretoraRepository = new Mock<ICorretoraRepository>();
        corretoraRepository.Setup(r => r.GetAsync(ownerId, corretoraId, true)).ReturnsAsync((Corretora?)null);

        var repositoryManager = new Mock<IRepositoryManager>();
        repositoryManager.SetupGet(r => r.Corretora).Returns(corretoraRepository.Object);

        var command = new DeleteCorretoraCommand(repositoryManager.Object);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() => command.ExecuteAsync(ownerId, corretoraId));
        repositoryManager.Verify(r => r.SaveAsync(), Times.Never);
    }
}
