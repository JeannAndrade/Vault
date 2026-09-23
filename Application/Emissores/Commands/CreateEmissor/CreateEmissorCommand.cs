using LumiaFoundation.Core.Validators;
using Persistence.Managment;

namespace Application.Emissores.Commands.CreateEmissor;

public class CreateEmissorCommand(IRepositoryManager repositoryManager) : ICreateEmissorCommand
{
  private readonly IRepositoryManager _repositoryManager = repositoryManager;

  public async Task<EmissorModel> ExecuteAsync(EmissorModelForCreation emissorModel)
  {
    CommandValidator.Validate(emissorModel);

    var emissor = emissorModel.ToDomain();
    _repositoryManager.Emissor.Create(emissor);
    await _repositoryManager.SaveAsync();

    return EmissorModel.FromDomain(emissor);
  }
}
