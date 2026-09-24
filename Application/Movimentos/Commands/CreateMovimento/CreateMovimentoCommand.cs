using LumiaFoundation.Core.Validators;
using Persistence.Managment;

namespace Application.Movimentos.Commands.CreateMovimento;

public class CreateMovimentoCommand(IRepositoryManager repositoryManager) : ICreateMovimentoCommand
{
  private readonly IRepositoryManager _repositoryManager = repositoryManager;

  public async Task<MovimentoModel> ExecuteAsync(MovimentoModelForCreation movimentoModel, Guid userId)
  {
    CommandValidator.Validate(movimentoModel);

    var movimento = movimentoModel.ToDomain(userId);
    _repositoryManager.Movimento.Create(movimento);
    await _repositoryManager.SaveAsync();

    return MovimentoModel.FromDomain(movimento);
  }
}
