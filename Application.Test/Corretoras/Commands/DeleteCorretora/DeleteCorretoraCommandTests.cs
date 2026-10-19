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
        corretoraRepository.Setup(r => r.GetAsync(ownerId, corretora.Id, true, CancellationToken.None)).ReturnsAsync(corretora);

        var movimentoRepository = new Mock<IMovimentoRepository>();
        movimentoRepository.Setup(r => r.ExistemPorCorretoraAsync(corretora.Id, CancellationToken.None)).ReturnsAsync(false);

        var repositoryManager = new Mock<IRepositoryManager>();
        repositoryManager.SetupGet(r => r.Corretora).Returns(corretoraRepository.Object);
        repositoryManager.SetupGet(r => r.Movimento).Returns(movimentoRepository.Object);

        var command = new DeleteCorretoraCommand(repositoryManager.Object);

        // Act
        await command.ExecuteAsync(ownerId, corretora.Id, CancellationToken.None);

        // Assert
        corretoraRepository.Verify(r => r.Delete(corretora), Times.Once);
        repositoryManager.Verify(r => r.SaveAsync(CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCorretoraTemMovimentosAssociados_LancaEntityInUseExceptionSemExcluir()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var corretora = new Corretora { UserId = ownerId };

        var corretoraRepository = new Mock<ICorretoraRepository>();
        corretoraRepository.Setup(r => r.GetAsync(ownerId, corretora.Id, true, CancellationToken.None)).ReturnsAsync(corretora);

        var movimentoRepository = new Mock<IMovimentoRepository>();
        movimentoRepository.Setup(r => r.ExistemPorCorretoraAsync(corretora.Id, CancellationToken.None)).ReturnsAsync(true);

        var repositoryManager = new Mock<IRepositoryManager>();
        repositoryManager.SetupGet(r => r.Corretora).Returns(corretoraRepository.Object);
        repositoryManager.SetupGet(r => r.Movimento).Returns(movimentoRepository.Object);

        var command = new DeleteCorretoraCommand(repositoryManager.Object);

        // Act & Assert
        await Assert.ThrowsAsync<EntityInUseException>(() => command.ExecuteAsync(ownerId, corretora.Id, CancellationToken.None));
        corretoraRepository.Verify(r => r.Delete(It.IsAny<Corretora>()), Times.Never);
        repositoryManager.Verify(r => r.SaveAsync(CancellationToken.None), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCorretoraNaoExiste_LancaEntityNotFoundException()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var corretoraId = Guid.NewGuid();

        var corretoraRepository = new Mock<ICorretoraRepository>();
        corretoraRepository.Setup(r => r.GetAsync(ownerId, corretoraId, true, CancellationToken.None)).ReturnsAsync((Corretora?)null);

        var repositoryManager = new Mock<IRepositoryManager>();
        repositoryManager.SetupGet(r => r.Corretora).Returns(corretoraRepository.Object);

        var command = new DeleteCorretoraCommand(repositoryManager.Object);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() => command.ExecuteAsync(ownerId, corretoraId, CancellationToken.None));
        repositoryManager.Verify(r => r.SaveAsync(CancellationToken.None), Times.Never);
    }
}
