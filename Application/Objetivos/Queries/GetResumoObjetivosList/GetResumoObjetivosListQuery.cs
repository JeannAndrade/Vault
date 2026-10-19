using LumiaFoundation.Logger.Contracts;
using Persistence.Managment;

namespace Application.Objetivos.Queries.GetResumoObjetivosList;

public class GetResumoObjetivosListQuery(IRepositoryManager repositoryManager, ILoggerManager logger) : IGetResumoObjetivosListQuery
{
    private readonly IRepositoryManager _repository = repositoryManager;
    private readonly ILoggerManager _logger = logger;

    public async Task<List<ResumoObjetivoModel>> ExecuteAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        try
        {
            var objetivos = await _repository.Objetivo.GetAllWithRelatedEntitiesAsync(ownerId, cancellationToken);

            return [.. objetivos.Where(o => o.EstaAtivo).Select(ResumoObjetivoModel.FromDomain)];
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError($"Algo deu errado no método de serviço {nameof(GetResumoObjetivosListQuery)}: {ex}");
            throw;
        }
    }
}
