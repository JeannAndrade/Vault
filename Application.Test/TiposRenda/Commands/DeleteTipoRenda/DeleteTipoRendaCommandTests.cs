using Application.TiposRenda.Commands.DeleteTipoRenda;
using Domain.TiposRenda;
using LumiaFoundation.Core.Domain.Exceptions;
using Moq;
using Persistence.Managment;
using Persistence.Movimentos;
using Persistence.TiposRenda;

namespace Application.Test.TiposRenda.Commands.DeleteTipoRenda;

public class DeleteTipoRendaCommandTests
{
    [Fact]
    public async Task ExecuteAsync_WhenTipoRendaNaoTemMovimentosAssociados_ExcluiEPersiste()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var tipoRenda = new TipoRenda { UserId = ownerId };

        var tipoRendaRepository = new Mock<ITipoRendaRepository>();
        tipoRendaRepository.Setup(r => r.GetAsync(ownerId, tipoRenda.Id, true)).ReturnsAsync(tipoRenda);

        var movimentoRepository = new Mock<IMovimentoRepository>();
        movimentoRepository.Setup(r => r.ExistemPorTipoRendaAsync(tipoRenda.Id)).ReturnsAsync(false);

        var repositoryManager = new Mock<IRepositoryManager>();
        repositoryManager.SetupGet(r => r.TipoRenda).Returns(tipoRendaRepository.Object);
        repositoryManager.SetupGet(r => r.Movimento).Returns(movimentoRepository.Object);

        var command = new DeleteTipoRendaCommand(repositoryManager.Object);

        // Act
        await command.ExecuteAsync(ownerId, tipoRenda.Id);

        // Assert
        tipoRendaRepository.Verify(r => r.Delete(tipoRenda), Times.Once);
        repositoryManager.Verify(r => r.SaveAsync(), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTipoRendaTemMovimentosAssociados_LancaEntityInUseExceptionSemExcluir()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var tipoRenda = new TipoRenda { UserId = ownerId };

        var tipoRendaRepository = new Mock<ITipoRendaRepository>();
        tipoRendaRepository.Setup(r => r.GetAsync(ownerId, tipoRenda.Id, true)).ReturnsAsync(tipoRenda);

        var movimentoRepository = new Mock<IMovimentoRepository>();
        movimentoRepository.Setup(r => r.ExistemPorTipoRendaAsync(tipoRenda.Id)).ReturnsAsync(true);

        var repositoryManager = new Mock<IRepositoryManager>();
        repositoryManager.SetupGet(r => r.TipoRenda).Returns(tipoRendaRepository.Object);
        repositoryManager.SetupGet(r => r.Movimento).Returns(movimentoRepository.Object);

        var command = new DeleteTipoRendaCommand(repositoryManager.Object);

        // Act & Assert
        await Assert.ThrowsAsync<EntityInUseException>(() => command.ExecuteAsync(ownerId, tipoRenda.Id));
        tipoRendaRepository.Verify(r => r.Delete(It.IsAny<TipoRenda>()), Times.Never);
        repositoryManager.Verify(r => r.SaveAsync(), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenTipoRendaNaoExiste_LancaEntityNotFoundException()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var tipoRendaId = Guid.NewGuid();

        var tipoRendaRepository = new Mock<ITipoRendaRepository>();
        tipoRendaRepository.Setup(r => r.GetAsync(ownerId, tipoRendaId, true)).ReturnsAsync((TipoRenda?)null);

        var repositoryManager = new Mock<IRepositoryManager>();
        repositoryManager.SetupGet(r => r.TipoRenda).Returns(tipoRendaRepository.Object);

        var command = new DeleteTipoRendaCommand(repositoryManager.Object);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() => command.ExecuteAsync(ownerId, tipoRendaId));
        repositoryManager.Verify(r => r.SaveAsync(), Times.Never);
    }
}
