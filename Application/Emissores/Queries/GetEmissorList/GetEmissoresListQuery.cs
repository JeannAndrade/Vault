using Application.Repositories;

namespace Application.Emissores.Queries.GetEmissorList;

public class GetEmissoresListQuery(IRepositoryManager repositoryManager) : IGetEmissoresListQuery
{
    private readonly IRepositoryManager _repository = repositoryManager;

    public async Task<List<EmissorModel>> ExecuteAsync(Guid ownerId, CancellationToken cancellationToken)
    {
            var emissores = await _repository.Emissor.GetAllAsync(ownerId, trackChanges: false, cancellationToken);

            return [.. emissores.Select(EmissorModel.FromDomain)];

    }
}
