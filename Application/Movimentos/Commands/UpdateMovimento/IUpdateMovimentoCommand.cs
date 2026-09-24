namespace Application.Movimentos.Commands.UpdateMovimento;

public interface IUpdateMovimentoCommand
{
  Task<MovimentoModel> ExecuteAsync(MovimentoModelForUpdate movimentoModel, Guid ownerId, Guid movimentoId);
}
