using LumiaFoundation.Logger.Contracts;
using LumiaFoundation.Core.TimeService;
using Persistence.Managment;

namespace Application.Objetivos.Queries.GetResumoObjetivosList;

public class GetResumoObjetivosListQuery(IRepositoryManager repositoryManager, ILoggerManager logger, IDateTimeProvider dateTimeProvider) : IGetResumoObjetivosListQuery
{
    private readonly IRepositoryManager _repository = repositoryManager;
    private readonly ILoggerManager _logger = logger;
    private readonly IDateTimeProvider _dateTimeProvider = dateTimeProvider;

    public async Task<List<ResumoObjetivoModel>> ExecuteAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        try
        {
            var objetivos = await _repository.Objetivo.GetAllWithRelatedEntitiesAsync(ownerId, cancellationToken);

            var hoje = _dateTimeProvider.GetDateTime().Date;
            return [.. objetivos.Where(o => o.EstaAtivo).Select(o => ResumoObjetivoModel.FromDomain(o, hoje))];
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError($"Algo deu errado no método de serviço {nameof(GetResumoObjetivosListQuery)}: {ex}");
            throw;
        }
    }
}
