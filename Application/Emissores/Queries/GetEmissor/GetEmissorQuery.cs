using LumiaFoundation.Core.Domain.Exceptions;
using Persistence.Managment;

namespace Application.Emissores.Queries.GetEmissor;

public class GetEmissorQuery(IRepositoryManager repositoryManager) : IGetEmissorQuery
{
  private readonly IRepositoryManager _repository = repositoryManager;

  public async Task<EmissorModel> ExecuteAsync(Guid ownerId, Guid emissorId)
  {
    var emissor = await _repository.Emissor.GetAsync(ownerId, emissorId, trackChanges: false)
        ?? throw new EntityNotFoundException("Emissor not found");

    return new EmissorModel
    {
      Id = emissor.Id,
      Nome = emissor.Nome,
      UserId = emissor.UserId
    };
  }
}
