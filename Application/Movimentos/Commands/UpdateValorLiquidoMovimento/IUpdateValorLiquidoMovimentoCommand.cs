namespace Application.Movimentos.Commands.UpdateValorLiquidoMovimento;

public interface IUpdateValorLiquidoMovimentoCommand
{
    Task ExecuteAsync(
        MovimentoModelForUpdateValorLiquido model,
        Guid ownerId,
        Guid movimentoId,
        CancellationToken cancellationToken);
}
