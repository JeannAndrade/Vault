using Persistence.Managment;

namespace Application.Produtos.Queries.GetProdutoList;

public class GetProdutosListQuery(IRepositoryManager repositoryManager) : IGetProdutosListQuery
{
    private readonly IRepositoryManager _repository = repositoryManager;

    public async Task<List<ProdutoModel>> ExecuteAsync(Guid ownerId, CancellationToken cancellationToken)
    {
            var produtos = await _repository.Produto.GetAllAsync(ownerId, trackChanges: false, cancellationToken);

            return [.. produtos.Select(ProdutoModel.FromDomain)];

    }
}
