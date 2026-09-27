using LumiaFoundation.Logger.Contracts;
using Persistence.Managment;

namespace Application.TiposRenda.Queries.GetTiposRendaList;

public class GetTiposRendaListQuery(IRepositoryManager repositoryManager, ILoggerManager logger) : IGetTiposRendaListQuery
{
    private readonly IRepositoryManager _repository = repositoryManager;
    private readonly ILoggerManager _logger = logger;

    public async Task<List<TipoRendaModel>> ExecuteAsync(Guid ownerId)
    {
        try
        {
            var tiposRenda = await _repository.TipoRenda.GetAllAsync(ownerId, trackChanges: false);

            return [.. tiposRenda.Select(TipoRendaModel.FromDomain)];
        }
        catch (Exception ex)
        {
            _logger.LogError($"Algo deu errado no método de serviço {nameof(GetTiposRendaListQuery)}: {ex}");
            throw;
        }
    }
}
