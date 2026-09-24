using LumiaFoundation.Core.Domain.Exceptions;
using LumiaFoundation.Core.Validators;
using Persistence.Managment;

namespace Application.Objetivos.Commands.UpdateObjetivo;

public class UpdateObjetivoCommand(IRepositoryManager repositoryManager) : IUpdateObjetivoCommand
{
  private readonly IRepositoryManager _repositoryManager = repositoryManager;

  public async Task<ObjetivoModel> ExecuteAsync(ObjetivoModelForUpdate objetivoModel, Guid ownerId, Guid objetivoId)
  {
    CommandValidator.Validate(objetivoModel);

    var objetivo = await _repositoryManager.Objetivo.GetAsync(ownerId, objetivoId, trackChanges: true)
    ?? throw new EntityNotFoundException("Objetivo not found");

    _repositoryManager.Objetivo.Update(objetivoModel.UpdateDomain(objetivo));
    await _repositoryManager.SaveAsync();

    return ObjetivoModel.FromDomain(objetivo);
  }
}
