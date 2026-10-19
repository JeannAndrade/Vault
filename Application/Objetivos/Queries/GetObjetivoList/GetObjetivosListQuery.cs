using LumiaFoundation.Logger.Contracts;
using Persistence.Managment;

namespace Application.Objetivos.Queries.GetObjetivoList;

public class GetObjetivosListQuery(IRepositoryManager repositoryManager, ILoggerManager logger) : IGetObjetivosListQuery
{
    private readonly IRepositoryManager _repository = repositoryManager;
    private readonly ILoggerManager _logger = logger;

    public async Task<List<ObjetivoModel>> ExecuteAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        try
        {
            var objetivos = await _repository.Objetivo.GetAllAsync(ownerId, trackChanges: false, cancellationToken);

            return [.. objetivos.Select(ObjetivoModel.FromDomain)];
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError($"Algo deu errado no método de serviço {nameof(GetObjetivosListQuery)}: {ex}");
            throw;
        }
    }
}
