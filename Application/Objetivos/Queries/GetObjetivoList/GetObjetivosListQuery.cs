using LumiaFoundation.Logger.Contracts;
using Persistence.Managment;

namespace Application.Objetivos.Queries.GetObjetivoList;

public class GetObjetivosListQuery(IRepositoryManager repositoryManager, ILoggerManager logger) : IGetObjetivosListQuery
{
    private readonly IRepositoryManager _repository = repositoryManager;
    private readonly ILoggerManager _logger = logger;

    public async Task<List<ObjetivoModel>> ExecuteAsync(Guid ownerId)
    {
        try
        {
            var objetivos = await _repository.Objetivo.GetAllAsync(ownerId, trackChanges: false);

            return [.. objetivos.Select(ObjetivoModel.FromDomain)];
        }
        catch (Exception ex)
        {
            _logger.LogError($"Something went wrong in the {nameof(GetObjetivosListQuery)} service method {ex}");
            throw;
        }
    }
}
