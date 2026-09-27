using Application.Produtos.Commands.DeleteProduto;
using Domain.Produtos;
using LumiaFoundation.Core.Domain.Exceptions;
using Moq;
using Persistence.Managment;
using Persistence.Movimentos;
using Persistence.Produtos;

namespace Application.Test.Produtos.Commands.DeleteProduto;

public class DeleteProdutoCommandTests
{
    [Fact]
    public async Task ExecuteAsync_WhenProdutoNaoTemMovimentosAssociados_ExcluiEPersiste()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var produto = new Produto { UserId = ownerId };

        var produtoRepository = new Mock<IProdutoRepository>();
        produtoRepository.Setup(r => r.GetAsync(ownerId, produto.Id, true)).ReturnsAsync(produto);

        var movimentoRepository = new Mock<IMovimentoRepository>();
        movimentoRepository.Setup(r => r.ExistemPorProdutoAsync(produto.Id)).ReturnsAsync(false);

        var repositoryManager = new Mock<IRepositoryManager>();
        repositoryManager.SetupGet(r => r.Produto).Returns(produtoRepository.Object);
        repositoryManager.SetupGet(r => r.Movimento).Returns(movimentoRepository.Object);

        var command = new DeleteProdutoCommand(repositoryManager.Object);

        // Act
        await command.ExecuteAsync(ownerId, produto.Id);

        // Assert
        produtoRepository.Verify(r => r.Delete(produto), Times.Once);
        repositoryManager.Verify(r => r.SaveAsync(), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WhenProdutoTemMovimentosAssociados_LancaEntityInUseExceptionSemExcluir()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var produto = new Produto { UserId = ownerId };

        var produtoRepository = new Mock<IProdutoRepository>();
        produtoRepository.Setup(r => r.GetAsync(ownerId, produto.Id, true)).ReturnsAsync(produto);

        var movimentoRepository = new Mock<IMovimentoRepository>();
        movimentoRepository.Setup(r => r.ExistemPorProdutoAsync(produto.Id)).ReturnsAsync(true);

        var repositoryManager = new Mock<IRepositoryManager>();
        repositoryManager.SetupGet(r => r.Produto).Returns(produtoRepository.Object);
        repositoryManager.SetupGet(r => r.Movimento).Returns(movimentoRepository.Object);

        var command = new DeleteProdutoCommand(repositoryManager.Object);

        // Act & Assert
        await Assert.ThrowsAsync<EntityInUseException>(() => command.ExecuteAsync(ownerId, produto.Id));
        produtoRepository.Verify(r => r.Delete(It.IsAny<Produto>()), Times.Never);
        repositoryManager.Verify(r => r.SaveAsync(), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_WhenProdutoNaoExiste_LancaEntityNotFoundException()
    {
        // Arrange
        var ownerId = Guid.NewGuid();
        var produtoId = Guid.NewGuid();

        var produtoRepository = new Mock<IProdutoRepository>();
        produtoRepository.Setup(r => r.GetAsync(ownerId, produtoId, true)).ReturnsAsync((Produto?)null);

        var repositoryManager = new Mock<IRepositoryManager>();
        repositoryManager.SetupGet(r => r.Produto).Returns(produtoRepository.Object);

        var command = new DeleteProdutoCommand(repositoryManager.Object);

        // Act & Assert
        await Assert.ThrowsAsync<EntityNotFoundException>(() => command.ExecuteAsync(ownerId, produtoId));
        repositoryManager.Verify(r => r.SaveAsync(), Times.Never);
    }
}
