using Application.Movimentos.AtualizacaoPorCotacao;

namespace Application.Movimentos.Commands.AtualizarPorCotacao;

public interface IAtualizarPorCotacaoCommand
{
    Task<AtualizacaoPorCotacaoResultadoModel> ExecuteAsync(
        AtualizacaoPorCotacaoParameters parameters,
        Guid ownerId,
        CancellationToken cancellationToken);
}
