using LumiaFoundation.Core.Validators;
using Application.Repositories;

namespace Application.Emissores.Commands.CreateEmissor;

public class CreateEmissorCommand(IRepositoryManager repositoryManager) : ICreateEmissorCommand
{
  private readonly IRepositoryManager _repositoryManager = repositoryManager;

  public async Task<EmissorModel> ExecuteAsync(EmissorModelForCreation emissorModel, Guid ownerId, CancellationToken cancellationToken)
  {
    CommandValidator.Validate(emissorModel);

    var emissor = emissorModel.ToDomain(ownerId);
    _repositoryManager.Emissor.Create(emissor);
    await _repositoryManager.SaveAsync(cancellationToken);

    return EmissorModel.FromDomain(emissor);
  }
}
