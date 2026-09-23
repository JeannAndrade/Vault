using LumiaFoundation.Core.Validators;
using Persistence.Managment;

namespace Application.TiposRenda.Commands.CreateTipoRenda;

public class CreateTipoRendaCommand(IRepositoryManager repositoryManager) : ICreateTipoRendaCommand
{
  private readonly IRepositoryManager _repositoryManager = repositoryManager;

  public async Task<TipoRendaModel> ExecuteAsync(TipoRendaModelForCreation tipoRendaModel)
  {
    CommandValidator.Validate(tipoRendaModel);

    var tipoRenda = tipoRendaModel.ToDomain();
    _repositoryManager.TipoRenda.Create(tipoRenda);
    await _repositoryManager.SaveAsync();

    return TipoRendaModel.FromDomain(tipoRenda);
  }
}
