using LumiaFoundation.Core.Domain.Exceptions;
using LumiaFoundation.Core.Validators;
using Persistence.Managment;

namespace Application.TiposRenda.Commands.UpdateTipoRenda;

public class UpdateTipoRendaCommand(IRepositoryManager repositoryManager) : IUpdateTipoRendaCommand
{
  private readonly IRepositoryManager _repositoryManager = repositoryManager;

  public async Task<TipoRendaModel> ExecuteAsync(TipoRendaModelForUpdate tipoRendaModel, Guid ownerId, Guid tipoRendaId)
  {
    CommandValidator.Validate(tipoRendaModel);

    var tipoRenda = await _repositoryManager.TipoRenda.GetAsync(ownerId, tipoRendaId, trackChanges: true)
    ?? throw new EntityNotFoundException("Tipo de Renda not found");

    _repositoryManager.TipoRenda.Update(tipoRendaModel.UpdateDomain(tipoRenda));
    await _repositoryManager.SaveAsync();

    return TipoRendaModel.FromDomain(tipoRenda);
  }
}
