using LumiaFoundation.Core.Domain.Exceptions;
using LumiaFoundation.Core.Validators;
using Application.Repositories;

namespace Application.TiposRenda.Commands.UpdateTipoRenda;

public class UpdateTipoRendaCommand(IRepositoryManager repositoryManager) : IUpdateTipoRendaCommand
{
  private readonly IRepositoryManager _repositoryManager = repositoryManager;

  public async Task<TipoRendaModel> ExecuteAsync(TipoRendaModelForUpdate tipoRendaModel, Guid ownerId, Guid tipoRendaId, CancellationToken cancellationToken)
  {
    CommandValidator.Validate(tipoRendaModel);

    var tipoRenda = await _repositoryManager.TipoRenda.GetAsync(ownerId, tipoRendaId, trackChanges: true, cancellationToken)
    ?? throw new EntityNotFoundException("Tipo de renda não encontrado");

    _repositoryManager.TipoRenda.Update(tipoRendaModel.UpdateDomain(tipoRenda));
    await _repositoryManager.SaveAsync(cancellationToken);

    return TipoRendaModel.FromDomain(tipoRenda);
  }
}
