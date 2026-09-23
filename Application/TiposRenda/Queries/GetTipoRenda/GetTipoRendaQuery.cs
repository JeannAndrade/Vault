using LumiaFoundation.Core.Domain.Exceptions;
using Persistence.Managment;

namespace Application.TiposRenda.Queries.GetTipoRenda;

public class GetTipoRendaQuery(IRepositoryManager repositoryManager) : IGetTipoRendaQuery
{
  private readonly IRepositoryManager _repository = repositoryManager;

  public async Task<TipoRendaModel> ExecuteAsync(Guid ownerId, Guid tipoRendaId)
  {
    var tipoRenda = await _repository.TipoRenda.GetTipoRendaAsync(ownerId, tipoRendaId, trackChanges: false)
        ?? throw new EntityNotFoundException("Tipo de renda not found");

    return new TipoRendaModel
    {
      Id = tipoRenda.Id,
      Nome = tipoRenda.Nome,
      UserId = tipoRenda.UserId
    };
  }
}
