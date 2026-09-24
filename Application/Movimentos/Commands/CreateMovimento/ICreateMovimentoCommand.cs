namespace Application.Movimentos.Commands.CreateMovimento;

public interface ICreateMovimentoCommand
{
  Task<MovimentoModel> ExecuteAsync(MovimentoModelForCreation movimentoModel, Guid userId);
}
