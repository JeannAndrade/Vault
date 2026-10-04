using LumiaFoundation.Logger.Contracts;
using Persistence.Managment;

namespace Application.Objetivos.Queries.GetObjetivoComValoresList;

public class GetObjetivoComValoresListQuery(IRepositoryManager repositoryManager, ILoggerManager logger) : IGetObjetivoComValoresListQuery
{
    private readonly IRepositoryManager _repository = repositoryManager;
    private readonly ILoggerManager _logger = logger;

    public async Task<List<ObjetivoComValoresModel>> ExecuteAsync(Guid ownerId)
    {
        try
        {
            var objetivos = await _repository.Objetivo.GetAllWithRelatedEntitiesAsync(ownerId);

            return [.. objetivos.Select(ObjetivoComValoresModel.FromDomain)];
        }
        catch (Exception ex)
        {
            _logger.LogError($"Algo deu errado no método de serviço {nameof(GetObjetivoComValoresListQuery)}: {ex}");
            throw;
        }
    }
}
