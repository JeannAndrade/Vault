using LumiaFoundation.Logger.Contracts;
using Persistence.Managment;

namespace Application.Produtos.Queries.GetProdutoList;

public class GetProdutosListQuery(IRepositoryManager repositoryManager, ILoggerManager logger) : IGetProdutosListQuery
{
  private readonly IRepositoryManager _repository = repositoryManager;
  private readonly ILoggerManager _logger = logger;

  public async Task<List<ProdutoModel>> ExecuteAsync(Guid ownerId)
  {
    try
    {
      var produtos = await _repository.Produto.GetAllProdutosAsync(ownerId, trackChanges: false);

      return [.. produtos.Select(p => new ProdutoModel
            {
                Id = p.Id,
                Nome = p.Nome,
                UserId = p.UserId
            })];
    }
    catch (Exception ex)
    {
      _logger.LogError($"Something went wrong in the {nameof(GetProdutosListQuery)} service method {ex}");
      throw;
    }
  }
}
