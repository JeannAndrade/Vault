using LumiaFoundation.Core.Validators;
using Persistence.Managment;

namespace Application.Corretoras.Commands.CreateCorretora;

public class CreateCorretoraCommand(IRepositoryManager repositoryManager) : ICreateCorretoraCommand
{
  private readonly IRepositoryManager _repositoryManager = repositoryManager;

  public async Task<CorretoraModel> ExecuteAsync(CorretoraModelForCreation corretoraModel)
  {
    CommandValidator.Validate(corretoraModel);

    var corretora = corretoraModel.ToDomain();
    _repositoryManager.Corretora.CreateCorretora(corretora);
    await _repositoryManager.SaveAsync();

    return CorretoraModel.FromDomain(corretora);
  }
}
