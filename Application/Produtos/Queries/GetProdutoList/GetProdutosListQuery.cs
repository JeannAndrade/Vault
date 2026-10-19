using LumiaFoundation.Logger.Contracts;
using Persistence.Managment;

namespace Application.Produtos.Queries.GetProdutoList;

public class GetProdutosListQuery(IRepositoryManager repositoryManager, ILoggerManager logger) : IGetProdutosListQuery
{
    private readonly IRepositoryManager _repository = repositoryManager;
    private readonly ILoggerManager _logger = logger;

    public async Task<List<ProdutoModel>> ExecuteAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        try
        {
            var produtos = await _repository.Produto.GetAllAsync(ownerId, trackChanges: false, cancellationToken);

            return [.. produtos.Select(ProdutoModel.FromDomain)];
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError($"Algo deu errado no método de serviço {nameof(GetProdutosListQuery)}: {ex}");
            throw;
        }
    }
}
