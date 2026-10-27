namespace Application.Movimentos.Commands.ReativarMovimento;

public interface IReativarMovimentoCommand
{
    Task ExecuteAsync(Guid ownerId, Guid movimentoId, CancellationToken cancellationToken);
}
