using Application.Movimentos.AtualizacaoPorCotacao;

namespace Application.Movimentos.Queries.GetAtualizacaoPorCotacao;

public interface IGetAtualizacaoPorCotacaoQuery
{
    Task<List<PapelParaCotacaoModel>> ExecuteAsync(Guid ownerId, CancellationToken cancellationToken);
}
