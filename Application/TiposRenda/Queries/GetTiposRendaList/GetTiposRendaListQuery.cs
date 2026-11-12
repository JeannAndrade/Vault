using Application.Repositories;

namespace Application.TiposRenda.Queries.GetTiposRendaList;

public class GetTiposRendaListQuery(IRepositoryManager repositoryManager) : IGetTiposRendaListQuery
{
    private readonly IRepositoryManager _repository = repositoryManager;

    public async Task<List<TipoRendaModel>> ExecuteAsync(Guid ownerId, CancellationToken cancellationToken)
    {
            var tiposRenda = await _repository.TipoRenda.GetAllAsync(ownerId, trackChanges: false, cancellationToken);

            return [.. tiposRenda.Select(TipoRendaModel.FromDomain)];

    }
}
