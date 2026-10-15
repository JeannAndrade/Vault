using Application.Movimentos.DistribuicaoValorLiquido;
namespace Application.Movimentos.Commands.DistribuirValorLiquido;
public interface IDistribuirValorLiquidoCommand
{
    Task ExecuteAsync(DistribuicaoValorLiquidoParameters parametros, Guid ownerId, CancellationToken cancellationToken);
}
