using Application.Objetivos.Commands.DeleteObjetivo;
using Domain.Objetivos;
using LumiaFoundation.Core.Domain.Exceptions;
using Moq;
using Persistence.Managment;
using Persistence.Movimentos;
using Persistence.Objetivos;

namespace Application.Test.Objetivos.Commands.DeleteObjetivo;

public class DeleteObjetivoCommandTests
{
    [Fact]
    public async Task ExecuteAsync_WhenObjetivoNaoTemMovimentosAssociados_ExcluiEPersiste()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var objetivo = new Objetivo { UserId = ownerId };

        var objetivoRepository = new Mock<IObjetivoRepository>();
        objetivoRepository.Setup(r => r.GetAsync(ownerId, objetivo.Id, true)).ReturnsAsync(objetivo);

        var movimentoRepository = new Mock<IMovimentoRepository>();
        movimentoRepository.Setup(r => r.ExistemPorObjetivoAsync(objetivo.Id)).ReturnsAsync(false);

        var repositoryManager = new Mock<IRepositoryManager>();
        repositoryManager.SetupGet(r => r.Objetivo).Returns(objetivoRepository.Object);
        repositoryManager.SetupGet(r => r.Movimento).Returns(movimentoRepository.Object);

        var command = new DeleteObjetivoCommand(repositoryManager.Object);

        // Act
        await command.ExecuteAsync(ownerId, objetivo.Id);

        // Assert
        objetivoRepository.Verify(r => r.Delete(objetivo), Times.Once);
        repositoryManager.Verify(r => r.SaveAsync(), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenObjetivoTemMovimentosAssociados_LancaEntityInUseExceptionSemExcluir()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var objetivo = new Objetivo { UserId = ownerId };

        var objetivoRepository = new Mock<IObjetivoRepository>();
        objetivoRepository.Setup(r => r.GetAsync(ownerId, objetivo.Id, true)).ReturnsAsync(objetivo);

        var movimentoRepository = new Mock<IMovimentoRepository>();
        movimentoRepository.Setup(r => r.ExistemPorObjetivoAsync(objetivo.Id)).ReturnsAsync(true);

        var repositoryManager = new Mock<IRepositoryManager>();
        repositoryManager.SetupGet(r => r.Objetivo).Returns(objetivoRepository.Object);
        repositoryManager.SetupGet(r => r.Movimento).Returns(movimentoRepository.Object);

        var command = new DeleteObjetivoCommand(repositoryManager.Object);

        // Act & Assert
        await Assert.ThrowsAsync<EntityInUseException>(() => command.ExecuteAsync(ownerId, objetivo.Id));
        objetivoRepository.Verify(r => r.Delete(It.IsAny<Objetivo>()), Times.Never);
        repositoryManager.Verify(r => r.SaveAsync(), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenObjetivoNaoExiste_LancaEntityNotFoundException()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var objetivoId = Guid.NewGuid();

        var objetivoRepository = new Mock<IObjetivoRepository>();
        objetivoRepository.Setup(r => r.GetAsync(ownerId, objetivoId, true)).ReturnsAsync((Objetivo?)null);

        var repositoryManager = new Mock<IRepositoryManager>();
        repositoryManager.SetupGet(r => r.Objetivo).Returns(objetivoRepository.Object);

        var command = new DeleteObjetivoCommand(repositoryManager.Object);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() => command.ExecuteAsync(ownerId, objetivoId));
        repositoryManager.Verify(r => r.SaveAsync(), Times.Never);
    }
}
