using LumiaFoundation.Logger.Contracts;
using Persistence.Managment;

namespace Application.TiposRenda.Queries.GetTiposRendaList;

public class GetTiposRendaListQuery(IRepositoryManager repositoryManager, ILoggerManager logger) : IGetTiposRendaListQuery
{
    private readonly IRepositoryManager _repository = repositoryManager;
    private readonly ILoggerManager _logger = logger;

    public async Task<List<TipoRendaModel>> ExecuteAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        try
        {
            var tiposRenda = await _repository.TipoRenda.GetAllAsync(ownerId, trackChanges: false, cancellationToken);

            return [.. tiposRenda.Select(TipoRendaModel.FromDomain)];
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError($"Algo deu errado no método de serviço {nameof(GetTiposRendaListQuery)}: {ex}");
            throw;
        }
    }
}
