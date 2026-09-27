using LumiaFoundation.Core.Domain.Exceptions;
using Persistence.Managment;

namespace Application.Produtos.Commands.DeleteProduto;

public class DeleteProdutoCommand(IRepositoryManager repositoryManager) : IDeleteProdutoCommand
{
    private readonly IRepositoryManager _repositoryManager = repositoryManager;

    public async Task ExecuteAsync(Guid ownerId, Guid produtoId)
    {
        var produto = await _repositoryManager.Produto.GetAsync(ownerId, produtoId, trackChanges: true)
            ?? throw new EntityNotFoundException("Produto não encontrado");

        if (await _repositoryManager.Movimento.ExistemPorProdutoAsync(produtoId))
            throw new EntityInUseException("Não é possível excluir o Produto pois existem Movimentos associados a ele.");

        _repositoryManager.Produto.Delete(produto);
        await _repositoryManager.SaveAsync();
    }
}
