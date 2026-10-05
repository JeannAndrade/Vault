using LumiaFoundation.Logger.Contracts;
using Persistence.Managment;

namespace Application.Objetivos.Queries.GetResumoObjetivosList;

public class GetResumoObjetivosListQuery(IRepositoryManager repositoryManager, ILoggerManager logger) : IGetResumoObjetivosListQuery
{
    private readonly IRepositoryManager _repository = repositoryManager;
    private readonly ILoggerManager _logger = logger;

    public async Task<List<ResumoObjetivoModel>> ExecuteAsync(Guid ownerId)
    {
        try
        {
            var objetivos = await _repository.Objetivo.GetAllWithRelatedEntitiesAsync(ownerId);

            return [.. objetivos.Select(ResumoObjetivoModel.FromDomain)];
        }
        catch (Exception ex)
        {
            _logger.LogError($"Algo deu errado no método de serviço {nameof(GetResumoObjetivosListQuery)}: {ex}");
            throw;
        }
    }
}
