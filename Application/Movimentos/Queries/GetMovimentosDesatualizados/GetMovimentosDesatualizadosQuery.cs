using LumiaFoundation.Core.TimeService;
using LumiaFoundation.Logger.Contracts;
using Persistence.Managment;

namespace Application.Movimentos.Queries.GetMovimentosDesatualizados;

public class GetMovimentosDesatualizadosQuery(
    IRepositoryManager repositoryManager,
    ILoggerManager logger,
    IDateTimeProvider dateTimeProvider) : IGetMovimentosDesatualizadosQuery
{
    private const int DiasLimite = 30;
    private const int QuantidadeMaxima = 12;
    private readonly IRepositoryManager _repository = repositoryManager;
    private readonly ILoggerManager _logger = logger;
    private readonly IDateTimeProvider _dateTimeProvider = dateTimeProvider;

    public async Task<List<MovimentoDesatualizadoModel>> ExecuteAsync(Guid ownerId, CancellationToken cancellationToken)
    {
        try
        {
            var limite = _dateTimeProvider.GetDateTime().Date.AddDays(-DiasLimite);
            var movimentos = await _repository.Movimento.GetDesatualizadosAsync(
                ownerId, limite, QuantidadeMaxima, cancellationToken);

            return [.. movimentos.Select(MovimentoDesatualizadoModel.FromDomain)];
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError($"Algo deu errado no método de serviço {nameof(GetMovimentosDesatualizadosQuery)}: {ex}");
            throw;
        }
    }
}
