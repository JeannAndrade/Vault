using Persistence.Management;

namespace Application.Movimentos.Queries.GetProximosVencimentos;

public class GetProximosVencimentosQuery(IRepositoryManager repositoryManager) : IGetProximosVencimentosQuery
{
    private const int QuantidadeMaxima = 12;
    private readonly IRepositoryManager _repository = repositoryManager;

    public async Task<List<ProximoVencimentoModel>> ExecuteAsync(Guid ownerId, CancellationToken cancellationToken)
    {
            var movimentos = await _repository.Movimento.GetProximosVencimentosAsync(ownerId, QuantidadeMaxima, cancellationToken);

            return [.. movimentos.Select(ProximoVencimentoModel.FromDomain)];

    }
}
