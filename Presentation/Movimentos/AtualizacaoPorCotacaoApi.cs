using LumiaFoundation.Http.Client.Services;
using Presentation.Cotacoes;

namespace Presentation.Movimentos;

public sealed class AtualizacaoPorCotacaoApi(IApiConnection connection) : IAtualizacaoPorCotacaoApi
{
    private const string BasePath = "api/movimentos/atualizacao-por-cotacao";

    public async Task<List<PapelParaCotacaoDto>> GetAsync(CancellationToken cancellationToken)
    {
        return await connection.SendAsync<List<PapelParaCotacaoDto>>(HttpMethod.Get, BasePath, ct: cancellationToken)
            ?? [];
    }

    public async Task<AtualizacaoPorCotacaoResultadoDto> AtualizarAsync(
        AtualizacaoPorCotacaoSolicitacaoDto solicitacao,
        CancellationToken cancellationToken)
    {
        return await connection.SendAsync<AtualizacaoPorCotacaoResultadoDto>(
            HttpMethod.Post,
            BasePath,
            solicitacao,
            cancellationToken)
            ?? throw new InvalidOperationException("A API respondeu sem corpo ao atualizar por cotação.");
    }
}
