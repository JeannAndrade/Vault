namespace Presentation.Movimentos;

public interface IDistribuicaoValorLiquidoApi
{
    Task<DistribuicaoValorLiquidoDto> SimularAsync(DistribuicaoValorLiquidoSolicitacaoDto solicitacao, CancellationToken cancellationToken = default);
    Task DistribuirAsync(DistribuicaoValorLiquidoSolicitacaoDto solicitacao, CancellationToken cancellationToken = default);
}
