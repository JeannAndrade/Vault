using LumiaFoundation.Logger.Contracts;
using Persistence.Managment;

namespace Application.Corretoras.Queries.GetCorretoraList;

public class GetCorretorasListQuery(IRepositoryManager repositoryManager, ILoggerManager logger) : IGetCorretorasListQuery
{
    private readonly IRepositoryManager _repository = repositoryManager;
    private readonly ILoggerManager _logger = logger;

    public async Task<List<CorretoraModel>> ExecuteAsync(Guid ownerId)
    {
        try
        {
            var corretoras = await _repository.Corretora.GetAllAsync(ownerId, trackChanges: false);

            return [.. corretoras.Select(CorretoraModel.FromDomain)];
        }
        catch (Exception ex)
        {
            _logger.LogError($"Algo deu errado no método de serviço {nameof(GetCorretorasListQuery)}: {ex}");
            throw;
        }
    }
}
