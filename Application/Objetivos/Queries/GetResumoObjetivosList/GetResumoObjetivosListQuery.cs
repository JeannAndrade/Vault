using LumiaFoundation.Core.TimeService;
using Persistence.Management;

namespace Application.Objetivos.Queries.GetResumoObjetivosList;

public class GetResumoObjetivosListQuery(IRepositoryManager repositoryManager, IDateTimeProvider dateTimeProvider) : IGetResumoObjetivosListQuery
{
    private readonly IRepositoryManager _repository = repositoryManager;
    private readonly IDateTimeProvider _dateTimeProvider = dateTimeProvider;

    public async Task<List<ResumoObjetivoModel>> ExecuteAsync(Guid ownerId, CancellationToken cancellationToken)
    {
            var objetivos = await _repository.Objetivo.GetAllWithRelatedEntitiesAsync(ownerId, cancellationToken);

            var hoje = _dateTimeProvider.GetDateTime().Date;
            return [.. objetivos.Where(o => o.EstaAtivo).Select(o => ResumoObjetivoModel.FromDomain(o, hoje))];

    }
}
