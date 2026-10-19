using LumiaFoundation.Core.Domain.Exceptions;
using Persistence.Managment;

namespace Application.Produtos.Queries.GetProduto;

public class GetProdutoQuery(IRepositoryManager repositoryManager) : IGetProdutoQuery
{
    private readonly IRepositoryManager _repository = repositoryManager;

    public async Task<ProdutoModel> ExecuteAsync(Guid ownerId, Guid produtoId, CancellationToken cancellationToken)
    {
        var produto = await _repository.Produto.GetAsync(ownerId, produtoId, trackChanges: false, cancellationToken)
            ?? throw new EntityNotFoundException("Produto não encontrado");

        return ProdutoModel.FromDomain(produto);
    }
}
