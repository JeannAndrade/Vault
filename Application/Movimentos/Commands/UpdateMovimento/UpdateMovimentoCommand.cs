using LumiaFoundation.Core.Domain.Exceptions;
using LumiaFoundation.Core.Validators;
using Persistence.Managment;

namespace Application.Movimentos.Commands.UpdateMovimento;

public class UpdateMovimentoCommand(IRepositoryManager repositoryManager) : IUpdateMovimentoCommand
{
  private readonly IRepositoryManager _repositoryManager = repositoryManager;

  public async Task<MovimentoModel> ExecuteAsync(MovimentoModelForUpdate movimentoModel, Guid ownerId, Guid movimentoId)
  {
    CommandValidator.Validate(movimentoModel);

    var movimento = await _repositoryManager.Movimento.GetAsync(ownerId, movimentoId, trackChanges: true)
        ?? throw new EntityNotFoundException("Movimento not found");

    _repositoryManager.Movimento.Update(movimentoModel.UpdateDomain(movimento));
    await _repositoryManager.SaveAsync();

    return MovimentoModel.FromDomain(movimento);
  }
}
