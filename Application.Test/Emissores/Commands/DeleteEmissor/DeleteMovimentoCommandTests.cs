using Application.Emissores.Commands.DeleteEmissor;
using Domain.Emissores;
using LumiaFoundation.Core.Domain.Exceptions;
using Moq;
using Persistence.Emissores;
using Persistence.Managment;
using Persistence.Movimentos;

namespace Application.Test.Emissores.Commands.DeleteEmissor;

public class DeleteEmissorCommandTests
{
    [Fact]
    public async Task ExecuteAsync_WhenEmissorNaoTemMovimentosAssociados_ExcluiEPersiste()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var emissor = new Emissor { UserId = ownerId };

        var emissorRepository = new Mock<IEmissorRepository>();
        emissorRepository.Setup(r => r.GetAsync(ownerId, emissor.Id, true)).ReturnsAsync(emissor);

        var movimentoRepository = new Mock<IMovimentoRepository>();
        movimentoRepository.Setup(r => r.ExistemPorEmissorAsync(emissor.Id)).ReturnsAsync(false);

        var repositoryManager = new Mock<IRepositoryManager>();
        repositoryManager.SetupGet(r => r.Emissor).Returns(emissorRepository.Object);
        repositoryManager.SetupGet(r => r.Movimento).Returns(movimentoRepository.Object);

        var command = new DeleteEmissorCommand(repositoryManager.Object);

        // Act
        await command.ExecuteAsync(ownerId, emissor.Id);

        // Assert
        emissorRepository.Verify(r => r.Delete(emissor), Times.Once);
        repositoryManager.Verify(r => r.SaveAsync(), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenEmissorTemMovimentosAssociados_LancaEntityInUseExceptionSemExcluir()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var emissor = new Emissor { UserId = ownerId };

        var emissorRepository = new Mock<IEmissorRepository>();
        emissorRepository.Setup(r => r.GetAsync(ownerId, emissor.Id, true)).ReturnsAsync(emissor);

        var movimentoRepository = new Mock<IMovimentoRepository>();
        movimentoRepository.Setup(r => r.ExistemPorEmissorAsync(emissor.Id)).ReturnsAsync(true);

        var repositoryManager = new Mock<IRepositoryManager>();
        repositoryManager.SetupGet(r => r.Emissor).Returns(emissorRepository.Object);
        repositoryManager.SetupGet(r => r.Movimento).Returns(movimentoRepository.Object);

        var command = new DeleteEmissorCommand(repositoryManager.Object);

        // Act & Assert
        await Assert.ThrowsAsync<EntityInUseException>(() => command.ExecuteAsync(ownerId, emissor.Id));
        emissorRepository.Verify(r => r.Delete(It.IsAny<Emissor>()), Times.Never);
        repositoryManager.Verify(r => r.SaveAsync(), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenEmissorNaoExiste_LancaEntityNotFoundException()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var emissorId = Guid.NewGuid();

        var emissorRepository = new Mock<IEmissorRepository>();
        emissorRepository.Setup(r => r.GetAsync(ownerId, emissorId, true)).ReturnsAsync((Emissor?)null);

        var repositoryManager = new Mock<IRepositoryManager>();
        repositoryManager.SetupGet(r => r.Emissor).Returns(emissorRepository.Object);

        var command = new DeleteEmissorCommand(repositoryManager.Object);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() => command.ExecuteAsync(ownerId, emissorId));
        repositoryManager.Verify(r => r.SaveAsync(), Times.Never);
    }
}
