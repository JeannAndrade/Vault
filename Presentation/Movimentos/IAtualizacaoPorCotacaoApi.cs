using Presentation.Cotacoes;

namespace Presentation.Movimentos;

public interface IAtualizacaoPorCotacaoApi
{
    Task<List<PapelParaCotacaoDto>> GetAsync(CancellationToken cancellationToken);
    Task<AtualizacaoPorCotacaoResultadoDto> AtualizarAsync(
        AtualizacaoPorCotacaoSolicitacaoDto solicitacao,
        CancellationToken cancellationToken);
}
