using LumiaFoundation.Core.Domain.Exceptions;
using LumiaFoundation.Core.Validators;
using Persistence.Managment;

namespace Application.Emissores.Commands.UpdateEmissor;

public class UpdateEmissorCommand(IRepositoryManager repositoryManager) : IUpdateEmissorCommand
{
  private readonly IRepositoryManager _repositoryManager = repositoryManager;

  public async Task<EmissorModel> ExecuteAsync(EmissorModelForUpdate emissorModel, Guid ownerId, Guid emissorId)
  {
    CommandValidator.Validate(emissorModel);

    var emissor = await _repositoryManager.Emissor.GetAsync(ownerId, emissorId, trackChanges: true)
    ?? throw new EntityNotFoundException("Emissor not found");

    _repositoryManager.Emissor.Update(emissorModel.UpdateDomain(emissor));
    await _repositoryManager.SaveAsync();

    return EmissorModel.FromDomain(emissor);
  }
}
