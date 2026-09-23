using LumiaFoundation.Core.Domain.Exceptions;
using Persistence.Managment;

namespace Application.Produtos.Queries.GetProduto;

public class GetProdutoQuery(IRepositoryManager repositoryManager) : IGetProdutoQuery
{
  private readonly IRepositoryManager _repository = repositoryManager;

  public async Task<ProdutoModel> ExecuteAsync(Guid ownerId, Guid produtoId)
  {
    var produto = await _repository.Produto.GetAsync(ownerId, produtoId, trackChanges: false)
        ?? throw new EntityNotFoundException("Produto not found");

    return new ProdutoModel
    {
      Id = produto.Id,
      Nome = produto.Nome,
      UserId = produto.UserId
    };
  }
}
