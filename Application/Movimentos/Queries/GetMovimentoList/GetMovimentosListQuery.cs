using LumiaFoundation.Logger.Contracts;
using Persistence.Managment;

namespace Application.Movimentos.Queries.GetMovimentoList;

public class GetMovimentosListQuery(IRepositoryManager repositoryManager, ILoggerManager logger) : IGetMovimentosListQuery
{
    private readonly IRepositoryManager _repository = repositoryManager;
    private readonly ILoggerManager _logger = logger;

    public async Task<List<MovimentoModel>> ExecuteAsync(Guid ownerId)
    {
        try
        {
            var movimentos = await _repository.Movimento.GetAllWithRelatedEntitiesAsync(ownerId);

            return [.. movimentos.Select(MovimentoModel.FromDomain)];
        }
        catch (Exception ex)
        {
            _logger.LogError($"Algo deu errado no método de serviço {nameof(GetMovimentosListQuery)}: {ex}");
            throw;
        }
    }
}
