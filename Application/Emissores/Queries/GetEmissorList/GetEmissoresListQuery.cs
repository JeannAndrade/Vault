using LumiaFoundation.Logger.Contracts;
using Persistence.Managment;

namespace Application.Emissores.Queries.GetEmissorList;

public class GetEmissoresListQuery(IRepositoryManager repositoryManager, ILoggerManager logger) : IGetEmissoresListQuery
{
    private readonly IRepositoryManager _repository = repositoryManager;
    private readonly ILoggerManager _logger = logger;

    public async Task<List<EmissorModel>> ExecuteAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        try
        {
            var emissores = await _repository.Emissor.GetAllAsync(ownerId, trackChanges: false, cancellationToken);

            return [.. emissores.Select(EmissorModel.FromDomain)];
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError($"Algo deu errado no método de serviço {nameof(GetEmissoresListQuery)}: {ex}");
            throw;
        }
    }
}
