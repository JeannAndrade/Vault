using Persistence.Managment;

namespace Application.Cotacoes.Queries.GetPapeisCotados;

public class GetPapeisCotadosQuery(IRepositoryManager repositoryManager) : IGetPapeisCotadosQuery
{
    private readonly IRepositoryManager _repository = repositoryManager;

    public Task<List<string>> ExecuteAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        return _repository.Cotacao.GetPapeisAsync(ownerId, cancellationToken);
    }
}
