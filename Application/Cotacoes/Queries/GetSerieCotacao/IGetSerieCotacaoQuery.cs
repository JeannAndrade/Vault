namespace Application.Cotacoes.Queries.GetSerieCotacao;

public interface IGetSerieCotacaoQuery
{
    Task<List<CotacaoPontoModel>> ExecuteAsync(Guid ownerId, string papel, CancellationToken cancellationToken);
}
