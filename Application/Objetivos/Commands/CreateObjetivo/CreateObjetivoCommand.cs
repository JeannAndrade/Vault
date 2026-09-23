using LumiaFoundation.Core.Validators;
using Persistence.Managment;

namespace Application.Objetivos.Commands.CreateObjetivo;

public class CreateObjetivoCommand(IRepositoryManager repositoryManager) : ICreateObjetivoCommand
{
  private readonly IRepositoryManager _repositoryManager = repositoryManager;

  public async Task<ObjetivoModel> ExecuteAsync(ObjetivoModelForCreation objetivoModel)
  {
    CommandValidator.Validate(objetivoModel);

    var objetivo = objetivoModel.ToDomain();
    _repositoryManager.Objetivo.Create(objetivo);
    await _repositoryManager.SaveAsync();

    return ObjetivoModel.FromDomain(objetivo);
  }
}
