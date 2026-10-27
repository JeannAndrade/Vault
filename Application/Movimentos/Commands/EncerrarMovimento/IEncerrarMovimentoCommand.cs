namespace Application.Movimentos.Commands.EncerrarMovimento;

public interface IEncerrarMovimentoCommand
{
    Task ExecuteAsync(
        MovimentoModelForEncerramento model,
        Guid ownerId,
        Guid movimentoId,
        CancellationToken cancellationToken);
}
