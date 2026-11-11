using LumiaFoundation.Core.TimeService;
using Persistence.Managment;

namespace Application.Movimentos.Queries.GetMovimentosDesatualizados;

public class GetMovimentosDesatualizadosQuery(
    IRepositoryManager repositoryManager,
    IDateTimeProvider dateTimeProvider) : IGetMovimentosDesatualizadosQuery
{
    private const int DiasLimite = 30;
    private const int QuantidadeMaxima = 12;
    private readonly IRepositoryManager _repository = repositoryManager;
    private readonly IDateTimeProvider _dateTimeProvider = dateTimeProvider;

    public async Task<List<MovimentoDesatualizadoModel>> ExecuteAsync(Guid ownerId, CancellationToken cancellationToken)
    {
            var limite = _dateTimeProvider.GetDateTime().Date.AddDays(-DiasLimite);
            var movimentos = await _repository.Movimento.GetDesatualizadosAsync(
                ownerId, limite, QuantidadeMaxima, cancellationToken);

            return [.. movimentos.Select(MovimentoDesatualizadoModel.FromDomain)];

    }
}
