using LumiaFoundation.Core.Domain.Exceptions;
using Persistence.Managment;

namespace Application.Corretoras.Queries.GetCorretora;

public class GetCorretoraQuery(IRepositoryManager repositoryManager) : IGetCorretoraQuery
{
    private readonly IRepositoryManager _repository = repositoryManager;

    public async Task<CorretoraModel> ExecuteAsync(Guid ownerId, Guid corretoraId, CancellationToken cancellationToken)
    {
        var corretora = await _repository.Corretora.GetAsync(ownerId, corretoraId, trackChanges: false, cancellationToken)
            ?? throw new EntityNotFoundException("Corretora não encontrada");

        return CorretoraModel.FromDomain(corretora);
    }
}
