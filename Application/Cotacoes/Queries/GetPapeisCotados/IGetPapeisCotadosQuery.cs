namespace Application.Cotacoes.Queries.GetPapeisCotados;

public interface IGetPapeisCotadosQuery
{
    Task<List<string>> ExecuteAsync(Guid ownerId, CancellationToken cancellationToken);
}
