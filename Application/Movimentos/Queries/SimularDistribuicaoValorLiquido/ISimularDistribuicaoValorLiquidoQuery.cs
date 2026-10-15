using Application.Movimentos.DistribuicaoValorLiquido;

namespace Application.Movimentos.Queries.SimularDistribuicaoValorLiquido;
public interface ISimularDistribuicaoValorLiquidoQuery
{
    Task<DistribuicaoValorLiquidoModel> ExecuteAsync(Guid ownerId, DistribuicaoValorLiquidoParameters parametros, CancellationToken cancellationToken);
}
