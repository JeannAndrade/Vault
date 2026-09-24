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
            var movimentos = await _repository.Movimento.GetAllAsync(ownerId, trackChanges: false);

            return [.. movimentos.Select(MovimentoModel.FromDomain)];
        }
        catch (Exception ex)
        {
            _logger.LogError($"Something went wrong in the {nameof(GetMovimentosListQuery)} service method {ex}");
            throw;
        }
    }
}
