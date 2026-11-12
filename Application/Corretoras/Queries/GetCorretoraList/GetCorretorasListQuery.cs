using Persistence.Management;

namespace Application.Corretoras.Queries.GetCorretoraList;

public class GetCorretorasListQuery(IRepositoryManager repositoryManager) : IGetCorretorasListQuery
{
    private readonly IRepositoryManager _repository = repositoryManager;

    public async Task<List<CorretoraModel>> ExecuteAsync(Guid ownerId, CancellationToken cancellationToken)
    {
            var corretoras = await _repository.Corretora.GetAllAsync(ownerId, trackChanges: false, cancellationToken);

            return [.. corretoras.Select(CorretoraModel.FromDomain)];

    }
}
