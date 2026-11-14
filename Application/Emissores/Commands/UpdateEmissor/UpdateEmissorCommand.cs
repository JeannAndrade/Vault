using LumiaFoundation.Core.Domain.Exceptions;
using LumiaFoundation.Core.Validators;
using Application.Repositories;

namespace Application.Emissores.Commands.UpdateEmissor;

public class UpdateEmissorCommand(IRepositoryManager repositoryManager) : IUpdateEmissorCommand
{
  private readonly IRepositoryManager _repositoryManager = repositoryManager;

  public async Task<EmissorModel> ExecuteAsync(EmissorModelForUpdate emissorModel, Guid ownerId, Guid emissorId, CancellationToken cancellationToken)
  {
    CommandValidator.Validate(emissorModel);

    var emissor = await _repositoryManager.Emissor.GetAsync(ownerId, emissorId, trackChanges: true, cancellationToken)
    ?? throw new EntityNotFoundException("Emissor não encontrado");

    _repositoryManager.Emissor.Update(emissorModel.UpdateDomain(emissor));
    await _repositoryManager.SaveAsync(cancellationToken);

    return EmissorModel.FromDomain(emissor);
  }
}
