using Persistence.Management;

namespace Application.Objetivos.Queries.GetObjetivoList;

public class GetObjetivosListQuery(IRepositoryManager repositoryManager) : IGetObjetivosListQuery
{
    private readonly IRepositoryManager _repository = repositoryManager;

    public async Task<List<ObjetivoModel>> ExecuteAsync(Guid ownerId, CancellationToken cancellationToken)
    {
            var objetivos = await _repository.Objetivo.GetAllAsync(ownerId, trackChanges: false, cancellationToken);

            return [.. objetivos.Select(ObjetivoModel.FromDomain)];

    }
}
