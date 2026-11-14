using LumiaFoundation.Core.Validators;
using Application.Repositories;

namespace Application.TiposRenda.Commands.CreateTipoRenda;

public class CreateTipoRendaCommand(IRepositoryManager repositoryManager) : ICreateTipoRendaCommand
{
  private readonly IRepositoryManager _repositoryManager = repositoryManager;

  public async Task<TipoRendaModel> ExecuteAsync(TipoRendaModelForCreation tipoRendaModel, Guid ownerId, CancellationToken cancellationToken)
  {
    CommandValidator.Validate(tipoRendaModel);

    var tipoRenda = tipoRendaModel.ToDomain(ownerId);
    _repositoryManager.TipoRenda.Create(tipoRenda);
    await _repositoryManager.SaveAsync(cancellationToken);

    return TipoRendaModel.FromDomain(tipoRenda);
  }
}
