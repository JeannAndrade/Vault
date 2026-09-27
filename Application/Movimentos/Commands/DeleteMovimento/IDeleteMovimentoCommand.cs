namespace Application.Movimentos.Commands.DeleteMovimento;

public interface IDeleteMovimentoCommand
{
    Task ExecuteAsync(Guid ownerId, Guid movimentoId);
}
