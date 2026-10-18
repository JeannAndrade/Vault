namespace Application.Cotacoes.Queries.GetCotacao;

public interface IGetCotacaoQuery
{
    Task<CotacaoModel> ExecuteAsync(Guid ownerId, Guid cotacaoId, CancellationToken cancellationToken);
}
