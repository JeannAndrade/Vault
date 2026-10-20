using LumiaFoundation.Http.Client.Services;

namespace Presentation.Movimentos;

public sealed class DistribuicaoValorLiquidoApi(IApiConnection connection) : IDistribuicaoValorLiquidoApi
{
    private const string BasePath = "api/movimentos/distribuicao-valor-liquido";

    public async Task<DistribuicaoValorLiquidoDto> SimularAsync(DistribuicaoValorLiquidoSolicitacaoDto solicitacao, CancellationToken cancellationToken) =>
        await connection.SendAsync<DistribuicaoValorLiquidoDto>(HttpMethod.Post, $"{BasePath}/simulacao", solicitacao, cancellationToken)
        ?? throw new InvalidOperationException("A API respondeu sem corpo ao simular a distribuição.");

    public Task DistribuirAsync(DistribuicaoValorLiquidoSolicitacaoDto solicitacao, CancellationToken cancellationToken) =>
        connection.SendAsync(HttpMethod.Post, BasePath, solicitacao, cancellationToken);
}
